using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MermaidNotationReferenceRenderingTests {
    [MermaidReferenceTheory]
    [Trait("Category", "MermaidReference")]
    [InlineData(false, 390)]
    [InlineData(true, 1100)]
    public async Task CurrentReferenceCapturesParOverAndParGeometryBeforeQualifyingTheirCommonLayout(bool dark, int width) {
        var corpus = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(corpus, "compatibility.json")));
        var reference = manifest.RootElement.GetProperty("references").GetProperty("12");
        var bundle = Path.Combine(corpus, "node_modules", reference.GetProperty("package").GetString()!);
        using var installed = JsonDocument.Parse(File.ReadAllText(Path.Combine(bundle, "package.json")));
        Assert.Equal(reference.GetProperty("version").GetString(), installed.RootElement.GetProperty("version").GetString());
        var source = MermaidSequenceParOverTests.Source(dark);
        var ordinary = source.Replace("par_over ", "par ", StringComparison.Ordinal);
        var rendered = MermaidRenderer.Render(source, new MermaidRenderOptions {
            Presentation = new MermaidPresentationOptions { FontFamily = BrowserProofFonts.Family },
            Sequence = new MermaidSequenceRenderOptions { Width = width - 32, Height = 620 }
        });
        Assert.Empty(rendered.Diagnostics);
        var artifact = rendered.Artifact!; var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var html = "<style>body{margin:0;padding:16px;box-sizing:border-box;background:" + (dark ? "#171b23;color:#edf2f7" : "#fff;color:#182235")
            + ";font-family:'" + BrowserProofFonts.Family + "'}h2{font-size:18px}section{max-width:100%;overflow:auto;margin-bottom:24px}svg,img{display:block;max-width:none}</style>"
            + "<h2>Pinned Mermaid " + reference.GetProperty("version").GetString() + " / par_over</h2><section id='reference'></section>"
            + "<h2>Pinned Mermaid / par control</h2><section id='parallel-control'></section>"
            + "<h2>Native SVG</h2><section id='native'>" + svg + "</section><h2>Native PNG</h2><section><img id='png' src='data:image/png;base64," + Convert.ToBase64String(png) + "'></section>";
        await using var session = await BrowserProofFonts.OpenAsync(html, width, 1000);
        await session.Page.AddScriptTagAsync(new PageAddScriptTagOptions { Path = Path.Combine(bundle, "dist", "mermaid.min.js") });
        var observed = await ObserveAsync(session.Page, source, "", dark,
            new Dictionary<string, int> { ["sequence-message"] = 8, ["sequence-block"] = 3, ["sequence-note"] = 3, ["sequence-activation-bar"] = 4 });
        var geometry = await session.Page.EvaluateReferenceJsonAsync("""
            async ({source,ordinary}) => {
                const control=await mermaid.render('parallel-control-diagram',ordinary);
                document.querySelector('#parallel-control').innerHTML=control.svg;
                const controlSvg=document.querySelector('#parallel-control svg'), view=controlSvg.viewBox.baseVal;
                controlSvg.style.width=view.width+'px';controlSvg.style.height=view.height+'px';controlSvg.style.maxWidth='none';
                await document.fonts.ready;
                const collect=selector=>[...document.querySelectorAll(selector+' .messageText,'+selector+' .noteText,'+selector+' .loopText,'+selector+' .loopLine')].map(node=>{
                    const b=node.getBBox(),round=value=>Math.round(value*100)/100;
                    return {element:node.localName,role:node.getAttribute('class'),text:node.textContent.replace(/\s+/g,' ').trim(),
                        x:round(b.x),y:round(b.y),width:round(b.width),height:round(b.height)};
                });
                const over=collect('#reference'),par=collect('#parallel-control');
                const overDb=(await mermaid.mermaidAPI.getDiagramFromText(source)).db;
                const overStart=overDb.getMessages().filter(message=>message.type===overDb.LINETYPE.PAR_OVER_START).length;
                const parDb=(await mermaid.mermaidAPI.getDiagramFromText(ordinary)).db;
                const parStart=parDb.getMessages().filter(message=>message.type===parDb.LINETYPE.PAR_START).length;
                return {over,par,overStart,parStart,commonMeasuredGeometry:JSON.stringify(over)===JSON.stringify(par)};
            }
            """, new { source, ordinary });
        var name = "sequence-par-over-reference12-" + width + (dark ? "-dark" : "-light");
        await CaptureAsync(session.Page, name, svg, png, artifact.ToInterchangeJson(), observed);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            File.WriteAllText(Path.Combine(output, name + ".geometry.json"), geometry.GetRawText());
            File.WriteAllText(Path.Combine(output, name + ".par-control.svg"), await session.Page.Locator("#parallel-control svg").EvaluateAsync<string>("element=>element.outerHTML"));
        }
        Assert.Equal(1, geometry.GetProperty("overStart").GetInt32());
        Assert.Equal(2, geometry.GetProperty("parStart").GetInt32());
        Assert.True(geometry.GetProperty("over").GetArrayLength() >= 11);
        Assert.True(geometry.GetProperty("commonMeasuredGeometry").GetBoolean(), "Pinned par_over/par geometry differs; inspect the captured primary observation before changing native policy.");
        Assert.Empty(observed.GetProperty("clip").EnumerateArray()); Assert.False(observed.GetProperty("horizontalOverflow").GetBoolean());
        Assert.Equal(0, observed.GetProperty("nativeScripts").GetInt32());
        foreach (var (role, count) in new Dictionary<string, int> { ["sequence-message"] = 8, ["sequence-block"] = 3, ["sequence-note"] = 3, ["sequence-activation-bar"] = 4 })
            Assert.Equal(count, observed.GetProperty("nativeRegions").GetProperty(role).GetInt32());
        foreach (var label in new[] { "Alpha request", "Forward", "Reply", "Alpha done", "Beta request", "Recovery", "Beta done", "After fragment", "First task note", "Guard note", "Terminal task note" })
            foreach (var surface in new[] { "referenceText", "nativeText" }) Assert.Contains(label, observed.GetProperty(surface).GetString());
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}

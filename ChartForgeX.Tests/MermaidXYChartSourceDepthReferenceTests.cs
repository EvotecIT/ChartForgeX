using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MermaidNotationReferenceRenderingTests {
    [MermaidReferenceTheory]
    [Trait("Category", "MermaidReference")]
    [InlineData(false, 390, false)] [InlineData(false, 1100, true)]
    [InlineData(true, 390, true)] [InlineData(true, 1100, false)]
    public async Task CurrentPinnedXYRendersNamedLegendAndQuotedLineCaptionsWithoutSynthesizedNumericLabels(bool horizontal, int width, bool dark) {
        var corpus = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(corpus, "compatibility.json")));
        var reference = manifest.RootElement.GetProperty("references").GetProperty("12");
        var bundle = Path.Combine(corpus, "node_modules", reference.GetProperty("package").GetString()!);
        using var installed = JsonDocument.Parse(File.ReadAllText(Path.Combine(bundle, "package.json")));
        Assert.Equal(reference.GetProperty("version").GetString(), installed.RootElement.GetProperty("version").GetString());
        var source = MermaidXYChartSourceDepthTests.Source(horizontal); var direct = MermaidRenderer.Render(source, MermaidXYChartSourceDepthTests.Options(width, dark)); Assert.Empty(direct.Diagnostics);
        var artifact = direct.Artifact!; var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var body = "<style>body{padding:12px;background:" + (dark ? "#171b23;color:#edf2f7" : "#fff;color:#182235")
            + ";}section{max-width:100%;overflow:auto;margin-bottom:16px}svg,img{display:block;max-width:none}</style>"
            + "<h2>Current source renderer</h2><section id='reference'></section><h2>Native SVG</h2><section id='native'>" + svg
            + "</section><h2>Native PNG</h2><section><img id='png' src='data:image/png;base64," + Convert.ToBase64String(png) + "'></section>";
        await using var session = await BrowserProofFonts.OpenAsync(body, width + 24, 1100); var page = session.Page;
        var errors = new List<string>(); page.PageError += (_, message) => errors.Add(message);
        await page.AddScriptTagAsync(new PageAddScriptTagOptions { Path = Path.Combine(bundle, "dist", "mermaid.min.js") });
        await page.AddScriptTagAsync(new PageAddScriptTagOptions { Content = File.ReadAllText(Path.Combine(corpus, "reference-xychart.mjs")).Replace("export function ", "function ", StringComparison.Ordinal) });
        var observed = await page.EvaluateReferenceJsonAsync("""
            async ({source,dark,fontFamily}) => {
              mermaid.initialize({startOnLoad:false,deterministicIds:true,securityLevel:'strict',fontFamily,theme:dark?'dark':'default',look:'classic'});
              const parsed=await mermaid.mermaidAPI.getDiagramFromText(source), facts=xyChartSourceFacts(parsed.db);
              const result=await mermaid.render('xy-source-reference',source);document.querySelector('#reference').innerHTML=result.svg;
              const reference=document.querySelector('#reference svg'),native=document.querySelector('#native svg');
              for(const svg of [reference,native]){const v=svg.viewBox.baseVal;svg.style.width=v.width+'px';svg.style.height=v.height+'px';svg.style.maxWidth='none'}
              await document.fonts.ready;await document.querySelector('#png').decode();
              return {facts, referenceLegend:[...reference.querySelectorAll('.legend .label text')].map(n=>n.textContent),
                referenceCaptions:[...reference.querySelectorAll('.line-plot-1 .labels text')].map(n=>n.textContent),
                nativeLegend:[...native.querySelectorAll('[data-cfx-role="legend-label"]')].map(n=>n.textContent),
                nativeCaptions:[...native.querySelectorAll('[data-cfx-role="data-label-source"][data-cfx-full-label]')].map(n=>n.dataset.cfxFullLabel),
                nativeActive:native.querySelectorAll('script,image,foreignObject').length};
            }
            """, new { source, dark, fontFamily = BrowserProofFonts.Family });
        await CaptureAsync(page, "xy-source-reference12-" + (horizontal ? "horizontal-" : "vertical-") + width + (dark ? "-dark" : "-light"), svg, png, artifact.ToInterchangeJson(), observed);
        var facts = observed.GetProperty("facts"); Assert.Equal(horizontal ? "horizontal" : "vertical", facts.GetProperty("orientation").GetString());
        Assert.Equal(new[] { "Intake", "Completed" }, facts.GetProperty("series").EnumerateArray().Select(series => series.GetProperty("name").GetString()));
        Assert.Equal(new[] { 12d, 60, 33, 75 }, facts.GetProperty("series")[1].GetProperty("data").EnumerateArray().Select(point => point[1].GetDouble()));
        Assert.Equal(new[] { "Opened", "", "Reviewed, ready", "Released" }, facts.GetProperty("series")[1].GetProperty("labels").EnumerateArray().Select(value => value.GetString()));
        Assert.Equal(observed.GetProperty("referenceLegend").GetRawText(), observed.GetProperty("nativeLegend").GetRawText());
        Assert.Equal(new[] { "Intake", "Completed" }, observed.GetProperty("nativeLegend").EnumerateArray().Select(value => value.GetString()));
        var expected = new[] { "Opened", "Reviewed, ready", "Released" }.OrderBy(text => text);
        Assert.Equal(expected, observed.GetProperty("referenceCaptions").EnumerateArray().Select(value => value.GetString()).OrderBy(text => text));
        Assert.Equal(expected, observed.GetProperty("nativeCaptions").EnumerateArray().Select(value => value.GetString()).OrderBy(text => text));
        Assert.Equal(0, observed.GetProperty("nativeActive").GetInt32()); Assert.Empty(errors); InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}

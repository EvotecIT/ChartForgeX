using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MermaidNotationReferenceRenderingTests {
    [MermaidReferenceTheory] [Trait("Category", "MermaidReference")]
    [InlineData("flowchart")] [InlineData("agentflow")]
    public async Task PinnedClassicOpacityBelongsToOwnSurfacesAndDoesNotFadeLabelsOrDescendants(string family) {
        var corpus = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(corpus, "compatibility.json")));
        var reference = manifest.RootElement.GetProperty("references").GetProperty("12");
        var bundle = Path.Combine(corpus, "node_modules", reference.GetProperty("package").GetString()!);
        using var installed = JsonDocument.Parse(File.ReadAllText(Path.Combine(bundle, "package.json")));
        Assert.Equal(reference.GetProperty("version").GetString(), installed.RootElement.GetProperty("version").GetString());
        var source = MermaidSourceContourPaintCausalTests.OpacitySource(family);
        var result = MermaidRenderer.Render(source, MermaidSourceContourPaintCausalTests.Options());
        var artifact = result.Artifact ?? throw new InvalidOperationException("A retained surface warning must not reject ordinary rendering.");
        var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var html = "<style>body{margin:0;padding:16px}section{max-width:100%;overflow:auto;margin-bottom:20px}svg,img{display:block;max-width:none}</style>"
            + "<h2>Pinned Mermaid</h2><section id='reference'></section><h2>Native SVG before lowering</h2><section id='native'>" + svg
            + "</section><h2>Native PNG before lowering</h2><section><img id='png' src='data:image/png;base64," + Convert.ToBase64String(png) + "'></section>";
        await using var session = await BrowserProofFonts.OpenAsync(html, 900, 1000);
        var errors = new List<string>(); session.Page.PageError += (_, error) => errors.Add(error);
        await session.Page.AddScriptTagAsync(new PageAddScriptTagOptions { Path = Path.Combine(bundle, "dist", "mermaid.min.js") });
        var observed = await session.Page.EvaluateAsync<JsonElement>("""
            async ({source,fontFamily}) => {
                mermaid.initialize({startOnLoad:false,securityLevel:'strict',htmlLabels:false,fontFamily,theme:'default',look:'classic',flowchart:{htmlLabels:false}});
                document.querySelector('#reference').innerHTML=(await mermaid.render('surface-opacity-reference',source)).svg;
                await document.fonts.ready; await document.querySelector('#png').decode();
                const svg=document.querySelector('#reference svg');
                const node=label=>[...svg.querySelectorAll('g.node')].find(g=>[...g.querySelectorAll('.label')].some(l=>l.textContent.trim()===label));
                const person=node('Work'),child=node('Child');
                const group=[...svg.querySelectorAll('g.cluster')].find(g=>[...g.querySelectorAll('.cluster-label')].some(l=>l.textContent.trim()==='Review'));
                const own=g=>[...g.querySelectorAll('rect,circle,path')].filter(mark=>!mark.closest('.label,.cluster-label') && mark.closest('g.node')===g);
                const labelOpacity=g=>[...g.querySelectorAll('.label text,.label span,.cluster-label text,.cluster-label span')].map(mark=>Number(getComputedStyle(mark).opacity));
                const surface=own(person).map(mark=>({kind:mark.tagName,opacity:Number(getComputedStyle(mark).opacity),fill:getComputedStyle(mark).fill,stroke:getComputedStyle(mark).stroke}));
                const groupSurface=[...group.querySelectorAll('rect,path')].filter(mark=>!mark.closest('.label,.cluster-label') && !mark.closest('g.node') && mark.closest('g.cluster')===group).map(mark=>Number(getComputedStyle(mark).opacity));
                return {surface,groupSurface,personGroupOpacity:Number(getComputedStyle(person).opacity),groupOpacity:Number(getComputedStyle(group).opacity),
                    personLabelOpacity:labelOpacity(person),groupLabelOpacity:labelOpacity(group),childSurface:own(child).map(mark=>Number(getComputedStyle(mark).opacity)),
                    referenceActive:svg.querySelectorAll('script,foreignObject').length,nativeSurface:[...document.querySelectorAll('#native [data-cfx-role="topology-node-surface"]')].map(mark=>Number(getComputedStyle(mark).opacity))};
            }
            """, new { source, fontFamily = BrowserProofFonts.Family });
        await CaptureAsync(session.Page, "surface-opacity-reference-12-" + family, svg, png, artifact.ToInterchangeJson(), observed);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, "surface-opacity-reference-12-" + family + ".mmd"), source);
            File.WriteAllText(Path.Combine(output, "surface-opacity-reference-12-" + family + ".diagnostics.json"), JsonSerializer.Serialize(result.Diagnostics.Select(item => new { item.Code, item.Message, item.Severity, item.Span.Line, item.Span.Column, item.Span.Length })));
        }
        Assert.False(result.HasErrors); Assert.Equal(family == "flowchart" ? 2 : 1, observed.GetProperty("surface").GetArrayLength());
        foreach (var part in observed.GetProperty("surface").EnumerateArray()) Assert.Equal(.5, part.GetProperty("opacity").GetDouble());
        Assert.NotEmpty(observed.GetProperty("groupSurface").EnumerateArray());
        foreach (var opacity in observed.GetProperty("groupSurface").EnumerateArray()) Assert.Equal(family == "flowchart" ? .5 : 1, opacity.GetDouble());
        Assert.Equal(1, observed.GetProperty("personGroupOpacity").GetDouble()); Assert.Equal(1, observed.GetProperty("groupOpacity").GetDouble());
        foreach (var key in new[] { "personLabelOpacity", "groupLabelOpacity", "childSurface" }) {
            Assert.NotEmpty(observed.GetProperty(key).EnumerateArray()); foreach (var opacity in observed.GetProperty(key).EnumerateArray()) Assert.Equal(1, opacity.GetDouble());
        }
        Assert.Equal(0, observed.GetProperty("referenceActive").GetInt32()); Assert.Empty(errors); InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}

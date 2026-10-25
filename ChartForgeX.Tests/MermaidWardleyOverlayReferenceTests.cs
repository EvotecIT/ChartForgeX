using ChartForgeX.VisualArtifacts;
using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MermaidNotationReferenceRenderingTests {
    [MermaidReferenceTheory]
    [Trait("Category", "MermaidReference")]
    [InlineData(false, 390)] [InlineData(true, 1100)]
    public async Task PinnedWardleySourceRetainsActualAnnotationPipelineAndSourcingFactsBesideNativePaint(bool dark, int width) {
        var corpus = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(corpus, "compatibility.json")));
        var reference = manifest.RootElement.GetProperty("references").GetProperty("12"); var bundle = Path.Combine(corpus, "node_modules", reference.GetProperty("package").GetString()!);
        using var installed = JsonDocument.Parse(File.ReadAllText(Path.Combine(bundle, "package.json"))); Assert.Equal("12.2.0", installed.RootElement.GetProperty("version").GetString());
        var source = MermaidWardleyOverlayTests.Fixture(dark); var rendered = MermaidRenderer.Render(source, MermaidWardleyOverlayTests.Options(width, BrowserProofFonts.Family)); Assert.Empty(rendered.Diagnostics);
        var artifact = rendered.Artifact!; var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var html = "<style>body{margin:0;padding:16px;box-sizing:border-box;background:" + (dark ? "#171b23;color:#edf2f7" : "#fff;color:#172033")
            + "}h2{font-size:18px}section{max-width:100%;overflow:auto;margin-bottom:24px}svg,img{display:block;max-width:none}</style>"
            + "<h2>Pinned Mermaid 12.2.0 / Wardley source overlays</h2><section id='reference'></section><h2>Native SVG</h2><section id='native'>" + svg
            + "</section><h2>Native PNG</h2><section><img id='png' src='data:image/png;base64," + Convert.ToBase64String(png) + "'></section>";
        await using var session = await BrowserProofFonts.OpenAsync(html, width + 32, 1100);
        await session.Page.AddScriptTagAsync(new PageAddScriptTagOptions { Path = Path.Combine(bundle, "dist", "mermaid.min.js") });
        var observed = await ObserveAsync(session.Page, source, "", dark, new Dictionary<string, int> { ["wardley-annotation-box"] = 1, ["wardley-annotation"] = 2, ["wardley-pipeline-component"] = 2 });
        var facts = await session.Page.EvaluateAsync<JsonElement>("""
            async source => {
              const data=(await mermaid.mermaidAPI.getDiagramFromText(source)).db.getWardleyData(),svg=document.querySelector('#reference svg'),box=svg.querySelector('.wardley-annotations-box'),rect=box.querySelector('rect');
              return {nodes:data.nodes.map(n=>({id:n.id,label:n.label,x:n.x,y:n.y,strategy:n.sourceStrategy,inertia:n.inertia,labelX:n.labelOffsetX,labelY:n.labelOffsetY})),
                links:data.links.map(l=>[l.source,l.target]),axes:data.axes,pipelines:data.pipelines,annotations:data.annotations,annotationsBox:data.annotationsBox,
                legend:[...box.querySelectorAll('text')].map(n=>n.textContent),paint:{fill:getComputedStyle(rect).fill,stroke:getComputedStyle(rect).stroke,text:getComputedStyle(box.querySelector('text')).fill},
                paintedSourcing:svg.querySelectorAll('.wardley-node').length,pipelineBoxes:svg.querySelectorAll('.wardley-pipeline-box').length,gridLines:svg.querySelectorAll('.wardley-grid line').length};
            }
            """, source);
        var name = "wardley-overlay-reference12-" + width + (dark ? "-dark" : "-light");
        await CaptureAsync(session.Page, name, svg, png, artifact.ToInterchangeJson(), observed);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) File.WriteAllText(Path.Combine(output, name + ".wardley-facts.json"), facts.GetRawText());
        Assert.Equal(7, facts.GetProperty("nodes").GetArrayLength()); Assert.Equal(4, facts.GetProperty("links").GetArrayLength()); Assert.Equal(1, facts.GetProperty("pipelineBoxes").GetInt32());
        Assert.Equal(new[] { "1. Keep the user need visible", "2. Prefer a portable service contract" }, facts.GetProperty("legend").EnumerateArray().Select(e => e.GetString()));
        var scale = facts.GetProperty("nodes").EnumerateArray().Single(n => n.GetProperty("id").GetString() == "Service_Scale"); Assert.Equal(48, scale.GetProperty("x").GetDouble()); Assert.Equal(-8, scale.GetProperty("labelX").GetDouble()); Assert.Equal(30, scale.GetProperty("labelY").GetDouble());
        Assert.Equal(new[] { .30, .55, .78, 1d }, facts.GetProperty("axes").GetProperty("stageBoundaries").EnumerateArray().Select(e => e.GetDouble()));
        Assert.Equal(new[] { "New / Idea", "Custom", "Product", "Commodity" }, facts.GetProperty("axes").GetProperty("stages").EnumerateArray().Select(e => e.GetString()));
        var strategies = facts.GetProperty("nodes").EnumerateArray().Where(n => n.TryGetProperty("strategy", out _)).Select(n => n.GetProperty("strategy").GetString()).ToArray();
        Assert.Equal(new[] { "build", "buy", "outsource", "market" }, strategies);
        Assert.Equal(dark ? "rgb(16, 59, 54)" : "rgb(209, 250, 229)", facts.GetProperty("paint").GetProperty("fill").GetString()); Assert.Equal("rgb(5, 150, 105)", facts.GetProperty("paint").GetProperty("stroke").GetString());
        Assert.Equal(6, facts.GetProperty("gridLines").GetInt32());
        Assert.Equal(0, observed.GetProperty("nativeScripts").GetInt32()); Assert.False(observed.GetProperty("horizontalOverflow").GetBoolean());
        foreach (var label in new[] { "User", "Build", "Buy", "Service", "Market", "Small", "Scale", "Keep the user need visible", "Prefer a portable service contract" })
            foreach (var surface in new[] { "referenceText", "nativeText" }) Assert.Contains(label, observed.GetProperty(surface).GetString());
        // Native measured annotation wrapping and source coordinates are qualified here, not upstream pixel identity.
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}

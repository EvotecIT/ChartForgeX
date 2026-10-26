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
    [InlineData(true, 390)]
    [InlineData(false, 800)]
    [InlineData(true, 800)]
    public async Task CurrentPinnedQuadrantUsesTheSameClassCascadeAndIndependentCaptionCoordinates(bool dark, int width) {
        var corpus = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(corpus, "compatibility.json")));
        var reference = manifest.RootElement.GetProperty("references").GetProperty("12");
        var bundle = Path.Combine(corpus, "node_modules", reference.GetProperty("package").GetString()!);
        using var installed = JsonDocument.Parse(File.ReadAllText(Path.Combine(bundle, "package.json")));
        Assert.Equal(reference.GetProperty("version").GetString(), installed.RootElement.GetProperty("version").GetString());
        var source = QuadrantSourceDepthFixture.Read(dark);
        var rendered = MermaidRenderer.Render(source, new MermaidRenderOptions {
            WarningPolicy = MermaidWarningPolicy.Reject,
            Presentation = new MermaidPresentationOptions { FontFamily = BrowserProofFonts.Family },
            Quadrant = new MermaidQuadrantRenderOptions { Width = width, Height = 500 }
        });
        Assert.False(rendered.HasErrors); Assert.Empty(rendered.Diagnostics);
        var artifact = rendered.Artifact!; var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var html = "<style>body{margin:0;padding:16px;box-sizing:border-box;background:" + (dark ? "#171b23;color:#edf2f7" : "#fff;color:#182235")
            + ";font-family:'" + BrowserProofFonts.Family + "'}h2{font-size:18px}section{max-width:100%;overflow:auto;margin-bottom:24px}svg,img{display:block;max-width:none}</style>"
            + "<h2>Pinned Mermaid " + reference.GetProperty("version").GetString() + " / Quadrant source styles</h2><section id='reference'></section>"
            + "<h2>Native SVG</h2><section id='native'>" + svg + "</section><h2>Native PNG</h2><section><img id='png' src='data:image/png;base64," + Convert.ToBase64String(png) + "'></section>";
        await using var session = await BrowserProofFonts.OpenAsync(html, width, 1100);
        await session.Page.AddScriptTagAsync(new PageAddScriptTagOptions { Path = Path.Combine(bundle, "dist", "mermaid.min.js") });
        var observed = await ObserveAsync(session.Page, source, "", dark,
            new Dictionary<string, int> { ["mermaid-quadrant-point"] = 4, ["plot-region"] = 4 });
        var facts = await session.Page.EvaluateReferenceJsonAsync("""
            async source => {
                const data=(await mermaid.mermaidAPI.getDiagramFromText(source)).db.getQuadrantData();
                const minX=Math.min(...data.quadrants.map(q=>q.x)),minY=Math.min(...data.quadrants.map(q=>q.y));
                const w=Math.max(...data.quadrants.map(q=>q.x+q.width))-minX,h=Math.max(...data.quadrants.map(q=>q.y+q.height))-minY;
                return {points:data.points.map(p=>({label:p.text.text,radius:p.radius,fill:p.fill,stroke:p.strokeColor,width:parseFloat(p.strokeWidth)})),
                    captions:data.axisLabels.map(p=>({text:p.text,x:(p.x-minX)/w,y:1-(p.y-minY)/h}))};
            }
            """, source);
        foreach (var point in facts.GetProperty("points").EnumerateArray()) {
            var label = point.GetProperty("label").GetString();
            var ordinal = Array.IndexOf(new[] { "Opportunity", "Investment", "Experiment", "Review" }, label);
            Assert.InRange(ordinal, 0, 3);
            Assert.Equal(new[] { 9d, 7d, 6d, 5d }[ordinal], point.GetProperty("radius").GetDouble());
            Assert.Equal(new[] { "#B45309", "#0369A1", "#7C3AED", "#334155" }[ordinal], point.GetProperty("fill").GetString(), ignoreCase: true);
            Assert.Equal(new[] { 2d, 1d, 0d, 0d }[ordinal], point.GetProperty("width").GetDouble());
        }
        foreach (var caption in facts.GetProperty("captions").EnumerateArray()) {
            var text = caption.GetProperty("text").GetString()!;
            Assert.Equal(text.StartsWith("Low", StringComparison.Ordinal) ? .25 : .75,
                caption.GetProperty(text.EndsWith("effort", StringComparison.Ordinal) ? "x" : "y").GetDouble(), 8);
        }
        foreach (var text in new[] { "Low effort", "High effort", "Low impact", "High impact", "Opportunity", "Investment", "Experiment", "Review" })
            foreach (var surface in new[] { "referenceText", "nativeText" }) Assert.Contains(text, observed.GetProperty(surface).GetString());
        var name = $"quadrant-source-reference12-{width}-{dark}";
        await CaptureAsync(session.Page, name, svg, png, artifact.ToInterchangeJson(), observed);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output)
            await File.WriteAllTextAsync(Path.Combine(output, name + ".quadrant.json"), facts.GetRawText());
        Assert.Empty(observed.GetProperty("clip").EnumerateArray()); Assert.False(observed.GetProperty("horizontalOverflow").GetBoolean());
        Assert.Equal(0, observed.GetProperty("nativeScripts").GetInt32());
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}

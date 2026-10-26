using System.Text.Json;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MermaidNotationReferenceRenderingTests {
    [MermaidReferenceTheory] [Trait("Category", "MermaidReference")]
    [InlineData("compact", false)] [InlineData("wide", true)]
    public async Task PinnedCurrentFlowchartAndNativeCurvesRetainTheOrdinarySourceWorkflow(string variant, bool dark) {
        var corpus = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(corpus, "compatibility.json")));
        var reference = manifest.RootElement.GetProperty("references").GetProperty("12");
        var bundle = Path.Combine(corpus, "node_modules", reference.GetProperty("package").GetString()!);
        using var installed = JsonDocument.Parse(File.ReadAllText(Path.Combine(bundle, "package.json")));
        Assert.Equal(reference.GetProperty("version").GetString(), installed.RootElement.GetProperty("version").GetString());
        // The shared YAML spelling retains the same native curve; compact metadata remains a native parser contract.
        var source = FlowchartCurveRecipe.Source("flowchart").Replace("curve:bumpX", "curve: bumpX", StringComparison.Ordinal);
        var artifact = FlowchartCurveRecipe.Create("flowchart", variant, dark ? VisualThemeMode.Dark : VisualThemeMode.Light, BrowserProofFonts.Family);
        var svg = artifact.ToSvg(); var png = artifact.ToPng(); var semantics = artifact.ToInterchangeJson();
        var html = "<style>body{margin:0;padding:16px;box-sizing:border-box}section{max-width:100%;overflow:auto;margin-bottom:24px}svg,img{display:block;max-width:none}</style>"
            + "<h2>Pinned Mermaid</h2><section id='reference'></section><h2>ChartForgeX SVG</h2><section id='native'>" + svg
            + "</section><h2>ChartForgeX PNG</h2><section><img id='png' src='data:image/png;base64," + Convert.ToBase64String(png) + "'></section>";
        await using var session = await BrowserProofFonts.OpenAsync(html, variant == "compact" ? 640 : 1100, 1000);
        var errors = new List<string>(); session.Page.PageError += (_, error) => errors.Add(error);
        await session.Page.AddScriptTagAsync(new PageAddScriptTagOptions { Path = Path.Combine(bundle, "dist", "mermaid.min.js") });
        var observed = await ObserveAsync(session.Page, source, "", dark, new() { ["topology-node"] = 6, ["topology-group"] = 2, ["topology-edge"] = 6 });
        var curves = await session.Page.EvaluateReferenceJsonAsync("""
            async source => {
                const db=(await mermaid.mermaidAPI.getDiagramFromText(source)).db,edges=db.getEdges();
                return edges.map(e=>({id:e.id,curve:e.interpolate||edges.defaultInterpolate||mermaid.mermaidAPI.getConfig().flowchart.curve}));
            }
            """, source);
        var name = "curve-reference-12-" + variant + (dark ? "-dark" : "-light");
        await CaptureAsync(session.Page, name, svg, png, semantics, observed);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, name + ".curves.json"), curves.GetRawText()); File.WriteAllText(Path.Combine(output, name + ".mmd"), source);
        }
        Assert.Equal(6, curves.GetArrayLength()); Assert.Equal("bumpX", curves[0].GetProperty("curve").GetString());
        Assert.All(curves.EnumerateArray().Skip(1), edge => Assert.Equal("basis", edge.GetProperty("curve").GetString()));
        foreach (var label in new[] { "Receive request", "Review request", "Clarify details", "Approve request", "Publish result", "Archive receipt" })
            foreach (var surface in new[] { "referenceText", "nativeText" }) Assert.Contains(label, observed.GetProperty(surface).GetString()!, StringComparison.Ordinal);
        Assert.Empty(observed.GetProperty("clip").EnumerateArray()); Assert.Equal(0, observed.GetProperty("nativeScripts").GetInt32());
        Assert.False(observed.GetProperty("horizontalOverflow").GetBoolean()); Assert.True(observed.GetProperty("referencePaintElements").GetInt32() > 0);
        Assert.Equal(6, observed.GetProperty("nativeRegions").GetProperty("topology-node").GetInt32()); Assert.Empty(errors); InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}

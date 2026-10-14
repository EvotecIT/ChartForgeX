using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class TopologyPaletteSelectionTests {
    [BrowserTheory]
    [InlineData("node")]
    [InlineData("icon")]
    public async Task ExplicitVariablesWithoutFallbackKeepStatusPixelsAndResolveInTheHost(string source) {
        if (!Enabled) return;
        var (chart, options) = VariableNode(source, true);
        var prepared = chart.Prepare(options); var status = chart.Theme.Resolve(VisualThemeMode.Light).Status.Pass.Fill;
        var (referenceChart, referenceOptions) = VariableNode(source, false);
        var reference = referenceChart.ToPng(referenceOptions);
        var sceneSurface = prepared.Visual.Scene.Nodes.OfType<VisualSceneMark>().First(node => node.Role == "topology-node-surface");
        var glyphSurfaces = prepared.Visual.Scene.Nodes.OfType<VisualSceneMark>().Where(node => node.Role == "topology-icon-surface").ToArray();
        await using var session = await OpenAsync("<!doctype html><html><body style='margin:0'>" + prepared.ToSvg() + "</body></html>", 520, 280);
        var node = session.Page.Locator("[data-cfx-role=topology-node][data-node-id=first]");
        var undefined = await node.EvaluateAsync<object>("""
            node=>({surface:{stroke:getComputedStyle(node.querySelector('[data-cfx-role=topology-node-surface]')).stroke,
                fill:getComputedStyle(node.querySelector('[data-cfx-role=topology-node-surface]')).fill},
              glyphs:[...node.querySelectorAll('[data-cfx-role=topology-icon-surface]')].map(mark=>({stroke:getComputedStyle(mark).stroke,fill:getComputedStyle(mark).fill}))})
            """);
        await TopologyPaletteRegressionEvidence.CaptureAsync(session.Page, "explicit-variable-" + source + "-undefined");
        await AssertCssColorsEqualAsync(session.Page, status.ToCss(), await node.Locator("[data-cfx-role=topology-node-surface]").EvaluateAsync<string>("mark=>getComputedStyle(mark).stroke"));
        const string authored = "#D97706";
        await session.Page.Locator("svg").EvaluateAsync("(svg,color)=>svg.style.setProperty('--custom',color)", authored);
        var resolved = await node.Locator("[data-cfx-role=topology-node-surface]").EvaluateAsync<string>("mark=>getComputedStyle(mark).stroke");
        await TopologyPaletteRegressionEvidence.CaptureAsync(session.Page, "explicit-variable-" + source + "-resolved");
        TopologyPaletteRegressionEvidence.Write("explicit-variable-" + source, prepared, new {
            source, status = status.ToCss(), palette = chart.Theme.Resolve(VisualThemeMode.Light).Palette[1].ToCss(),
            nativeSurface = new { stroke = sceneSurface.Stroke?.ToCss(), fill = sceneSurface.Fill?.ToCss() },
            nativeGlyphs = glyphSurfaces.Select(mark => new { stroke = mark.Stroke?.ToCss(), fill = mark.Fill?.ToCss() }),
            undefined, resolved, nativePngMatchesWithoutPalette = prepared.ToPng().SequenceEqual(reference)
        }, reference);
        await AssertCssColorsEqualAsync(session.Page, authored, resolved);
        AssertNoConsoleErrors(session);
        Assert.Equal(status, sceneSurface.Stroke);
        Assert.NotEmpty(glyphSurfaces); Assert.All(glyphSurfaces, mark => Assert.Equal(status, mark.Stroke));
        Assert.Equal(reference, prepared.ToPng());
    }

    [Fact]
    public void ArtworkSelectionAndExternalFallbackKeepExplicitVariableStatusPixels() {
        var png = PngWriter.WriteRgba(new RgbaImage(1, 1, new byte[] { 125, 180, 225, 255 }));
        var chart = TopologyChart.Create().WithViewport(420, 180, 20).WithLegend(null).WithTheme(Theme())
            .AddArtworkNode("embedded", "Embedded", TopologyIconArtwork.Image("data:image/png;base64," + Convert.ToBase64String(png)), 40, 50, width: 100, height: 70)
            .AddArtworkNode("external", "External", TopologyIconArtwork.Image("/assets/owner.png"), 250, 50, width: 100, height: 70);
        foreach (var node in chart.Nodes) { node.Color = "var(--custom)"; node.PaletteIndex = 1; node.Status = TopologyHealthStatus.Healthy; }
        var options = Options(); options.SelectedNodeIds.Add("embedded"); options.SelectedNodeIds.Add("external");
        var prepared = chart.Prepare(options); var status = chart.Theme.Resolve(VisualThemeMode.Light).Status.Pass.Fill;
        var outlines = prepared.Visual.Scene.Nodes.OfType<VisualSceneMark>().Where(mark => mark.Role == "topology-node-artwork-selection").ToArray();
        var fallback = prepared.Visual.Scene.Nodes.OfType<VisualSceneMark>().Where(mark => mark.Role == "topology-icon-surface").ToArray();
        foreach (var node in chart.Nodes) node.PaletteIndex = null;
        var reference = chart.ToPng(options);
        TopologyPaletteRegressionEvidence.Write("explicit-variable-artwork", prepared, new {
            status = status.ToCss(), outlines = outlines.Select(mark => mark.Stroke?.ToCss()), fallback = fallback.Select(mark => mark.Stroke?.ToCss()),
            nativePngMatchesWithoutPalette = prepared.ToPng().SequenceEqual(reference), diagnostics = prepared.Visual.Diagnostics.Select(diagnostic => diagnostic.Code)
        }, reference);
        Assert.Equal(2, outlines.Length); Assert.All(outlines, outline => Assert.Equal(status, outline.Stroke));
        Assert.NotEmpty(fallback); Assert.All(fallback, mark => Assert.Equal(status, mark.Stroke));
        Assert.Equal(reference, prepared.ToPng());
    }

    private static (TopologyChart Chart, TopologyRenderOptions Options) VariableNode(string source, bool palette) {
        var chart = Diagram(VisualThemeMode.Light); var options = Options();
        chart.Nodes[0].Kind = TopologyNodeKind.Service; chart.Nodes[0].Status = TopologyHealthStatus.Healthy;
        chart.Nodes[0].PaletteIndex = palette ? 1 : null;
        if (source == "node") chart.Nodes[0].Color = "var(--custom)";
        else {
            options.IconCatalog = TopologyIconCatalog.Default().AddPack(new TopologyIconPack("variable", "Variable")
                .AddIcon(new TopologyIconDefinition("variable", "owner", "Owner", TopologyNodeKind.Service, TopologyIconShape.Application) { Color = "var(--custom)" }));
            chart.Nodes[0].IconId = "variable:owner";
        }
        return (chart, options);
    }
}

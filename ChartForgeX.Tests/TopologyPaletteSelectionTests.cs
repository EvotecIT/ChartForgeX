using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyPaletteSelectionTests {
    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void PaletteSlotsUseTheSelectedThemeAndCycleForSvgAndNativePixels(VisualThemeMode mode) {
        var chart = Diagram(mode);
        chart.Nodes[0].PaletteIndex = 0;
        chart.Nodes[1].PaletteIndex = 4;
        chart.Edges[0].PaletteIndex = 5;
        var colors = chart.Theme.Resolve(mode);
        var options = Options();
        var prepared = chart.Prepare(options);
        var svg = XDocument.Parse(prepared.ToSvg());
        Assert.Equal(colors.Palette[0].ToCss(), (string?)Surface(svg, "first").Attribute("stroke"));
        Assert.Equal(colors.Palette[1].ToCss(), (string?)Surface(svg, "second").Attribute("stroke"));
        Assert.Equal(colors.Palette[2].ToCss(), (string?)Route(svg).Attribute("stroke"));
        Assert.Equal(new[] { colors.Palette[0], colors.Palette[1] }, prepared.Visual.Scene.Nodes.OfType<VisualSceneMark>()
            .Where(mark => mark.Role == "topology-node-surface").Select(mark => mark.Stroke!.Value));
        Assert.Equal(colors.Palette[2], Assert.Single(prepared.Visual.Scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "topology-edge-line").Stroke);

        var reference = Diagram(mode);
        reference.Nodes[0].Color = colors.Palette[0].ToCss();
        reference.Nodes[1].Color = colors.Palette[1].ToCss();
        reference.Edges[0].Color = colors.Palette[2].ToCss();
        Assert.Equal(reference.ToPng(options), prepared.ToPng());
    }

    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void ExplicitColorsBackgroundsAndStatusBadgesKeepTheirExistingMeaning(VisualThemeMode mode) {
        var chart = Diagram(mode);
        chart.Nodes[0].PaletteIndex = 1;
        chart.Nodes[0].Color = "var(--authored-accent, #7C3AED)";
        chart.Nodes[0].BackgroundColor = "var(--authored-fill, #FDE68A)";
        chart.Nodes[0].Status = TopologyHealthStatus.Healthy;
        chart.Edges[0].PaletteIndex = 2;
        chart.Edges[0].Color = "var(--authored-route, #C026D3)";
        var options = Options(); options.IncludeStatusBadges = true;
        var prepared = chart.Prepare(options);
        var svg = XDocument.Parse(prepared.ToSvg());
        Assert.Equal("var(--authored-accent, #7C3AED)", (string?)Surface(svg, "first").Attribute("stroke"));
        Assert.Equal("var(--authored-fill, #FDE68A)", (string?)Surface(svg, "first").Attribute("fill"));
        Assert.Equal("var(--authored-route, #C026D3)", (string?)Route(svg).Attribute("stroke"));
        var node = Owner(svg, "topology-node", "data-node-id", "first");
        Assert.Equal(chart.Theme.Resolve(mode).Status.Pass.Fill.ToCss(), (string?)node.Descendants()
            .Single(element => (string?)element.Attribute("data-cfx-role") == "topology-status").Attribute("fill"));
        var pixels = prepared.ToPng();
        chart.Nodes[0].PaletteIndex = null; chart.Edges[0].PaletteIndex = null;
        chart.Nodes[0].Color = "#7C3AED"; chart.Nodes[0].BackgroundColor = "#FDE68A";
        chart.Edges[0].Color = "#C026D3";
        Assert.Equal(chart.ToPng(options), pixels);

        chart.Edges[0].PaletteIndex = 2; chart.Edges[0].IsMuted = true;
        var muted = chart.Prepare(options);
        Assert.Equal(chart.Theme.Resolve(mode).Border.ToCss(), (string?)Route(XDocument.Parse(muted.ToSvg())).Attribute("stroke"));
    }

    [Fact]
    public void ResolvedIconColorsTakePrecedenceOverAPaletteSlot() {
        var catalog = TopologyIconCatalog.Default().AddPack(new TopologyIconPack("palette", "Palette")
            .AddIcon(new TopologyIconDefinition("palette", "owner", "Owner", TopologyNodeKind.Service, TopologyIconShape.Application) { Color = "#0F766E" }));
        var chart = Diagram(VisualThemeMode.Light);
        chart.Nodes[0].IconId = "palette:owner";
        chart.Nodes[0].PaletteIndex = 2;
        var options = Options(); options.IconCatalog = catalog;
        var prepared = chart.Prepare(options);
        Assert.Equal("#0F766E", (string?)Surface(XDocument.Parse(prepared.ToSvg()), "first").Attribute("stroke"));
        chart.Nodes[0].PaletteIndex = null;
        Assert.Equal(chart.ToPng(options), prepared.ToPng());
    }

    [Fact]
    public void PreparingSnapshotsPaletteAssignmentsAndRecompilingUsesTheUpdatedSource() {
        var chart = Diagram(VisualThemeMode.Light);
        chart.Nodes[0].PaletteIndex = 0; chart.Nodes[1].PaletteIndex = 1; chart.Edges[0].PaletteIndex = 2;
        var options = Options();
        var prepared = chart.Prepare(options);
        var originalSvg = prepared.ToSvg(); var originalPng = prepared.ToPng();
        var original = XDocument.Parse(originalSvg);
        Assert.Equal(chart.Theme.Resolve(VisualThemeMode.Light).Palette[0].ToCss(), (string?)Surface(original, "first").Attribute("stroke"));
        Assert.Equal(chart.Theme.Resolve(VisualThemeMode.Light).Palette[2].ToCss(), (string?)Route(original).Attribute("stroke"));
        chart.Nodes[0].PaletteIndex = 2; chart.Nodes[1].PaletteIndex = 0; chart.Edges[0].PaletteIndex = 1;
        chart.ConfigureTheme(builder => builder.Light.Palette = ChartPalettes.FromHex("#831843", "#9D174D", "#BE185D"));
        var fresh = XDocument.Parse(chart.Prepare(options).ToSvg());
        Assert.Equal("#BE185D", (string?)Surface(fresh, "first").Attribute("stroke"));
        Assert.Equal("#9D174D", (string?)Route(fresh).Attribute("stroke"));
        Assert.Equal(originalSvg, prepared.ToSvg());
        Assert.Equal(originalPng, prepared.ToPng());
    }

    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void MermaidDepthSectionsReachCustomThemePaintAndPermitHostOverrides(VisualThemeMode mode) {
        var parsed = new MermaidParser().ParseMindMap("%%{init: {'mindmap': {'colorBy': 'depth'}}}%%\nmindmap\n Root\n   Owner\n       Child");
        Assert.Empty(parsed.Diagnostics);
        var chart = parsed.Document!.ToTopologyChart().WithTheme(Theme(), mode);
        var colors = chart.Theme.Resolve(mode);
        var options = Options();
        var svg = XDocument.Parse(chart.ToSvg(options));
        for (var index = 0; index < chart.Nodes.Count; index++)
            Assert.Equal(colors.Palette[index].ToCss(), (string?)Surface(svg, chart.Nodes[index].Id).Attribute("stroke"));
        Assert.All(chart.Edges, edge => Assert.Equal(colors.Palette[edge.PaletteIndex!.Value].ToCss(), (string?)Route(svg, edge.Id).Attribute("stroke")));
        chart.Nodes[1].Color = "#115E59"; chart.Nodes[1].BackgroundColor = "#CCFBF1";
        chart.Edges[0].Color = "#115E59";
        var overridden = XDocument.Parse(chart.ToSvg(options));
        Assert.Equal("#115E59", (string?)Surface(overridden, chart.Nodes[1].Id).Attribute("stroke"));
        Assert.Equal("#CCFBF1", (string?)Surface(overridden, chart.Nodes[1].Id).Attribute("fill"));
        Assert.Equal("#115E59", (string?)Route(overridden, chart.Edges[0].Id).Attribute("stroke"));
    }

    [Fact]
    public void OptionalPaletteIndexesRejectNegativeAssignmentsAtomically() {
        var node = new TopologyNode(); var edge = new TopologyEdge();
        Assert.Null(node.PaletteIndex); Assert.Null(edge.PaletteIndex);
        node.PaletteIndex = int.MaxValue; edge.PaletteIndex = 4;
        Assert.Throws<ArgumentOutOfRangeException>(() => node.PaletteIndex = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => edge.PaletteIndex = -1);
        Assert.Equal(int.MaxValue, node.PaletteIndex); Assert.Equal(4, edge.PaletteIndex);
        node.PaletteIndex = null; edge.PaletteIndex = null;
        Assert.Null(node.PaletteIndex); Assert.Null(edge.PaletteIndex);
    }

    private static VisualTheme Theme() => VisualTheme.Graphite().Configure(builder => {
        builder.Light.Palette = ChartPalettes.FromHex("#BE123C", "#0F766E", "#4338CA");
        builder.Dark.Palette = ChartPalettes.FromHex("#FDA4AF", "#5EEAD4", "#A5B4FC");
    });

    private static TopologyChart Diagram(VisualThemeMode mode) => TopologyChart.Create().WithId("palette-selection")
        .WithTheme(Theme(), mode).WithViewport(500, 240, 20).WithLegend(null).WithLayout(TopologyLayoutMode.Manual)
        .AddNode("first", "First", 40, 80, width: 130, height: 60)
        .AddNode("second", "Second", 320, 80, width: 130, height: 60)
        .AddEdge("route", "first", "second", routing: TopologyEdgeRouting.Straight);

    private static TopologyRenderOptions Options() => new TopologyRenderOptions {
        IncludeLegend = false, IncludeTitle = false, IncludeStatusBadges = false, NodeSurfaceStyle = TopologyNodeSurfaceStyle.Tinted
    }.WithPlainTopologyEdges();

    private static XElement Owner(XDocument svg, string role, string key, string id) => svg.Descendants().Single(element =>
        (string?)element.Attribute("data-cfx-role") == role && (string?)element.Attribute(key) == id);

    private static XElement Surface(XDocument svg, string id) => Owner(svg, "topology-node", "data-node-id", id).Descendants()
        .First(element => (string?)element.Attribute("data-cfx-role") == "topology-node-surface");

    private static XElement Route(XDocument svg, string id = "route") => Owner(svg, "topology-edge", "data-edge-id", id).Descendants()
        .Single(element => (string?)element.Attribute("data-cfx-role") == "topology-edge-line");
}

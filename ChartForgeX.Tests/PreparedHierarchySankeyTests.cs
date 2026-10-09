using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class PreparedHierarchySankeyTests {
    private static VisualRenderContext Context(double width = 720, double height = 460, VisualThemeMode mode = VisualThemeMode.Light) =>
        new(new VisualLayoutOptions(new VisualSize(width, height)), themeMode: mode, frame: new VisualFrame("Shared frame", showLegend: false));
    private static Chart Hierarchy(ChartSeriesKind kind) {
        var chart = Chart.Create();
        if (kind == ChartSeriesKind.Treemap) return chart.AddTreemap("Budget", new[] { new ChartTreemapItem("Large", 9), new ChartTreemapItem("Small", 1), new ChartTreemapItem("Equal A", 2), new ChartTreemapItem("Equal B", 2) });
        var links = new[] { new ChartTreeLink("Root with a long measured label", "First branch", 9), new ChartTreeLink("Root with a long measured label", "Second branch", 1),
            new ChartTreeLink("First branch", "First leaf", 6), new ChartTreeLink("First branch", "Second leaf", 3) };
        return kind == ChartSeriesKind.Tree ? chart.AddTree("Structure", new[] { new ChartNode("Root with a long measured label", "Root with a long measured label"), new ChartNode("First branch", "First branch"), new ChartNode("Second branch", "Second branch"), new ChartNode("First leaf", "First leaf"), new ChartNode("Second leaf", "Second leaf") }, links) : chart.AddSunburst("Structure", new[] { new ChartNode("Root with a long measured label", "Root with a long measured label"), new ChartNode("First branch", "First branch"), new ChartNode("Second branch", "Second branch"), new ChartNode("First leaf", "First leaf"), new ChartNode("Second leaf", "Second leaf") }, links);
    }
    private static IEnumerable<XElement> Role(XDocument xml, string role) => xml.Descendants().Where(e => (string?)e.Attribute("data-cfx-role") == role);
    private static double Number(XElement element, string name) => double.Parse(element.Attribute(name)!.Value, CultureInfo.InvariantCulture);

    [Theory]
    [InlineData(ChartSeriesKind.Tree, 320, 260, VisualThemeMode.Light)]
    [InlineData(ChartSeriesKind.Tree, 720, 460, VisualThemeMode.Dark)]
    [InlineData(ChartSeriesKind.Sunburst, 320, 260, VisualThemeMode.Dark)]
    [InlineData(ChartSeriesKind.Sunburst, 720, 460, VisualThemeMode.Light)]
    [InlineData(ChartSeriesKind.Treemap, 320, 260, VisualThemeMode.Light)]
    [InlineData(ChartSeriesKind.Treemap, 720, 460, VisualThemeMode.Dark)]
    public void HierarchiesUseBoundedNativeGeometryAndRetainEverySourceLabel(ChartSeriesKind kind, int width, int height, VisualThemeMode mode) {
        var chart = Hierarchy(kind); var prepared = chart.Prepare(Context(width, height, mode));
        string role = kind == ChartSeriesKind.Tree ? "tree-node" : kind == ChartSeriesKind.Sunburst ? "sunburst-segment" : "treemap-tile";
        var regions = prepared.Regions.Where(r => r.Role == role).ToArray();
        Assert.Equal(kind == ChartSeriesKind.Treemap ? 4 : 5, regions.Length);
        Assert.Equal(regions.Length, regions.Select(r => r.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.All(regions, r => { Assert.True(r.Bounds.X >= 24 && r.Bounds.Y >= 24); Assert.True(r.Bounds.Right <= width - 24 + .001 && r.Bounds.Bottom <= height - 24 + .001); });
        var xml = XDocument.Parse(prepared.ToSvg());
        Assert.Equal(regions.Length, Role(xml, role).Count());
        Assert.All(Role(xml, role), e => Assert.False(string.IsNullOrWhiteSpace((string?)e.Attribute("data-cfx-label"))));
        if (kind != ChartSeriesKind.Treemap) Assert.Contains(Role(xml, role), e => (string?)e.Attribute("data-cfx-label") == "Root with a long measured label");
        var image = prepared.ToRgba(new VisualRenderOptions(supersampling: 1)); Assert.Equal(width, image.Width); Assert.Equal(height, image.Height);
    }

    [Fact]
    public void SunburstUsesLeafWeightsAndPreservesTheFullCircleRoot() {
        var chart = Chart.Create().AddSunburst("Weights", new[] { new ChartNode("Root", "Root"), new ChartNode("Small", "Small"), new ChartNode("Large", "Large") }, new[] { new ChartTreeLink("Root", "Small", 1), new ChartTreeLink("Root", "Large", 9) });
        var xml = XDocument.Parse(chart.Prepare(Context()).ToSvg());
        var root = Role(xml, "sunburst-segment").Single(e => (string?)e.Attribute("data-cfx-label") == "Root");
        var small = Role(xml, "sunburst-segment").Single(e => (string?)e.Attribute("data-cfx-label") == "Small");
        var large = Role(xml, "sunburst-segment").Single(e => (string?)e.Attribute("data-cfx-label") == "Large");
        Assert.Equal(Math.PI * 2, Number(root, "data-cfx-sweep"), 10); Assert.Equal(10, Number(root, "data-cfx-value"));
        Assert.Equal(9, Number(large, "data-cfx-sweep") / Number(small, "data-cfx-sweep"), 10);
        Assert.Equal(Number(root, "data-cfx-outer-radius"), Number(small, "data-cfx-inner-radius"), 10);
    }

    [Fact]
    public void TreemapKeepsSourceIdsAfterWeightOrderingAndZeroValuesRemainSemantic() {
        var chart = Chart.Create().AddTreemap("Tiles", new[] { new ChartTreemapItem("Small", 1), new ChartTreemapItem("Large", 3), new ChartTreemapItem("Zero", 0), new ChartTreemapItem("Equal", 1) });
        var prepared = chart.Prepare(Context()); var xml = XDocument.Parse(prepared.ToSvg());
        var tiles = Role(xml, "treemap-tile").ToArray();
        Assert.Equal("1", (string?)tiles[0].Attribute("data-cfx-point"));
        Assert.Equal(new[] { "1", "0", "3" }, tiles.Select(e => (string?)e.Attribute("data-cfx-point")).ToArray());
        var small = prepared.Regions.Single(r => r.Id == "series-0-point-0"); var large = prepared.Regions.Single(r => r.Id == "series-0-point-1");
        Assert.InRange(large.Bounds.Width * large.Bounds.Height / (small.Bounds.Width * small.Bounds.Height), 2.8, 3.2);
        var zero = Assert.Single(prepared.Regions, r => r.Role == "treemap-zero-value"); Assert.Equal(0, zero.Bounds.Width); Assert.Equal("Zero: 0", zero.Label);
    }

    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void SankeyFractionalRibbonsAndNodesShareOneExactWeightScale(VisualThemeMode mode) {
        var chart = Chart.Create().AddSankey("Flows", new[] { new ChartNode("Source", "Source"), new ChartNode("Tiny", "Tiny"), new ChartNode("Large", "Large") }, new[] { new ChartFlowLink("flow-1", "Source", "Tiny", .001), new ChartFlowLink("flow-2", "Source", "Large", 1) });
        chart.Series[0].WithNodeState(chart.Series[0].Nodes[0].Id, ChartSeriesState.Danger);
        var prepared = chart.Prepare(Context(mode: mode)); var xml = XDocument.Parse(prepared.ToSvg());
        var links = Role(xml, "sankey-link").OrderBy(e => Number(e, "data-cfx-value")).ToArray();
        Assert.Equal(1000, Number(links[1], "data-cfx-width") / Number(links[0], "data-cfx-width"), 9);
        var source = Role(xml, "sankey-node").Single(e => (string?)e.Attribute("data-cfx-label") == "Source");
        var bar = source.Descendants().Single(e => (string?)e.Attribute("data-cfx-role") == "sankey-node-mark");
        Assert.Equal(Number(links[0], "data-cfx-width") + Number(links[1], "data-cfx-width"), prepared.Regions.Single(r => r.Id == "series-0-node-Source").Bounds.Height, 8);
        Assert.Equal(Context(mode: mode).Theme.Resolve(mode).Status.Critical.Fill.ToCss(), (string?)bar.Attribute("fill"));
        Assert.Equal(2, prepared.Regions.Count(r => r.Role == "sankey-link")); Assert.Equal(3, prepared.Regions.Count(r => r.Role == "sankey-node"));
        Assert.All(prepared.Regions, r => Assert.True(r.Bounds.X >= 24 && r.Bounds.Right <= 696 + .001 && r.Bounds.Y >= 24 && r.Bounds.Bottom <= 436 + .001));
        Assert.NotEmpty(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    [Fact]
    public void WeightedFamiliesDetachGeometryFormattingAndStateFromMutableInputs() {
        foreach (var chart in new[] { Hierarchy(ChartSeriesKind.Tree), Hierarchy(ChartSeriesKind.Sunburst), Hierarchy(ChartSeriesKind.Treemap),
            Chart.Create().AddSankey("Flows", new[] { new ChartNode("Input", "Input"), new ChartNode("Output", "Output") }, new[] { new ChartFlowLink("flow-3", "Input", "Output", 4) }) }) {
            chart.Options.ValueFormatter = value => "value " + value.ToString("0.0", CultureInfo.InvariantCulture);
            chart.Series[0].FillPattern = ChartFillPattern.DiagonalForward;
            var prepared = chart.Prepare(Context()); string svg = prepared.ToSvg(); byte[] png = prepared.ToPng(new VisualRenderOptions(supersampling: 1));
            chart.Series[0].Points.Clear(); chart.Series[0].Color = ChartColor.Black; if (chart.Series[0].Nodes.Count > 0) chart.Series[0].WithNodeState(chart.Series[0].Nodes[0].Id, ChartSeriesState.Danger);
            chart.Options.ValueFormatter = _ => "changed"; chart.Series[0].DataLabelStyle.FontSize = 30;
            Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        }
    }

    [Fact]
    public void IncompatibleRawPointsAreRejectedBeforeRelationshipLayout() {
        var tree = Hierarchy(ChartSeriesKind.Tree); tree.Series[0].Points.Add(new ChartPoint(1, 0));
        Assert.Throws<InvalidOperationException>(() => tree.Prepare(Context()));
        var sankey = Chart.Create().AddSankey("Flows", new[] { new ChartNode("A", "A"), new ChartNode("B", "B"), new ChartNode("C", "C") }, new[] { new ChartFlowLink("flow-4", "A", "B", 1), new ChartFlowLink("flow-5", "B", "C", 1) });
        sankey.Series[0].Points.Add(new ChartPoint(1, 0));
        Assert.Throws<InvalidOperationException>(() => sankey.Prepare(Context()));
        var treemap = Hierarchy(ChartSeriesKind.Treemap); treemap.Series[0].Points[0] = new ChartPoint(1, -1);
        Assert.Throws<InvalidOperationException>(() => treemap.Prepare(Context()));
    }
}

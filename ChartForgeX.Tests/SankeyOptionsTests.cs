using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class SankeyOptionsTests {
    [Theory]
    [InlineData(ChartSankeyAlignment.Left, 0, 1, 2, 3, 1, 0, 0, 1)]
    [InlineData(ChartSankeyAlignment.Right, 0, 1, 2, 3, 3, 2, 2, 3)]
    [InlineData(ChartSankeyAlignment.Center, 0, 1, 2, 3, 2, 1, 1, 2)]
    [InlineData(ChartSankeyAlignment.Justify, 0, 1, 2, 3, 3, 0, 0, 3)]
    public void UnequalPathsUseTheSelectedLayersAndOneTruthfulFlowScale(ChartSankeyAlignment alignment, params int[] layers) {
        var chart = UnequalPaths().ConfigureSankey(options => { options.Alignment = alignment; options.NodeWidth = 18; options.NodeGap = 24; });
        var nodes = chart.Series[0].Nodes.ToArray(); var flows = chart.Series[0].FlowLinks.ToArray();
        var prepared = Prepare(chart); var targets = Targets(prepared);
        Assert.Equal(layers, targets.Where(IsNode).Select(node => (int)Number(node, "layer")));
        var bounds = prepared.Regions.Where(region => region.Role == "sankey-node")
            .ToDictionary(region => region.Id.Substring("series-0-node-".Length), region => region.Bounds, StringComparer.Ordinal);
        var facts = targets.Where(IsNode).ToDictionary(node => (string)node.Attribute("data-cfx-target-id")!, StringComparer.Ordinal);
        double scale = bounds["a"].Height / Number(facts["a"], "value");
        foreach (var link in targets.Where(node => !IsNode(node))) {
            string source = (string)link.Attribute("data-cfx-source")!, target = (string)link.Attribute("data-cfx-target")!;
            Assert.True(bounds[source].Right < bounds[target].Left);
            Assert.Equal(scale, Number(link, "width") / Number(link, "value"), 10);
        }
        Assert.All(bounds, node => { Assert.Equal(18, node.Value.Width); Assert.Equal(scale, node.Value.Height / Number(facts[node.Key], "value"), 10); });
        Assert.Equal(nodes, chart.Series[0].Nodes); Assert.Equal(flows, chart.Series[0].FlowLinks);
        Assert.NotEmpty(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    [Theory]
    [InlineData(ChartSankeyVerticalAlignment.Top)]
    [InlineData(ChartSankeyVerticalAlignment.Center)]
    [InlineData(ChartSankeyVerticalAlignment.Bottom)]
    public void VerticalPlacementUsesEachColumnsUnusedSpaceWithoutChangingGapOrThickness(ChartSankeyVerticalAlignment alignment) {
        var chart = OrderedBranches().ConfigureSankey(options => {
            options.NodeOrder = ChartSankeyNodeOrder.Input; options.VerticalAlignment = alignment; options.NodeGap = 24; options.NodeWidth = 18;
        });
        var prepared = Prepare(chart);
        ChartRect Bounds(string id) => prepared.Regions.Single(region => region.Id == "series-0-node-" + id).Bounds;
        var root = Bounds("root"); var first = Bounds("b"); var middle = Bounds("z"); var last = Bounds("a");
        Assert.Equal(24, middle.Top - first.Bottom, 10); Assert.Equal(24, last.Top - middle.Bottom, 10);
        Assert.Equal(first.Height + middle.Height + last.Height, root.Height, 10);
        double expectedTop = alignment == ChartSankeyVerticalAlignment.Top ? first.Top
            : alignment == ChartSankeyVerticalAlignment.Bottom ? last.Bottom - root.Height : (first.Top + last.Bottom - root.Height) / 2;
        Assert.Equal(expectedTop, root.Top, 10);
        Assert.All(prepared.Regions.Where(region => region.Role == "sankey-node"), region => Assert.Equal(18, region.Bounds.Width));
    }

    [Theory]
    [InlineData(ChartSankeyNodeOrder.Input, "b", "z", "a")]
    [InlineData(ChartSankeyNodeOrder.LabelAscending, "b", "a", "z")]
    [InlineData(ChartSankeyNodeOrder.LabelDescending, "z", "b", "a")]
    public void DisplayOrderingRetainsTiesAuthoredIdentityAndPointStyleIndexes(ChartSankeyNodeOrder order, params string[] expected) {
        var chart = OrderedBranches().ConfigureSankey(options => { options.NodeOrder = order; options.NodeFill = ChartColor.Black; });
        var authored = ChartColor.FromHex("#7356BD"); chart.Series[0].WithPointColor(1, authored);
        var prepared = Prepare(chart);
        Assert.Equal(expected, prepared.Regions.Where(region => region.Role == "sankey-node" && region.Id != "series-0-node-root")
            .OrderBy(region => region.Bounds.Top).Select(region => region.Id.Substring("series-0-node-".Length)));
        var targets = Targets(prepared);
        Assert.Equal(new[] { "root", "b", "z", "a" }, targets.Where(IsNode).Select(node => (string?)node.Attribute("data-cfx-target-id")));
        Assert.Equal(new[] { "0", "1", "2", "3" }, targets.Where(IsNode).Select(node => (string?)node.Attribute("data-cfx-source-node-index")));
        Assert.Equal(new[] { "to-z", "to-b", "to-a" }, targets.Where(node => !IsNode(node)).Select(node => (string?)node.Attribute("data-cfx-target-id")));
        Assert.Equal(authored, prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "sankey-node-mark").ElementAt(1).Fill);
        Assert.Equal(prepared.ToSvg(), Prepare(chart).ToSvg());
        Assert.Equal(new[] { "root", "b", "z", "a" }, chart.Series[0].Nodes.Select(node => node.Id));
    }

    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void FamilyPaintRespectsPointOverridesAndClampsRadiusInTheSharedScene(VisualThemeMode mode) {
        var point = ChartColor.FromHex("#7356BD"); var nodeFill = ChartColor.FromHex("#365F63"); var ribbonFill = ChartColor.FromHex("#607DAA");
        var chart = OrderedBranches().ConfigureSankey(options => {
            options.NodeFill = nodeFill; options.RibbonFill = ribbonFill; options.RibbonOpacity = .6; options.NodeWidth = 18; options.NodeCornerRadius = 30;
        });
        chart.Series[0].Color = ChartColor.Black;
        chart.Series[0].WithPointColor(1, point).WithNodeState("a", ChartSeriesState.Warning);
        var prepared = Prepare(chart, mode);
        var bars = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "sankey-node-mark").ToArray();
        Assert.Equal(new[] { nodeFill, point, nodeFill, nodeFill }, bars.Select(node => node.Fill!.Value));
        Assert.All(bars, node => Assert.Equal(Math.Min(node.Bounds.Width, node.Bounds.Height) / 2, node.Radius));
        Assert.All(prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "sankey-ribbon"),
            ribbon => Assert.Equal(ChartColorMath.WithOpacity(ribbonFill, .6), ribbon.Fill));
        chart.Series[0].WithPointColor(0, point);
        Assert.All(Prepare(chart, mode).Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "sankey-ribbon"),
            ribbon => Assert.Equal(ChartColorMath.WithOpacity(point, .6), ribbon.Fill));
    }

    [Fact]
    public void PreparedGeometryAndPaintRemainDetachedFromLaterOptionEdits() {
        var chart = UnequalPaths(); var prepared = Prepare(chart);
        var svg = prepared.ToSvg(); var png = prepared.ToPng(new VisualRenderOptions(supersampling: 1));
        chart.ConfigureSankey(options => {
            options.Alignment = ChartSankeyAlignment.Center; options.VerticalAlignment = ChartSankeyVerticalAlignment.Top;
            options.NodeOrder = ChartSankeyNodeOrder.LabelDescending; options.NodeWidth = 18; options.NodeGap = 24; options.NodeCornerRadius = 0;
            options.NodeFill = ChartColor.Black; options.RibbonFill = ChartColor.Black; options.RibbonOpacity = .8;
        });
        Assert.NotEqual(svg, Prepare(chart).ToSvg());
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void InvalidNumbersLeaveExistingOptionsUnchanged(double invalid) {
        var options = new ChartSankeyOptions { NodeWidth = 18, NodeGap = 24, NodeCornerRadius = 1, RibbonOpacity = .6 };
        Assert.Throws<ArgumentOutOfRangeException>(() => options.NodeWidth = invalid);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.NodeGap = invalid);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.NodeCornerRadius = invalid);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.RibbonOpacity = invalid);
        Assert.Equal(18, options.NodeWidth); Assert.Equal(24d, options.NodeGap); Assert.Equal(1d, options.NodeCornerRadius); Assert.Equal(.6, options.RibbonOpacity);
    }

    [Fact]
    public void OptionBoundariesAndUnfittableDimensionsHaveExplicitFailures() {
        var chart = OrderedBranches(); var options = chart.Options.Sankey;
        Assert.Throws<ArgumentOutOfRangeException>(() => options.Alignment = (ChartSankeyAlignment)99);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.VerticalAlignment = (ChartSankeyVerticalAlignment)99);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.NodeOrder = (ChartSankeyNodeOrder)99);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.NodeWidth = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.RibbonOpacity = 1.1);
        Assert.Throws<ArgumentNullException>(() => chart.ConfigureSankey(null!));
        options.NodeGap = 0; options.NodeCornerRadius = 0; options.RibbonOpacity = 0;
        Assert.NotEmpty(Prepare(chart).ToPng(new VisualRenderOptions(supersampling: 1)));
        options.RibbonOpacity = 1; options.NodeWidth = 1000;
        Assert.Throws<NotSupportedException>(() => Prepare(chart));
        options.NodeWidth = 10; options.NodeGap = 1000;
        Assert.Throws<NotSupportedException>(() => Prepare(chart));
        options.NodeGap = null; options.NodeCornerRadius = null;
        Assert.NotEmpty(Prepare(chart).Regions); Assert.Equal(4, chart.Series[0].Nodes.Count); Assert.Equal(3, chart.Series[0].FlowLinks.Count);
    }

    private static Chart UnequalPaths() => Chart.Create().AddSankey("Paths", new[] {
        new ChartNode("a", "A"), new ChartNode("b", "B"), new ChartNode("c", "C"), new ChartNode("d", "D"),
        new ChartNode("early", "Early sink"), new ChartNode("late", "Late source"), new ChartNode("x", "Other source"), new ChartNode("y", "Other sink")
    }, new[] {
        new ChartFlowLink("ab", "a", "b", 6), new ChartFlowLink("bc", "b", "c", 6), new ChartFlowLink("cd", "c", "d", 6),
        new ChartFlowLink("early", "a", "early", 2), new ChartFlowLink("late", "late", "d", 4), new ChartFlowLink("other", "x", "y", 3)
    });
    private static Chart OrderedBranches() => Chart.Create().AddSankey("Order", new[] {
        new ChartNode("root", "Root"), new ChartNode("b", "Alpha"), new ChartNode("z", "alpha"), new ChartNode("a", "Alpha")
    }, new[] { new ChartFlowLink("to-z", "root", "z", 7), new ChartFlowLink("to-b", "root", "b", 4), new ChartFlowLink("to-a", "root", "a", 2) });
    private static PreparedVisual Prepare(Chart chart, VisualThemeMode mode = VisualThemeMode.Light) {
        chart.Series[0].WithDataLabels(false);
        return chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(720, 460)), themeMode: mode, frame: new VisualFrame(showLegend: false)));
    }
    private static XElement[] Targets(PreparedVisual prepared) => XDocument.Parse(prepared.ToSvg()).Descendants().Where(node => node.Attribute("data-cfx-target-kind") != null).ToArray();
    private static bool IsNode(XElement node) => (string?)node.Attribute("data-cfx-target-kind") == "node";
    private static double Number(XElement node, string name) => double.Parse(node.Attribute("data-cfx-" + name)!.Value, CultureInfo.InvariantCulture);
}

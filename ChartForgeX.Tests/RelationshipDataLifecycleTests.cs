using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RelationshipDataLifecycleTests {
    [Theory]
    [InlineData(ChartSeriesKind.Sankey)]
    [InlineData(ChartSeriesKind.Chord)]
    [InlineData(ChartSeriesKind.Tree)]
    [InlineData(ChartSeriesKind.Sunburst)]
    [InlineData(ChartSeriesKind.Treemap)]
    public void ReplacementSnapshotsTheGraphClearsItemOverridesAndKeepsTheSeriesHandle(ChartSeriesKind kind) {
        var series = Create(kind).WithName("Localized requests").WithState(ChartSeriesState.Warning)
            .WithColor(ChartColor.FromHex("#123456")).WithFillPattern(ChartFillPattern.DiagonalForward)
            .WithLegendEntry(false).WithDataLabels(false).WithSemanticRole("requests")
            .WithObservationId(0, new ChartObservationId("ordinal-key"))
            .WithPointColor(0, "#B34C89").WithPointFillPattern(0, ChartFillPattern.Crosshatch).WithPointLabel(0, "Old caption")
            .ConfigurePointDataLabelStyle(0, style => style.WithColor("#FFFFFF"))
            .WithNodeState("input", ChartSeriesState.Danger);
        if (IsFlow(kind)) series.WithFlowState("received", ChartSeriesState.Danger).WithFlowStyle("received", new ChartFlowStyle(fill: ChartColor.FromHex("#FF0000")));
        var chart = Chart.Create().AddSeries(series).WithLegend(false);
        var before = chart.Prepare(Context()); var svg = before.ToSvg(); var png = before.ToPng(Raster());
        var oldNodes = series.Nodes;
        var nodes = Nodes(reversed: true); var flows = Flows(9); var branches = Branches(9); var items = Items(9, reversed: true);
        Replace(series, nodes, flows, branches, items);
        nodes[0] = new ChartNode("mutated", "Mutated"); flows[0] = new ChartFlowLink("mutated", "input", "output", 99);
        branches[0] = new ChartTreeLink("input", "output", 99); items[0] = new ChartHierarchyItem("mutated", "Mutated", value: 99);

        Assert.Same(series, Assert.Single(chart.Series)); Assert.Equal("traffic", series.Id.Value);
        Assert.Equal("Localized requests", series.Name); Assert.Equal(ChartSeriesState.Warning, series.StateRole);
        Assert.Equal(ChartColor.FromHex("#123456"), series.Color); Assert.Equal(ChartFillPattern.DiagonalForward, series.FillPattern);
        Assert.False(series.ShowInLegend); Assert.False(series.ShowDataLabels); Assert.Equal("requests", series.SemanticRole);
        Assert.False(chart.Options.ShowLegend);
        Assert.Empty(series.ObservationIds); Assert.Empty(series.PointColors); Assert.Empty(series.PointFillPatterns);
        Assert.Empty(series.PointLabels); Assert.Empty(series.PointDataLabelStyles); Assert.Empty(series.NodeStates);
        Assert.Empty(series.FlowStates); Assert.Empty(series.FlowStyles);
        Assert.Equal(new[] { "output", "input" }, series.Nodes.Select(node => node.Id));
        Assert.Equal(new[] { "input", "output" }, oldNodes.Select(node => node.Id));
        Assert.Equal(2, series.ObservationCount); Assert.Equal(0, series.SourcePointCount); Assert.Empty(series.Points); Assert.True(series.Points.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => ((IList<ChartNode>)series.Nodes)[0] = nodes[0]);
        Assert.Equal(svg, before.ToSvg()); Assert.Equal(png, before.ToPng(Raster()));
        var fresh = chart.Prepare(Context()); var targets = Targets(fresh.ToSvg());
        Assert.Equal(new[] { "input", "output" }, targets.Where(IsNode).Select(Id).OrderBy(id => id, StringComparer.Ordinal));
        Assert.All(targets, target => Assert.Equal("traffic", (string?)target.Attribute("data-cfx-series-key")));
        var output = targets.Single(target => IsNode(target) && Id(target) == "output");
        Assert.Equal("Resolved", (string?)output.Attribute("data-cfx-label")); Assert.Equal("9", (string?)output.Attribute("data-cfx-value"));
        if (IsFlow(kind)) Assert.Equal("received", Id(Assert.Single(targets, target => !IsNode(target))));
        Assert.NotEqual(svg, fresh.ToSvg()); Assert.NotEmpty(fresh.ToPng(Raster()));
        Assert.Contains("data-cfx-target-id=\"output\"", chart.ToHtmlPage());
        Assert.Contains("data-cfx-target-id=\"output\"", chart.ToInteractiveHtmlPage());
    }

    [Theory]
    [InlineData(ChartSeriesKind.Sankey)]
    [InlineData(ChartSeriesKind.Chord)]
    [InlineData(ChartSeriesKind.Tree)]
    [InlineData(ChartSeriesKind.Sunburst)]
    [InlineData(ChartSeriesKind.Treemap)]
    public void InvalidOrInterruptedInputsLeaveFactsIdentitiesAndStylesIntact(ChartSeriesKind kind) {
        var series = Create(kind).WithObservationId(0, new ChartObservationId("retained"))
            .WithPointColor(0, "#B34C89").WithNodeState("input", ChartSeriesState.Warning);
        if (IsFlow(kind)) series.WithFlowState("received", ChartSeriesState.Danger).WithFlowStyle("received", new ChartFlowStyle(fillOpacity: .4));
        var chart = Chart.Create().AddSeries(series); var before = chart.Prepare(Context()).ToSvg();
        var oldNodes = series.Nodes; var oldFlows = series.FlowLinks; var oldBranches = series.TreeLinks; var oldItems = series.HierarchyItems;
        if (IsFlow(kind)) {
            Assert.Throws<ArgumentException>(() => series.ReplaceFlowData(Nodes(), new[] { new ChartFlowLink("invalid", "input", "missing", 1) }));
            Assert.Throws<ArgumentException>(() => series.ReplaceFlowData(Nodes(), new[] { Flows(1)[0], Flows(2)[0] }));
            Assert.Throws<InvalidOperationException>(() => series.ReplaceFlowData(Interrupted(Nodes()), Flows(2)));
            Assert.Throws<InvalidOperationException>(() => series.ReplaceFlowData(Nodes(), Interrupted(Flows(2))));
            Assert.Throws<ArgumentNullException>(() => series.ReplaceFlowData(null!, Flows(2)));
            Assert.Throws<ArgumentNullException>(() => series.ReplaceFlowData(Nodes(), null!));
        } else if (kind == ChartSeriesKind.Tree) {
            Assert.Throws<ArgumentException>(() => series.ReplaceTreeData(Nodes(), new[] { new ChartTreeLink("input", "missing") }));
            Assert.Throws<ArgumentException>(() => series.ReplaceTreeData(Nodes(), new[] { Branches(1)[0], new ChartTreeLink("output", "input") }));
            Assert.Throws<InvalidOperationException>(() => series.ReplaceTreeData(Interrupted(Nodes()), Branches(2)));
            Assert.Throws<InvalidOperationException>(() => series.ReplaceTreeData(Nodes(), Interrupted(Branches(2))));
            Assert.Throws<ArgumentNullException>(() => series.ReplaceTreeData(null!, Branches(2)));
            Assert.Throws<ArgumentNullException>(() => series.ReplaceTreeData(Nodes(), null!));
        } else {
            Assert.Throws<ArgumentException>(() => series.ReplaceHierarchyItems(new[] { new ChartHierarchyItem("output", "Output", "missing", 1) }));
            Assert.Throws<ArgumentException>(() => series.ReplaceHierarchyItems(new[] { Items(1)[0], Items(1)[0] }));
            Assert.Throws<InvalidOperationException>(() => series.ReplaceHierarchyItems(Interrupted(Items(2))));
            Assert.Throws<ArgumentNullException>(() => series.ReplaceHierarchyItems(null!));
        }
        Assert.Same(oldNodes, series.Nodes); Assert.Same(oldFlows, series.FlowLinks); Assert.Same(oldBranches, series.TreeLinks); Assert.Same(oldItems, series.HierarchyItems);
        Assert.Equal("retained", series.ObservationIds[0].Value); Assert.Equal(ChartColor.FromHex("#B34C89"), series.PointColors[0]);
        Assert.Equal(ChartSeriesState.Warning, series.NodeStates["input"]);
        if (IsFlow(kind)) { Assert.Equal(ChartSeriesState.Danger, series.FlowStates["received"]); Assert.Equal(.4, series.FlowStyles["received"].FillOpacity); }
        Assert.Equal(before, chart.Prepare(Context()).ToSvg());
    }

    [Theory]
    [InlineData(ChartSeriesKind.Sankey)]
    [InlineData(ChartSeriesKind.Chord)]
    [InlineData(ChartSeriesKind.Tree)]
    [InlineData(ChartSeriesKind.Sunburst)]
    [InlineData(ChartSeriesKind.Treemap)]
    public void EmptyFactoriesAndClearRefillKeepTheSameSeriesAndExportNoInventedTargets(ChartSeriesKind kind) {
        var series = Create(kind, empty: true); var chart = Chart.Create().AddSeries(series);
        AssertEmpty();
        Replace(series, Nodes(), Flows(5), Branches(5), Items(5));
        Assert.Equal(2, Targets(chart.Prepare(Context()).ToSvg()).Count(IsNode));
        series.WithObservationId(0, new ChartObservationId("cleared"));
        Replace(series, Array.Empty<ChartNode>(), Array.Empty<ChartFlowLink>(), Array.Empty<ChartTreeLink>(), Array.Empty<ChartHierarchyItem>());
        Assert.Empty(series.ObservationIds); AssertEmpty();
        Replace(series, Nodes(), Flows(7), Branches(7), Items(7));
        Assert.Equal("7", (string?)Targets(chart.Prepare(Context()).ToSvg()).Single(target => IsNode(target) && Id(target) == "output").Attribute("data-cfx-value"));
        Assert.Same(series, Assert.Single(chart.Series)); Assert.Equal("traffic", series.Id.Value);
        void AssertEmpty() {
            Assert.Equal(0, series.ObservationCount); Assert.Equal(0, series.SourcePointCount); Assert.Empty(series.Nodes); Assert.True(series.Points.IsReadOnly);
            var empty = chart.Prepare(Context()); Assert.Empty(Targets(empty.ToSvg())); Assert.NotEmpty(empty.ToPng(Raster()));
            Assert.DoesNotContain("data-cfx-target-kind=\"node\"", chart.ToHtmlPage());
        }
    }

    [Fact]
    public void EmptyEndpointsDoNotRelaxNonemptyGraphRulesOrAllowWrongFamilyReplacements() {
        var sankey = Create(ChartSeriesKind.Sankey); var tree = Create(ChartSeriesKind.Tree);
        Assert.Throws<ArgumentException>(() => sankey.ReplaceFlowData(Nodes(), Array.Empty<ChartFlowLink>()));
        Assert.Throws<ArgumentException>(() => sankey.ReplaceFlowData(Array.Empty<ChartNode>(), Flows(1)));
        Assert.Throws<ArgumentException>(() => tree.ReplaceTreeData(Nodes(), Array.Empty<ChartTreeLink>()));
        Assert.Throws<ArgumentException>(() => tree.ReplaceTreeData(Array.Empty<ChartNode>(), Branches(1)));
        Assert.Throws<ArgumentException>(() => sankey.ReplaceFlowData(Nodes().Concat(new[] { new ChartNode("unused", "Unused") }), Flows(1)));
        Assert.Throws<ArgumentException>(() => tree.ReplaceTreeData(Nodes().Concat(new[] { new ChartNode("unused", "Unused") }), Branches(1)));
        Assert.Throws<InvalidOperationException>(() => tree.ReplaceFlowData(Nodes(), Flows(1)));
        Assert.Throws<InvalidOperationException>(() => sankey.ReplaceTreeData(Nodes(), Branches(1)));
        Assert.Throws<InvalidOperationException>(() => sankey.ReplaceHierarchyItems(Items(1)));
    }

    internal static ChartSeries Create(ChartSeriesKind kind, bool empty = false) {
        var id = new ChartSeriesId("traffic"); var nodes = empty ? Array.Empty<ChartNode>() : Nodes();
        var flows = empty ? Array.Empty<ChartFlowLink>() : Flows(5); var branches = empty ? Array.Empty<ChartTreeLink>() : Branches(5);
        var items = empty ? Array.Empty<ChartHierarchyItem>() : Items(5);
        return kind switch {
            ChartSeriesKind.Sankey => ChartSeries.CreateSankey(id, "Traffic", nodes, flows),
            ChartSeriesKind.Chord => ChartSeries.CreateChord(id, "Traffic", nodes, flows),
            ChartSeriesKind.Tree => ChartSeries.CreateTree(id, "Traffic", nodes, branches),
            ChartSeriesKind.Sunburst => ChartSeries.CreateSunburst(id, "Traffic", items),
            _ => ChartSeries.CreateTreemap(id, "Traffic", items)
        };
    }
    internal static ChartNode[] Nodes(bool reversed = false) => reversed
        ? new[] { new ChartNode("output", "Resolved"), new ChartNode("input", "Received") }
        : new[] { new ChartNode("input", "Input"), new ChartNode("output", "Output") };
    internal static ChartFlowLink[] Flows(double value) => new[] { new ChartFlowLink("received", "input", "output", value) };
    private static ChartTreeLink[] Branches(double value) => new[] { new ChartTreeLink("input", "output", value) };
    private static ChartHierarchyItem[] Items(double value, bool reversed = false) => reversed
        ? new[] { new ChartHierarchyItem("output", "Resolved", "input", value), new ChartHierarchyItem("input", "Received") }
        : new[] { new ChartHierarchyItem("input", "Input"), new ChartHierarchyItem("output", "Output", "input", value) };
    private static void Replace(ChartSeries series, IEnumerable<ChartNode> nodes, IEnumerable<ChartFlowLink> flows, IEnumerable<ChartTreeLink> branches, IEnumerable<ChartHierarchyItem> items) {
        if (IsFlow(series.Kind)) series.ReplaceFlowData(nodes, flows);
        else if (series.Kind == ChartSeriesKind.Tree) series.ReplaceTreeData(nodes, branches);
        else series.ReplaceHierarchyItems(items);
    }
    private static IEnumerable<T> Interrupted<T>(IEnumerable<T> items) { foreach (var item in items) yield return item; throw new InvalidOperationException("Interrupted source"); }
    private static bool IsFlow(ChartSeriesKind kind) => kind is ChartSeriesKind.Sankey or ChartSeriesKind.Chord;
    private static bool IsNode(XElement target) => (string?)target.Attribute("data-cfx-target-kind") == "node";
    private static string? Id(XElement target) => (string?)target.Attribute("data-cfx-target-id");
    private static XElement[] Targets(string svg) => XDocument.Parse(svg).Descendants().Where(node => node.Attribute("data-cfx-target-kind") != null && (string?)node.Attribute("data-cfx-role") != "legend-entry").ToArray();
    private static VisualRenderContext Context() => new(new VisualLayoutOptions(new VisualSize(500, 320)), VisualTheme.Graphite(), frame: new VisualFrame(showLegend: false));
    private static VisualRenderOptions Raster() => new(supersampling: 1);
}

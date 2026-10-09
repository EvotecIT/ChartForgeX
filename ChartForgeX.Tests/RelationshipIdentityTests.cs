using System.Collections;
using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.Rendering;
using ChartForgeX.Primitives;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RelationshipIdentityTests {
    internal static ChartNode[] SupportNodes() => new[] {
        new ChartNode("root", "Teams"), new ChartNode("north", "North"), new ChartNode("south", "South"),
        new ChartNode("north-support", "Support"), new ChartNode("south-support", "Support")
    };
    internal static ChartTreeLink[] SupportBranches() => new[] {
        new ChartTreeLink("root", "north", 5), new ChartTreeLink("root", "south", 8),
        new ChartTreeLink("north", "north-support", 5), new ChartTreeLink("south", "south-support", 8)
    };
    internal static Chart RepeatedLabels(ChartSeriesKind kind) => Add(Chart.Create(), kind, SupportNodes(), SupportBranches());
    private static Chart Add(Chart chart, ChartSeriesKind kind, IEnumerable<ChartNode> nodes, IEnumerable<ChartTreeLink> links) => kind switch {
        ChartSeriesKind.Tree => chart.AddTree("Teams", nodes, links),
        ChartSeriesKind.Sunburst => chart.AddSunburst("Teams", nodes, links),
        _ => chart.AddSankey("Teams", nodes, links.Select(link => new ChartFlowLink("flow-" + link.ChildId, link.ParentId, link.ChildId, link.Value)))
    };
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(720, 460)), frame: new VisualFrame(showLegend: false)));
    private static XElement[] Targets(Chart chart) => XDocument.Parse(Prepare(chart).ToSvg()).Descendants().Where(e => e.Attribute("data-cfx-target-kind") != null).ToArray();
    private static double Number(XElement node, string name) => double.Parse(node.Attribute("data-cfx-" + name)!.Value, CultureInfo.InvariantCulture);

    [Theory]
    [InlineData(ChartSeriesKind.Tree)]
    [InlineData(ChartSeriesKind.Sunburst)]
    [InlineData(ChartSeriesKind.Sankey)]
    public void RepeatedLabelsRetainIndependentAuthoredNodesAndWeights(ChartSeriesKind kind) {
        var chart = RepeatedLabels(kind); var series = chart.Series[0];
        ChartDescriptionFacts? facts = null;
        chart.Options.Labels.AccessibleTextFormatter = value => { facts = value; return null; };
        var prepared = Prepare(chart);
        Assert.Equal(5, series.Nodes.Count); Assert.Empty(series.Points); Assert.Equal(0, series.SourcePointCount);
        Assert.Equal(4, kind == ChartSeriesKind.Sankey ? series.FlowLinks.Count : series.TreeLinks.Count);
        Assert.NotNull(facts); Assert.Equal(ChartDescriptionKind.Series, facts.Kind);
        Assert.Equal(new[] { "Teams" }, facts.SeriesNames);
        var supports = Targets(chart).Where(e => (string?)e.Attribute("data-cfx-label") == "Support").ToArray();
        Assert.Equal(new[] { "north-support", "south-support" }, supports.Select(e => (string?)e.Attribute("data-cfx-target-id")));
        Assert.Equal(new[] { 5d, 8d }, supports.Select(e => Number(e, "value")));
        Assert.Equal(new[] { "3", "4" }, supports.Select(e => (string?)e.Attribute("data-cfx-source-node-index")));
        Assert.All(supports, e => {
            Assert.Equal("node", (string?)e.Attribute("data-cfx-target-kind"));
            Assert.Equal("0", (string?)e.Attribute("data-cfx-series"));
            Assert.Equal("Teams", (string?)e.Attribute("data-cfx-series-key"));
            Assert.Null(e.Attribute("data-cfx-point")); Assert.Null(e.Attribute("data-cfx-source-point"));
        });
        Assert.Equal(2, prepared.Regions.Count(region => region.Id.EndsWith("support", StringComparison.Ordinal) && region.Role != "tree-link" && region.Role != "sankey-link"));
    }

    [Fact]
    public void ParallelSankeyFlowsRetainSeparateIdsAndConserveTheirWidths() {
        var chart = Chart.Create().AddSankey("Traffic", new[] { new ChartNode("a", "Support"), new ChartNode("b", "Support") }, new[] {
            new ChartFlowLink("standard", "a", "b", 5), new ChartFlowLink("priority", "a", "b", 8)
        });
        var targets = Targets(chart); var links = targets.Where(e => (string?)e.Attribute("data-cfx-target-kind") == "link").ToArray();
        Assert.Equal(new[] { "standard", "priority" }, links.Select(e => (string?)e.Attribute("data-cfx-target-id")));
        Assert.Equal(new[] { "0", "1" }, links.Select(e => (string?)e.Attribute("data-cfx-source-link-index")));
        Assert.All(links, e => { Assert.Equal("a", (string?)e.Attribute("data-cfx-source")); Assert.Equal("b", (string?)e.Attribute("data-cfx-target")); });
        Assert.Equal(8d / 5, Number(links[1], "width") / Number(links[0], "width"), 12);
        Assert.All(targets.Where(e => (string?)e.Attribute("data-cfx-target-kind") == "node"), e => Assert.Equal(13, Number(e, "value")));
        var source = Prepare(chart).Regions.Single(region => region.Id == "series-0-node-a");
        Assert.Equal(links.Sum(e => Number(e, "width")), source.Bounds.Height, 10);
    }

    [Theory]
    [InlineData(ChartSeriesKind.Tree)]
    [InlineData(ChartSeriesKind.Sunburst)]
    [InlineData(ChartSeriesKind.Sankey)]
    public void IdentitySurvivesInputReorderingAndLabelRenaming(ChartSeriesKind kind) {
        var first = RepeatedLabels(kind);
        var nodes = SupportNodes().AsEnumerable().Reverse().Select(node => new ChartNode(node.Id, node.Id == "north-support" ? "Customer care" : node.Label)).ToArray();
        var second = Add(Chart.Create(), kind, nodes, SupportBranches().AsEnumerable().Reverse());
        if (kind == ChartSeriesKind.Sankey) {
            first.WithSankeyNodeState("north-support", ChartSeriesState.Warning);
            second.WithSankeyNodeState("north-support", ChartSeriesState.Warning);
            Assert.Throws<ArgumentException>(() => second.WithSankeyNodeState("Support", ChartSeriesState.Danger));
        }
        var old = Targets(first).ToDictionary(e => (string)e.Attribute("data-cfx-source-id")!, StringComparer.Ordinal);
        var reordered = Targets(second);
        Assert.Equal(old.Keys.OrderBy(id => id, StringComparer.Ordinal), reordered.Select(e => (string)e.Attribute("data-cfx-source-id")!).OrderBy(id => id, StringComparer.Ordinal));
        foreach (var target in reordered) {
            var original = old[(string)target.Attribute("data-cfx-source-id")!];
            Assert.Equal((string?)original.Attribute("data-cfx-target-id"), (string?)target.Attribute("data-cfx-target-id"));
            Assert.Equal((string?)original.Attribute("data-cfx-value"), (string?)target.Attribute("data-cfx-value"));
        }
        var renamed = reordered.Single(e => (string?)e.Attribute("data-cfx-target-kind") == "node" && (string?)e.Attribute("data-cfx-target-id") == "north-support");
        Assert.Equal("Customer care", (string?)renamed.Attribute("data-cfx-label"));
        if (kind == ChartSeriesKind.Sankey) Assert.Equal("Warning", (string?)renamed.Attribute("data-cfx-state"));
    }

    [Theory]
    [InlineData(ChartSeriesKind.Tree)]
    [InlineData(ChartSeriesKind.Sunburst)]
    [InlineData(ChartSeriesKind.Sankey)]
    public void InvalidNodesReferencesAndCyclesLeaveTheChartUnchanged(ChartSeriesKind kind) {
        var chart = Chart.Create().AddLine("Existing", new[] { new ChartPoint(1, 2) });
        var existing = chart.Series[0];
        void Reject(IEnumerable<ChartNode> nodes, IEnumerable<ChartTreeLink> links) {
            Assert.Throws<ArgumentException>(() => Add(chart, kind, nodes, links));
            Assert.Same(existing, Assert.Single(chart.Series)); Assert.Equal(2, existing.Points[0].Y);
        }
        var valid = new[] { new ChartNode("a", "Same"), new ChartNode("b", "Same") };
        Reject(new[] { default(ChartNode), valid[1] }, new[] { new ChartTreeLink("a", "b") });
        Reject(new[] { valid[0], new ChartNode("a", "Another label") }, new[] { new ChartTreeLink("a", "b") });
        Reject(valid, new[] { new ChartTreeLink("a", "missing") });
        Reject(valid, new[] { new ChartTreeLink("a", "a") });
        Reject(valid, new[] { new ChartTreeLink("a", "b"), new ChartTreeLink("b", "a") });
        if (kind != ChartSeriesKind.Sankey) {
            Reject(valid, new[] { default(ChartTreeLink) });
            Reject(SupportNodes(), new[] { new ChartTreeLink("north", "north-support"), new ChartTreeLink("south", "north-support") });
            Reject(SupportNodes(), new[] { new ChartTreeLink("north", "north-support"), new ChartTreeLink("south", "south-support") });
        }
    }

    [Fact]
    public void SankeyRejectsDefaultAndDuplicateFlowIdsWithoutAddingPartialData() {
        var chart = Chart.Create(); var nodes = new[] { new ChartNode("a", "A"), new ChartNode("b", "B") };
        Assert.Throws<ArgumentException>(() => chart.AddSankey("Flow", nodes, new[] { default(ChartFlowLink) }));
        Assert.Throws<ArgumentException>(() => chart.AddSankey("Flow", nodes, new[] { new ChartFlowLink("same", "a", "b", 1), new ChartFlowLink("same", "a", "b", 2) }));
        Assert.Empty(chart.Series);
        Assert.Throws<ArgumentException>(() => new ChartNode(" ", "Label"));
        Assert.Throws<ArgumentException>(() => new ChartNode("id", " "));
        Assert.Throws<ArgumentException>(() => new ChartFlowLink(" ", "a", "b", 1));
        Assert.Throws<ArgumentException>(() => new ChartTreeLink(" ", "b"));
    }

    [Theory]
    [InlineData(ChartSeriesKind.Tree)]
    [InlineData(ChartSeriesKind.Sunburst)]
    [InlineData(ChartSeriesKind.Sankey)]
    public void SourceCollectionsAreImmutableSnapshotsAndPreparedExportsAreDetached(ChartSeriesKind kind) {
        var nodes = SupportNodes().ToList(); var branches = SupportBranches().ToList();
        var flows = branches.Select(link => new ChartFlowLink("flow-" + link.ChildId, link.ParentId, link.ChildId, link.Value)).ToList();
        var chart = kind == ChartSeriesKind.Sankey ? Chart.Create().AddSankey("Teams", nodes, flows) : Add(Chart.Create(), kind, nodes, branches);
        var prepared = Prepare(chart); var svg = prepared.ToSvg(); var png = prepared.ToPng(new VisualRenderOptions(supersampling: 1));
        nodes.Clear(); branches.Clear(); flows.Clear();
        Assert.Equal(5, chart.Series[0].Nodes.Count); Assert.Equal(svg, Prepare(chart).ToSvg());
        Assert.Throws<NotSupportedException>(() => ((IList)chart.Series[0].Nodes)[0] = new ChartNode("changed", "Changed"));
        var links = kind == ChartSeriesKind.Sankey ? (IList)chart.Series[0].FlowLinks : (IList)chart.Series[0].TreeLinks;
        Assert.Throws<NotSupportedException>(() => links.Clear());
        chart.Series[0].PointLabels.Add("Changed"); chart.Series[0].DataLabelStyle.FontSize = 30;
        chart.Series[0].Color = ChartColor.Black; chart.Options.SankeyNodeStates.Clear();
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    [Theory]
    [InlineData(5e-8, 5e-7)]
    [InlineData(1e-300, 2e-300)]
    [InlineData(double.Epsilon, double.Epsilon * 2)]
    [InlineData(double.MaxValue / 8, double.MaxValue / 4)]
    public void SunburstPreservesAuthoredWeightsAndNormalizesBeforeMultiplyingAngles(double small, double large) {
        var chart = Chart.Create().AddSunburst("Weights", new[] { new ChartNode("root", "Root"), new ChartNode("small", "Small"), new ChartNode("large", "Large") },
            new[] { new ChartTreeLink("root", "small", small), new ChartTreeLink("root", "large", large) });
        var nodes = Targets(chart).ToDictionary(e => (string)e.Attribute("data-cfx-target-id")!);
        Assert.Equal(small, Number(nodes["small"], "authored-weight")); Assert.Equal(large, Number(nodes["large"], "authored-weight"));
        Assert.Equal(small, Number(nodes["small"], "value")); Assert.Equal(large, Number(nodes["large"], "value"));
        Assert.Equal(small + large, Number(nodes["root"], "value")); Assert.Null(nodes["root"].Attribute("data-cfx-authored-weight"));
        Assert.Equal(Math.PI * 2, Number(nodes["small"], "sweep") + Number(nodes["large"], "sweep"), 12);
        Assert.Equal(large / small, Number(nodes["large"], "sweep") / Number(nodes["small"], "sweep"), 12);
        Assert.All(Prepare(chart).Regions, region => { Assert.True(double.IsFinite(region.Bounds.X)); Assert.True(double.IsFinite(region.Bounds.Width)); });
        Assert.NotEmpty(Prepare(chart).ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    [Fact]
    public void TreeRetainsTinyWeightsWithoutChangingUnweightedNodePlacement() {
        var nodes = new[] { new ChartNode("r", "Root"), new ChartNode("a", "A"), new ChartNode("b", "B") };
        var tiny = Chart.Create().AddTree("Structure", nodes, new[] { new ChartTreeLink("r", "a", 5e-8), new ChartTreeLink("r", "b", 5e-7) });
        var scaled = Chart.Create().AddTree("Structure", nodes, new[] { new ChartTreeLink("r", "a", 1), new ChartTreeLink("r", "b", 10) });
        Assert.Equal(Prepare(scaled).Regions.Where(r => r.Role == "tree-node").Select(r => r.Bounds), Prepare(tiny).Regions.Where(r => r.Role == "tree-node").Select(r => r.Bounds));
        Assert.Equal(new[] { 5e-8, 5e-7 }, Targets(tiny).Where(e => (string?)e.Attribute("data-cfx-target-kind") == "link").Select(e => Number(e, "value")));
    }

    [Fact]
    public void SunburstKeepsInternalIncomingWeightsSeparateFromLeafAggregatesAndRejectsOverflow() {
        var chart = Chart.Create().AddSunburst("Nested", new[] { new ChartNode("r", "Root"), new ChartNode("p", "Parent"), new ChartNode("leaf", "Leaf") },
            new[] { new ChartTreeLink("r", "p", 100), new ChartTreeLink("p", "leaf", 5e-8) });
        var parent = Targets(chart).Single(e => (string?)e.Attribute("data-cfx-target-id") == "p");
        Assert.Equal(100, Number(parent, "authored-weight")); Assert.Equal(5e-8, Number(parent, "value"));
        var invalid = Chart.Create();
        Assert.Throws<ArgumentException>(() => invalid.AddSunburst("Too large", new[] { new ChartNode("r", "Root"), new ChartNode("a", "A"), new ChartNode("b", "B") },
            new[] { new ChartTreeLink("r", "a", double.MaxValue), new ChartTreeLink("r", "b", double.MaxValue) }));
        Assert.Empty(invalid.Series);
    }

    [Fact]
    public void MermaidCsvMapsLanguageIdentitiesIntoTypedNodesAndParallelFlows() {
        var parsed = new MermaidParser().ParseSankey("sankey-beta\nNorth,Support,5\nNorth,Support,8");
        Assert.False(parsed.HasErrors); var chart = parsed.Document!.ToChart();
        Assert.Equal(new[] { "North", "Support" }, chart.Series[0].Nodes.Select(node => node.Id));
        Assert.Equal(new[] { 5d, 8d }, chart.Series[0].FlowLinks.Select(link => link.Value));
        Assert.Equal(2, chart.Series[0].FlowLinks.Select(link => link.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.Empty(chart.Series[0].Points); Assert.NotEmpty(chart.ToPng());
    }

    [Fact]
    public void DirectedFlowFactsDoNotImposeTheSankeyGraphPolicy() {
        var self = new ChartFlowLink("self", "a", "a", 1);
        var forward = new ChartFlowLink("forward", "a", "b", 1);
        var reverse = new ChartFlowLink("reverse", "b", "a", 2);
        var nodes = new[] { new ChartNode("a", "A"), new ChartNode("b", "B") };
        Assert.Equal("a", self.TargetId); Assert.Equal("a", reverse.TargetId);
        Assert.Throws<ArgumentException>(() => Chart.Create().AddSankey("Flow", nodes, new[] { self }));
        Assert.Throws<ArgumentException>(() => Chart.Create().AddSankey("Flow", nodes, new[] { forward, reverse }));
    }

    [Theory]
    [InlineData(5e-8, 5e-7)]
    [InlineData(1e307, 2e307)]
    public void SankeyPreservesSupportedFiniteWeightsAndTheirProportions(double small, double large) {
        var chart = Chart.Create().AddSankey("Weights", new[] { new ChartNode("r", "Root"), new ChartNode("a", "A"), new ChartNode("b", "B") },
            new[] { new ChartFlowLink("small", "r", "a", small), new ChartFlowLink("large", "r", "b", large) });
        var links = Targets(chart).Where(e => (string?)e.Attribute("data-cfx-target-kind") == "link").ToArray();
        Assert.Equal(new[] { small, large }, links.Select(e => Number(e, "value")));
        Assert.Equal(large / small, Number(links[1], "width") / Number(links[0], "width"), 12);
        Assert.All(Prepare(chart).Regions, region => Assert.True(double.IsFinite(region.Bounds.Height)));
    }
}

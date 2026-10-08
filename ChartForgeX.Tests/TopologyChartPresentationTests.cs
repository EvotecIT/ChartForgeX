using System.Xml.Linq;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Render options carried by a chart, legend line samples, and tile captions.</summary>
public sealed class TopologyChartPresentationTests {
    private static readonly TopologyRenderOptions Tiles = new() { ReadableDenseLayout = true, NodeDisplayMode = TopologyNodeDisplayMode.Tile };

    [Fact]
    public void DefaultRenderOptions_AreUsedWhenACallPassesNone() {
        var chart = Chart().WithRenderOptions(Tiles);

        Assert.Equal(Chart().ToSvg(Tiles), chart.ToSvg());
        Assert.Equal(Chart().ToPng(Tiles), chart.ToPng());
        Assert.Equal(Chart().ToHtmlFragment(Tiles), chart.ToHtmlFragment());
        Assert.Equal(Chart().Prepare(Tiles).ToSvg(), chart.Prepare().ToSvg());
        Assert.NotNull(TopologyLayoutDiagnostics.Analyze(chart).Nodes[0].CaptionBounds);
        Assert.NotEqual(Chart().ToSvg(), chart.ToSvg());
    }

    [Fact]
    public void DefaultRenderOptions_ReachArtifactInterchangeAndInteractiveHtml() {
        var chart = Chart().WithRenderOptions(Tiles);

        Assert.Equal(Chart().ToVisualArtifact().ToSvg(new VisualArtifactRenderOptions { Topology = Tiles }), chart.ToVisualArtifact().ToSvg());
        var fitted = Chart().ToVisualArtifact();
        fitted.PreserveNaturalSize = true;
        var stored = chart.ToVisualArtifact();
        stored.PreserveNaturalSize = true;
        // A discovered size preserves the prepared layout; replacing it requests a new viewport.
        // Give both artifacts the same actual replacement, distinct from either discovery snapshot.
        var explicitSize = new VisualArtifactSize(stored.NaturalSize!.Value.Width + 20, stored.NaturalSize.Value.Height + 20);
        fitted.NaturalSize = explicitSize; stored.NaturalSize = explicitSize;
        Assert.Equal(fitted.ToSvg(new VisualArtifactRenderOptions { Topology = Tiles }), stored.ToSvg());
        Assert.Equal(Chart().ToVisualArtifact().ToInterchangeJson(new VisualArtifactRenderOptions { Topology = Tiles }), chart.ToVisualArtifact().ToInterchangeJson());
        Assert.Equal(new HtmlInteractiveTopologyRenderer().RenderFragment(Chart(), Tiles), new HtmlInteractiveTopologyRenderer().RenderFragment(chart));
        Assert.Null(chart.DefaultRenderOptions!.View);
    }

    [Fact]
    public void DefaultRenderOptions_AreReplacedByOptionsPassedToACall() {
        var chart = Chart().WithRenderOptions(Tiles);
        var cards = new TopologyRenderOptions();
        Assert.Equal(Chart().ToSvg(cards), chart.ToSvg(cards));
    }

    [Fact]
    public void WithRenderOptions_KeepsItsOwnCopy() {
        var options = Tiles.Clone();
        var chart = Chart().WithRenderOptions(options);
        var before = chart.ToSvg();
        options.NodeDisplayMode = TopologyNodeDisplayMode.Card;
        options.ReadableDenseLayout = false;
        Assert.Equal(before, chart.ToSvg());
        Assert.Equal(Chart().ToSvg(), chart.WithRenderOptions(null).ToSvg());
    }

    [Theory]
    [InlineData(TopologyEdgeLineStyle.Auto, TopologyEdgeLineStyle.Auto, "none")]
    [InlineData(TopologyEdgeLineStyle.Auto, TopologyEdgeLineStyle.Dashed, "8 5")]
    [InlineData(TopologyEdgeLineStyle.Auto, TopologyEdgeLineStyle.Dotted, "2 5")]
    [InlineData(TopologyEdgeLineStyle.Dashed, TopologyEdgeLineStyle.Solid, "8 5")]
    [InlineData(TopologyEdgeLineStyle.Solid, TopologyEdgeLineStyle.Dashed, "none")]
    public void LegendEdgeSample_FollowsTheEdgesOfItsKind(TopologyEdgeLineStyle itemStyle, TopologyEdgeLineStyle edgeStyle, string expectedDash) {
        var chart = Chart().WithLegend(TopologyLegend.Create("Legend").AddEdgeKind("Replication", TopologyEdgeKind.Replication, lineStyle: itemStyle));
        foreach (var edge in chart.Edges) edge.LineStyle = edgeStyle;

        var sample = XDocument.Parse(chart.ToSvg()).Descendants()
            .Single(element => (string?)element.Attribute("data-legend-kind") == "edge")
            .Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "topology-legend-edge");
        Assert.Equal(expectedDash, (string?)sample.Attribute("stroke-dasharray") ?? "none");
        Assert.True(chart.ToPng().Length > 64);
    }

    [Fact]
    public void LegendEdgeSample_IsDashedWhenEveryEdgeHasADashPattern() {
        var chart = Chart().WithLegend(TopologyLegend.Create("Legend").AddEdgeKind("Replication", TopologyEdgeKind.Replication));
        foreach (var edge in chart.Edges) chart.WithEdgeStroke(edge.Id, dashPattern: new[] { 6.0, 3.0 });

        var sample = XDocument.Parse(chart.ToSvg()).Descendants()
            .Single(element => (string?)element.Attribute("data-legend-kind") == "edge")
            .Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "topology-legend-edge");
        Assert.Equal("8 5", (string?)sample.Attribute("stroke-dasharray"));
        Assert.Contains("\"lineStyle\":\"Dashed\"", chart.ToVisualArtifact().ToInterchangeJson().Replace(" ", string.Empty), StringComparison.Ordinal);
    }

    [Fact]
    public void LegendEdgeSample_IsSolidWhenEdgesOfTheKindDiffer() {
        var chart = Chart().WithLegend(TopologyLegend.Create("Legend").AddEdgeKind("Replication", TopologyEdgeKind.Replication));
        chart.Edges[0].Status = TopologyHealthStatus.Critical;

        var sample = XDocument.Parse(chart.ToSvg()).Descendants()
            .Single(element => (string?)element.Attribute("data-legend-kind") == "edge")
            .Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "topology-legend-edge");
        Assert.Equal("none", (string?)sample.Attribute("stroke-dasharray") ?? "none");
    }

    [Fact]
    public void ReadableTileCaption_WrapsAtSeparatorsBeforeItIsShortened() {
        var chart = TopologyChart.Create().WithId("captions").WithViewport(520, 260, 24).WithLegend(null)
            .AddNode("a", "REGION-SouthEastAsia", 80, 80, width: 64, height: 40)
            .AddNode("b", "Short", 300, 80, width: 64, height: 40);

        var readable = NodeCaption(chart.ToSvg(Tiles), "a");
        Assert.Equal(new[] { "REGION-", "SouthEastAsia" }, readable);
        Assert.Equal(new[] { "Short" }, NodeCaption(chart.ToSvg(Tiles), "b"));

        var classic = NodeCaption(chart.ToSvg(new TopologyRenderOptions { NodeDisplayMode = TopologyNodeDisplayMode.Tile }), "a");
        Assert.Single(classic);
        Assert.EndsWith("...", classic[0], StringComparison.Ordinal);

        var report = chart.Prepare(Tiles).Analyze();
        Assert.True(report.Nodes[0].CaptionBounds!.Value.Height > report.Nodes[1].CaptionBounds!.Value.Height, "A two-line caption reserves more height.");
        Assert.True(chart.ToPng(Tiles).Length > 64);
    }

    [Fact]
    public void ReadableTileCaption_ShortensOnlyTheLastAllowedLine() {
        var chart = TopologyChart.Create().WithId("long-caption").WithViewport(520, 260, 24).WithLegend(null)
            .AddNode("a", "Alpha-Bravo-Charlie-Delta-Echo-Foxtrot-Golf-Hotel", 200, 80, width: 64, height: 40);
        chart.Nodes[0].MaximumLabelCharacters = 64;

        var lines = NodeCaption(chart.ToSvg(Tiles), "a");
        Assert.Equal(2, lines.Length);
        Assert.False(lines[0].EndsWith("...", StringComparison.Ordinal));
        Assert.EndsWith("...", lines[1], StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ReadableTileCaption_OverTheCharacterLimit_EndsWithOneEllipsisOnTheLastLine(int maxLines) {
        var chart = TopologyChart.Create().WithId("limit-caption").WithViewport(640, 280, 24).WithLegend(null)
            .AddNode("a", "Alpha.Bravo.Charlie.Delta.Echo.Foxtrot.Golf.Hotel.India.Juliet.Kilo.Lima.Mike", 240, 80, width: 150, height: 40);
        chart.Nodes[0].MaximumLabelCharacters = 10;
        var options = Tiles.Clone();
        options.MaxNodeLabelLines = maxLines;

        var lines = NodeCaption(chart.ToSvg(options), "a");
        Assert.InRange(lines.Length, 1, maxLines);
        Assert.EndsWith("...", lines[^1], StringComparison.Ordinal);
        Assert.False(lines[^1].EndsWith("....", StringComparison.Ordinal));
        Assert.All(lines.Take(lines.Length - 1), line => Assert.False(line.EndsWith("..", StringComparison.Ordinal)));
        Assert.All(lines, line => Assert.NotEqual(0, line.Trim('.').Length));
    }

    private static string[] NodeCaption(string svg, string nodeId) {
        var node = XDocument.Parse(svg).Descendants().First(element => (string?)element.Attribute("data-cfx-role") == "topology-node" && (string?)element.Attribute("data-node-id") == nodeId);
        return node.Descendants().Where(element => element.Name.LocalName == "text" && element.Ancestors().Any(parent => (string?)parent.Attribute("data-cfx-role") == "topology-node-label"))
            .SelectMany(text => text.Elements().Any(child => child.Name.LocalName == "tspan") ? text.Elements().Where(child => child.Name.LocalName == "tspan").Select(child => child.Value) : new[] { text.Value })
            .ToArray();
    }

    private static TopologyChart Chart() => TopologyChart.Create().WithId("presentation").WithTitle("Presentation").WithViewport(720, 420, 24)
        .WithLayout(TopologyLayoutMode.DenseGrouped, TopologyLayoutDirection.LeftToRight)
        .WithLegend(TopologyLegend.Create("Legend").AddStatus("Healthy", TopologyHealthStatus.Healthy))
        .AddAutoGroup("left", "Left", TopologyHealthStatus.Healthy)
        .AddAutoGroup("right", "Right", TopologyHealthStatus.Healthy)
        .AddAutoNode("a", "Server A", TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "left", width: 100, height: 40)
        .AddAutoNode("b", "Server B", TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "left", width: 100, height: 40)
        .AddAutoNode("c", "Server C", TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "right", width: 100, height: 40)
        .AddEdge("a-c", "a", "c", null, TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal)
        .AddEdge("b-c", "b", "c", null, TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal);
}

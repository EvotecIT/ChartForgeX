using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyC4SharedPolicyClosureTests {
    [Theory]
    [InlineData(TopologyEdgeRouting.Straight)]
    [InlineData(TopologyEdgeRouting.Curved)]
    public void BoundedGridKeepsExplicitNonOrthogonalRoutesThroughPortableManualRestoration(TopologyEdgeRouting routing) {
        var chart = BoundedSource();
        var edge = chart.Edges.Single(item => item.SourceNodeId == "operator");
        edge.Routing = routing;
        edge.SourcePort = TopologyEdgePort.Right;
        edge.TargetPort = TopologyEdgePort.Left;
        var prepared = chart.Prepare();
        var saved = prepared.ToInterchangeEnvelope();
        var actual = saved.Edges.Single(item => item.Id == edge.Id);
        Assert.Equal(routing, actual.Topology!.Routing);
        Assert.Equal(TopologyEdgePort.Right, actual.Topology.SourcePort);
        Assert.Equal(TopologyEdgePort.Left, actual.Topology.TargetPort);
        var restored = VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson()).ToTopologyChart();
        Assert.Equal(TopologyLayoutMode.Manual, restored.LayoutMode);
        var after = restored.Prepare().ToInterchangeEnvelope();
        Assert.Equal(routing, after.Edges.Single(item => item.Id == edge.Id).Topology!.Routing);
        Assert.Equal(saved.Nodes.Select(node => (node.Id, node.X, node.Y, node.Width, node.Height)),
            after.Nodes.Select(node => (node.Id, node.X, node.Y, node.Width, node.Height)));
        Assert.Equal(routing, edge.Routing);
        Capture(prepared, "c4-review-explicit-" + routing);
    }

    [Fact]
    public void BoundedGridKeepsAuthoredWaypointsAndEndpointSides() {
        var chart = BoundedSource();
        var edge = chart.Edges.Single(item => item.SourceNodeId == "operator");
        edge.SourcePort = TopologyEdgePort.Right;
        edge.TargetPort = TopologyEdgePort.Left;
        edge.Waypoints.AddRange(new[] { new ChartPoint(600, 180), new ChartPoint(650, 280) });
        var sourcePoints = edge.Waypoints.ToArray();
        var saved = chart.Prepare().ToInterchangeEnvelope();
        var actual = saved.Edges.Single(item => item.Id == edge.Id);
        Assert.Equal(TopologyEdgeRouting.Orthogonal, actual.Topology!.Routing);
        Assert.Equal(TopologyEdgePort.Right, actual.Topology.SourcePort);
        Assert.Equal(TopologyEdgePort.Left, actual.Topology.TargetPort);
        Assert.Equal(2, actual.Topology.Waypoints.Count);
        Assert.Equal(50, actual.Topology.Waypoints[1].X - actual.Topology.Waypoints[0].X, 6);
        Assert.Equal(100, actual.Topology.Waypoints[1].Y - actual.Topology.Waypoints[0].Y, 6);
        var restored = saved.ToTopologyChart().Edges.Single(item => item.Id == edge.Id);
        Assert.Equal(actual.Topology.Waypoints.Select(point => (point.X, point.Y)), restored.Waypoints.Select(point => (point.X, point.Y)));
        Assert.Equal(sourcePoints, edge.Waypoints);
    }

    [Fact]
    public void PromotedOrthogonalRoutePreservesNamedOffsetsAndTheMutableSource() {
        var chart = BoundedSource();
        chart.AddEdge("named", "b", "d");
        var edge = chart.Edges.Last();
        chart.Nodes.Single(node => node.Id == "b").Ports.Add(new TopologyNodePort { Id = "out", Side = TopologyEdgePort.Right, Offset = .35 });
        chart.Nodes.Single(node => node.Id == "d").Ports.Add(new TopologyNodePort { Id = "in", Side = TopologyEdgePort.Left, Offset = .65 });
        edge.SourcePortId = "out"; edge.TargetPortId = "in";
        var prepared = chart.Prepare();
        var saved = prepared.ToInterchangeEnvelope();
        var actual = saved.Edges.Single(item => item.Id == "named");
        Assert.Equal(TopologyEdgeRouting.ObstacleAvoidingOrthogonal, actual.Topology!.Routing);
        Assert.Equal("out", actual.SourcePortId); Assert.Equal("in", actual.TargetPortId);
        Assert.Equal(TopologyEdgePort.Right, actual.Topology.SourcePort); Assert.Equal(TopologyEdgePort.Left, actual.Topology.TargetPort);
        var source = saved.Nodes.Single(node => node.Id == "b"); var target = saved.Nodes.Single(node => node.Id == "d");
        Assert.Equal(source.Y!.Value + source.Height!.Value * .35, actual.ResolvedRoute[0].Y, 6);
        Assert.Equal(target.Y!.Value + target.Height!.Value * .65, actual.ResolvedRoute[^1].Y, 6);
        foreach (var other in saved.Nodes.Where(node => node.Id is not ("b" or "d")))
            Assert.False(TopologyAlignmentReviewCausalTests.CrossesBody(actual, other));
        Assert.Equal(TopologyEdgeRouting.Orthogonal, edge.Routing);
        Assert.Equal(TopologyEdgePort.Auto, edge.SourcePort); Assert.Equal(TopologyEdgePort.Auto, edge.TargetPort);
        Capture(prepared, "c4-review-named-grid-route");
    }

    [Theory]
    [InlineData(TopologyNodeShape.Rectangle)]
    [InlineData(TopologyNodeShape.Rounded)]
    public void LabelOnlyPaddingWithoutWidthPolicyUsesTheSharedEnclosedAllocation(TopologyNodeShape shape) {
        var chart = TopologyChart.Create().WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family))
            .WithViewport(600, 300).WithLayout(TopologyLayoutMode.GroupGrid)
            .AddAutoNode("padded", "Complete caption", width: 80, height: 36);
        var node = chart.Nodes[0]; node.Shape = shape; node.ShowStatusBadge = false;
        node.FitLabelToContent = true; node.PreserveDisplayModeSize = true; node.ContentPadding = 30;
        var prepared = chart.Prepare(new TopologyRenderOptions { IncludeTitle = false, IncludeLegend = false });
        var actual = Assert.Single(prepared.ToInterchangeEnvelope().Nodes);
        var label = Assert.Single(prepared.Visual.Scene.Nodes.OfType<VisualSceneText>(), text => text.Role == "topology-node-label");
        Assert.Equal("Complete caption", string.Concat(label.Text.Lines.Select(line => line.Text)));
        Assert.True(label.X - label.Text.Metrics.Width / 2 >= actual.X!.Value + 30);
        Assert.True(label.X + label.Text.Metrics.Width / 2 <= actual.X!.Value + actual.Width!.Value - 30);
        Assert.True(actual.Width!.Value >= label.Text.Metrics.Width + 60);
        Assert.Null(actual.Topology!.MaximumTextWidth); Assert.Equal(30, actual.Topology.ContentPadding);
        Assert.Equal(80, node.Width); Assert.Equal(36, node.Height);
        Capture(prepared, "c4-review-padding-only-" + shape);
    }

    [Fact]
    public void OptedInIconCaptionIsAnActualRouteObstacle() {
        var chart = TopologyChart.Create().WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family))
            .WithViewport(700, 400).WithLayout(TopologyLayoutMode.Manual)
            .AddNode("source", "Source", 40, 170, width: 70, height: 40)
            .AddNode("icon", "Wide WWW", 270, 140, width: 28, height: 28)
            .AddNode("target", "Target", 500, 170, width: 70, height: 40)
            .AddEdge("connection", "source", "target", routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal);
        foreach (var node in chart.Nodes) { node.ShowStatusBadge = false; node.PreserveDisplayModeSize = true; }
        var icon = chart.Nodes[1]; icon.DisplayMode = TopologyNodeDisplayMode.Icon;
        icon.TextStyle = new TextStyleOverride { FontFamily = BrowserProofFonts.Family, FontSize = 36 };
        var prepared = chart.Prepare(new TopologyRenderOptions { IncludeTitle = false, IncludeLegend = false,
            IncludeIconLabels = true, TextMeasurementMode = TextMeasurementMode.InstalledFonts });
        var plate = Assert.Single(prepared.Visual.Scene.Nodes.OfType<VisualSceneRectangle>(), mark => mark.Role == "topology-node-icon-label");
        var edge = Assert.Single(prepared.ToInterchangeEnvelope().Edges);
        var paintedCaption = new VisualArtifactInterchangeNode { Id = "painted-icon-caption", X = plate.Bounds.X, Y = plate.Bounds.Y,
            Width = plate.Bounds.Width, Height = plate.Bounds.Height };
        Assert.False(TopologyAlignmentReviewCausalTests.CrossesBody(edge, paintedCaption));
        Capture(prepared, "c4-review-icon-caption-obstacle");
    }

    private static void Capture(PreparedTopology prepared, string name) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } directory) return;
        Directory.CreateDirectory(directory); var stem = Path.Combine(directory, name);
        File.WriteAllText(stem + ".svg", prepared.ToSvg()); File.WriteAllBytes(stem + ".native.png", prepared.ToPng());
        File.WriteAllText(stem + ".json", prepared.ToInterchangeEnvelope().ToJson());
    }

    private static TopologyChart BoundedSource() => MermaidC4ConfigurationDepthTests.Parse(File.ReadAllText(
        Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures", "c4-layout-config-native.mmd")))
        .ToTopologyChart(MermaidC4ConfigurationDepthTests.Options(false, 390).C4);
}

using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class V2TopologyMigrationTests {
    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void PreparedTopologyAllShapesAndMarkersExportOneDetachedSemanticGeometry(VisualThemeMode mode) {
        var chart = TopologyChart.Create(); chart.Id = "shape-atlas";
        var shapes = Enum.GetValues(typeof(TopologyNodeShape)).Cast<TopologyNodeShape>().ToArray();
        for (var i = 0; i < shapes.Length; i++) chart.Nodes.Add(new TopologyNode {
            Id = "shape-" + i, Label = shapes[i].ToString(), X = 24 + i % 5 * 150, Y = 24 + i / 5 * 130,
            Width = 120, Height = 100, Shape = shapes[i], PreserveDisplayModeSize = true, ShowStatusBadge = false
        });
        var markers = Enum.GetValues(typeof(TopologyMarkerKind)).Cast<TopologyMarkerKind>().ToArray();
        for (var i = 0; i < markers.Length; i++) chart.Edges.Add(new TopologyEdge {
            Id = "marker-" + i, SourceNodeId = "shape-" + i, TargetNodeId = "shape-" + (i + 1),
            Routing = TopologyEdgeRouting.Curved, SourceMarker = markers[i], TargetMarker = markers[i]
        });
        var prepared = chart.Prepare(Context(mode), new TopologyRenderOptions { FitContentToViewport = true, WrapNodeLabels = true });
        var artifact = prepared.ToArtifact("shape-atlas", VisualArtifactKind.Topology);
        var envelope = artifact.ToInterchangeEnvelope();
        Assert.Equal(shapes.Length, envelope.Nodes.Count);
        Assert.All(envelope.Edges, edge => Assert.True(edge.ResolvedRoute.Count >= 2));
        foreach (var region in prepared.Regions.Where(region => region.Role == "topology-node")) {
            var node = envelope.Nodes.Single(node => node.Id == region.Id);
            Assert.Equal(region.Bounds.X, node.X); Assert.Equal(region.Bounds.Y, node.Y);
            Assert.Equal(region.Bounds.Width, node.Width); Assert.Equal(region.Bounds.Height, node.Height);
        }
        Assert.Contains("topology-marker", prepared.ToSvg());
        Assert.Contains("topology-node-surface", prepared.ToSvg());
        Assert.Equal(960, prepared.ToRgba().Width);
        var svg = prepared.ToSvg(); var json = artifact.ToInterchangeJson();
        chart.Nodes.Clear(); chart.Edges.Clear();
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(json, artifact.ToInterchangeJson());
    }

    [Fact]
    public void PreparedTopologyReusesEveryLayoutAndPreservesSourceCoordinates() {
        foreach (TopologyLayoutMode mode in Enum.GetValues(typeof(TopologyLayoutMode))) {
            var chart = TopologyChart.Create(); chart.Id = "layouts"; chart.LayoutMode = mode;
            chart.Groups.Add(new TopologyGroup { Id = "lane", Label = "Region", X = 12, Y = 12, Width = 420, Height = 260, Longitude = 10, Latitude = 30 });
            chart.Nodes.Add(new TopologyNode { Id = "first", Label = "First", X = 30, Y = 80, Width = 120, Height = 64, GroupId = "lane", Longitude = 5, Latitude = 30 });
            chart.Nodes.Add(new TopologyNode { Id = "second", Label = "Second", X = 260, Y = 80, Width = 120, Height = 64, GroupId = "lane", Longitude = 15, Latitude = 35 });
            chart.Edges.Add(new TopologyEdge { Id = "relation", SourceNodeId = "first", TargetNodeId = "second", Routing = TopologyEdgeRouting.Orthogonal });
            var options = new TopologyRenderOptions { FitContentToViewport = true, ForceLayoutIterations = 20 };
            var prepared = chart.Prepare(Context(), options);
            Assert.Equal(mode, prepared.ToArtifact("layouts", VisualArtifactKind.Topology).ToInterchangeEnvelope().Topology!.LayoutMode);
            Assert.Equal(30, chart.Nodes[0].X); Assert.Equal(80, chart.Nodes[0].Y);
            Assert.Equal(prepared.ToSvg(), chart.Prepare(Context(), options).ToSvg());
        }
    }

    [Fact]
    public void PreparedWallpaperOverlayRetainsTransparentCanvasGroupsIconsDetailsAndRoutes() {
        var chart = TopologyChart.Create(); chart.Id = "wallpaper-overlay";
        chart.Groups.Add(new TopologyGroup { Id = "site", Label = "Site", X = 12, Y = 12, Width = 480, Height = 240, Status = TopologyHealthStatus.Healthy });
        var first = new TopologyNode { Id = "server", Label = "Server", Subtitle = "Primary", Kind = TopologyNodeKind.Server, GroupId = "site", X = 30, Y = 80, Width = 184, Height = 90, Status = TopologyHealthStatus.Healthy };
        first.Details.Add(new TopologyNodeDetail { Label = "State", Value = "Available", Status = TopologyHealthStatus.Healthy });
        chart.Nodes.Add(first);
        chart.Nodes.Add(new TopologyNode { Id = "endpoint", Label = "Endpoint", Kind = TopologyNodeKind.Endpoint, GroupId = "site", X = 300, Y = 80, Width = 130, Height = 64 });
        chart.Edges.Add(new TopologyEdge { Id = "path", SourceNodeId = "server", TargetNodeId = "endpoint", Routing = TopologyEdgeRouting.ObstacleAvoidingOrthogonal, Label = "Link", SecondaryLabel = "Active", SourceLabel = "1", TargetLabel = "many" });
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(560, 310)), frame: new VisualFrame(showLegend: false, transparentBackground: true));
        var prepared = chart.Prepare(context, new TopologyRenderOptions { FitContentToViewport = true, IncludeGroupStatusDots = true });
        Assert.Equal(0, prepared.ToRgba().Pixels[3]);
        var svg = XDocument.Parse(prepared.ToSvg());
        Assert.Contains(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "topology-group-surface");
        Assert.Contains(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "topology-node-icon");
        Assert.Contains("Available", prepared.ToSvg());
        Assert.Equal(2, prepared.ToArtifact("wallpaper-overlay", VisualArtifactKind.Topology).ToInterchangeEnvelope().Nodes.Count);
        var edge = Assert.Single(prepared.ToArtifact("wallpaper-overlay", VisualArtifactKind.Topology).ToInterchangeEnvelope().Edges);
        Assert.NotNull(edge.ResolvedLabelBounds); Assert.True(edge.ResolvedLabelBounds!.Value.Width > 0);
    }

    [Fact]
    public void CenterBannerUsesCommonFrameAlignment() {
        var chart = TopologyChart.Create(); chart.Id = "centered"; chart.Title = "Centered topology";
        chart.Nodes.Add(new TopologyNode { Id = "node", Label = "Node", X = 20, Y = 20, Width = 120, Height = 64 });
        var context = Context();
        var svg = XDocument.Parse(chart.Prepare(context, new TopologyRenderOptions { HeaderStyle = TopologyHeaderStyle.CenterBanner }).ToSvg());
        var heading = svg.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "frame-heading").Elements().Single();
        var measured = new VisualSceneBuilder(context.Layout.Size, context.Font).MeasureText(chart.Title!, context.Theme.Typography.TitleSize, 600);
        Assert.Equal((context.Layout.Size.Width - measured.Width) / 2, double.Parse(heading.Attribute("x")!.Value, System.Globalization.CultureInfo.InvariantCulture), 3);
    }

    [Fact]
    public void PreparedTopologyLegendRetainsNodeGlyphStatusAndDashedEdgeSwatchesInCommonFrame() {
        var chart = TopologyChart.Create(); chart.Id = "legend";
        chart.Nodes.Add(new TopologyNode { Id = "server", Label = "Server", X = 20, Y = 20, Width = 120, Height = 64 });
        chart.Legend = TopologyLegend.Create().AddStatus("Healthy", TopologyHealthStatus.Healthy)
            .AddNodeKind("Server", TopologyNodeKind.Server).AddEdgeKind("Dependency", TopologyEdgeKind.Dependency, lineStyle: TopologyEdgeLineStyle.Dashed);
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(640, 400)), frame: new VisualFrame(showLegend: true));
        var svg = chart.Prepare(context).ToSvg();
        Assert.Contains("topology-legend-status", svg); Assert.Contains("topology-legend-node", svg); Assert.Contains("topology-legend-edge", svg);
        Assert.Contains("stroke-dasharray", svg); Assert.Contains("topology-node-icon", svg);
    }

    [Fact]
    public void PreparedFlowRetainsLaneStepConnectorSemanticsAndResolvedGeometry() {
        var flow = FlowArtifact.Create("workflow").WithTitle("Workflow").AddLane("team", "Team")
            .AddStep("start", "Start", FlowArtifactStepKind.Start, "team")
            .AddStep("decision", "Approve?", FlowArtifactStepKind.Decision, "team")
            .AddStep("end", "Done", FlowArtifactStepKind.End, "team")
            .AddConnector("start", "decision", "Review")
            .AddConnector("decision", "end", "Approved");
        var prepared = flow.Prepare(Context());
        var envelope = prepared.ToArtifact("workflow", VisualArtifactKind.Flow).ToInterchangeEnvelope();
        Assert.Equal(VisualArtifactInterchangeFamily.Flow, envelope.Family);
        Assert.Equal(VisualArtifactInterchangeGroupRole.FlowLane, Assert.Single(envelope.Groups).Role);
        Assert.Equal(FlowArtifactStepKind.Decision, envelope.Nodes.Single(node => node.Id == "decision").Flow!.Kind);
        Assert.All(envelope.Nodes, node => { Assert.NotNull(node.X); Assert.NotNull(node.Y); Assert.Null(node.Topology); });
        Assert.All(envelope.Edges, edge => { Assert.NotNull(edge.Flow); Assert.Null(edge.Topology); Assert.True(edge.ResolvedRoute.Count >= 2); });
    }

    [Fact]
    public void ResolvedRouteRoundTripsSeparatelyFromAuthoredWaypointsAndRejectsIncompleteGeometry() {
        var envelope = new VisualArtifactInterchangeEnvelope { Id = "portable", Kind = VisualArtifactKind.Chart };
        envelope.Nodes.Add(new VisualArtifactInterchangeNode { Id = "a" }); envelope.Nodes.Add(new VisualArtifactInterchangeNode { Id = "b" });
        var edge = new VisualArtifactInterchangeEdge { Id = "route", SourceId = "a", TargetId = "b" };
        edge.ResolvedLabelBounds = new ChartRect(14, 16, 25, 12);
        edge.ResolvedRoute.Add(new VisualArtifactInterchangePoint { X = 12, Y = 24 }); edge.ResolvedRoute.Add(new VisualArtifactInterchangePoint { X = 50, Y = 24 });
        envelope.Edges.Add(edge);
        var copy = VisualArtifactInterchangeEnvelope.FromJson(envelope.ToJson());
        Assert.Equal(12, copy.Edges[0].ResolvedRoute[0].X); Assert.Equal(50, copy.Edges[0].ResolvedRoute[1].X);
        Assert.Equal(edge.ResolvedLabelBounds, copy.Edges[0].ResolvedLabelBounds);
        var additive = envelope.ToJson().Replace("\"resolvedRoute\":", "\"futureRoute\":");
        Assert.Empty(VisualArtifactInterchangeEnvelope.FromJson(additive).Edges[0].ResolvedRoute);
        Assert.Null(VisualArtifactInterchangeEnvelope.FromJson(envelope.ToJson().Replace("\"resolvedLabelBounds\":", "\"futureLabelBounds\":")).Edges[0].ResolvedLabelBounds);
        edge.ResolvedRoute.Clear();
        Assert.Empty(VisualArtifactInterchangeEnvelope.FromJson(envelope.ToJson()).Edges[0].ResolvedRoute);
        edge.ResolvedRoute.Add(new VisualArtifactInterchangePoint { X = 12, Y = 24 }); edge.ResolvedRoute.Add(new VisualArtifactInterchangePoint { X = 50, Y = 24 });
        edge.ResolvedRoute.RemoveAt(1);
        Assert.Throws<ArgumentException>(() => envelope.Validate());
        edge.ResolvedRoute.Add(new VisualArtifactInterchangePoint { X = double.NaN, Y = 1 });
        Assert.Throws<ArgumentOutOfRangeException>(() => envelope.Validate());
        edge.ResolvedRoute.Clear();
        Assert.Throws<ArgumentOutOfRangeException>(() => VisualArtifactInterchangeEnvelope.FromJson(envelope.ToJson().Replace("\"width\":25", "\"width\":-1")));
    }

    private static VisualRenderContext Context(VisualThemeMode mode = VisualThemeMode.Light) => new(
        new VisualLayoutOptions(new VisualSize(960, 640)), themeMode: mode, frame: new VisualFrame(showLegend: false));
}

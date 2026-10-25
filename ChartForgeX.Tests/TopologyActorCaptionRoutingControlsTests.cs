using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyActorCaptionRoutingControlsTests {
    [Theory]
    [InlineData("ordinary")]
    [InlineData("path")]
    [InlineData("waypoint")]
    [InlineData("source-side")]
    [InlineData("target-side")]
    [InlineData("source-name")]
    [InlineData("target-name")]
    [InlineData("nonactor")]
    [InlineData("hidden-caption")]
    public void OrdinaryAndAuthoredRouteOwnersRetainTheirFacts(string control) {
        var chart = TopologyChart.Create().WithId("actor-caption-control-" + control).WithViewport(800, 500, 24)
            .WithLayout(TopologyLayoutMode.Manual).WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family))
            .AddNode("source", "Participant", 60, 150, width: 140, height: 116)
            .AddNode("target", "Action", 580, 150, width: 160, height: 100)
            .AddEdge("association", "source", "target", routing: TopologyEdgeRouting.Straight);
        chart.Nodes[0].Shape = TopologyNodeShape.ActorBusiness; chart.Nodes[0].Subtitle = "«participant»";
        chart.Nodes[1].Shape = TopologyNodeShape.BusinessEllipse;
        if (control != "ordinary") {
            chart.AddNode("foreign", "Foreign participant", 300, 105, width: 160, height: 116);
            chart.Nodes[2].Shape = TopologyNodeShape.ActorHollow; chart.Nodes[2].Subtitle = "«external system»";
        }
        foreach (var node in chart.Nodes) { node.DisplayMode = TopologyNodeDisplayMode.Card; node.PreserveDisplayModeSize = true; node.ShowStatusBadge = false; }
        var edge = chart.Edges[0];
        if (control == "path") edge.PathGeometry = new TopologyPathGeometry("M 207 190 L 320 90 L 573 190");
        if (control == "waypoint") edge.Waypoints.Add(new ChartPoint(320, 90));
        if (control == "source-side") edge.SourcePort = TopologyEdgePort.Top;
        if (control == "target-side") edge.TargetPort = TopologyEdgePort.Bottom;
        if (control == "source-name") { chart.AddNodePort("source", "authored", TopologyEdgePort.Right, .3); edge.SourcePortId = "authored"; }
        if (control == "target-name") { chart.AddNodePort("target", "authored", TopologyEdgePort.Left, .7); edge.TargetPortId = "authored"; }
        if (control == "nonactor") chart.Nodes[0].Shape = TopologyNodeShape.Rectangle;
        var options = new TopologyRenderOptions { FitContentToViewport = false, IncludeLegend = false, IncludeNodeLabels = control != "hidden-caption" };
        var authoredSourcePort = edge.SourcePort; var authoredTargetPort = edge.TargetPort;
        var prepared = chart.Prepare(options); var envelope = prepared.ToInterchangeEnvelope();
        Capture(control, prepared.ToSvg(), prepared.ToPng(), envelope.ToJson());
        var captured = Assert.Single(envelope.Edges); var facts = captured.Topology!;
        Assert.Equal(edge.Routing, facts.Routing);
        Assert.Equal(control == "source-name" ? TopologyEdgePort.Right : authoredSourcePort, facts.SourcePort);
        Assert.Equal(control == "target-name" ? TopologyEdgePort.Left : authoredTargetPort, facts.TargetPort);
        Assert.Equal(authoredSourcePort, edge.SourcePort); Assert.Equal(authoredTargetPort, edge.TargetPort);
        Assert.Equal(edge.SourcePortId, captured.SourcePortId); Assert.Equal(edge.TargetPortId, captured.TargetPortId);
        Assert.Equal(edge.PathGeometry?.PathData, facts.PathGeometry?.PathData);
        Assert.Equal(edge.Waypoints.Select(point => (point.X, point.Y)), facts.Waypoints.Select(point => (point.X, point.Y)));
        if (control == "path") Assert.Equal(edge.PathGeometry!.RoutePoints().Select(point => (point.X, point.Y)), captured.ResolvedRoute.Select(point => (point.X, point.Y)));
        if (control == "waypoint") Assert.Contains(captured.ResolvedRoute, point => point.X == 320 && point.Y == 90);
        Assert.All(captured.ResolvedRoute, point => { Assert.True(double.IsFinite(point.X)); Assert.True(double.IsFinite(point.Y)); });
    }

    private static void Capture(string name, string svg, byte[] png, string json) {
        var output = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output); var prefix = Path.Combine(output, "actor-caption-control-" + name);
        File.WriteAllText(prefix + ".svg", svg); File.WriteAllBytes(prefix + ".native.png", png); File.WriteAllText(prefix + ".json", json);
    }
}

using System;
using System.Linq;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void TopologyEdgesCanAvoidGroupHeaders() {
        var chart = TopologyChart.Create()
            .WithId("group-header-route")
            .WithViewport(620, 320, 20)
            .WithLegend(null)
            .AddGroup("middle", "Bridgehead Region", 225, 95, 170, 120, TopologyHealthStatus.Warning)
            .AddNode("left", "Left Hub", 40, 80, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, width: 100, height: 60)
            .AddNode("right", "Right Hub", 420, 80, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, width: 100, height: 60)
            .AddEdge("hub-link", "left", "right", "64 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Healthy, VisualLinkDirection.Bidirectional, TopologyEdgeRouting.ObstacleAvoidingOrthogonal)
            .WithEdgePorts("hub-link", TopologyEdgePort.Right, TopologyEdgePort.Left);

        var prepared = chart.Prepare(new TopologyRenderOptions { IncludeLegend = false });
        var svg = prepared.ToSvg();
        Assert(svg.Contains("data-route-strategy=\"ObstacleAvoidingOrthogonal\"", StringComparison.Ordinal), "Topology SVG should expose the obstacle-aware route strategy for group-header routes.");
        Assert(svg.Contains("data-route-fallback-reason=\"none\"", StringComparison.Ordinal), "Topology SVG should expose clear group-header routes without a fallback reason.");
        Assert(svg.Contains("data-route-obstacle-hits=\"0\"", StringComparison.Ordinal), "Obstacle-aware topology routes should avoid group header obstacles when a clear lane is available.");
        Assert(svg.Contains("data-route-label-obstacle-hits=\"0\"", StringComparison.Ordinal), "The group-header detour should also keep its edge label clear.");
        var points = ParseSvgPathPoints(ExtractEdgePathData(svg, "hub-link"));
        var caption = prepared.Visual.Scene.Nodes.OfType<ChartForgeX.Rendering.VisualSceneText>().Single(text => text.Role == "topology-group-label");
        var left = caption.X; var top = caption.Baseline - caption.Text.Ascent;
        var right = left + caption.Text.Metrics.Width; var bottom = top + caption.Text.Metrics.Height;
        Assert(right > left && bottom > top, "The group-header fixture must contain a painted caption.");
        Assert(PathIntersectsBox(new[] { points[0], points[points.Count - 1] }, left, top, right, bottom),
            "The direct port-to-port path must cross the actual caption so the fixture requires avoidance.");
        Assert(points.Any(point => Math.Abs(point.Y - points[0].Y) > .01), "The route must detour around the painted group caption.");
        Assert(!PathIntersectsBox(points, left, top, right, bottom), "The routed path must avoid the measured painted group caption.");
        var edge = prepared.Analyze().Edges.Single(item => item.Id == "hub-link");
        Assert(edge.SourceAttached && edge.TargetAttached, "The group-header detour must remain attached to both requested node ports.");
        Assert(prepared.ToPng().Length > 64, "Topology PNG should render the same prepared group-header detour as SVG.");
    }
}

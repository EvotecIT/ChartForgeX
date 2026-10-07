using System;
using ChartForgeX.Core;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void TopologySharedPortsSpreadFanRoutes() {
        var chart = TopologyChart.Create()
            .WithId("port-fan")
            .WithViewport(360, 220, 20)
            .WithLegend(null)
            .AddNode("hub", "Inter-Forest", 88, 86, TopologyNodeKind.Cloud, TopologyHealthStatus.Healthy, width: 56, height: 56, symbol: "CL")
            .AddNode("a", "Site A", 244, 42, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, width: 44, height: 44, symbol: "DC")
            .AddNode("b", "Site B", 244, 92, TopologyNodeKind.Server, TopologyHealthStatus.Warning, width: 44, height: 44, symbol: "DC")
            .AddNode("c", "Site C", 244, 142, TopologyNodeKind.Server, TopologyHealthStatus.Critical, width: 44, height: 44, symbol: "DC")
            .AddEdge("hub-a", "hub", "a", null, TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, VisualLinkDirection.Forward, TopologyEdgeRouting.Straight)
            .AddEdge("hub-b", "hub", "b", null, TopologyEdgeKind.Replication, TopologyHealthStatus.Warning, VisualLinkDirection.Forward, TopologyEdgeRouting.Straight)
            .AddEdge("hub-c", "hub", "c", null, TopologyEdgeKind.Replication, TopologyHealthStatus.Critical, VisualLinkDirection.Forward, TopologyEdgeRouting.Straight)
            .WithEdgePorts("hub-a", TopologyEdgePort.Right, TopologyEdgePort.Left)
            .WithEdgePorts("hub-b", TopologyEdgePort.Right, TopologyEdgePort.Left)
            .WithEdgePorts("hub-c", TopologyEdgePort.Right, TopologyEdgePort.Left);

        var options = new TopologyRenderOptions { IncludeLegend = false }
            .WithMonitoringDashboardStyle();
        var svg = chart.ToSvg(options);

        var firstY = GetAttribute(svg, "data-edge-id=\"hub-a\"", "data-route-start-y");
        var secondY = GetAttribute(svg, "data-edge-id=\"hub-b\"", "data-route-start-y");
        var thirdY = GetAttribute(svg, "data-edge-id=\"hub-c\"", "data-route-start-y");
        Assert(firstY < secondY && secondY < thirdY, "Edges sharing one explicit hub port should spread along that side instead of all starting from the same midpoint.");
        Assert(chart.ToPng(options).Length > 64, "Port-spread fan routes should render as PNG.");
    }

    private static void TopologyOrthogonalReciprocalRoutesKeepPortAnchors() {
        var chart = TopologyChart.Create()
            .WithId("reciprocal-orthogonal")
            .WithViewport(360, 260, 20)
            .WithLegend(null)
            .AddNode("top", "Top", 150, 56, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, width: 56, height: 44, symbol: "DC")
            .AddNode("bottom", "Bottom", 150, 176, TopologyNodeKind.Server, TopologyHealthStatus.Warning, width: 56, height: 44, symbol: "DC")
            .AddEdge("top-bottom", "top", "bottom", "105 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, VisualLinkDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .AddEdge("bottom-top", "bottom", "top", "112 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Warning, VisualLinkDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .WithEdgePorts("top-bottom", TopologyEdgePort.Bottom, TopologyEdgePort.Top)
            .WithEdgePorts("bottom-top", TopologyEdgePort.Top, TopologyEdgePort.Bottom)
            .WithEdgeRouteLane("top-bottom", -18)
            .WithEdgeRouteLane("bottom-top", 18);

        var options = new TopologyRenderOptions { IncludeLegend = false }
            .WithMonitoringDashboardStyle();
        var svg = chart.ToSvg(options);

        var downStartY = GetAttribute(svg, "data-edge-id=\"top-bottom\"", "data-route-start-y");
        var upStartY = GetAttribute(svg, "data-edge-id=\"bottom-top\"", "data-route-start-y");
        Assert(Math.Abs(downStartY - (56 + 44 + 7)) < 0.01, "Orthogonal reciprocal routes should keep source endpoints anchored to explicit ports.");
        Assert(Math.Abs(upStartY - (176 - 7)) < 0.01, "Orthogonal reciprocal routes should not receive a whole-route parallel offset that pulls them off their ports.");
        Assert(chart.ToPng(options).Length > 64, "Port-anchored reciprocal orthogonal routes should render as PNG.");
    }

    private static void TopologyEdgeRouteBundlesAssignCenteredLanes() {
        var chart = TopologyChart.Create()
            .WithId("route-bundle")
            .WithViewport(380, 260, 20)
            .WithLegend(null)
            .AddNode("a", "A", 80, 66, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, width: 44, height: 44, symbol: "DC")
            .AddNode("b", "B", 256, 66, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, width: 44, height: 44, symbol: "DC")
            .AddNode("c", "C", 168, 176, TopologyNodeKind.Server, TopologyHealthStatus.Warning, width: 44, height: 44, symbol: "DC")
            .AddEdge("a-c", "a", "c", "105 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, VisualLinkDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .AddEdge("b-c", "b", "c", "112 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Warning, VisualLinkDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .AddEdge("c-a", "c", "a", "107 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, VisualLinkDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .WithEdgePorts("a-c", TopologyEdgePort.Bottom, TopologyEdgePort.Top)
            .WithEdgePorts("b-c", TopologyEdgePort.Bottom, TopologyEdgePort.Top)
            .WithEdgePorts("c-a", TopologyEdgePort.Top, TopologyEdgePort.Bottom)
            .WithEdgeRouteBundle(0, 18, "a-c", "b-c", "c-a");

        var svg = chart.ToSvg(new TopologyRenderOptions { IncludeLegend = false }.WithMonitoringDashboardStyle());

        Assert(svg.Contains("data-edge-id=\"a-c\"", StringComparison.Ordinal), "Bundled route lanes should preserve the first edge.");
        Assert(svg.Contains("data-route-lane=\"-18\"", StringComparison.Ordinal), "Centered route bundles should assign the first lane below center.");
        Assert(svg.Contains("data-route-lane=\"0\"", StringComparison.Ordinal), "Centered route bundles should assign the middle lane at the center.");
        Assert(svg.Contains("data-route-lane=\"18\"", StringComparison.Ordinal), "Centered route bundles should assign the last lane above center.");
        Assert(chart.ToPng(new TopologyRenderOptions { IncludeLegend = false }.WithMonitoringDashboardStyle()).Length > 64, "Bundled route lanes should render as PNG.");
    }

    private static void TopologyReciprocalEdgeRouteBundlesInferCenteredLanes() {
        var chart = TopologyChart.Create()
            .WithId("reciprocal-route-bundle")
            .WithViewport(340, 220, 20)
            .WithLegend(null)
            .AddNode("a", "A", 82, 86, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, width: 44, height: 44, symbol: "DC")
            .AddNode("b", "B", 228, 86, TopologyNodeKind.Server, TopologyHealthStatus.Warning, width: 44, height: 44, symbol: "DC")
            .AddEdge("a-b", "a", "b", "105 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, VisualLinkDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .AddEdge("b-a", "b", "a", "107 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Warning, VisualLinkDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .WithEdgePorts("a-b", TopologyEdgePort.Right, TopologyEdgePort.Left)
            .WithEdgePorts("b-a", TopologyEdgePort.Left, TopologyEdgePort.Right)
            .WithReciprocalEdgeRouteBundles(20);

        var svg = chart.ToSvg(new TopologyRenderOptions { IncludeLegend = false }.WithMonitoringDashboardStyle());

        Assert(svg.Contains("data-route-lane=\"-10\"", StringComparison.Ordinal), "Reciprocal route bundling should assign the first unconfigured edge below center.");
        Assert(svg.Contains("data-route-lane=\"10\"", StringComparison.Ordinal), "Reciprocal route bundling should assign the second unconfigured edge above center.");
        Assert(chart.ToPng(new TopologyRenderOptions { IncludeLegend = false }.WithMonitoringDashboardStyle()).Length > 64, "Inferred reciprocal bundles should render as PNG.");
    }
}

using System;
using ChartForgeX.Core;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void TopologySharedPortsSpreadFanRoutes() {
        var chart = TopologyChart.Create()
            .Id("port-fan")
            .Viewport(new TopologyViewport(360, 220, 20))
            .Legend((TopologyLegend?)null)
            .AddNode("hub", "Inter-Forest", 88, 86, TopologyNodeKind.Cloud, TopologyHealthStatus.Healthy, width: 56, height: 56, symbol: "CL")
            .AddNode("a", "Site A", 244, 42, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, width: 44, height: 44, symbol: "DC")
            .AddNode("b", "Site B", 244, 92, TopologyNodeKind.Server, TopologyHealthStatus.Warning, width: 44, height: 44, symbol: "DC")
            .AddNode("c", "Site C", 244, 142, TopologyNodeKind.Server, TopologyHealthStatus.Critical, width: 44, height: 44, symbol: "DC")
            .AddEdge("hub-a", "hub", "a", null, TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, VisualLinkDirection.Forward, TopologyEdgeRouting.Straight)
            .AddEdge("hub-b", "hub", "b", null, TopologyEdgeKind.Replication, TopologyHealthStatus.Warning, VisualLinkDirection.Forward, TopologyEdgeRouting.Straight)
            .AddEdge("hub-c", "hub", "c", null, TopologyEdgeKind.Replication, TopologyHealthStatus.Critical, VisualLinkDirection.Forward, TopologyEdgeRouting.Straight)
            .UpdateEdge("hub-a", e => e.SourcePort(TopologyEdgePort.Right).TargetPort(TopologyEdgePort.Left))
            .UpdateEdge("hub-b", e => e.SourcePort(TopologyEdgePort.Right).TargetPort(TopologyEdgePort.Left))
            .UpdateEdge("hub-c", e => e.SourcePort(TopologyEdgePort.Right).TargetPort(TopologyEdgePort.Left));

        var options = new TopologyRenderOptions { IncludeLegend = false }
            .MonitoringDashboardStyle();
        var svg = chart.ToSvg(options);

        var firstY = GetAttribute(svg, "data-edge-id=\"hub-a\"", "data-route-start-y");
        var secondY = GetAttribute(svg, "data-edge-id=\"hub-b\"", "data-route-start-y");
        var thirdY = GetAttribute(svg, "data-edge-id=\"hub-c\"", "data-route-start-y");
        Assert(firstY < secondY && secondY < thirdY, "Edges sharing one explicit hub port should spread along that side instead of all starting from the same midpoint.");
        Assert(chart.ToPng(options).Length > 64, "Port-spread fan routes should render as PNG.");
    }

    private static void TopologyOrthogonalReciprocalRoutesKeepPortAnchors() {
        var chart = TopologyChart.Create()
            .Id("reciprocal-orthogonal")
            .Viewport(new TopologyViewport(360, 260, 20))
            .Legend((TopologyLegend?)null)
            .AddNode("top", "Top", 150, 56, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, width: 56, height: 44, symbol: "DC")
            .AddNode("bottom", "Bottom", 150, 176, TopologyNodeKind.Server, TopologyHealthStatus.Warning, width: 56, height: 44, symbol: "DC")
            .AddEdge("top-bottom", "top", "bottom", "105 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, VisualLinkDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .AddEdge("bottom-top", "bottom", "top", "112 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Warning, VisualLinkDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .UpdateEdge("top-bottom", e => e.SourcePort(TopologyEdgePort.Bottom).TargetPort(TopologyEdgePort.Top))
            .UpdateEdge("bottom-top", e => e.SourcePort(TopologyEdgePort.Top).TargetPort(TopologyEdgePort.Bottom))
            .EdgeRouteLane("top-bottom", -18)
            .EdgeRouteLane("bottom-top", 18);

        var options = new TopologyRenderOptions { IncludeLegend = false }
            .MonitoringDashboardStyle();
        var svg = chart.ToSvg(options);

        var downStartY = GetAttribute(svg, "data-edge-id=\"top-bottom\"", "data-route-start-y");
        var upStartY = GetAttribute(svg, "data-edge-id=\"bottom-top\"", "data-route-start-y");
        Assert(Math.Abs(downStartY - (56 + 44)) < 0.01, "Orthogonal reciprocal routes should keep source endpoints anchored to explicit ports.");
        Assert(Math.Abs(upStartY - 176) < 0.01, "Orthogonal reciprocal routes should not receive a whole-route parallel offset that pulls them off their ports.");
        Assert(chart.ToPng(options).Length > 64, "Port-anchored reciprocal orthogonal routes should render as PNG.");
    }

    private static void TopologyEdgeRouteBundlesAssignCenteredLanes() {
        var chart = TopologyChart.Create()
            .Id("route-bundle")
            .Viewport(new TopologyViewport(380, 260, 20))
            .Legend((TopologyLegend?)null)
            .AddNode("a", "A", 80, 66, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, width: 44, height: 44, symbol: "DC")
            .AddNode("b", "B", 256, 66, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, width: 44, height: 44, symbol: "DC")
            .AddNode("c", "C", 168, 176, TopologyNodeKind.Server, TopologyHealthStatus.Warning, width: 44, height: 44, symbol: "DC")
            .AddEdge("a-c", "a", "c", "105 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, VisualLinkDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .AddEdge("b-c", "b", "c", "112 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Warning, VisualLinkDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .AddEdge("c-a", "c", "a", "107 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, VisualLinkDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .UpdateEdge("a-c", e => e.SourcePort(TopologyEdgePort.Bottom).TargetPort(TopologyEdgePort.Top))
            .UpdateEdge("b-c", e => e.SourcePort(TopologyEdgePort.Bottom).TargetPort(TopologyEdgePort.Top))
            .UpdateEdge("c-a", e => e.SourcePort(TopologyEdgePort.Top).TargetPort(TopologyEdgePort.Bottom))
            .EdgeRouteBundle(0, 18, "a-c", "b-c", "c-a");

        var svg = chart.ToSvg(new TopologyRenderOptions { IncludeLegend = false }.MonitoringDashboardStyle());

        Assert(svg.Contains("data-edge-id=\"a-c\"", StringComparison.Ordinal), "Bundled route lanes should preserve the first edge.");
        Assert(svg.Contains("data-route-lane=\"-18\"", StringComparison.Ordinal), "Centered route bundles should assign the first lane below center.");
        Assert(svg.Contains("data-route-lane=\"0\"", StringComparison.Ordinal), "Centered route bundles should assign the middle lane at the center.");
        Assert(svg.Contains("data-route-lane=\"18\"", StringComparison.Ordinal), "Centered route bundles should assign the last lane above center.");
        Assert(chart.ToPng(new TopologyRenderOptions { IncludeLegend = false }.MonitoringDashboardStyle()).Length > 64, "Bundled route lanes should render as PNG.");
    }

    private static void TopologyReciprocalEdgeRouteBundlesInferCenteredLanes() {
        var chart = TopologyChart.Create()
            .Id("reciprocal-route-bundle")
            .Viewport(new TopologyViewport(340, 220, 20))
            .Legend((TopologyLegend?)null)
            .AddNode("a", "A", 82, 86, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, width: 44, height: 44, symbol: "DC")
            .AddNode("b", "B", 228, 86, TopologyNodeKind.Server, TopologyHealthStatus.Warning, width: 44, height: 44, symbol: "DC")
            .AddEdge("a-b", "a", "b", "105 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, VisualLinkDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .AddEdge("b-a", "b", "a", "107 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Warning, VisualLinkDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .UpdateEdge("a-b", e => e.SourcePort(TopologyEdgePort.Right).TargetPort(TopologyEdgePort.Left))
            .UpdateEdge("b-a", e => e.SourcePort(TopologyEdgePort.Left).TargetPort(TopologyEdgePort.Right))
            .ReciprocalEdgeRouteBundles(20);

        var svg = chart.ToSvg(new TopologyRenderOptions { IncludeLegend = false }.MonitoringDashboardStyle());

        Assert(svg.Contains("data-route-lane=\"-10\"", StringComparison.Ordinal), "Reciprocal route bundling should assign the first unconfigured edge below center.");
        Assert(svg.Contains("data-route-lane=\"10\"", StringComparison.Ordinal), "Reciprocal route bundling should assign the second unconfigured edge above center.");
        Assert(chart.ToPng(new TopologyRenderOptions { IncludeLegend = false }.MonitoringDashboardStyle()).Length > 64, "Inferred reciprocal bundles should render as PNG.");
    }
}

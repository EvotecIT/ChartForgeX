using System;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void TopologyInferredLegendsUseSharedEdgeKindStyling() {
        var chart = TopologyChart.Create()
            .WithId("styled-relationship-map")
            .WithTitle("Styled Relationship Map")
            .WithViewport(560, 340, 24)
            .AddNode("app", "Application", 80, 110, TopologyNodeKind.Service, TopologyHealthStatus.Healthy, symbol: "APP")
            .AddNode("db", "Database", 300, 90, TopologyNodeKind.Database, TopologyHealthStatus.Healthy, symbol: "SQL")
            .AddNode("queue", "Queue", 300, 180, TopologyNodeKind.Service, TopologyHealthStatus.Warning, symbol: "Q")
            .AddEdge("app-db", "app", "db", "reads", TopologyEdgeKind.Dependency, TopologyHealthStatus.Healthy, TopologyDirection.Forward)
            .AddEdge("app-queue", "app", "queue", "publishes", TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning, TopologyDirection.Forward)
            .WithEdgesOfKind(TopologyEdgeKind.Dependency, lineStyle: TopologyEdgeLineStyle.Dotted, color: "#64748B");

        var svg = chart.ToSvg(new TopologyRenderOptions { LegendMode = TopologyLegendMode.Auto });
        var legendStart = svg.IndexOf("data-cfx-role=\"topology-legend\"", StringComparison.Ordinal);
        Assert(legendStart >= 0, "Topology auto legend should render for styled relationship maps.");
        var legend = svg.Substring(legendStart);
        Assert(legend.Contains(">Dependency<", StringComparison.Ordinal), "Topology auto legend should include the styled edge kind.");
        Assert(legend.Contains("stroke=\"#64748B\"", StringComparison.Ordinal), "Topology auto legend should reuse a shared edge-kind color.");
        Assert(legend.Contains("stroke-dasharray=\"2 5\"", StringComparison.Ordinal), "Topology auto legend should reuse a shared edge-kind line style.");
        Assert(chart.ToPng(new TopologyRenderOptions { LegendMode = TopologyLegendMode.Auto }).Length > 64, "Styled inferred topology legends should render as PNG.");
    }
}

using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

internal static class TopologyRoutingFixtures {
    internal static TopologyRenderOptions Options() => new() {
        ReadableDenseLayout = true, IncludeLegend = false, IncludeEdgeLabels = false, IncludeTitle = false
    };

    internal static TopologyChart CenteredHeader() => TopologyChart.Create().WithId("centered-header").WithViewport(1000, 400).WithLegend(null)
        .AddGroup("group", "Middle", 200, 40, 600, 220)
        .AddNode("a", "Source", 350, 40, width: 60, height: 40)
        .AddNode("b", "Target", 650, 40, width: 60, height: 40)
        .AddEdge("planned", "a", "b", routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal);

    internal static TopologyChart Mixed(TopologyEdgeRouting routing) => TopologyChart.Create().WithId("mixed-routes").WithViewport(900, 440).WithLegend(null)
        .AddNode("a", "Source", 100, 120, width: 60, height: 44)
        .AddNode("b", "Target", 700, 120, width: 60, height: 44)
        .AddNode("c", "Fixed source", 100, 300, width: 60, height: 44)
        .AddNode("d", "Fixed target", 700, 300, width: 60, height: 44)
        .AddEdge("planned", "a", "b", status: TopologyHealthStatus.Healthy, routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal)
        .AddEdge("fixed", "c", "d", status: TopologyHealthStatus.Warning, routing: routing);

    internal static TopologyChart FanIn(bool named) {
        var chart = TopologyChart.Create().WithId("shared-trunks").WithViewport(900, 460).WithLegend(null)
            .AddNode("target", "Service", 700, 180, width: 100, height: 44);
        if (named) chart.AddNodePort("target", "bus", TopologyEdgePort.Left, 0.35);
        for (var i = 0; i < 3; i++) {
            chart.AddNode("n" + i, "Input " + i, 100, 60 + i * 120, width: 100, height: 44)
                .AddEdge("e" + i, "n" + i, "target", kind: TopologyEdgeKind.Dependency, status: TopologyHealthStatus.Healthy,
                    direction: VisualLinkDirection.Forward, routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal)
                .WithEdgePorts("e" + i, TopologyEdgePort.Right, TopologyEdgePort.Left);
            if (named) chart.WithEdgeNamedPorts("e" + i, null, "bus");
        }
        return chart;
    }
}

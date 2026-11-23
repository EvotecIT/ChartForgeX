using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

internal static class TopologyRoutingFixtures {
    internal static TopologyRenderOptions Options() => new() {
        ReadableDenseLayout = true, IncludeLegend = false, IncludeEdgeLabels = false, IncludeTitle = false
    };

    internal static TopologyChart PaintedHeader() => TopologyChart.Create().Id("painted-header").Viewport(new TopologyViewport(1000, 400)).Legend((TopologyLegend?)null)
        .AddGroup("group", "Middle", 430, 40, 240, 220)
        .AddNode("a", "Source", 350, 40, width: 60, height: 40)
        .AddNode("b", "Target", 650, 40, width: 60, height: 40)
        .AddEdge("planned", "a", "b", routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal);

    internal static TopologyChart Mixed(TopologyEdgeRouting routing) => TopologyChart.Create().Id("mixed-routes").Viewport(new TopologyViewport(900, 440)).Legend((TopologyLegend?)null)
        .AddNode("a", "Source", 100, 120, width: 60, height: 44)
        .AddNode("b", "Target", 700, 120, width: 60, height: 44)
        .AddNode("c", "Fixed source", 100, 300, width: 60, height: 44)
        .AddNode("d", "Fixed target", 700, 300, width: 60, height: 44)
        .AddEdge("planned", "a", "b", status: TopologyHealthStatus.Healthy, routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal)
        .AddEdge("fixed", "c", "d", status: TopologyHealthStatus.Warning, routing: routing);

    internal static TopologyChart FanIn(bool named) {
        var chart = TopologyChart.Create().Id("shared-trunks").Viewport(new TopologyViewport(900, 460)).Legend((TopologyLegend?)null)
            .AddNode("target", "Service", 700, 180, width: 100, height: 44);
        if (named) chart.AddNodePort("target", "bus", TopologyEdgePort.Left, 0.35);
        for (var i = 0; i < 3; i++) {
            chart.AddNode("n" + i, "Input " + i, 100, 60 + i * 120, width: 100, height: 44)
                .AddEdge("e" + i, "n" + i, "target", kind: TopologyEdgeKind.Dependency, status: TopologyHealthStatus.Healthy,
                    direction: VisualLinkDirection.Forward, routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal)
                .UpdateEdge("e" + i, e => e.SourcePort(TopologyEdgePort.Right).TargetPort(TopologyEdgePort.Left));
            if (named) chart.UpdateEdge("e" + i, e => e.SourcePortId(null).TargetPortId("bus"));
        }
        return chart;
    }
}

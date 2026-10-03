using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;

internal static partial class TopologyVisualExamples {
    private static void WriteRoutingExamples(string target, List<VisualArtifact> artifacts) {
        var sharedTrunks = new TopologyRenderOptions {
            ReadableDenseLayout = true, ShareIncomingTrunks = true, IncludeLegend = false, IncludeEdgeLabels = false
        }.WithMonitoringDashboardStyle();
        SaveTopology(target, artifacts, "visual-topology-shared-trunks", BuildSharedIncomingTrunks(), "Shared Incoming Trunks",
            "Matching solid relationships join an opt-in trunk while every input retains a complete semantic route.", sharedTrunks);
        SaveTopology(target, artifacts, "visual-topology-mixed-routing", BuildMixedRouteOccupancy(), "Mixed Route Occupancy",
            "Obstacle-avoiding routing accounts for a fixed waypoint corridor without changing its authored geometry.",
            new TopologyRenderOptions { ReadableDenseLayout = true, IncludeLegend = false, IncludeEdgeLabels = false }.WithMonitoringDashboardStyle());
        SaveTopology(target, artifacts, "visual-topology-fallback-attachments", BuildFallbackAttachments(), "Fallback Route Attachments",
            "A corridor route leaves each card on its port axis and turns clear of a wide tile caption.",
            new TopologyRenderOptions { ReadableDenseLayout = true, NodeDisplayMode = TopologyNodeDisplayMode.Tile, IncludeLegend = false });
    }

    private static TopologyChart BuildFallbackAttachments() => TopologyChart.Create().WithId("fallback-route-attachments")
        .WithTitle("Fallback Route Attachments").WithViewport(600, 440, 24).WithLegend(null)
        .AddNode("source", "Source caption extends far beyond its narrow card", 80, 100, width: 60, height: 40)
        .AddNode("target", "Target", 145, 160, width: 60, height: 40)
        .AddEdge("relationship", "source", "target", direction: VisualLinkDirection.Forward,
            routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal)
        .WithEdgePorts("relationship", TopologyEdgePort.Right, TopologyEdgePort.Left);

    private static TopologyChart BuildSharedIncomingTrunks() {
        var chart = TopologyChart.Create().WithId("shared-incoming-trunks")
            .WithTitle("Shared Incoming Trunks")
            .WithSubtitle("Three solid relationships join one tail; each input keeps its own identity and source marker.")
            .WithViewport(1000, 500).WithLegend(null)
            .AddNode("service", "Service", 740, 240, width: 130, height: 52);
        for (var i = 0; i < 3; i++) {
            chart.AddNode("input-" + i, "Input " + (i + 1), 110, 130 + i * 110, width: 130, height: 52)
                .AddEdge("input-route-" + i, "input-" + i, "service", kind: TopologyEdgeKind.Dependency,
                    status: TopologyHealthStatus.Healthy, direction: VisualLinkDirection.Forward,
                    routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal)
                .WithEdgePorts("input-route-" + i, TopologyEdgePort.Right, TopologyEdgePort.Left);
        }
        return chart;
    }

    private static TopologyChart BuildMixedRouteOccupancy() => TopologyChart.Create().WithId("mixed-route-occupancy")
        .WithTitle("Mixed Route Occupancy")
        .WithSubtitle("The planned relationship avoids the occupied corridor while the authored waypoint route stays intact.")
        .WithViewport(1000, 530).WithLegend(null)
        .AddNode("source", "Planned source", 110, 200, width: 140, height: 52)
        .AddNode("target", "Planned target", 740, 200, width: 140, height: 52)
        .AddNode("fixed-source", "Fixed source", 110, 390, width: 140, height: 52)
        .AddNode("fixed-target", "Fixed target", 740, 390, width: 140, height: 52)
        .AddEdge("planned", "source", "target", status: TopologyHealthStatus.Healthy,
            direction: VisualLinkDirection.Forward, routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal)
        .AddEdge("authored", "fixed-source", "fixed-target", status: TopologyHealthStatus.Warning,
            direction: VisualLinkDirection.Forward, routing: TopologyEdgeRouting.Orthogonal)
        .WithEdgeWaypoints("authored", new ChartPoint(400, 226), new ChartPoint(590, 226));
}

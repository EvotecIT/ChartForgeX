using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;

internal static partial class TopologyVisualExamples {
    private static void WriteRoutingExamples(string target, List<VisualArtifact> artifacts) {
        var sharedTrunks = new TopologyRenderOptions {
            ReadableDenseLayout = true, ShareIncomingTrunks = true, IncludeLegend = false, IncludeEdgeLabels = false
        }.MonitoringDashboardStyle();
        SaveTopology(target, artifacts, "visual-topology-shared-trunks", BuildSharedIncomingTrunks(), "Shared Incoming Trunks",
            "Matching solid relationships join an opt-in trunk while every input retains a complete semantic route.", sharedTrunks);
        SaveTopology(target, artifacts, "visual-topology-mixed-routing", BuildMixedRouteOccupancy(), "Mixed Route Occupancy",
            "Obstacle-avoiding routing accounts for a fixed waypoint corridor without changing its authored geometry.",
            new TopologyRenderOptions { ReadableDenseLayout = true, IncludeLegend = false, IncludeEdgeLabels = false }.MonitoringDashboardStyle());
        SaveTopology(target, artifacts, "visual-topology-fallback-attachments", BuildFallbackAttachments(), "Fallback Route Attachments",
            "A corridor route leaves each card on its port axis and turns clear of a wide tile caption.",
            new TopologyRenderOptions { ReadableDenseLayout = true, NodeDisplayMode = TopologyNodeDisplayMode.Tile, IncludeLegend = false });
    }

    private static TopologyChart BuildFallbackAttachments() => TopologyChart.Create().Id("fallback-route-attachments")
        .Title("Fallback Route Attachments").Viewport(new TopologyViewport(600, 440, 24)).Legend((TopologyLegend?)null)
        .AddNode("source", "Source caption extends far beyond its narrow card", 80, 100, width: 60, height: 40)
        .AddNode("target", "Target", 145, 160, width: 60, height: 40)
        .AddEdge("relationship", "source", "target", direction: VisualLinkDirection.Forward,
            routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal)
        .UpdateEdge("relationship", e => e.SourcePort(TopologyEdgePort.Right).TargetPort(TopologyEdgePort.Left));

    private static TopologyChart BuildSharedIncomingTrunks() {
        var chart = TopologyChart.Create().Id("shared-incoming-trunks")
            .Title("Shared Incoming Trunks")
            .Subtitle("Three solid relationships join one tail; each input keeps its own identity and source marker.")
            .Viewport(new TopologyViewport(1000, 500)).Legend((TopologyLegend?)null)
            .AddNode("service", "Service", 740, 240, width: 130, height: 52);
        for (var i = 0; i < 3; i++) {
            chart.AddNode("input-" + i, "Input " + (i + 1), 110, 130 + i * 110, width: 130, height: 52)
                .AddEdge("input-route-" + i, "input-" + i, "service", kind: TopologyEdgeKind.Dependency,
                    status: TopologyHealthStatus.Healthy, direction: VisualLinkDirection.Forward,
                    routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal)
                .UpdateEdge("input-route-" + i, e => e.SourcePort(TopologyEdgePort.Right).TargetPort(TopologyEdgePort.Left));
        }
        return chart;
    }

    private static TopologyChart BuildMixedRouteOccupancy() => TopologyChart.Create().Id("mixed-route-occupancy")
        .Title("Mixed Route Occupancy")
        .Subtitle("The planned relationship avoids the occupied corridor while the authored waypoint route stays intact.")
        .Viewport(new TopologyViewport(1000, 530)).Legend((TopologyLegend?)null)
        .AddNode("source", "Planned source", 110, 200, width: 140, height: 52)
        .AddNode("target", "Planned target", 740, 200, width: 140, height: 52)
        .AddNode("fixed-source", "Fixed source", 110, 390, width: 140, height: 52)
        .AddNode("fixed-target", "Fixed target", 740, 390, width: 140, height: 52)
        .AddEdge("planned", "source", "target", status: TopologyHealthStatus.Healthy,
            direction: VisualLinkDirection.Forward, routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal)
        .AddEdge("authored", "fixed-source", "fixed-target", status: TopologyHealthStatus.Warning,
            direction: VisualLinkDirection.Forward, routing: TopologyEdgeRouting.Orthogonal)
        .EdgeWaypoints("authored", new ChartPoint(400, 226), new ChartPoint(590, 226));
}

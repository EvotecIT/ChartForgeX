using System;
using System.Linq;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void TopologyPortSpreadingPreservesOrthogonalEndpointLegs() {
        var chart = TopologyChart.Create()
            .WithId("orthogonal-port-fan")
            .WithViewport(420, 300, 20)
            .WithLegend(null)
            .AddNode("hub", "Hub", 180, 52, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, width: 60, height: 44, symbol: "H")
            .AddNode("left", "Left", 92, 196, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, width: 54, height: 40, symbol: "DC")
            .AddNode("middle", "Middle", 183, 196, TopologyNodeKind.Server, TopologyHealthStatus.Warning, width: 54, height: 40, symbol: "DC")
            .AddNode("right", "Right", 274, 196, TopologyNodeKind.Server, TopologyHealthStatus.Critical, width: 54, height: 40, symbol: "DC")
            .AddEdge("hub-left", "hub", "left", "105 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .AddEdge("hub-middle", "hub", "middle", "112 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Warning, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .AddEdge("hub-right", "hub", "right", "142 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Critical, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .WithEdgePorts("hub-left", TopologyEdgePort.Bottom, TopologyEdgePort.Top)
            .WithEdgePorts("hub-middle", TopologyEdgePort.Bottom, TopologyEdgePort.Top)
            .WithEdgePorts("hub-right", TopologyEdgePort.Bottom, TopologyEdgePort.Top);

        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        foreach (var edge in chart.Edges) {
            var points = TopologyRenderPrimitives.EdgePoints(chart, edge, nodes);
            Assert(Math.Abs(points[0].X - points[1].X) < 0.01, "Port spreading should keep the source endpoint leg vertical for orthogonal routes.");
            Assert(Math.Abs(points[points.Count - 1].X - points[points.Count - 2].X) < 0.01, "Port spreading should keep the target endpoint leg vertical for orthogonal routes.");
        }

        Assert(chart.ToPng(new TopologyRenderOptions { IncludeLegend = false }.WithMonitoringDashboardStyle()).Length > 64, "Orthogonal port-spread fan routes should render as PNG.");
    }
}

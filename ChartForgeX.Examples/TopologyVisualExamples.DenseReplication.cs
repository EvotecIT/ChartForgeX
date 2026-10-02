using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Topology;

internal static partial class TopologyVisualExamples {
    private static TopologyChart BuildReadableDenseReplication() {
        var chart = TopologyChart.Create().WithId("visual-readable-dense-replication")
            .WithTitle("Regional replication")
            .WithSubtitle("Site panels wrap within the viewport; routes share corridors on separate lanes and carry their labels.")
            .WithViewport(1400, 760, 24)
            .WithLayout(TopologyLayoutMode.DenseGrouped, TopologyLayoutDirection.LeftToRight)
            .WithLegend(TopologyLegend.Default());

        for (var site = 0; site < 8; site++) {
            var groupId = "site-" + (site + 1).ToString("00", CultureInfo.InvariantCulture);
            var count = site == 0 ? 12 : 3 + site % 3;
            chart.AddAutoGroup(groupId, "Site " + (site + 1).ToString(CultureInfo.InvariantCulture),
                TopologyHealthStatus.Healthy, count.ToString(CultureInfo.InvariantCulture) + " servers");
            for (var node = 0; node < count; node++) {
                var nodeId = groupId + "-server-" + (node + 1).ToString("00", CultureInfo.InvariantCulture);
                chart.AddAutoNode(nodeId, "S" + (site + 1).ToString("00", CultureInfo.InvariantCulture) + "-N" + (node + 1).ToString("00", CultureInfo.InvariantCulture),
                    TopologyNodeKind.Server, node == count - 1 && site == 4 ? TopologyHealthStatus.Warning : TopologyHealthStatus.Healthy,
                    groupId, width: 88, height: 40);
                if (node > 0) chart.AddEdge(groupId + "-local-" + node.ToString("00", CultureInfo.InvariantCulture),
                    groupId + "-server-" + node.ToString("00", CultureInfo.InvariantCulture), nodeId,
                    routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal);
            }
        }

        // Links between sites compete for the same corridors: every site reports to the first, and neighbours pair up.
        for (var site = 1; site < 8; site++) {
            var groupId = "site-" + (site + 1).ToString("00", CultureInfo.InvariantCulture);
            chart.AddEdge(groupId + "-uplink", groupId + "-server-01", "site-01-server-" + (1 + site % 4).ToString("00", CultureInfo.InvariantCulture),
                "cost " + (100 + site * 10).ToString(CultureInfo.InvariantCulture), TopologyEdgeKind.Link,
                site == 5 ? TopologyHealthStatus.Warning : TopologyHealthStatus.Healthy, VisualLinkDirection.Bidirectional, TopologyEdgeRouting.ObstacleAvoidingOrthogonal);
            if (site % 2 == 1 && site + 1 < 8) {
                chart.AddEdge(groupId + "-peer", groupId + "-server-02", "site-" + (site + 2).ToString("00", CultureInfo.InvariantCulture) + "-server-02",
                    null, TopologyEdgeKind.Replication, site == 3 ? TopologyHealthStatus.Critical : TopologyHealthStatus.Healthy,
                    VisualLinkDirection.Forward, TopologyEdgeRouting.ObstacleAvoidingOrthogonal);
            }
        }

        return chart;
    }
}

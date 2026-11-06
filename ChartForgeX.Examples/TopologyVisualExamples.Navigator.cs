using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using ChartForgeX.Topology;

internal static partial class TopologyVisualExamples {
    private static readonly (string Name, TopologyNodeKind Kind)[] EstateRoles = {
        ("Gateway", TopologyNodeKind.Gateway), ("API", TopologyNodeKind.Service), ("Worker", TopologyNodeKind.Process),
        ("Queue", TopologyNodeKind.Queue), ("Database", TopologyNodeKind.Database), ("Cache", TopologyNodeKind.Storage),
        ("Host", TopologyNodeKind.Server)
    };

    // Interactive HTML only: a dense estate opens at a readable scale with find, group and kind filters.
    private static void WriteDenseEstateNavigator(string target) {
        var options = new TopologyRenderOptions { ReadableDenseLayout = true, NodeDisplayMode = TopologyNodeDisplayMode.Tile }
            .WithHtmlFindControls();
        options.EnableHtmlSelectionPanel = true;
        BuildDenseServiceEstate().SaveInteractiveHtml(Path.Combine(target, "visual-dense-service-estate-navigator.html"), options);
        BuildDenseServiceEstate().WithTheme(VisualTheme.Graphite(), VisualThemeMode.Dark)
            .SaveInteractiveHtml(Path.Combine(target, "visual-dense-service-estate-navigator-dark.html"), options);
    }

    private static TopologyChart BuildDenseServiceEstate() {
        var chart = TopologyChart.Create().WithId("visual-dense-service-estate")
            .WithTitle("Service estate")
            .WithSubtitle("Twelve sites and their services. Find a service, or narrow by site or kind.")
            .WithViewport(1600, 900, 24)
            .WithLayout(TopologyLayoutMode.DenseGrouped, TopologyLayoutDirection.LeftToRight)
            .WithLegend(TopologyLegend.Default());
        for (var site = 0; site < 12; site++) {
            var groupId = "site-" + (site + 1).ToString("00", CultureInfo.InvariantCulture);
            var count = 6 + site % 5;
            chart.AddAutoGroup(groupId, "Site " + (site + 1).ToString(CultureInfo.InvariantCulture),
                site == 7 ? TopologyHealthStatus.Warning : TopologyHealthStatus.Healthy, count.ToString(CultureInfo.InvariantCulture) + " services");
            for (var node = 0; node < count; node++) {
                var role = EstateRoles[node % EstateRoles.Length];
                var nodeId = groupId + "-" + node.ToString("00", CultureInfo.InvariantCulture);
                var status = site == 7 && node == 4 ? TopologyHealthStatus.Critical : site == 3 && node == 2 ? TopologyHealthStatus.Warning : TopologyHealthStatus.Healthy;
                chart.AddAutoNode(nodeId, role.Name + " " + (site + 1).ToString("00", CultureInfo.InvariantCulture) + "-" + (node + 1).ToString(CultureInfo.InvariantCulture),
                    role.Kind, status, groupId, width: 96, height: 40);
                if (node > 0) chart.AddEdge(groupId + "-call-" + node.ToString("00", CultureInfo.InvariantCulture), groupId + "-00", nodeId,
                    routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal);
            }

            if (site > 0) chart.AddEdge(groupId + "-uplink", groupId + "-00", "site-01-00", null, TopologyEdgeKind.Link,
                TopologyHealthStatus.Healthy, VisualLinkDirection.Bidirectional, TopologyEdgeRouting.ObstacleAvoidingOrthogonal);
        }

        return chart;
    }
}

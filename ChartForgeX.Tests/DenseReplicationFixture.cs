using System.Globalization;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

/// <summary>
/// A site-level replication topology as a reporting host draws it at large scale: 60 captioned site tiles in
/// 3 regional groups and 131 obstacle-avoiding links. The shape is a capture of the largest scale
/// fixture of a directory-monitoring report, so dense-routing and rendering changes are measured on real density.
/// </summary>
internal static class DenseReplicationFixture {
    // Region index, status (H, W, C or U), controller count, then the site name.
    private static readonly string[] Sites = {
        "0H3:AMER-Atlanta", "0H3:AMER-Bogota", "0H1:AMER-Boston", "0H2:AMER-BuenosAires", "0H1:AMER-Chicago", "0H5:AMER-Dallas",
        "0H2:AMER-Denver", "0H2:AMER-Lima", "0H3:AMER-LosAngeles", "0H1:AMER-MexicoCity", "0H4:AMER-Miami", "0H2:AMER-Montreal",
        "0H5:AMER-NewYork", "0H2:AMER-Phoenix", "0H1:AMER-SanFrancisco", "0H4:AMER-Santiago", "0H5:AMER-SaoPaulo", "0H3:AMER-Seattle",
        "0H3:AMER-Toronto", "0H3:AMER-Vancouver", "1H1:APAC-Auckland", "1H1:APAC-Bangalore", "1H3:APAC-Bangkok", "1H4:APAC-Beijing",
        "1H1:APAC-HongKong", "1H2:APAC-Jakarta", "1H2:APAC-KualaLumpur", "1H2:APAC-Manila", "1H4:APAC-Melbourne", "1H1:APAC-Mumbai",
        "1H6:APAC-Osaka", "1H2:APAC-Seoul", "1H3:APAC-Shanghai", "1H3:APAC-Singapore", "1H4:APAC-Sydney", "1H1:APAC-Tokyo",
        "2H2:EMEA-Amsterdam", "2H3:EMEA-Athens", "2H2:EMEA-Brussels", "2C5:EMEA-Cairo", "2H1:EMEA-Copenhagen", "2H2:EMEA-Dubai",
        "2H3:EMEA-Dublin", "2H5:EMEA-Frankfurt", "2H2:EMEA-Helsinki", "2H2:EMEA-Istanbul", "2H2:EMEA-Johannesburg", "2C3:EMEA-Lisbon",
        "2H1:EMEA-London", "2H1:EMEA-Madrid", "2H2:EMEA-Milan", "2C3:EMEA-Oslo", "2H2:EMEA-Paris", "2H4:EMEA-Prague", "2H2:EMEA-Riyadh",
        "2H2:EMEA-Stockholm", "2H1:EMEA-TelAviv", "2H2:EMEA-Vienna", "2H1:EMEA-Warsaw", "2H2:EMEA-Zurich"
    };

    // Source and target site indexes, status, then the link count with an optional failing count.
    private static readonly string[] Links = {
        "0-5H2", "0-7H2", "0-8H2", "0-9H2", "0-28H2", "0-39H2", "1-8H2", "1-10H4", "1-12H2", "1-14H2", "1-18H2", "2-16H2", "2-17H2",
        "3-7H2", "3-10H2", "3-11H2", "3-19H2", "4-6H2", "4-7H2", "5-12H2", "5-15H6", "5-16H2", "5-19H2", "5-26H2", "5-27H2", "5-28H2",
        "6-10H2", "6-15H2", "6-18H2", "7-13H2", "8-10H2", "8-12H2", "8-13H2", "8-18H2", "9-12H2", "10-30H2", "11-14H2", "11-50H2",
        "11-51H2", "12-15H2", "12-17H2", "12-23H2", "12-27H2", "12-41H2", "12-51H2", "13-16H2", "13-17H2", "15-17H2", "15-19H2", "15-39H2",
        "16-17H4", "16-18H4", "16-19H2", "18-19H2", "19-39H2", "20-29H2", "20-30H2", "21-24H2", "21-35H2", "22-25H2", "22-26H2", "22-32H4",
        "22-33H4", "23-28H2", "23-30H4", "23-32H2", "23-33H2", "23-34H2", "23-35H2", "24-31H2", "25-28H2", "25-30H2", "25-33H2", "26-27H2",
        "26-41H2", "27-30H2", "28-33H4", "28-43H2", "28-50H2", "29-34H2", "30-31H2", "30-32H4", "30-34H2", "30-42H2", "30-53H2", "31-34H4",
        "32-34H2", "36-39H2", "36-43H4", "36-55H2", "37-43H2", "37-44H2", "37-45H2", "37-54H2", "37-57H2", "37-58H2", "38-43H2", "38-44H2",
        "38-52H4", "39-42H2", "39-43H4", "39-47C2/2", "39-51C2/2", "39-54H2", "40-50H2", "40-54H2", "41-45H2", "41-49H2", "42-47H2",
        "42-52H2", "42-53H2", "42-58H2", "43-45H2", "43-48H2", "43-59H2", "44-50H2", "44-55H2", "45-53H2", "46-48H2", "46-51H4", "46-55H2",
        "47-51H2", "47-53H4", "47-57H2", "49-59H2", "52-59H2", "53-54H2", "53-56H2", "53-57H2", "55-59H2", "56-57H2"
    };

    private static readonly string[] Regions = { "AMER", "APAC", "EMEA" };

    /// <summary>The render options the host draws the capture with.</summary>
    public static TopologyRenderOptions Options() => new() {
        ReadableDenseLayout = true,
        NodeDisplayMode = TopologyNodeDisplayMode.Tile,
        IncludeTileSubtitles = true,
        IncludeEdgeLabels = false
    };

    /// <summary>Creates the captured site topology.</summary>
    public static TopologyChart Sites60() {
        var chart = TopologyChart.Create()
            .WithId("dense-replication-sites")
            .WithTitle("Replication topology")
            .WithSubtitle("Sites: 60, domain controllers: 150; a line stands for all replication between two sites")
            .WithViewport(1180, 1110, 24)
            .WithLayout(TopologyLayoutMode.DenseGrouped, TopologyLayoutDirection.LeftToRight)
            .WithLegend(TopologyLegend.Create("Legend")
                .AddStatus("Healthy", TopologyHealthStatus.Healthy)
                .AddStatus("Delayed", TopologyHealthStatus.Warning)
                .AddStatus("Failing", TopologyHealthStatus.Critical)
                .AddStatus("Not observed", TopologyHealthStatus.Unknown)
                .AddNodeKind("Site", TopologyNodeKind.Location)
                .AddEdgeKind("Replication", TopologyEdgeKind.Replication));
        for (var region = 0; region < Regions.Length; region++) {
            var count = 0;
            foreach (var site in Sites) if (site[0] - '0' == region) count++;
            chart.AddAutoGroup("region-" + region.ToString(CultureInfo.InvariantCulture), Regions[region], subtitle: count.ToString(CultureInfo.InvariantCulture));
        }

        for (var i = 0; i < Sites.Length; i++) {
            var site = Sites[i];
            var colon = site.IndexOf(':');
            chart.AddAutoNode(SiteId(i), site.Substring(colon + 1), TopologyNodeKind.Location, Status(site[1]), "region-" + site[0],
                "DCs: " + site.Substring(2, colon - 2), width: 132, height: 48);
        }

        for (var i = 0; i < Links.Length; i++) {
            var link = Links[i];
            var dash = link.IndexOf('-');
            var statusAt = dash + 1;
            while (char.IsDigit(link[statusAt])) statusAt++;
            var counts = link.Substring(statusAt + 1).Split('/');
            var label = "Links: " + counts[0] + (counts.Length > 1 ? ", failing: " + counts[1] : "");
            chart.AddEdge("link-" + i.ToString(CultureInfo.InvariantCulture), SiteId(int.Parse(link.Substring(0, dash), CultureInfo.InvariantCulture)),
                SiteId(int.Parse(link.Substring(dash + 1, statusAt - dash - 1), CultureInfo.InvariantCulture)), label, TopologyEdgeKind.Replication,
                Status(link[statusAt]), VisualLinkDirection.None, TopologyEdgeRouting.ObstacleAvoidingOrthogonal, tooltip: label);
        }

        return chart;
    }

    private static string SiteId(int index) => "site-" + index.ToString(CultureInfo.InvariantCulture);

    private static TopologyHealthStatus Status(char code) => code switch {
        'H' => TopologyHealthStatus.Healthy,
        'W' => TopologyHealthStatus.Warning,
        'C' => TopologyHealthStatus.Critical,
        _ => TopologyHealthStatus.Unknown
    };
}

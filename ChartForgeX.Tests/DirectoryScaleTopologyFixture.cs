using System.Globalization;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

/// <summary>
/// Readable dense topologies shaped like a directory report at its largest scale tier: privileged-group membership
/// across five domains, a 60-site replication overview and a 200-controller detail view. The report draws each one
/// at a 1,360 px content width and shows the SVG at that width or narrower.
/// </summary>
internal static class DirectoryScaleTopologyFixture {
    internal const double ReportWidth = 1360;
    private static readonly string[] RootGroups = { "Domain Admins", "Group Policy Creator Owners", "Administrators", "Account Operators" };

    /// <summary>Five domain panels, each with four privileged groups, one nested group each and three users per nested group.</summary>
    public static TopologyChart CriticalGroups(int domains = 5) {
        var nodes = domains * 20;
        var chart = Create("critical-groups", "Critical group membership", Height(260, nodes + domains * 2, 420, 1400));
        chart.WithRenderOptions(new TopologyRenderOptions {
            ReadableDenseLayout = true, NodeDisplayMode = TopologyNodeDisplayMode.Tile, IncludeTileSubtitles = true,
            IncludeEdgeLabels = false, IncludeStatusBadges = false
        });
        var edge = 0;
        for (var d = 0; d < domains; d++) {
            var domain = "domain-" + d.ToString(CultureInfo.InvariantCulture);
            chart.AddAutoGroup(domain, "ad" + d.ToString(CultureInfo.InvariantCulture) + ".contoso.example");
            for (var r = 0; r < RootGroups.Length; r++) {
                var root = domain + "-root-" + r.ToString(CultureInfo.InvariantCulture);
                var nested = root + "-nested";
                chart.AddAutoNode(root, RootGroups[r], TopologyNodeKind.Group, TopologyHealthStatus.Unknown, domain, "Security group", width: 120, height: 40, symbol: "G");
                chart.AddAutoNode(nested, "Tier " + r.ToString(CultureInfo.InvariantCulture) + " operators", TopologyNodeKind.Group, TopologyHealthStatus.Unknown, domain, "Security group", width: 120, height: 40, symbol: "G");
                chart.AddEdge("membership-" + (edge++).ToString(CultureInfo.InvariantCulture), root, nested, null, TopologyEdgeKind.Mapping, TopologyHealthStatus.Unknown,
                    VisualLinkDirection.Forward, TopologyEdgeRouting.ObstacleAvoidingOrthogonal);
                for (var u = 0; u < 3; u++) {
                    var user = nested + "-user-" + u.ToString(CultureInfo.InvariantCulture);
                    chart.AddAutoNode(user, "adm.user" + (r * 3 + u).ToString(CultureInfo.InvariantCulture), TopologyNodeKind.Person, TopologyHealthStatus.Unknown, domain, "User", width: 120, height: 40);
                    chart.AddEdge("membership-" + (edge++).ToString(CultureInfo.InvariantCulture), nested, user, null, TopologyEdgeKind.Mapping, TopologyHealthStatus.Unknown,
                        VisualLinkDirection.Forward, TopologyEdgeRouting.ObstacleAvoidingOrthogonal);
                }
            }
        }
        return chart;
    }

    /// <summary>60 sites in three regions with 170 aggregated site links; a handful are delayed or failing.</summary>
    public static TopologyChart SiteOverview(int sites = 60, int links = 170) {
        var regions = new[] { "AMER", "APAC", "EMEA" };
        var chart = Create("site-overview", "Replication topology", Height(300, sites + regions.Length * 2, 520, 1800));
        chart.WithRenderOptions(new TopologyRenderOptions {
            ReadableDenseLayout = true, NodeDisplayMode = TopologyNodeDisplayMode.Tile, IncludeTileSubtitles = true, IncludeEdgeLabels = false
        });
        chart.WithLegend(TopologyLegend.Create("Legend")
            .AddStatus("Healthy", TopologyHealthStatus.Healthy).AddStatus("Delayed", TopologyHealthStatus.Warning)
            .AddStatus("Failing", TopologyHealthStatus.Critical).AddStatus("Not observed", TopologyHealthStatus.Unknown)
            .AddNodeKind("Site", TopologyNodeKind.Location).AddEdgeKind("Replication", TopologyEdgeKind.Replication));
        for (var r = 0; r < regions.Length; r++)
            chart.AddAutoGroup("region-" + r.ToString(CultureInfo.InvariantCulture), regions[r], subtitle: (sites / regions.Length).ToString(CultureInfo.InvariantCulture));
        for (var s = 0; s < sites; s++) {
            var status = s % 23 == 7 ? TopologyHealthStatus.Critical : s % 17 == 3 ? TopologyHealthStatus.Warning : TopologyHealthStatus.Healthy;
            chart.AddAutoNode(Site(s), regions[s * regions.Length / sites] + "-Site" + s.ToString("00", CultureInfo.InvariantCulture), TopologyNodeKind.Location, status,
                "region-" + (s * regions.Length / sites).ToString(CultureInfo.InvariantCulture), (2 + s % 4).ToString(CultureInfo.InvariantCulture) + " domain controllers", width: 132, height: 48);
        }
        var random = new Random(60170);
        var seen = new HashSet<(int, int)>();
        // A ring per region keeps every site connected; the rest are random pairs, mostly within a region.
        for (var s = 0; s < sites && seen.Count < links; s++) {
            var next = s % (sites / regions.Length) == sites / regions.Length - 1 ? s - (sites / regions.Length - 1) : s + 1;
            Link(Math.Min(s, next), Math.Max(s, next));
        }
        while (seen.Count < links) {
            var a = random.Next(sites);
            var b = random.Next(10) < 7 ? a / (sites / regions.Length) * (sites / regions.Length) + random.Next(sites / regions.Length) : random.Next(sites);
            if (a != b) Link(Math.Min(a, b), Math.Max(a, b));
        }
        return chart;

        void Link(int a, int b) {
            if (!seen.Add((a, b))) return;
            var index = seen.Count - 1;
            var status = index % 29 == 11 ? TopologyHealthStatus.Critical : index % 19 == 5 ? TopologyHealthStatus.Warning : index % 37 == 2 ? TopologyHealthStatus.Unknown : TopologyHealthStatus.Healthy;
            var label = "Links: " + (1 + index % 4).ToString(CultureInfo.InvariantCulture);
            chart.AddEdge("link-" + index.ToString(CultureInfo.InvariantCulture), Site(a), Site(b), label, TopologyEdgeKind.Replication, status,
                VisualLinkDirection.None, TopologyEdgeRouting.ObstacleAvoidingOrthogonal, tooltip: label);
        }
    }

    /// <summary>200 domain controllers in 60 site panels; each domain's controllers form a replication ring.</summary>
    public static TopologyChart ControllerDetail(int controllers = 200, int sites = 60) {
        // Two forest-wide domains and one per region; regional domains place their extra controllers in their own region.
        var domains = new[] { ("CORP", -1), ("EMEA", 2), ("AMER", 0), ("APAC", 1), ("LAB", -1) };
        var regions = new[] { "AMER", "APAC", "EMEA" };
        var chart = Create("controller-detail", "Replication topology", Height(300, controllers + sites * 2, 520, 1800));
        chart.WithRenderOptions(new TopologyRenderOptions {
            ReadableDenseLayout = true, NodeDisplayMode = TopologyNodeDisplayMode.Tile, IncludeEdgeLabels = false
        });
        var random = new Random(200);
        var domainOf = Enumerable.Range(0, controllers).Select(c => c % domains.Length).ToArray();
        // Every site gets one controller first; the rest land on random sites, so a few sites hold many controllers.
        var siteOf = Enumerable.Range(0, controllers).Select(c => {
            if (c < sites) return c;
            var region = domains[domainOf[c]].Item2;
            return region < 0 ? random.Next(sites) : region * (sites / regions.Length) + random.Next(sites / regions.Length);
        }).ToArray();
        for (var s = 0; s < sites; s++) {
            var count = siteOf.Count(site => site == s);
            chart.AddAutoGroup(Site(s), regions[s * regions.Length / sites] + "-Site" + s.ToString("00", CultureInfo.InvariantCulture), TopologyHealthStatus.Healthy,
                count.ToString(CultureInfo.InvariantCulture) + " domain controllers");
        }
        var serial = new int[domains.Length];
        for (var c = 0; c < controllers; c++) {
            var status = c % 41 == 9 ? TopologyHealthStatus.Critical : c % 31 == 4 ? TopologyHealthStatus.Warning : TopologyHealthStatus.Healthy;
            var domain = domains[domainOf[c]].Item1;
            chart.AddAutoNode(Controller(c), domain + "-DC" + (++serial[domainOf[c]]).ToString("000", CultureInfo.InvariantCulture), TopologyNodeKind.Server, status, Site(siteOf[c]),
                domain.ToLowerInvariant() + ".contoso.example", width: 108, height: 40, symbol: "DC");
        }
        var edge = 0;
        for (var d = 0; d < domains.Length; d++) {
            var members = Enumerable.Range(0, controllers).Where(c => domainOf[c] == d).ToArray();
            for (var i = 0; i < members.Length; i++) {
                var a = members[i]; var b = members[(i + 1) % members.Length];
                var status = edge % 23 == 4 ? TopologyHealthStatus.Critical : edge % 17 == 9 ? TopologyHealthStatus.Warning : TopologyHealthStatus.Healthy;
                chart.AddEdge("replication-" + (edge++).ToString(CultureInfo.InvariantCulture), Controller(a), Controller(b), null, TopologyEdgeKind.Replication, status,
                    VisualLinkDirection.Bidirectional, TopologyEdgeRouting.ObstacleAvoidingOrthogonal);
            }
        }
        return chart;
    }

    private static TopologyChart Create(string id, string title, double height) => TopologyChart.Create().WithId("directory-scale-" + id).WithTitle(title)
        .WithViewport(ReportWidth, height, 24).WithLayout(TopologyLayoutMode.DenseGrouped, TopologyLayoutDirection.LeftToRight);

    private static double Height(double baseHeight, int items, double minimum, double maximum) =>
        Math.Max(minimum, Math.Min(maximum, baseHeight + Math.Ceiling(Math.Sqrt(items)) * 90));

    private static string Site(int index) => "site-" + index.ToString(CultureInfo.InvariantCulture);
    private static string Controller(int index) => "dc-" + index.ToString(CultureInfo.InvariantCulture);
}

using System.Globalization;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

/// <summary>
/// Builds deterministic Active Directory-style replication topologies: regional hub sites with many domain controllers,
/// branch sites with a few, intra-site replication rings, hub-and-spoke site links, and an inter-hub site-link ring.
/// </summary>
internal static class ReplicationTopologyFixture {
    private static readonly string[] Regions = { "AMER", "EMEA", "APAC", "LATAM" };

    /// <summary>Creates a fixture with the given branch sites per region and domain controllers per hub site.</summary>
    public static TopologyChart Create(int branchSitesPerRegion, int hubDomainControllers, double width = 1600, double height = 1000) {
        var chart = TopologyChart.Create()
            .WithId("replication-" + branchSitesPerRegion.ToString(CultureInfo.InvariantCulture) + "-" + hubDomainControllers.ToString(CultureInfo.InvariantCulture))
            .WithTitle("Replication topology")
            .WithViewport(width, height, 24)
            .WithLegend(null)
            .WithLayout(TopologyLayoutMode.DenseGrouped, TopologyLayoutDirection.LeftToRight);
        for (var regionIndex = 0; regionIndex < Regions.Length; regionIndex++) {
            var region = Regions[regionIndex];
            var hubSite = SiteId(region, 0);
            AddSite(chart, hubSite, region + " Hub", hubDomainControllers, regionIndex);
            for (var branch = 1; branch <= branchSitesPerRegion; branch++) {
                var site = SiteId(region, branch);
                var controllers = 2 + (branch + regionIndex) % 3;
                AddSite(chart, site, region + " Branch " + branch.ToString(CultureInfo.InvariantCulture), controllers, regionIndex);
                var status = (branch * 7 + regionIndex) % 11 == 0 ? TopologyHealthStatus.Warning : TopologyHealthStatus.Healthy;
                chart.AddEdge(site + "-link", DcId(site, 0), DcId(hubSite, 0), "cost 100", TopologyEdgeKind.Link, status, VisualLinkDirection.Bidirectional, TopologyEdgeRouting.ObstacleAvoidingOrthogonal);
                chart.AddEdge(site + "-inbound", DcId(hubSite, 1), DcId(site, 0), null, TopologyEdgeKind.Replication, status, VisualLinkDirection.Forward, TopologyEdgeRouting.ObstacleAvoidingOrthogonal);
            }
        }

        for (var regionIndex = 0; regionIndex < Regions.Length; regionIndex++) {
            var from = SiteId(Regions[regionIndex], 0);
            var to = SiteId(Regions[(regionIndex + 1) % Regions.Length], 0);
            chart.AddEdge(from + "-core-link", DcId(from, 0), DcId(to, 0), "cost 20", TopologyEdgeKind.Link, TopologyHealthStatus.Healthy, VisualLinkDirection.Bidirectional, TopologyEdgeRouting.ObstacleAvoidingOrthogonal);
        }

        return chart;
    }

    /// <summary>Counts the domain controllers a fixture creates.</summary>
    public static int DomainControllerCount(int branchSitesPerRegion, int hubDomainControllers) {
        var total = 0;
        for (var regionIndex = 0; regionIndex < Regions.Length; regionIndex++) {
            total += hubDomainControllers;
            for (var branch = 1; branch <= branchSitesPerRegion; branch++) total += 2 + (branch + regionIndex) % 3;
        }

        return total;
    }

    /// <summary>
    /// Counts (edge, foreign node) pairs where the edge's route passes through the card of a node other than its own
    /// endpoints (1 px inset) — the "routes cross node cards" measure tracked in TODO.md. Each pair counts once, however
    /// many route segments or bends fall inside the card, and zero-length segments are ignored.
    /// </summary>
    public static int NodeCardCrossings(TopologyLayoutDiagnosticReport report) {
        var crossings = 0;
        foreach (var edge in report.Edges) {
            foreach (var node in report.Nodes) {
                if (node.Id == edge.SourceNodeId || node.Id == edge.TargetNodeId) continue;
                var bounds = node.Bounds;
                for (var i = 0; i + 1 < edge.Points.Count; i++) {
                    if (!SegmentCrossesRect(edge.Points[i], edge.Points[i + 1], bounds.Left + 1, bounds.Top + 1, bounds.Right - 1, bounds.Bottom - 1)) continue;
                    crossings++;
                    break;
                }
            }
        }

        return crossings;
    }

    private static bool SegmentCrossesRect(ChartPoint a, ChartPoint b, double left, double top, double right, double bottom) {
        if (right <= left || bottom <= top) return false;
        // Liang-Barsky clipping: the segment crosses the open rectangle when a non-empty parameter interval remains.
        double t0 = 0, t1 = 1;
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        if (Math.Abs(dx) < 1e-9 && Math.Abs(dy) < 1e-9) return false;
        bool Clip(double p, double q) {
            if (Math.Abs(p) < 1e-12) return q > 0;
            var r = q / p;
            if (p < 0) {
                if (r > t1) return false;
                if (r > t0) t0 = r;
            } else {
                if (r < t0) return false;
                if (r < t1) t1 = r;
            }

            return true;
        }

        return Clip(-dx, a.X - left) && Clip(dx, right - a.X) && Clip(-dy, a.Y - top) && Clip(dy, bottom - a.Y) && t1 - t0 > 1e-9;
    }

    private static void AddSite(TopologyChart chart, string site, string label, int controllers, int regionIndex) {
        chart.AddAutoGroup(site, label, TopologyHealthStatus.Healthy, controllers.ToString(CultureInfo.InvariantCulture) + " DCs", symbol: "site");
        for (var i = 0; i < controllers; i++) {
            var status = (i * 5 + regionIndex + controllers) % 17 == 0 ? TopologyHealthStatus.Critical : TopologyHealthStatus.Healthy;
            chart.AddAutoNode(DcId(site, i), site.ToUpperInvariant() + "-DC" + (i + 1).ToString("00", CultureInfo.InvariantCulture), TopologyNodeKind.Server, status, site, width: 88, height: 40, symbol: "DC");
        }

        for (var i = 0; controllers > 1 && i < controllers; i++) {
            var next = (i + 1) % controllers;
            if (controllers == 2 && i == 1) break;
            chart.AddEdge(DcId(site, i) + "-repl", DcId(site, i), DcId(site, next), null, TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, VisualLinkDirection.Bidirectional, TopologyEdgeRouting.ObstacleAvoidingOrthogonal);
        }
    }

    private static string SiteId(string region, int index) => region.ToLowerInvariant() + "-" + (index == 0 ? "hub" : "b" + index.ToString("00", CultureInfo.InvariantCulture));

    private static string DcId(string site, int index) => site + "-dc" + (index + 1).ToString("00", CultureInfo.InvariantCulture);
}

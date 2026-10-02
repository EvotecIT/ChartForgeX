using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

/// <summary>
/// Deterministic dense topologies shaped like the charts a reporting host draws: a few groups with links between every
/// pair, a wrapped mesh of many small groups, an overview of large groups of captioned tiles, and a two-panel drill-down.
/// Every edge is obstacle-avoiding so the fixtures exercise the dense router rather than authored routes.
/// </summary>
internal static class DenseRouteFixture {
    /// <summary>The render options the fixtures are meant to be drawn with.</summary>
    public static TopologyRenderOptions Options(bool subtitles = false, bool legend = true) => new() {
        ReadableDenseLayout = true,
        NodeDisplayMode = TopologyNodeDisplayMode.Tile,
        IncludeTileSubtitles = subtitles,
        IncludeLegend = legend
    };

    /// <summary>Three groups and five nodes with links between the groups, which all compete for one corridor.</summary>
    public static TopologyChart Small() {
        var chart = Create("dense-small", "Small linked groups", 1180, 520);
        AddGroup(chart, 0, 2, TopologyHealthStatus.Critical);
        AddGroup(chart, 1, 1, TopologyHealthStatus.Healthy);
        AddGroup(chart, 2, 2, TopologyHealthStatus.Unknown);
        Link(chart, 0, NodeId(1, 0), NodeId(0, 0), TopologyHealthStatus.Healthy);
        Link(chart, 1, NodeId(1, 0), NodeId(2, 1), TopologyHealthStatus.Healthy);
        Link(chart, 2, NodeId(0, 0), NodeId(2, 0), TopologyHealthStatus.Healthy);
        Link(chart, 3, NodeId(0, 1), NodeId(2, 0), TopologyHealthStatus.Critical);
        Link(chart, 4, NodeId(0, 1), NodeId(2, 1), TopologyHealthStatus.Critical);
        return chart;
    }

    /// <summary>Many small groups wrapped into rows, with links between nodes that are often several rows apart.</summary>
    public static TopologyChart Mesh(int groups = 30, int nodes = 40, int links = 80) {
        var chart = Create("dense-mesh", "Wrapped mesh", 1180, 1100);
        var random = new Lcg(20260930);
        var sizes = new int[groups];
        for (var i = 0; i < groups; i++) sizes[i] = 1;
        for (var extra = nodes - groups; extra > 0; extra--) sizes[groups - 1 - random.Next(Math.Max(1, groups / 3))]++;
        var ids = new List<string>();
        for (var group = 0; group < groups; group++) {
            AddGroup(chart, group, sizes[group], Status(random));
            for (var node = 0; node < sizes[group]; node++) ids.Add(NodeId(group, node));
        }

        var pairs = new HashSet<string>(StringComparer.Ordinal);
        for (var attempt = 0; pairs.Count < links && attempt < links * 40; attempt++) {
            var first = random.Next(ids.Count);
            var second = random.Next(ids.Count);
            if (first == second) continue;
            var key = Math.Min(first, second).ToString(CultureInfo.InvariantCulture) + ":" + Math.Max(first, second).ToString(CultureInfo.InvariantCulture);
            if (!pairs.Add(key)) continue;
            Link(chart, pairs.Count - 1, ids[first], ids[second], Status(random));
        }

        return chart;
    }

    /// <summary>A few large groups of captioned tiles with one link per related pair, as in an overview of many sites.</summary>
    public static TopologyChart Overview(int groups = 3, int nodesPerGroup = 20, int links = 90) {
        var chart = Create("dense-overview", "Overview", 1710, 1000);
        var random = new Lcg(7919);
        var ids = new List<string>();
        for (var group = 0; group < groups; group++) {
            var id = GroupId(group);
            chart.AddAutoGroup(id, "Region " + (group + 1).ToString(CultureInfo.InvariantCulture), subtitle: nodesPerGroup.ToString(CultureInfo.InvariantCulture));
            for (var node = 0; node < nodesPerGroup; node++) {
                var nodeId = NodeId(group, node);
                ids.Add(nodeId);
                chart.AddAutoNode(nodeId, "Location " + (group + 1).ToString(CultureInfo.InvariantCulture) + "-" + (node + 1).ToString("00", CultureInfo.InvariantCulture),
                    TopologyNodeKind.Location, Status(random), id, "Items: " + (1 + random.Next(5)).ToString(CultureInfo.InvariantCulture), width: 132, height: 48);
            }
        }

        var pairs = new HashSet<string>(StringComparer.Ordinal);
        for (var attempt = 0; pairs.Count < links && attempt < links * 40; attempt++) {
            var first = random.Next(ids.Count);
            var second = random.Next(ids.Count);
            if (first == second) continue;
            var key = Math.Min(first, second).ToString(CultureInfo.InvariantCulture) + ":" + Math.Max(first, second).ToString(CultureInfo.InvariantCulture);
            if (!pairs.Add(key)) continue;
            chart.AddEdge("link-" + (pairs.Count - 1).ToString(CultureInfo.InvariantCulture), ids[first], ids[second], null, TopologyEdgeKind.Replication,
                Status(random), VisualLinkDirection.None, TopologyEdgeRouting.ObstacleAvoidingOrthogonal);
        }

        return chart;
    }

    /// <summary>One group of servers beside one group of summary tiles, with links from each server to several tiles.</summary>
    public static TopologyChart Drill(int servers = 6, int neighbours = 10) {
        var chart = Create("dense-drill", "Drill-down", 1180, 620);
        var random = new Lcg(104729);
        AddGroup(chart, 0, servers, TopologyHealthStatus.Healthy);
        chart.AddAutoGroup("other", "Other groups", subtitle: neighbours.ToString(CultureInfo.InvariantCulture));
        for (var node = 0; node < neighbours; node++) {
            chart.AddAutoNode("other-" + node.ToString(CultureInfo.InvariantCulture), "Neighbour " + (node + 1).ToString("00", CultureInfo.InvariantCulture),
                TopologyNodeKind.Location, Status(random), "other", width: 132, height: 48);
        }

        var edge = 0;
        for (var node = 1; node < servers; node++) Link(chart, edge++, NodeId(0, node - 1), NodeId(0, node), TopologyHealthStatus.Healthy);
        for (var node = 0; node < neighbours; node++) {
            chart.AddEdge("link-" + (edge++).ToString(CultureInfo.InvariantCulture), NodeId(0, node % servers), "other-" + node.ToString(CultureInfo.InvariantCulture),
                null, TopologyEdgeKind.Replication, Status(random), VisualLinkDirection.None, TopologyEdgeRouting.ObstacleAvoidingOrthogonal);
        }

        return chart;
    }

    private static TopologyChart Create(string id, string title, double width, double height) => TopologyChart.Create()
        .WithId(id)
        .WithTitle(title)
        .WithSubtitle("Generic dense routing fixture")
        .WithViewport(width, height, 24)
        .WithLayout(TopologyLayoutMode.DenseGrouped, TopologyLayoutDirection.LeftToRight)
        .WithLegend(TopologyLegend.Create("Legend")
            .AddStatus("Healthy", TopologyHealthStatus.Healthy)
            .AddStatus("Warning", TopologyHealthStatus.Warning)
            .AddStatus("Critical", TopologyHealthStatus.Critical)
            .AddStatus("Unknown", TopologyHealthStatus.Unknown)
            .AddNodeKind("Server", TopologyNodeKind.Server, symbol: "SV")
            .AddEdgeKind("Replication", TopologyEdgeKind.Replication));

    private static void AddGroup(TopologyChart chart, int group, int nodes, TopologyHealthStatus status) {
        var id = GroupId(group);
        chart.AddAutoGroup(id, "Group " + (group + 1).ToString("00", CultureInfo.InvariantCulture), status, "Servers: " + nodes.ToString(CultureInfo.InvariantCulture));
        for (var node = 0; node < nodes; node++) {
            chart.AddAutoNode(NodeId(group, node), "SRV-" + (group + 1).ToString("00", CultureInfo.InvariantCulture) + "-" + (node + 1).ToString("00", CultureInfo.InvariantCulture),
                TopologyNodeKind.Server, node == 0 ? status : TopologyHealthStatus.Healthy, id, width: 108, height: 40, symbol: "SV");
        }
    }

    private static void Link(TopologyChart chart, int index, string source, string target, TopologyHealthStatus status) =>
        chart.AddEdge("link-" + index.ToString(CultureInfo.InvariantCulture), source, target, null, TopologyEdgeKind.Replication, status,
            index % 2 == 0 ? VisualLinkDirection.Bidirectional : VisualLinkDirection.Forward, TopologyEdgeRouting.ObstacleAvoidingOrthogonal);

    private static TopologyHealthStatus Status(Lcg random) => random.Next(10) switch {
        0 => TopologyHealthStatus.Critical,
        1 or 2 => TopologyHealthStatus.Warning,
        3 => TopologyHealthStatus.Unknown,
        _ => TopologyHealthStatus.Healthy
    };

    private static string GroupId(int group) => "group-" + group.ToString(CultureInfo.InvariantCulture);

    private static string NodeId(int group, int node) => GroupId(group) + "-node-" + node.ToString(CultureInfo.InvariantCulture);

    /// <summary>A fixed linear congruential generator, so the fixtures never depend on the runtime's random source.</summary>
    private sealed class Lcg {
        private uint _state;

        public Lcg(uint seed) => _state = seed;

        public int Next(int exclusiveMaximum) {
            _state = unchecked(_state * 1664525u + 1013904223u);
            return (int)((_state >> 8) % (uint)exclusiveMaximum);
        }
    }
}

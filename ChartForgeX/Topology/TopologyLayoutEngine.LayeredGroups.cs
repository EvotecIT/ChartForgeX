using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyLayoutEngine {
    /// <summary>Reserves a complete automatic edge badge between ranks before label placement.</summary>
    private static double LayeredRankGap(TopologyChart chart, TopologyRenderOptions? options, bool horizontal) {
        if (options?.LayeredRankSpacing is { } authored) return authored;
        if (options?.IncludeEdgeLabels == false) return 42;
        var gap = 42d;
        var labelOptions = options ?? new TopologyRenderOptions();
        foreach (var edge in chart.Edges) {
            var primary = EdgeLabel(edge, labelOptions.EdgeLabelMetricKey, edge.Label);
            var secondary = EdgeLabel(edge, labelOptions.EdgeSecondaryLabelMetricKey, edge.SecondaryLabel);
            var tertiary = EdgeLabel(edge, labelOptions.EdgeTertiaryLabelMetricKey, edge.TertiaryLabel);
            var lines = new[] { primary, secondary, tertiary }.Count(text => !string.IsNullOrWhiteSpace(text));
            if (lines == 0) continue;
            var extent = horizontal ? EdgeLabelTextWidth(primary, secondary, tertiary, chart.TextMeasurement, labelOptions.ResolvedEdgeLabelScale)
                : EdgeLabelHeight(lines, labelOptions);
            gap = Math.Max(gap, extent + EdgeEndpointGap * 2 + 16);
        }
        return gap;
    }

    /// <summary>Bounds automatic group shells around their layered members and separates overlapping shells.</summary>
    private static void PlaceAutomaticLayeredGroups(TopologyChart chart, TopologyChart authored, TopologyRenderOptions? options) {
        if (options?.IncludeGroups == false) return;
        var automatic = new HashSet<string>(authored.Groups.Where(g => g.Width <= 0 && g.Height <= 0 && g.X == 0 && g.Y == 0).Select(g => g.Id), StringComparer.Ordinal);
        if (automatic.Count < 2) return;
        automatic.RemoveWhere(id => authored.Nodes.Any(n => n.GroupId == id && (!IsUnset(n.X) || !IsUnset(n.Y))));
        var authoredNodes = authored.Nodes.ToDictionary(n => n.Id, StringComparer.Ordinal);
        automatic.RemoveWhere(id => authored.Edges.Any(e => e.Waypoints.Count > 0 &&
            (authoredNodes[e.SourceNodeId].GroupId == id || authoredNodes[e.TargetNodeId].GroupId == id)));
        var horizontal = chart.LayoutDirection is TopologyLayoutDirection.LeftToRight or TopologyLayoutDirection.RightToLeft;
        var nodeSpacing = options?.LayeredNodeSpacing ?? (horizontal ? 24 : 28);
        var rankSpacing = LayeredRankGap(chart, options, horizontal);
        var members = chart.Nodes.Where(n => n.GroupId != null).GroupBy(n => n.GroupId!, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
        var groups = chart.Groups.Where(g => automatic.Contains(g.Id) && members.ContainsKey(g.Id)).ToList();
        foreach (var group in groups) {
            var nodes = members[group.Id];
            // Global rank spacing belongs to the viewport; compact it inside an automatic shell.
            var ranks = nodes.GroupBy(GetLayer).OrderBy(g => g.Average(n => horizontal ? n.X : n.Y)).ToList();
            var crossSize = ranks.Max(g => g.Sum(n => horizontal ? n.Height : n.Width) + Math.Max(0, g.Count() - 1) * nodeSpacing);
            var main = horizontal ? nodes.Min(n => n.X) : nodes.Min(n => n.Y);
            var cross = horizontal ? nodes.Min(n => n.Y) : nodes.Min(n => n.X);
            for (var rankIndex = 0; rankIndex < ranks.Count; rankIndex++) {
                var rank = ranks[rankIndex];
                var ordered = rank.OrderBy(n => horizontal ? n.Y : n.X).ToArray();
                var length = ordered.Sum(n => horizontal ? n.Height : n.Width) + Math.Max(0, ordered.Length - 1) * nodeSpacing;
                var cursor = cross + (crossSize - length) / 2;
                foreach (var node in ordered) {
                    if (horizontal) { node.X = main; node.Y = cursor; } else { node.X = cursor; node.Y = main; }
                    cursor += (horizontal ? node.Height : node.Width) + nodeSpacing;
                }
                var rankDelta = rankIndex + 1 < ranks.Count ? Math.Abs(ranks[rankIndex + 1].Key - rank.Key) : 1;
                main += ordered.Max(n => horizontal ? n.Width : n.Height) + rankSpacing * Math.Max(1, rankDelta);
            }
            group.X = nodes.Min(n => n.X) - 24;
            group.Width = Math.Max(190, nodes.Max(n => n.X + n.Width) - group.X + 24);
            group.Y = nodes.Min(n => n.Y) - 24;
            group.Height = Math.Max(80, nodes.Max(n => n.Y + n.Height) - group.Y + 24);
            var header = options == null || TopologyGroupHeader.IsDrawn(options)
                ? TopologyGroupHeader.ReservedBounds(chart, group, options ?? new TopologyRenderOptions()).Height + 12 : 0;
            group.Y -= header; group.Height += header;
        }
        var placed = chart.Groups.Where(g => !automatic.Contains(g.Id)).ToList();
        var shifts = new Dictionary<string, PlacementDelta>(StringComparer.Ordinal);
        foreach (var group in groups.OrderBy(g => horizontal ? g.X : g.Y)) {
            var x = group.X; var y = group.Y;
            // Moving only along the flow axis preserves each group's internal layered order.
            foreach (var other in placed.OrderBy(g => horizontal ? g.X : g.Y)) {
                if (group.X < other.X + other.Width + 24 && group.X + group.Width + 24 > other.X
                    && group.Y < other.Y + other.Height + 24 && group.Y + group.Height + 24 > other.Y) {
                    if (horizontal) group.X = other.X + other.Width + 24;
                    else group.Y = other.Y + other.Height + 24;
                }
            }
            var delta = new PlacementDelta(group.X - x, group.Y - y);
            shifts[group.Id] = delta;
            foreach (var node in members[group.Id]) { node.X += delta.Dx; node.Y += delta.Dy; }
            placed.Add(group);
        }
        var nodesById = chart.Nodes.ToDictionary(n => n.Id, StringComparer.Ordinal);
        var originalEdges = authored.Edges.ToDictionary(e => e.Id, StringComparer.Ordinal);
        foreach (var edge in chart.Edges) {
            if (!nodesById.TryGetValue(edge.SourceNodeId, out var source) || !nodesById.TryGetValue(edge.TargetNodeId, out var target)) continue;
            var moved = shifts.ContainsKey(source.GroupId ?? "") || shifts.ContainsKey(target.GroupId ?? "");
            if (!moved || !edge.Metadata.TryGetValue("layout.route", out var route) || route != "long-edge-lane") continue;
            // These corridors were inferred before packing. Replan against the final node and header
            // obstacles through the shared obstacle-aware router instead of retaining stale coordinates.
            var original = originalEdges[edge.Id];
            edge.Waypoints.Clear(); edge.Waypoints.AddRange(original.Waypoints);
            edge.SourcePort = original.SourcePort; edge.TargetPort = original.TargetPort;
            edge.Routing = TopologyEdgeRouting.ObstacleAvoidingOrthogonal;
            edge.Metadata["layout.route"] = "packed-group-obstacle-route";
        }
    }
}

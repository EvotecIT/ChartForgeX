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
        var nodesById = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        foreach (var edge in chart.Edges) {
            if (!nodesById.TryGetValue(edge.SourceNodeId, out var source) || !nodesById.TryGetValue(edge.TargetNodeId, out var target)
                || GetLayer(source) == GetLayer(target)) continue;
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
    private static void PlaceAutomaticLayeredGroups(TopologyChart chart, TopologyChart authored, TopologyRenderOptions? options,
        IReadOnlyDictionary<TopologyEdge, TopologyEdge> authoredEdges) {
        if (options?.IncludeGroups == false) return;
        var automatic = new HashSet<string>(authored.Groups.Where(g => g.Width <= 0 && g.Height <= 0 && g.X == 0 && g.Y == 0).Select(g => g.Id), StringComparer.Ordinal);
        if (automatic.Count < 2) return;
        automatic.RemoveWhere(id => authored.Nodes.Any(n => n.GroupId == id && (!IsUnset(n.X) || !IsUnset(n.Y))));
        var authoredNodes = authored.Nodes.ToDictionary(n => n.Id, StringComparer.Ordinal);
        automatic.RemoveWhere(id => authored.Edges.Any(e => e.Waypoints.Count > 0 &&
            ((authoredNodes.TryGetValue(e.SourceNodeId, out var source) && source.GroupId == id)
             || (authoredNodes.TryGetValue(e.TargetNodeId, out var target) && target.GroupId == id))));
        var horizontal = chart.LayoutDirection is TopologyLayoutDirection.LeftToRight or TopologyLayoutDirection.RightToLeft;
        var nodeSpacing = options?.LayeredNodeSpacing ?? (horizontal ? 24 : 28);
        var members = chart.Nodes.Where(n => n.GroupId != null).GroupBy(n => n.GroupId!, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
        var groups = chart.Groups.Where(g => automatic.Contains(g.Id) && members.ContainsKey(g.Id)).ToList();
        var rankPositions = chart.Nodes.GroupBy(GetLayer).ToDictionary(group => group.Key,
            group => group.Min(node => horizontal ? node.X : node.Y));
        foreach (var group in groups) {
            var nodes = members[group.Id];
            // Preserve global rank slots, including wide ungrouped nodes between this group's ranks.
            // Compact only across ranks so a shell can never erase another node's flow-axis barrier.
            var ranks = nodes.GroupBy(GetLayer).OrderBy(g => g.Key).ToList();
            var crossSize = ranks.Max(g => g.Sum(n => horizontal ? n.Height : n.Width) + Math.Max(0, g.Count() - 1) * nodeSpacing);
            var cross = horizontal ? nodes.Min(n => n.Y) : nodes.Min(n => n.X);
            foreach (var rank in ranks) {
                var ordered = rank.OrderBy(n => horizontal ? n.Y : n.X).ToArray();
                var length = ordered.Sum(n => horizontal ? n.Height : n.Width) + Math.Max(0, ordered.Length - 1) * nodeSpacing;
                var cursor = cross + (crossSize - length) / 2;
                var main = rankPositions[rank.Key];
                foreach (var node in ordered) {
                    if (horizontal) { node.X = main; node.Y = cursor; } else { node.X = cursor; node.Y = main; }
                    cursor += (horizontal ? node.Height : node.Width) + nodeSpacing;
                }
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
        var shiftedGroupIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var group in groups.OrderBy(g => horizontal ? g.X : g.Y)) {
            var x = group.X; var y = group.Y;
            // Separate shells across the flow axis' cross dimension. Moving them along the
            // flow axis can put an interleaved rank (A0 -> B1 -> A2) after its successor.
            while (true) {
                var collision = placed.FirstOrDefault(other =>
                    group.X < other.X + other.Width + 24 && group.X + group.Width + 24 > other.X
                    && group.Y < other.Y + other.Height + 24 && group.Y + group.Height + 24 > other.Y);
                if (collision == null) break;
                if (horizontal) group.Y = collision.Y + collision.Height + 24;
                else group.X = collision.X + collision.Width + 24;
            }
            var delta = new PlacementDelta(group.X - x, group.Y - y);
            if (Math.Abs(delta.Dx) > .001 || Math.Abs(delta.Dy) > .001) shiftedGroupIds.Add(group.Id);
            foreach (var node in members[group.Id]) { node.X += delta.Dx; node.Y += delta.Dy; }
            placed.Add(group);
        }
        var nodesById = chart.Nodes.ToDictionary(n => n.Id, StringComparer.Ordinal);
        foreach (var edge in chart.Edges) {
            if (!nodesById.TryGetValue(edge.SourceNodeId, out var source) || !nodesById.TryGetValue(edge.TargetNodeId, out var target)) continue;
            var moved = shiftedGroupIds.Contains(source.GroupId ?? "") || shiftedGroupIds.Contains(target.GroupId ?? "");
            if (!moved || !edge.Metadata.TryGetValue("layout.route", out var route) || route != "long-edge-lane") continue;
            // These corridors were inferred before packing. Replan against the final node and header
            // obstacles through the shared obstacle-aware router instead of retaining stale coordinates.
            if (!authoredEdges.TryGetValue(edge, out var original)) continue;
            edge.Waypoints.Clear(); edge.Waypoints.AddRange(original.Waypoints);
            edge.SourcePort = original.SourcePort; edge.TargetPort = original.TargetPort;
            edge.Routing = TopologyEdgeRouting.ObstacleAvoidingOrthogonal;
            edge.Metadata["layout.route"] = "packed-group-obstacle-route";
        }
        if (groups.Count > 0) {
            // The hierarchy bus planner ran before automatic group shells were packed. Rebuild
            // its generated fan-out routes against the final node positions and group obstacles.
            foreach (var edge in chart.Edges) {
                if (!edge.Metadata.ContainsKey("hierarchy.route")) continue;
                if (authoredEdges.TryGetValue(edge, out var original)) {
                    edge.Waypoints.Clear(); edge.Waypoints.AddRange(original.Waypoints);
                    edge.SourcePort = original.SourcePort; edge.TargetPort = original.TargetPort;
                    edge.Routing = original.Waypoints.Count > 0 ? original.Routing : TopologyEdgeRouting.ObstacleAvoidingOrthogonal;
                } else {
                    edge.Waypoints.Clear(); edge.Routing = TopologyEdgeRouting.ObstacleAvoidingOrthogonal;
                }
                edge.Metadata.Remove("hierarchy.route"); edge.Metadata.Remove("hierarchy.route.tier");
                edge.Metadata.Remove("hierarchy.route.busX"); edge.Metadata.Remove("hierarchy.route.busY");
            }
        }
    }
}

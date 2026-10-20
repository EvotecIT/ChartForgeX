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
        if (automatic.Count == 0) return;
        automatic.RemoveWhere(id => authored.Nodes.Any(n => n.GroupId == id && (!IsUnset(n.X) || !IsUnset(n.Y))));
        if (automatic.Count == 0 || automatic.Count < 2 && authored.Groups.All(g => automatic.Contains(g.Id))) return;
        var authoredNodes = authored.Nodes.ToDictionary(n => n.Id, StringComparer.Ordinal);
        automatic.RemoveWhere(id => authored.Edges.Any(e => e.Waypoints.Count > 0 &&
            ((authoredNodes.TryGetValue(e.SourceNodeId, out var source) && source.GroupId == id)
             || (authoredNodes.TryGetValue(e.TargetNodeId, out var target) && target.GroupId == id))));
        var horizontal = chart.LayoutDirection is TopologyLayoutDirection.LeftToRight or TopologyLayoutDirection.RightToLeft;
        var positionsBeforePacking = chart.Nodes.ToDictionary(node => node.Id, node => new ChartPoint(node.X, node.Y), StringComparer.Ordinal);
        var nodeSpacing = options?.LayeredNodeSpacing ?? (horizontal ? 24 : 28);
        var members = chart.Nodes.Where(n => n.GroupId != null).GroupBy(n => n.GroupId!, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
        var groups = chart.Groups.Where(g => automatic.Contains(g.Id) && members.ContainsKey(g.Id)).ToList();
        var automaticNodeIds = new HashSet<string>(authored.Nodes.Where(node => IsUnset(node.X) && IsUnset(node.Y)).Select(node => node.Id), StringComparer.Ordinal);
        var rankPositions = chart.Nodes.Where(node => automaticNodeIds.Contains(node.Id)
            && (node.GroupId == null || automatic.Contains(node.GroupId))).GroupBy(GetLayer).ToDictionary(group => group.Key,
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
                var preferred = cross + (crossSize - length) / 2;
                var cursor = ReserveUngroupedRankPeers(rank.Key, preferred, length);
                var main = rankPositions.TryGetValue(rank.Key, out var automaticRankOrigin)
                    ? automaticRankOrigin : rank.Min(node => horizontal ? node.X : node.Y);
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

            double ReserveUngroupedRankPeers(int rankIndex, double preferred, double groupLength) {
                var peers = chart.Nodes.Where(node => node.GroupId == null && GetLayer(node) == rankIndex)
                    .Select(node => (Start: horizontal ? node.Y : node.X, Length: horizontal ? node.Height : node.Width))
                    .ToArray();
                if (peers.Length == 0) return preferred;
                bool IsFree(double start) => peers.All(peer => start + groupLength + nodeSpacing <= peer.Start
                    || start >= peer.Start + peer.Length + nodeSpacing);
                var candidates = peers.SelectMany(peer => new[] { peer.Start - nodeSpacing - groupLength, peer.Start + peer.Length + nodeSpacing })
                    .Append(preferred).Distinct().Where(IsFree)
                    .OrderBy(start => Math.Abs(start - preferred)).ToArray();
                if (candidates.Length > 0) return candidates[0];
                return peers.Max(peer => peer.Start + peer.Length) + nodeSpacing;
            }
        }
        TopologyLayoutNormalizer.PrepareGroupShellsForPacking(chart, options);
        var placed = chart.Groups.Where(g => !automatic.Contains(g.Id)).ToList();
        var packSingleGroupAlongFlow = groups.Count == 1;
        var shiftedGroupIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var group in groups.OrderBy(g => horizontal ? g.X : g.Y)) {
            var x = group.X; var y = group.Y;
            // Keep one packed shell on the flow axis so a simple chain stays directional. With
            // several automatic shells, separate them across the cross dimension because moving
            // one along the flow axis can put an interleaved rank (A0 -> B1 -> A2) after its successor.
            var collision = placed.FirstOrDefault(other => GroupShellsOverlap(group, other));
            if (collision != null && packSingleGroupAlongFlow) {
                var positiveFlow = chart.LayoutDirection is TopologyLayoutDirection.LeftToRight or TopologyLayoutDirection.TopToBottom;
                var precedes = LayeredGroupPrecedes(group, collision, members, chart.Edges);
                var preferBefore = precedes == positiveFlow;
                var origin = horizontal ? x : y;
                var candidates = placed.SelectMany(other => horizontal
                        ? new[] { other.X - group.Width - 24, other.X + other.Width + 24 }
                        : new[] { other.Y - group.Height - 24, other.Y + other.Height + 24 })
                    .Append(origin).Distinct().OrderBy(value => preferBefore == (value < origin) ? 0 : 1)
                    .ThenBy(value => Math.Abs(value - origin)).ToArray();
                var candidate = candidates.Where(value => !placed.Any(other => GroupShellsOverlap(group, other, value, horizontal)))
                    .Select(value => (double?)value).FirstOrDefault();
                var coordinate = candidate ?? double.NaN;
                if (!candidate.HasValue) {
                    var extreme = horizontal
                        ? (preferBefore ? placed.Min(other => other.X) - group.Width - 24 : placed.Max(other => other.X + other.Width) + 24)
                        : (preferBefore ? placed.Min(other => other.Y) - group.Height - 24 : placed.Max(other => other.Y + other.Height) + 24);
                    coordinate = extreme;
                }
                if (horizontal) group.X = coordinate; else group.Y = coordinate;
            } else if (collision != null) {
                var before = placed.Where(other => horizontal
                        ? group.Y < other.Y + other.Height + 24 && group.Y + group.Height + 24 > other.Y
                        : group.X < other.X + other.Width + 24 && group.X + group.Width + 24 > other.X)
                    .Select(other => horizontal ? other.Y + other.Height + 24 : other.X + other.Width + 24).DefaultIfEmpty(0).Max();
                if (horizontal) group.Y = Math.Max(group.Y, before); else group.X = Math.Max(group.X, before);
            }
            var delta = new PlacementDelta(group.X - x, group.Y - y);
            if (Math.Abs(delta.Dx) > .001 || Math.Abs(delta.Dy) > .001) shiftedGroupIds.Add(group.Id);
            foreach (var node in members[group.Id]) { node.X += delta.Dx; node.Y += delta.Dy; }
            placed.Add(group);
        }
        var nodesById = chart.Nodes.ToDictionary(n => n.Id, StringComparer.Ordinal);
        var membersMoved = chart.Nodes.Any(node => positionsBeforePacking.TryGetValue(node.Id, out var before)
            && (Math.Abs(node.X - before.X) > .001 || Math.Abs(node.Y - before.Y) > .001));
        foreach (var edge in chart.Edges) {
            if (!nodesById.TryGetValue(edge.SourceNodeId, out var source) || !nodesById.TryGetValue(edge.TargetNodeId, out var target)) continue;
            // Compaction can move a member inside an unchanged shell. Its old corridor is
            // just as stale as a translated shell's corridor, including the final target leg.
            var moved = shiftedGroupIds.Contains(source.GroupId ?? "") || shiftedGroupIds.Contains(target.GroupId ?? "")
                || membersMoved;
            if (!moved || !edge.Metadata.TryGetValue("layout.route", out var route) || route != "long-edge-lane") continue;
            // These corridors were inferred before packing. Replan against the final node and header
            // obstacles through the shared obstacle-aware router instead of retaining stale coordinates.
            if (!authoredEdges.TryGetValue(edge, out var original)) continue;
            edge.Waypoints.Clear(); edge.Waypoints.AddRange(original.Waypoints);
            edge.SourcePort = original.SourcePort; edge.TargetPort = original.TargetPort;
            // Keep the shared rank-facing port policy so direct and skipping edges can
            // spread their arrows independently after the obstacle-aware route is planned.
            var sourceBefore = horizontal ? target.X >= source.X : target.Y >= source.Y;
            if (edge.SourcePort == TopologyEdgePort.Auto) edge.SourcePort = horizontal ? sourceBefore ? TopologyEdgePort.Right : TopologyEdgePort.Left
                : sourceBefore ? TopologyEdgePort.Bottom : TopologyEdgePort.Top;
            if (edge.TargetPort == TopologyEdgePort.Auto) edge.TargetPort = horizontal ? sourceBefore ? TopologyEdgePort.Left : TopologyEdgePort.Right
                : sourceBefore ? TopologyEdgePort.Top : TopologyEdgePort.Bottom;
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

    private static bool GroupShellsOverlap(TopologyGroup first, TopologyGroup second, double? flowCoordinate = null, bool horizontal = true) {
        var x = horizontal && flowCoordinate.HasValue ? flowCoordinate.Value : first.X;
        var y = !horizontal && flowCoordinate.HasValue ? flowCoordinate.Value : first.Y;
        return x < second.X + second.Width + 24 && x + first.Width + 24 > second.X
            && y < second.Y + second.Height + 24 && y + first.Height + 24 > second.Y;
    }

    private static bool LayeredGroupPrecedes(TopologyGroup group, TopologyGroup other,
        IReadOnlyDictionary<string, List<TopologyNode>> members, IReadOnlyList<TopologyEdge> edges) {
        if (members.TryGetValue(group.Id, out var current) && members.TryGetValue(other.Id, out var compared)
            && current.Count > 0 && compared.Count > 0) {
            var currentMin = current.Min(GetLayer); var currentMax = current.Max(GetLayer);
            var comparedMin = compared.Min(GetLayer); var comparedMax = compared.Max(GetLayer);
            if (currentMax < comparedMin) return true;
            if (currentMin > comparedMax) return false;
            var currentIds = new HashSet<string>(current.Select(node => node.Id), StringComparer.Ordinal);
            var comparedIds = new HashSet<string>(compared.Select(node => node.Id), StringComparer.Ordinal);
            var currentBefore = edges.Any(edge => currentIds.Contains(edge.SourceNodeId) && comparedIds.Contains(edge.TargetNodeId));
            var otherBefore = edges.Any(edge => comparedIds.Contains(edge.SourceNodeId) && currentIds.Contains(edge.TargetNodeId));
            if (currentBefore != otherBefore) return currentBefore;
        }
        return false;
    }
}

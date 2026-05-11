using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyLayoutEngine {
    private readonly struct PlacementDelta {
        internal PlacementDelta(double dx, double dy) {
            Dx = dx;
            Dy = dy;
        }

        internal double Dx { get; }
        internal double Dy { get; }
    }

    private static Dictionary<string, (double X, double Y)> ExplicitGroupPositions(IEnumerable<TopologyGroup> groups) {
        var result = new Dictionary<string, (double X, double Y)>(StringComparer.Ordinal);
        foreach (var group in groups) {
            if (!HasExplicitGroupPlacement(group) || result.ContainsKey(group.Id)) continue;
            result[group.Id] = (group.X, group.Y);
        }

        return result;
    }

    private static Dictionary<string, PlacementDelta> RestoreExplicitDenseGroupPositions(TopologyChart chart, IReadOnlyDictionary<string, (double X, double Y)> positions) {
        var restored = new Dictionary<string, PlacementDelta>(StringComparer.Ordinal);
        if (positions.Count == 0) return restored;
        foreach (var group in chart.Groups) {
            if (!positions.TryGetValue(group.Id, out var position)) continue;
            var dx = position.X - group.X;
            var dy = position.Y - group.Y;
            if (Math.Abs(dx) < 0.0001 && Math.Abs(dy) < 0.0001) continue;
            group.X = position.X;
            group.Y = position.Y;
            restored[group.Id] = new PlacementDelta(dx, dy);
            foreach (var node in chart.Nodes.Where(node => string.Equals(node.GroupId, group.Id, StringComparison.Ordinal))) {
                node.X += dx;
                node.Y += dy;
            }
        }

        return restored;
    }

    private static void ShiftManualWaypointsForRestoredGroups(TopologyChart chart, IReadOnlyDictionary<string, PlacementDelta> restoredGroups) {
        if (restoredGroups.Count == 0) return;
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        foreach (var edge in chart.Edges.Where(edge => edge.Waypoints.Count > 0)) {
            var deltas = new List<PlacementDelta>(2);
            if (nodes.TryGetValue(edge.SourceNodeId, out var source) &&
                !string.IsNullOrWhiteSpace(source.GroupId) &&
                restoredGroups.TryGetValue(source.GroupId!, out var sourceDelta)) {
                deltas.Add(sourceDelta);
            }

            if (nodes.TryGetValue(edge.TargetNodeId, out var target) &&
                !string.IsNullOrWhiteSpace(target.GroupId) &&
                restoredGroups.TryGetValue(target.GroupId!, out var targetDelta)) {
                deltas.Add(targetDelta);
            }

            if (deltas.Count == 0) continue;
            var dx = deltas.Average(delta => delta.Dx);
            var dy = deltas.Average(delta => delta.Dy);
            for (var i = 0; i < edge.Waypoints.Count; i++) {
                var waypoint = edge.Waypoints[i];
                edge.Waypoints[i] = new ChartPoint(waypoint.X + dx, waypoint.Y + dy);
            }
        }
    }

    private static void ResetDenseInferenceForRestoredGroups(TopologyChart chart, IReadOnlyDictionary<string, PlacementDelta> restoredGroups) {
        if (restoredGroups.Count == 0) return;
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var groups = chart.Groups.ToDictionary(group => group.Id, StringComparer.Ordinal);
        var affectedPairs = new HashSet<string>(StringComparer.Ordinal);
        foreach (var edge in chart.Edges) {
            if (!TryGetEdgeGroups(edge, nodes, groups, out _, out _)) continue;
            var sourceGroupId = nodes[edge.SourceNodeId].GroupId ?? string.Empty;
            var targetGroupId = nodes[edge.TargetNodeId].GroupId ?? string.Empty;
            if (!restoredGroups.ContainsKey(sourceGroupId) && !restoredGroups.ContainsKey(targetGroupId)) continue;
            affectedPairs.Add(DenseGroupPairKey(edge, nodes));
            if ((edge.LayoutInference & TopologyEdgeLayoutInference.SourcePort) != 0) edge.SourcePort = TopologyEdgePort.Auto;
            if ((edge.LayoutInference & TopologyEdgeLayoutInference.TargetPort) != 0) edge.TargetPort = TopologyEdgePort.Auto;
            edge.LayoutInference &= ~(TopologyEdgeLayoutInference.SourcePort | TopologyEdgeLayoutInference.TargetPort);
        }

        foreach (var edge in chart.Edges) {
            if (!TryGetEdgeGroups(edge, nodes, groups, out _, out _)) continue;
            if (!affectedPairs.Contains(DenseGroupPairKey(edge, nodes))) continue;
            if ((edge.LayoutInference & TopologyEdgeLayoutInference.RouteLane) == 0) continue;
            edge.RouteLane = 0;
            edge.LayoutInference &= ~TopologyEdgeLayoutInference.RouteLane;
        }
    }

    private static bool HasExplicitGroupPlacement(TopologyGroup group) =>
        group.HasPositionOverride || !IsUnset(group.X) || !IsUnset(group.Y);

    private static string DenseGroupPairKey(TopologyEdge edge, IReadOnlyDictionary<string, TopologyNode> nodes) {
        var sourceGroupId = nodes[edge.SourceNodeId].GroupId ?? string.Empty;
        var targetGroupId = nodes[edge.TargetNodeId].GroupId ?? string.Empty;
        return string.Compare(sourceGroupId, targetGroupId, StringComparison.Ordinal) <= 0
            ? sourceGroupId + "\u001F" + targetGroupId
            : targetGroupId + "\u001F" + sourceGroupId;
    }
}

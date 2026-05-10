using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Primitives;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyLayoutEngine {
    private const double HierarchyRouteMinimumGap = 18;
    private const double HierarchyRouteMaximumGap = 46;

    private static void ApplyHierarchyEdgeRoutesTopToBottom(TopologyChart chart) {
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        foreach (var group in HierarchyEdgesBySource(chart)) {
            if (group.Count <= 1 || !nodes.TryGetValue(group[0].SourceNodeId, out var source)) continue;
            var targets = group
                .Where(edge => nodes.ContainsKey(edge.TargetNodeId))
                .OrderBy(edge => CenterX(nodes[edge.TargetNodeId]))
                .ThenBy(edge => edge.TargetNodeId, StringComparer.Ordinal)
                .ToList();
            if (targets.Count <= 1) continue;
            var sourceBottom = source.Y + source.Height;
            var targetTop = targets.Select(edge => nodes[edge.TargetNodeId].Y).Min();
            var gap = targetTop - sourceBottom;
            if (gap <= 8) continue;
            var busY = sourceBottom + Math.Min(HierarchyRouteMaximumGap, Math.Max(HierarchyRouteMinimumGap, gap * 0.42));
            foreach (var edge in targets) {
                var target = nodes[edge.TargetNodeId];
                edge.SourcePort = TopologyEdgePort.Auto;
                edge.TargetPort = TopologyEdgePort.Top;
                edge.Routing = TopologyEdgeRouting.Orthogonal;
                edge.Waypoints.Clear();
                edge.Waypoints.Add(new ChartPoint(CenterX(source), busY));
                edge.Waypoints.Add(new ChartPoint(CenterX(target), busY));
                edge.Metadata["hierarchy.route"] = "shared-bus";
                edge.Metadata["hierarchy.route.busY"] = busY.ToString("0.###", CultureInfo.InvariantCulture);
            }
        }
    }

    private static void ApplyHierarchyEdgeRoutesLeftToRight(TopologyChart chart) {
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        foreach (var group in HierarchyEdgesBySource(chart)) {
            if (group.Count <= 1 || !nodes.TryGetValue(group[0].SourceNodeId, out var source)) continue;
            var targets = group
                .Where(edge => nodes.ContainsKey(edge.TargetNodeId))
                .OrderBy(edge => CenterY(nodes[edge.TargetNodeId]))
                .ThenBy(edge => edge.TargetNodeId, StringComparer.Ordinal)
                .ToList();
            if (targets.Count <= 1) continue;
            var sourceRight = source.X + source.Width;
            var targetLeft = targets.Select(edge => nodes[edge.TargetNodeId].X).Min();
            var gap = targetLeft - sourceRight;
            if (gap <= 8) continue;
            var busX = sourceRight + Math.Min(HierarchyRouteMaximumGap, Math.Max(HierarchyRouteMinimumGap, gap * 0.42));
            foreach (var edge in targets) {
                var target = nodes[edge.TargetNodeId];
                edge.SourcePort = TopologyEdgePort.Auto;
                edge.TargetPort = TopologyEdgePort.Left;
                edge.Routing = TopologyEdgeRouting.Orthogonal;
                edge.Waypoints.Clear();
                edge.Waypoints.Add(new ChartPoint(busX, CenterY(source)));
                edge.Waypoints.Add(new ChartPoint(busX, CenterY(target)));
                edge.Metadata["hierarchy.route"] = "shared-bus";
                edge.Metadata["hierarchy.route.busX"] = busX.ToString("0.###", CultureInfo.InvariantCulture);
            }
        }
    }

    private static List<List<TopologyEdge>> HierarchyEdgesBySource(TopologyChart chart) {
        return chart.Edges
            .Where(IsHierarchyParentChildEdge)
            .Where(edge => edge.Waypoints.Count == 0 || edge.Metadata.ContainsKey("hierarchy.route"))
            .GroupBy(edge => edge.SourceNodeId, StringComparer.Ordinal)
            .Select(group => group.OrderBy(edge => edge.Id, StringComparer.Ordinal).ToList())
            .ToList();
    }

    private static bool IsHierarchyParentChildEdge(TopologyEdge edge) {
        return edge.Metadata.TryGetValue("hierarchy.relationship", out var relationship)
            && string.Equals(relationship, "parent-child", StringComparison.Ordinal);
    }

}

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
        var nodes = HierarchyNodeLookup(chart);
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
            var busByRow = HierarchyRowBuses(targets.Select(edge => nodes[edge.TargetNodeId]), sourceBottom);
            foreach (var edge in targets) {
                var target = nodes[edge.TargetNodeId];
                var row = HierarchyRouteRow(target);
                var busY = busByRow.TryGetValue(row, out var rowBusY)
                    ? rowBusY
                    : sourceBottom + Math.Min(HierarchyRouteMaximumGap, Math.Max(HierarchyRouteMinimumGap, gap * 0.42));
                edge.SourcePort = TopologyEdgePort.Auto;
                edge.TargetPort = TopologyEdgePort.Top;
                edge.Routing = TopologyEdgeRouting.Orthogonal;
                edge.Waypoints.Clear();
                edge.Waypoints.Add(new ChartPoint(CenterX(source), busY));
                edge.Waypoints.Add(new ChartPoint(CenterX(target), busY));
                edge.Metadata["hierarchy.route"] = "shared-bus";
                edge.Metadata["hierarchy.route.tier"] = row.ToString(CultureInfo.InvariantCulture);
                edge.Metadata["hierarchy.route.busY"] = busY.ToString("0.###", CultureInfo.InvariantCulture);
            }
        }
    }

    private static void ApplyHierarchyEdgeRoutesLeftToRight(TopologyChart chart) {
        var nodes = HierarchyNodeLookup(chart);
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
            var busByColumn = HierarchyColumnBuses(targets.Select(edge => nodes[edge.TargetNodeId]), sourceRight);
            foreach (var edge in targets) {
                var target = nodes[edge.TargetNodeId];
                var column = HierarchyRouteColumn(target);
                var busX = busByColumn.TryGetValue(column, out var columnBusX)
                    ? columnBusX
                    : sourceRight + Math.Min(HierarchyRouteMaximumGap, Math.Max(HierarchyRouteMinimumGap, gap * 0.42));
                edge.SourcePort = TopologyEdgePort.Auto;
                edge.TargetPort = TopologyEdgePort.Left;
                edge.Routing = TopologyEdgeRouting.Orthogonal;
                edge.Waypoints.Clear();
                edge.Waypoints.Add(new ChartPoint(busX, CenterY(source)));
                edge.Waypoints.Add(new ChartPoint(busX, CenterY(target)));
                edge.Metadata["hierarchy.route"] = "shared-bus";
                edge.Metadata["hierarchy.route.tier"] = column.ToString(CultureInfo.InvariantCulture);
                edge.Metadata["hierarchy.route.busX"] = busX.ToString("0.###", CultureInfo.InvariantCulture);
            }
        }
    }

    private static Dictionary<int, double> HierarchyRowBuses(IEnumerable<TopologyNode> targets, double sourceBottom) {
        var rows = targets
            .GroupBy(HierarchyRouteRow)
            .OrderBy(group => group.Key)
            .Select(group => new HierarchyRouteBand(group.Key, group.Min(node => node.Y), group.Max(node => node.Y + node.Height)))
            .ToList();
        var result = new Dictionary<int, double>();
        var previousBottom = sourceBottom;
        foreach (var row in rows) {
            var gap = row.Start - previousBottom;
            result[row.Index] = gap > 8
                ? previousBottom + Math.Min(HierarchyRouteMaximumGap, Math.Max(HierarchyRouteMinimumGap, gap * 0.42))
                : row.Start - Math.Min(14, Math.Max(6, row.Start - previousBottom));
            previousBottom = Math.Max(previousBottom, row.End);
        }

        return result;
    }

    private static Dictionary<int, double> HierarchyColumnBuses(IEnumerable<TopologyNode> targets, double sourceRight) {
        var columns = targets
            .GroupBy(HierarchyRouteColumn)
            .OrderBy(group => group.Key)
            .Select(group => new HierarchyRouteBand(group.Key, group.Min(node => node.X), group.Max(node => node.X + node.Width)))
            .ToList();
        var result = new Dictionary<int, double>();
        var previousRight = sourceRight;
        foreach (var column in columns) {
            var gap = column.Start - previousRight;
            result[column.Index] = gap > 8
                ? previousRight + Math.Min(HierarchyRouteMaximumGap, Math.Max(HierarchyRouteMinimumGap, gap * 0.42))
                : column.Start - Math.Min(14, Math.Max(6, column.Start - previousRight));
            previousRight = Math.Max(previousRight, column.End);
        }

        return result;
    }

    private static int HierarchyRouteRow(TopologyNode node) => HierarchyRouteIndex(node, "layout.row");

    private static int HierarchyRouteColumn(TopologyNode node) => HierarchyRouteIndex(node, "layout.column");

    private static int HierarchyRouteIndex(TopologyNode node, string key) {
        return node.Metadata.TryGetValue(key, out var value) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index)
            ? Math.Max(0, index)
            : 0;
    }

    private static void SyncHierarchyRouteDiagnostics(TopologyChart chart) {
        var nodes = HierarchyNodeLookup(chart);
        foreach (var edge in chart.Edges) {
            if (!edge.Metadata.TryGetValue("hierarchy.route", out var route) || !string.Equals(route, "shared-bus", StringComparison.Ordinal)) continue;
            if (!nodes.TryGetValue(edge.SourceNodeId, out var source) || !nodes.TryGetValue(edge.TargetNodeId, out var target)) continue;
            if (edge.Waypoints.Count < 2) continue;

            if (edge.Metadata.ContainsKey("hierarchy.route.busY")) {
                var busY = edge.Waypoints[0].Y;
                edge.Waypoints.Clear();
                edge.Waypoints.Add(new ChartPoint(CenterX(source), busY));
                edge.Waypoints.Add(new ChartPoint(CenterX(target), busY));
                edge.Metadata["hierarchy.route.busY"] = busY.ToString("0.###", CultureInfo.InvariantCulture);
                continue;
            }

            if (edge.Metadata.ContainsKey("hierarchy.route.busX")) {
                var busX = edge.Waypoints[0].X;
                edge.Waypoints.Clear();
                edge.Waypoints.Add(new ChartPoint(busX, CenterY(source)));
                edge.Waypoints.Add(new ChartPoint(busX, CenterY(target)));
                edge.Metadata["hierarchy.route.busX"] = busX.ToString("0.###", CultureInfo.InvariantCulture);
            }
        }
    }

    private static Dictionary<string, TopologyNode> HierarchyNodeLookup(TopologyChart chart) {
        return chart.Nodes
            .Where(node => !string.IsNullOrWhiteSpace(node.Id))
            .GroupBy(node => node.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
    }

    private readonly struct HierarchyRouteBand {
        public HierarchyRouteBand(int index, double start, double end) {
            Index = index;
            Start = start;
            End = end;
        }

        public int Index { get; }

        public double Start { get; }

        public double End { get; }
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

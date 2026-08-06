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
            var firstRow = targets.Select(edge => HierarchyRouteRow(nodes[edge.TargetNodeId])).Min();
            var trunkX = SelectHierarchyTrunkTopToBottom(chart, targets, nodes, source, busByRow, firstRow);
            foreach (var edge in targets) {
                var target = nodes[edge.TargetNodeId];
                var row = HierarchyRouteRow(target);
                if (row > firstRow) {
                    var tierBusY = busByRow[row];
                    var entryBusY = busByRow[firstRow];
                    ApplyTieredHierarchyRouteTopToBottom(edge, source, target, row, entryBusY, tierBusY, trunkX);
                    continue;
                }

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
            var firstColumn = targets.Select(edge => HierarchyRouteColumn(nodes[edge.TargetNodeId])).Min();
            var trunkY = SelectHierarchyTrunkLeftToRight(chart, targets, nodes, source, busByColumn, firstColumn);
            foreach (var edge in targets) {
                var target = nodes[edge.TargetNodeId];
                var column = HierarchyRouteColumn(target);
                if (column > firstColumn) {
                    var tierBusX = busByColumn[column];
                    var entryBusX = busByColumn[firstColumn];
                    ApplyTieredHierarchyRouteLeftToRight(edge, source, target, column, entryBusX, tierBusX, trunkY);
                    continue;
                }

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

    private static void ApplyTieredHierarchyRouteTopToBottom(TopologyEdge edge, TopologyNode source, TopologyNode target, int tier, double entryBusY, double busY, double trunkX) {
        ApplyTieredHierarchyRoute(edge, TieredTopToBottomWaypoints(source, target, entryBusY, busY, trunkX), tier, TopologyEdgePort.Bottom, TopologyEdgePort.Top);
    }

    private static void ApplyTieredHierarchyRouteLeftToRight(TopologyEdge edge, TopologyNode source, TopologyNode target, int tier, double entryBusX, double busX, double trunkY) {
        ApplyTieredHierarchyRoute(edge, TieredLeftToRightWaypoints(source, target, entryBusX, busX, trunkY), tier, TopologyEdgePort.Right, TopologyEdgePort.Left);
    }

    private static IEnumerable<double> HierarchyTrunkCandidates(double minimum, double maximum, double sourceCenter, double viewportMinimum, double viewportMaximum) {
        const double clearance = 18;
        if (sourceCenter < minimum - clearance || sourceCenter > maximum + clearance) yield return sourceCenter;
        yield return minimum - clearance;
        yield return maximum + clearance;
        yield return viewportMinimum + clearance;
        yield return viewportMaximum - clearance;
    }

    private static void ApplyTieredHierarchyRoute(TopologyEdge edge, IReadOnlyList<ChartPoint> points, int tier, TopologyEdgePort sourcePort, TopologyEdgePort targetPort) {
        edge.SourcePort = sourcePort;
        edge.TargetPort = targetPort;
        edge.Routing = TopologyEdgeRouting.Orthogonal;
        edge.Waypoints.Clear();
        edge.Waypoints.AddRange(points);
        edge.Metadata["hierarchy.route"] = "tiered-trunk";
        edge.Metadata["hierarchy.route.tier"] = tier.ToString(CultureInfo.InvariantCulture);
        edge.Metadata.Remove("hierarchy.route.busX");
        edge.Metadata.Remove("hierarchy.route.busY");
    }

    private static double SelectHierarchyTrunkTopToBottom(TopologyChart chart, IReadOnlyList<TopologyEdge> edges, IReadOnlyDictionary<string, TopologyNode> nodes, TopologyNode source, IReadOnlyDictionary<int, double> busByRow, int firstRow) {
        var siblings = edges.Select(edge => nodes[edge.TargetNodeId]).ToList();
        var candidates = HierarchyTrunkCandidates(siblings.Min(node => node.X), siblings.Max(node => node.X + node.Width), CenterX(source), chart.Viewport.Padding, chart.Viewport.Width - chart.Viewport.Padding).Distinct().ToList();
        return SelectHierarchyTrunk(chart, edges, nodes, source, candidates, edge => {
            var target = nodes[edge.TargetNodeId];
            var row = HierarchyRouteRow(target);
            return row <= firstRow ? null : TieredTopToBottomWaypoints(source, target, busByRow[firstRow], busByRow[row], 0);
        }, true);
    }

    private static double SelectHierarchyTrunkLeftToRight(TopologyChart chart, IReadOnlyList<TopologyEdge> edges, IReadOnlyDictionary<string, TopologyNode> nodes, TopologyNode source, IReadOnlyDictionary<int, double> busByColumn, int firstColumn) {
        var siblings = edges.Select(edge => nodes[edge.TargetNodeId]).ToList();
        var candidates = HierarchyTrunkCandidates(siblings.Min(node => node.Y), siblings.Max(node => node.Y + node.Height), CenterY(source), chart.Viewport.Padding, chart.Viewport.Height - chart.Viewport.Padding).Distinct().ToList();
        return SelectHierarchyTrunk(chart, edges, nodes, source, candidates, edge => {
            var target = nodes[edge.TargetNodeId];
            var column = HierarchyRouteColumn(target);
            return column <= firstColumn ? null : TieredLeftToRightWaypoints(source, target, busByColumn[firstColumn], busByColumn[column], 0);
        }, false);
    }

    private static double SelectHierarchyTrunk(TopologyChart chart, IReadOnlyList<TopologyEdge> edges, IReadOnlyDictionary<string, TopologyNode> nodes, TopologyNode source, IReadOnlyList<double> candidates, Func<TopologyEdge, List<ChartPoint>?> routeFactory, bool replaceX) {
        var selected = candidates[0];
        var bestHits = int.MaxValue;
        var bestLabelHits = int.MaxValue;
        var bestLength = double.MaxValue;
        foreach (var candidate in candidates) {
            var hits = 0;
            var labelHits = 0;
            var length = 0.0;
            foreach (var edge in edges) {
                var points = routeFactory(edge);
                if (points == null) continue;
                for (var i = 0; i < points.Count; i++) points[i] = replaceX ? new ChartPoint(candidate, points[i].Y) : new ChartPoint(points[i].X, candidate);
                if (replaceX) {
                    points[0] = new ChartPoint(CenterX(source), points[0].Y);
                    points[points.Count - 1] = new ChartPoint(CenterX(nodes[edge.TargetNodeId]), points[points.Count - 1].Y);
                } else {
                    points[0] = new ChartPoint(points[0].X, CenterY(source));
                    points[points.Count - 1] = new ChartPoint(points[points.Count - 1].X, CenterY(nodes[edge.TargetNodeId]));
                }

                ApplyTieredHierarchyRoute(edge, points, HierarchyRouteIndex(nodes[edge.TargetNodeId], replaceX ? "layout.row" : "layout.column"), replaceX ? TopologyEdgePort.Bottom : TopologyEdgePort.Right, replaceX ? TopologyEdgePort.Top : TopologyEdgePort.Left);
                var diagnostics = TopologyEdgeRouter.Route(chart, edge, source, nodes[edge.TargetNodeId]).Diagnostics;
                hits += diagnostics.ObstacleHits;
                labelHits += diagnostics.LabelObstacleHits;
                length += HierarchyRouteLength(points);
            }

            if (hits < bestHits || (hits == bestHits && labelHits < bestLabelHits) || (hits == bestHits && labelHits == bestLabelHits && length < bestLength)) {
                selected = candidate;
                bestHits = hits;
                bestLabelHits = labelHits;
                bestLength = length;
            }
        }

        return selected;
    }

    private static List<ChartPoint> TieredTopToBottomWaypoints(TopologyNode source, TopologyNode target, double entryBusY, double busY, double trunkX) => new() {
        new(CenterX(source), entryBusY),
        new(trunkX, entryBusY),
        new(trunkX, busY),
        new(CenterX(target), busY)
    };

    private static List<ChartPoint> TieredLeftToRightWaypoints(TopologyNode source, TopologyNode target, double entryBusX, double busX, double trunkY) => new() {
        new(entryBusX, CenterY(source)),
        new(entryBusX, trunkY),
        new(busX, trunkY),
        new(busX, CenterY(target))
    };

    private static double HierarchyRouteLength(IReadOnlyList<ChartPoint> points) {
        var length = 0.0;
        for (var i = 0; i < points.Count - 1; i++) length += Math.Abs(points[i + 1].X - points[i].X) + Math.Abs(points[i + 1].Y - points[i].Y);
        return length;
    }

    private static Dictionary<int, double> HierarchyRowBuses(IEnumerable<TopologyNode> targets, double sourceBottom) {
        var rows = targets
            .GroupBy(HierarchyRouteRow)
            .OrderBy(group => group.Key)
            .Select(group => new HierarchyRouteBand(group.Key, group.Min(node => node.Y), group.Max(node => node.Y + node.Height)))
            .ToList();
        var result = new Dictionary<int, double>();
        var previousBottom = sourceBottom;
        var first = true;
        foreach (var row in rows) {
            var gap = row.Start - previousBottom;
            result[row.Index] = gap > 8
                ? first
                    ? previousBottom + Math.Min(HierarchyRouteMaximumGap, Math.Max(HierarchyRouteMinimumGap, gap * 0.42))
                    : previousBottom + gap / 2
                : row.Start - Math.Min(14, Math.Max(6, row.Start - previousBottom));
            previousBottom = Math.Max(previousBottom, row.End);
            first = false;
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
        var first = true;
        foreach (var column in columns) {
            var gap = column.Start - previousRight;
            result[column.Index] = gap > 8
                ? first
                    ? previousRight + Math.Min(HierarchyRouteMaximumGap, Math.Max(HierarchyRouteMinimumGap, gap * 0.42))
                    : previousRight + gap / 2
                : column.Start - Math.Min(14, Math.Max(6, column.Start - previousRight));
            previousRight = Math.Max(previousRight, column.End);
            first = false;
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

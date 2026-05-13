using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    public static List<TopologyEdgeLabelLayout> EdgeLabelLayouts(TopologyChart chart, TopologyRenderOptions options) {
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var nodeBoxes = chart.Nodes
            .Where(node => EffectiveNodeDisplayMode(node, options) != TopologyNodeDisplayMode.Hidden)
            .Select(node => EdgeLabelNodeObstacle(node, options, 8))
            .ToList();
        var groupHeaderBoxes = chart.Groups.Select(group => LabelBox.FromGroupHeader(group, 8)).ToList();
        var groupBoxes = chart.Groups.Select(group => LabelBox.FromGroup(group, 8)).ToList();
        var edgeSegments = EdgeSegments(chart, nodes);
        var edgeRenderOrders = EdgeRenderOrderMap(chart, options);
        var placed = new List<LabelBox>();
        var layouts = new List<TopologyEdgeLabelLayout>();

        foreach (var edge in OrderedEdgesForLabelPlacement(chart, options)) {
            var label = EdgeLabel(edge, options.EdgeLabelMetricKey, edge.Label);
            var secondary = EdgeLabel(edge, options.EdgeSecondaryLabelMetricKey, edge.SecondaryLabel);
            var tertiary = EdgeLabel(edge, options.EdgeTertiaryLabelMetricKey, edge.TertiaryLabel);
            if (string.IsNullOrWhiteSpace(label) && string.IsNullOrWhiteSpace(secondary) && string.IsNullOrWhiteSpace(tertiary)) continue;
            if (!nodes.ContainsKey(edge.SourceNodeId) || !nodes.ContainsKey(edge.TargetNodeId)) continue;

            var points = EdgePoints(chart, edge, nodes);
            var labelPoint = IsGeographicCurve(chart, edge, nodes)
                ? QuadraticPoint(points[0], GeographicCurveControlPoint(chart, edge, nodes, points), points[points.Count - 1], 0.5)
                : EdgeLabelPoint(points);
            var maxText = Math.Max(label.Length, Math.Max(secondary.Length, tertiary.Length));
            var lineCount = (string.IsNullOrWhiteSpace(label) ? 0 : 1) + (string.IsNullOrWhiteSpace(secondary) ? 0 : 1) + (string.IsNullOrWhiteSpace(tertiary) ? 0 : 1);
            var width = Math.Max(48, maxText * 7.2 + 18);
            var avoidOwnRoute = IsMonitoringDashboardStyle(options) && lineCount > 0;
            var height = EdgeLabelHeight(lineCount, options);
            var obstacles = new List<LabelBox>(nodeBoxes.Count + (options.IncludeGroups && options.IncludeGroupLabels ? groupHeaderBoxes.Count : 0) + (options.IncludeGroups ? groupBoxes.Count : 0));
            obstacles.AddRange(nodeBoxes);
            if (options.IncludeGroups && options.IncludeGroupLabels) obstacles.AddRange(groupHeaderBoxes);
            if (options.IncludeGroups && IsInterGroupEdge(edge, nodes)) obstacles.AddRange(groupBoxes);
            var preferredGroup = options.IncludeGroups ? SameGroupBox(edge, nodes, chart.Groups) : null;
            var currentOrder = edgeRenderOrders.TryGetValue(edge, out var order) ? order : 0;
            var routeClearance = avoidOwnRoute ? AutomaticRouteClearanceOffset(points, labelPoint, width, height, lineCount, chart.Viewport, chart.Legend) : new ChartPoint(0, 0);
            var baseX = labelPoint.X + edge.LabelOffsetX + (Math.Abs(edge.LabelOffsetX) < 0.000001 ? routeClearance.X : 0);
            var baseY = labelPoint.Y + edge.LabelOffsetY + (Math.Abs(edge.LabelOffsetY) < 0.000001 ? routeClearance.Y : 0);
            var center = PlaceLabel(edge, baseX, baseY, width, height, chart, options, obstacles, placed, edgeSegments, edgeRenderOrders, currentOrder, avoidOwnRoute, IsMonitoringDashboardStyle(options), preferredGroup);
            var box = LabelBox.FromCenter(center.X, center.Y, width, height);
            placed.Add(box);
            layouts.Add(new TopologyEdgeLabelLayout(edge, label, secondary, tertiary, center.X, center.Y, width, height));
        }

        return layouts;
    }

    private static LabelBox EdgeLabelNodeObstacle(TopologyNode node, TopologyRenderOptions options, double padding) {
        if (EffectiveNodeDisplayMode(node, options) != TopologyNodeDisplayMode.Icon || !options.IncludeNodeLabels || !options.IncludeIconLabels) return LabelBox.FromNode(node, padding);
        var labelWidth = IconLabelPlateWidth(node);
        var labelY = IconLabelPlateY(node);
        var centerX = CenterX(node);
        return LabelBox.FromBounds(
            Math.Min(node.X - padding, centerX - labelWidth / 2 - padding),
            Math.Min(node.Y - padding, labelY - padding),
            Math.Max(node.X + node.Width + padding, centerX + labelWidth / 2 + padding),
            Math.Max(node.Y + node.Height + padding, labelY + 15 + padding));
    }

    private static ChartPoint PlaceLabel(TopologyEdge edge, double baseX, double baseY, double width, double height, TopologyChart chart, TopologyRenderOptions options, IReadOnlyList<LabelBox> nodeBoxes, IReadOnlyList<LabelBox> placedLabels, IReadOnlyList<EdgeSegment> edgeSegments, IReadOnlyDictionary<TopologyEdge, int> edgeRenderOrders, int currentRenderOrder, bool avoidOwnRoute, bool monitoringStyle, LabelBox? preferredGroup) {
        var candidates = LabelCandidates(baseX, baseY, avoidOwnRoute);
        ChartPoint? best = null;
        var bestScore = double.MaxValue;
        var placedPadding = monitoringStyle ? 8 : 0;
        foreach (var candidate in candidates) {
            var clamped = ClampLabel(candidate, width, height, chart, options);
            var box = LabelBox.FromCenter(clamped.X, clamped.Y, width, height);
            var score = OverlapScore(box, nodeBoxes) * 4 +
                OverlapScore(box, placedLabels, placedPadding) * (monitoringStyle ? 10 : 2) +
                EdgeOverlapScore(edge, box, edgeSegments, edgeRenderOrders, currentRenderOrder, avoidOwnRoute) * 8 +
                Distance(new ChartPoint(baseX, baseY), clamped) * 0.05 +
                SameGroupPenalty(preferredGroup, clamped, monitoringStyle);
            if (score <= 0.0001) return clamped;
            if (score < bestScore) {
                best = clamped;
                bestScore = score;
            }
        }

        return best ?? ClampLabel(new ChartPoint(baseX, baseY), width, height, chart, options);
    }

    private static List<EdgeSegment> EdgeSegments(TopologyChart chart, IReadOnlyDictionary<string, TopologyNode> nodes) {
        var segments = new List<EdgeSegment>();
        foreach (var edge in chart.Edges) {
            if (!nodes.ContainsKey(edge.SourceNodeId) || !nodes.ContainsKey(edge.TargetNodeId)) continue;
            var points = EdgePoints(chart, edge, nodes);
            if (IsGeographicCurve(chart, edge, nodes)) points = GeographicCurveSamplePoints(chart, edge, nodes, points, 12);
            for (var i = 0; i < points.Count - 1; i++) segments.Add(new EdgeSegment(edge, points[i], points[i + 1]));
        }

        return segments;
    }

    private static bool IsInterGroupEdge(TopologyEdge edge, IReadOnlyDictionary<string, TopologyNode> nodes) {
        if (!nodes.TryGetValue(edge.SourceNodeId, out var source) || !nodes.TryGetValue(edge.TargetNodeId, out var target)) return false;
        if (string.IsNullOrWhiteSpace(source.GroupId) || string.IsNullOrWhiteSpace(target.GroupId)) return false;
        return !string.Equals(source.GroupId, target.GroupId, StringComparison.Ordinal);
    }

    private static LabelBox? SameGroupBox(TopologyEdge edge, IReadOnlyDictionary<string, TopologyNode> nodes, IReadOnlyList<TopologyGroup> groups) {
        if (!nodes.TryGetValue(edge.SourceNodeId, out var source) || !nodes.TryGetValue(edge.TargetNodeId, out var target)) return null;
        if (string.IsNullOrWhiteSpace(source.GroupId) || !string.Equals(source.GroupId, target.GroupId, StringComparison.Ordinal)) return null;
        var group = groups.FirstOrDefault(item => string.Equals(item.Id, source.GroupId, StringComparison.Ordinal));
        return group == null ? null : LabelBox.FromGroup(group, -8);
    }

    private static double SameGroupPenalty(LabelBox? preferredGroup, ChartPoint center, bool monitoringStyle) {
        if (!monitoringStyle || !preferredGroup.HasValue) return 0;
        return preferredGroup.Value.Contains(center) ? 0 : 20000;
    }

    private static double EdgeLabelHeight(int lineCount, TopologyRenderOptions options) {
        if (lineCount <= 1) return 22;
        if (IsMonitoringDashboardStyle(options) && !options.IncludeEdgeLabelBackplates) return lineCount == 2 ? 46 : 62;
        return lineCount == 2 ? 38 : 52;
    }

    private static ChartPoint AutomaticRouteClearanceOffset(IReadOnlyList<ChartPoint> points, ChartPoint labelPoint, double width, double height, int lineCount, TopologyViewport viewport, TopologyLegend? legend) {
        var segment = NearestRouteSegment(points, labelPoint);
        var dx = segment.End.X - segment.Start.X;
        var dy = segment.End.Y - segment.Start.Y;
        var horizontalClearance = lineCount <= 1 ? 26 : 46;
        var verticalClearance = lineCount <= 1 ? 24 : 54;
        if (Math.Abs(dy) > Math.Abs(dx)) {
            var leftSpace = labelPoint.X - viewport.Padding - width / 2;
            var rightSpace = viewport.Width - viewport.Padding - (labelPoint.X + width / 2);
            return new ChartPoint(rightSpace >= leftSpace ? horizontalClearance : -horizontalClearance, 0);
        }

        var topSpace = labelPoint.Y - viewport.Padding - height / 2;
        var bottomSpace = viewport.Height - viewport.Padding - LegendReservedHeight(legend, viewport) - (labelPoint.Y + height / 2);
        return new ChartPoint(0, bottomSpace >= topSpace ? verticalClearance : -verticalClearance);
    }

    private static (ChartPoint Start, ChartPoint End) NearestRouteSegment(IReadOnlyList<ChartPoint> points, ChartPoint labelPoint) {
        if (points.Count < 2) return (labelPoint, labelPoint);
        var bestStart = points[0];
        var bestEnd = points[1];
        var bestDistance = double.MaxValue;
        for (var i = 0; i < points.Count - 1; i++) {
            var start = points[i];
            var end = points[i + 1];
            var midpoint = new ChartPoint((start.X + end.X) / 2, (start.Y + end.Y) / 2);
            var distance = Distance(midpoint, labelPoint);
            if (distance >= bestDistance) continue;
            bestStart = start;
            bestEnd = end;
            bestDistance = distance;
        }

        return (bestStart, bestEnd);
    }

    private static IEnumerable<ChartPoint> LabelCandidates(double x, double y, bool preferRouteClearance) {
        yield return new ChartPoint(x, y);
        var vertical = preferRouteClearance ? 42 : 30;
        var diagonalVertical = preferRouteClearance ? 44 : 30;
        var farVertical = preferRouteClearance ? 74 : 60;
        var offsets = new[] {
            new ChartPoint(0, -vertical),
            new ChartPoint(0, vertical),
            new ChartPoint(44, 0),
            new ChartPoint(-44, 0),
            new ChartPoint(54, -diagonalVertical),
            new ChartPoint(-54, -diagonalVertical),
            new ChartPoint(54, diagonalVertical),
            new ChartPoint(-54, diagonalVertical),
            new ChartPoint(0, -farVertical),
            new ChartPoint(0, farVertical),
            new ChartPoint(82, 0),
            new ChartPoint(-82, 0),
            new ChartPoint(84, -farVertical),
            new ChartPoint(-84, -farVertical),
            new ChartPoint(84, farVertical),
            new ChartPoint(-84, farVertical),
            new ChartPoint(0, -104),
            new ChartPoint(0, 104),
            new ChartPoint(124, 0),
            new ChartPoint(-124, 0),
            new ChartPoint(0, -142),
            new ChartPoint(0, 142),
            new ChartPoint(0, -184),
            new ChartPoint(0, 184),
            new ChartPoint(148, -112),
            new ChartPoint(-148, -112),
            new ChartPoint(148, 112),
            new ChartPoint(-148, 112),
            new ChartPoint(186, 0),
            new ChartPoint(-186, 0)
        };
        foreach (var offset in offsets) yield return new ChartPoint(x + offset.X, y + offset.Y);
    }

    private static ChartPoint ClampLabel(ChartPoint point, double width, double height, TopologyChart chart, TopologyRenderOptions options) {
        var viewport = chart.Viewport;
        var panelPadding = CanvasSurfaceInset(chart, options);
        var minX = viewport.Padding + width / 2;
        var maxX = Math.Max(minX, viewport.Width - viewport.Padding - width / 2);
        var minY = viewport.Padding + height / 2;
        var maxY = Math.Max(minY, viewport.Height - viewport.Padding - LegendReservedHeight(chart.Legend, viewport) - panelPadding - height / 2);
        return new ChartPoint(Math.Min(maxX, Math.Max(minX, point.X)), Math.Min(maxY, Math.Max(minY, point.Y)));
    }

    private static double OverlapScore(LabelBox box, IReadOnlyList<LabelBox> others) {
        var score = 0.0;
        foreach (var other in others) score += box.OverlapArea(other);
        return score;
    }

    private static double OverlapScore(LabelBox box, IReadOnlyList<LabelBox> others, double padding) {
        if (padding <= 0) return OverlapScore(box, others);
        var score = 0.0;
        foreach (var other in others) score += box.OverlapArea(other.Expand(padding));
        return score;
    }

    private static double EdgeOverlapScore(TopologyEdge edge, LabelBox box, IReadOnlyList<EdgeSegment> segments, IReadOnlyDictionary<TopologyEdge, int> edgeRenderOrders, int currentRenderOrder, bool avoidOwnRoute) {
        var score = 0.0;
        var expanded = box.Expand(avoidOwnRoute ? 10 : 5);
        foreach (var segment in segments) {
            if (ReferenceEquals(edge, segment.Edge) && !avoidOwnRoute) continue;
            if (!ReferenceEquals(edge, segment.Edge) && edgeRenderOrders.TryGetValue(segment.Edge, out var segmentOrder) && segmentOrder < currentRenderOrder) continue;
            if (expanded.Intersects(segment.Start, segment.End)) score += ReferenceEquals(edge, segment.Edge) ? 96 : 120;
        }

        return score;
    }
}

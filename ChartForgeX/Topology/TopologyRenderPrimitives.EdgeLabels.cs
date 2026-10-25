using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    internal static int EdgeLabelRows(string? value, TopologyRenderOptions options) => string.IsNullOrWhiteSpace(value) ? 0
        : TopologyTextLayout.AuthoredRowCount(value, options.AllowMultilineNodeLabels);

    internal static int EdgeLabelRows(string? primary, string? secondary, string? tertiary, TopologyRenderOptions options) =>
        EdgeLabelRows(primary, options) + EdgeLabelRows(secondary, options) + EdgeLabelRows(tertiary, options);

    internal static double EdgeLabelTextWidth(string? primary, string? secondary, string? tertiary, TextMeasurementContext? measurement,
        double scale = 1, bool allowMultiline = true) {
        double Width(string? value, double size, bool bold) => value == null ? 0 : TopologyTextLayout.AuthoredRows(value, allowMultiline)
            .Max(row => EstimateTextWidth(row, size * scale, bold, measurement));
        return Math.Max(48 * scale, Math.Max(Width(primary, 12, true), Math.Max(Width(secondary, 10, false), Width(tertiary, 10, false))) + 18 * scale);
    }

    public static List<TopologyEdgeLabelLayout> EdgeLabelLayouts(TopologyChart chart, TopologyRenderOptions options) {
        var nodes = TopologyGroupEndpoints.Resolve(chart);
        var nodeBoxes = chart.Nodes
            .Where(node => EffectiveNodeDisplayMode(node, options) != TopologyNodeDisplayMode.Hidden)
            .Select(node => EdgeLabelNodeObstacle(node, options, 8))
            .ToList();
        var groupHeaderBoxes = chart.Groups.SelectMany(group => TopologyGroupHeader.PaintedParts(chart, group, options))
            .Select(header => LabelBox.FromBounds(header.Left - 8, header.Top - 8, header.Right + 8, header.Bottom + 8)).ToList();
        var groupBoxes = chart.Groups.Select(group => LabelBox.FromGroup(group, 8)).ToList();
        List<EdgeSegment>? edgeSegments = null;
        var edgeRenderOrders = EdgeRenderOrderMap(chart, options);
        var placed = new List<LabelBox>();
        var layouts = new List<TopologyEdgeLabelLayout>();
        List<LabelBox>? routeObstacles = null;

        foreach (var edge in OrderedEdgesForLabelPlacement(chart, options)) {
            var label = EdgeLabel(edge, options.EdgeLabelMetricKey, edge.Label);
            var secondary = EdgeLabel(edge, options.EdgeSecondaryLabelMetricKey, edge.SecondaryLabel);
            var tertiary = EdgeLabel(edge, options.EdgeTertiaryLabelMetricKey, edge.TertiaryLabel);
            if (string.IsNullOrWhiteSpace(label) && string.IsNullOrWhiteSpace(secondary) && string.IsNullOrWhiteSpace(tertiary)) continue;
            if (!nodes.ContainsKey(edge.SourceNodeId) || !nodes.ContainsKey(edge.TargetNodeId)) continue;

            if (TopologyRenderRouteCache.Geometry(chart, edge)?.MainLabel is { } capturedLabel) {
                var capturedBounds = capturedLabel.Bounds; var capturedAnchor = capturedLabel.Anchor;
                placed.Add(LabelBox.FromBounds(capturedBounds.Left, capturedBounds.Top, capturedBounds.Right, capturedBounds.Bottom));
                layouts.Add(new TopologyEdgeLabelLayout(edge, label, secondary, tertiary, capturedBounds.X + capturedBounds.Width / 2, capturedBounds.Y + capturedBounds.Height / 2,
                    capturedBounds.Width, capturedBounds.Height, capturedAnchor.X, capturedAnchor.Y));
                continue;
            }
            // Empty labels must not route the whole chart before viewport normalization has finished.
            edgeSegments ??= EdgeSegments(chart, nodes);

            var points = EdgePoints(chart, edge, nodes);
            if (edge.Interpolation.HasValue) points = RenderedEdgeSamplePoints(chart, edge, nodes, points);
            var labelPoint = IsGeographicCurve(chart, edge, nodes)
                ? QuadraticPoint(points[0], GeographicCurveControlPoint(chart, edge, nodes, points), points[points.Count - 1], 0.5)
                : EdgeLabelPoint(points);
            var units = TopologyPreparedEdgePlan.Units(edge);
            var lineCount = EdgeLabelRows(label, secondary, tertiary, options);
            var width = EdgeLabelTextWidth(label, secondary, tertiary, options.TextMeasurement, options.ResolvedEdgeLabelScale * units, options.AllowMultilineNodeLabels);
            var avoidOwnRoute = IsMonitoringDashboardStyle(options) && lineCount > 0;
            var height = EdgeLabelHeight(lineCount, options) * units;
            if (edge.TextStyle?.HasOverrides == true) {
                var sourceMeasurement = options.TextMeasurement ?? new TextMeasurementContext(chart.Theme.Typography.ToFontSpec(), options.TextMeasurementMode);
                var size = edge.TextStyle.FontSize ?? chart.Theme.Typography.DataLabelSize * units;
                var measuredWidth = 0d; var measuredHeight = 0d;
                var labels = new[] { label, secondary, tertiary };
                for (var index = 0; index < labels.Length; index++) {
                    var value = labels[index]; if (string.IsNullOrWhiteSpace(value)) continue;
                    var weight = index == 0 ? 600 : 400;
                    var style = TopologyTextPolicy.Resolve(edge.TextStyle, sourceMeasurement.Font, size, weight);
                    var measurement = TopologyTextPolicy.Measurement(edge.TextStyle, sourceMeasurement, weight);
                    var rows = TopologyTextLayout.AuthoredRows(value!, options.AllowMultilineNodeLabels);
                    var fontSize = size * (index == 0 ? 1 : .85);
                    measuredWidth = Math.Max(measuredWidth, rows.Max(row => measurement.MeasureMetrics(row, fontSize, style.Font.Weight).Width));
                    measuredHeight += rows.Length * measurement.MeasureMetrics("Ag", fontSize, style.Font.Weight).LineHeight;
                }
                width = Math.Max(width, measuredWidth + 18 * units); height = Math.Max(height, measuredHeight + 4 * units);
            }
            var inlineExtent = InlineEdgeLabelExtent(chart, edge, label, secondary, tertiary, options, width, height);
            width = inlineExtent.Width; height = inlineExtent.Height;
            if (edge.LabelPosition is TopologyLabelPosition position) {
                var explicitCenter = ResolveExplicitLabelCenter(chart, options, edge, position, label, secondary, tertiary, width, height, lineCount);
                var explicitAnchor = EdgeLabelAnchor(edge, nodes, labelPoint, explicitCenter);
                placed.Add(LabelBox.FromCenter(explicitCenter.X, explicitCenter.Y, width, height));
                layouts.Add(new TopologyEdgeLabelLayout(edge, label, secondary, tertiary, explicitCenter.X, explicitCenter.Y, width, height, explicitAnchor.X, explicitAnchor.Y));
                continue;
            }
            if (PlacesLabelAlongRoute(chart, options, edge)) {
                routeObstacles ??= RouteLabelObstacles(chart, options);
                if (PlaceLabelAlongRoute(chart, options, edge, points, width, height, lineCount, routeObstacles, placed, edgeSegments) is { } alongRoute) {
                    var alongCenter = alongRoute.Center; var alongAnchor = alongRoute.Anchor;
                    placed.Add(LabelBox.FromCenter(alongCenter.X, alongCenter.Y, width, height));
                    layouts.Add(new TopologyEdgeLabelLayout(edge, label, secondary, tertiary, alongCenter.X, alongCenter.Y, width, height, alongAnchor.X, alongAnchor.Y));
                    continue;
                }
            }
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
            var anchor = EdgeLabelAnchor(edge, nodes, labelPoint, center);
            var box = LabelBox.FromCenter(center.X, center.Y, width, height);
            placed.Add(box);
            layouts.Add(new TopologyEdgeLabelLayout(edge, label, secondary, tertiary, center.X, center.Y, width, height, anchor.X, anchor.Y));
        }

        return layouts;
    }

    private static ChartPoint EdgeLabelAnchor(TopologyEdge edge, IReadOnlyDictionary<string, TopologyNode> nodes, ChartPoint routeAnchor, ChartPoint labelCenter) {
        if (!string.IsNullOrWhiteSpace(edge.LabelAnchorNodeId) && nodes.TryGetValue(edge.LabelAnchorNodeId!, out var anchorNode)) return EdgeLabelNodeBoundaryAnchor(anchorNode, labelCenter);
        return edge.HasLabelAnchorOverride ? new ChartPoint(edge.LabelAnchorX, edge.LabelAnchorY) : routeAnchor;
    }

    private static ChartPoint EdgeLabelNodeBoundaryAnchor(TopologyNode node, ChartPoint labelCenter) {
        var left = node.X;
        var right = node.X + node.Width;
        var top = node.Y;
        var bottom = node.Y + node.Height;
        var centerX = node.X + node.Width / 2;
        var centerY = node.Y + node.Height / 2;
        var dx = labelCenter.X - centerX;
        var dy = labelCenter.Y - centerY;
        if (Math.Abs(dx) < 0.000001 && Math.Abs(dy) < 0.000001) return new ChartPoint(centerX, centerY);
        var tx = Math.Abs(dx) < 0.000001 ? double.PositiveInfinity : ((dx > 0 ? right : left) - centerX) / dx;
        var ty = Math.Abs(dy) < 0.000001 ? double.PositiveInfinity : ((dy > 0 ? bottom : top) - centerY) / dy;
        var t = Math.Min(tx, ty);
        return new ChartPoint(centerX + dx * t, centerY + dy * t);
    }

    private static LabelBox EdgeLabelNodeObstacle(TopologyNode node, TopologyRenderOptions options, double padding) {
        if (TopologyNodeFootprint.ExplicitOccupiedBounds(node, options) is {} occupied)
            return LabelBox.FromBounds(occupied.Left - padding, occupied.Top - padding, occupied.Right + padding, occupied.Bottom + padding);
        if (TopologyActorCaption.Applies(node, options)) {
            var caption = TopologyActorCaption.Prepare(node, options).Bounds;
            return LabelBox.FromBounds(Math.Min(node.X, caption.Left) - padding, Math.Min(node.Y, caption.Top) - padding,
                Math.Max(node.X + node.Width, caption.Right) + padding, Math.Max(node.Y + node.Height, caption.Bottom) + padding);
        }
        if (EffectiveNodeDisplayMode(node, options) != TopologyNodeDisplayMode.Icon || !options.IncludeNodeLabels || !options.IncludeIconLabels) return LabelBox.FromNode(node, padding);
        var labelWidth = IconLabelPlateWidth(node, options.TextMeasurement, options.ResolvedIconLabelFontSize);
        var labelY = IconLabelPlateY(node);
        var centerX = CenterX(node);
        return LabelBox.FromBounds(
            Math.Min(node.X - padding, centerX - labelWidth / 2 - padding),
            Math.Min(node.Y - padding, labelY - padding),
            Math.Max(node.X + node.Width + padding, centerX + labelWidth / 2 + padding),
            Math.Max(node.Y + node.Height + padding, labelY + IconLabelPlateHeight(node, options.TextMeasurement, options.ResolvedIconLabelFontSize) + padding));
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
            points = RenderedEdgeSamplePoints(chart, edge, nodes, points, 12);
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

    internal static double EdgeLabelHeight(int lineCount, TopologyRenderOptions options) {
        if (lineCount <= 1) return 22 * options.ResolvedEdgeLabelScale;
        if (IsMonitoringDashboardStyle(options) && !options.IncludeEdgeLabelBackplates) return (lineCount == 2 ? 46 : 62 + Math.Max(0, lineCount - 3) * 16) * options.ResolvedEdgeLabelScale;
        return (lineCount == 2 ? 38 : 52 + Math.Max(0, lineCount - 3) * 16) * options.ResolvedEdgeLabelScale;
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

    public static bool ShouldDrawEdgeLabelLeader(TopologyEdgeLabelLayout layout, TopologyRenderOptions options) {
        if (layout == null) throw new ArgumentNullException(nameof(layout));
        if (options == null) throw new ArgumentNullException(nameof(options));
        if (!options.IncludeEdgeLabelLeaders || !options.IncludeEdgeLabels) return false;
        var end = EdgeLabelLeaderEnd(layout);
        return Distance(new ChartPoint(layout.AnchorX, layout.AnchorY), end) >= 18;
    }

    public static ChartPoint EdgeLabelLeaderEnd(TopologyEdgeLabelLayout layout) {
        if (layout == null) throw new ArgumentNullException(nameof(layout));
        var left = layout.CenterX - layout.Width / 2;
        var right = layout.CenterX + layout.Width / 2;
        var top = layout.CenterY - layout.Height / 2;
        var bottom = layout.CenterY + layout.Height / 2;
        return new ChartPoint(
            Math.Min(right, Math.Max(left, layout.AnchorX)),
            Math.Min(bottom, Math.Max(top, layout.AnchorY)));
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

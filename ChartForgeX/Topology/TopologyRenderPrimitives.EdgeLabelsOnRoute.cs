using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    // Distance between the positions tried along a run.
    private const double RouteLabelStep = 8;
    private const double RouteLabelEndMargin = 4;
    private const double RouteLabelCardMargin = 8;
    private const double MonitoringRouteLabelMargin = 10;
    private const int MonitoringRouteLabelSteps = 16;

    /// <summary>
    /// Returns true when an automatic label can follow a straight run of its own resolved route.
    /// Labels the caller positioned (an offset, an anchor point, or an anchor node) keep the classic placement.
    /// </summary>
    private static bool PlacesLabelAlongRoute(TopologyChart chart, TopologyRenderOptions options, TopologyEdge edge) =>
        Math.Abs(edge.LabelOffsetX) < 0.000001 && Math.Abs(edge.LabelOffsetY) < 0.000001 &&
        !edge.HasLabelAnchorOverride && string.IsNullOrWhiteSpace(edge.LabelAnchorNodeId) &&
        (TopologyDenseRoutePlanner.IsPlanned(chart, edge) ||
            edge.Waypoints.Count == 0 && edge.Routing != TopologyEdgeRouting.Curved);

    /// <summary>The cards, tile captions, and group headers a label on a planned route must not cover.</summary>
    private static List<LabelBox> RouteLabelObstacles(TopologyChart chart, TopologyRenderOptions options) {
        var boxes = new List<LabelBox>(chart.Nodes.Count * 2 + chart.Groups.Count);
        foreach (var node in chart.Nodes) {
            if (EffectiveNodeDisplayMode(node, options) == TopologyNodeDisplayMode.Hidden) continue;
            boxes.Add(EdgeLabelNodeObstacle(node, options, 1));
            if (TopologyExternalCaption.HasPolicy(node)) continue;
            // The card and its caption count as one block as wide as the wider of the two.
            var caption = TopologyNodeFootprint.RenderedCaption(node, options, chart.TextMeasurement);
            var center = CenterX(node);
            var half = Math.Max(node.Width, caption.Width) / 2;
            boxes.Add(LabelBox.FromBounds(center - half - 1, node.Y - 1, center + half + 1, node.Y + node.Height + caption.Height + 1));
        }

        if (!TopologyGroupHeader.IsDrawn(options)) return boxes;
        foreach (var group in chart.Groups) {
            foreach (var header in TopologyGroupHeader.PaintedParts(chart, group, options))
                boxes.Add(LabelBox.FromBounds(header.Left - 2, header.Top - 2, header.Right + 2, header.Bottom + 2));
        }

        return boxes;
    }

    /// <summary>
    /// Places a label along a run of its own route that is long enough to carry it. Monitoring labels retain normal
    /// clearance beside that run and its actual anchor. Covering a card, caption, group
    /// header, or another label costs most, then hiding another route, then crowding a card (direction markers need
    /// the room), then distance from the route midpoint. A label near the edge of the content area is moved inside it
    /// only as far as it still sits on its run. Returns null when no run is long enough.
    /// </summary>
    private static (ChartPoint Center, ChartPoint Anchor)? PlaceLabelAlongRoute(TopologyChart chart, TopologyRenderOptions options, TopologyEdge edge, IReadOnlyList<ChartPoint> points, double width, double height, int lineCount, IReadOnlyList<LabelBox> obstacles, IReadOnlyList<LabelBox> placedLabels, IReadOnlyList<EdgeSegment> edgeSegments) {
        var middle = EdgeLabelPoint(points);
        var monitoring = IsMonitoringDashboardStyle(options) && !TopologyDenseRoutePlanner.IsPlanned(chart, edge);
        (ChartPoint Center, ChartPoint Anchor)? best = null;
        var bestScore = double.MaxValue;
        for (var i = 0; i + 1 < points.Count; i++) {
            var a = points[i];
            var b = points[i + 1];
            var horizontal = Math.Abs(a.Y - b.Y) < 0.01;
            var vertical = Math.Abs(a.X - b.X) < 0.01;
            // Orthogonal routes may split a straight run at coincident elbow points. The label
            // needs the complete continuous run, while the canonical route remains unchanged.
            var end = i + 1;
            while (end + 1 < points.Count) {
                var next = points[end + 1];
                var aligned = horizontal ? Math.Abs(next.Y - a.Y) < .01 : vertical ? Math.Abs(next.X - a.X) < .01
                    : Math.Abs((next.X - a.X) * (b.Y - a.Y) - (next.Y - a.Y) * (b.X - a.X)) < Distance(a, b) * .01;
                var direction = horizontal ? (b.X - a.X) * (next.X - b.X) : vertical ? (b.Y - a.Y) * (next.Y - b.Y)
                    : (b.X - a.X) * (next.X - b.X) + (b.Y - a.Y) * (next.Y - b.Y);
                if (!aligned || direction < 0) break;
                b = next; end++;
            }
            i = end - 1;
            var length = horizontal ? Math.Abs(b.X - a.X) : vertical ? Math.Abs(b.Y - a.Y) : Distance(a, b);
            // A diagonal run carries an axis-aligned badge. Reserve its projection along that run.
            var alongSize = horizontal ? width : vertical ? height : (Math.Abs(b.X - a.X) * width + Math.Abs(b.Y - a.Y) * height) / length;
            var room = length - alongSize - RouteLabelEndMargin * 2;
            if (room < 0) continue;
            var start = (horizontal ? Math.Min(a.X, b.X) : vertical ? Math.Min(a.Y, b.Y) : 0) + alongSize / 2 + RouteLabelEndMargin;
            var steps = (int)Math.Ceiling(room / RouteLabelStep);
            if (monitoring) steps = Math.Min(steps, MonitoringRouteLabelSteps);
            var normalX = -(b.Y - a.Y) / length; var normalY = (b.X - a.X) / length;
            var normalOffset = monitoring ? Math.Max(
                Math.Abs(normalX) * (lineCount <= 1 ? 26 : 46) + Math.Abs(normalY) * (lineCount <= 1 ? 24 : 54),
                (Math.Abs(normalX) * width + Math.Abs(normalY) * height) / 2 + MonitoringRouteLabelMargin + .01) : 0;
            for (var step = 0; step <= steps; step++) {
                var along = start + (steps == 0 ? 0 : room * step / steps);
                var anchor = horizontal ? new ChartPoint(along, a.Y) : vertical ? new ChartPoint(a.X, along)
                    : new ChartPoint(a.X + (b.X - a.X) * along / length, a.Y + (b.Y - a.Y) * along / length);
                for (var side = 0; side < (monitoring ? 2 : 1); side++) {
                    var direction = side == 0 ? -1 : 1;
                    var center = ClampLabel(new ChartPoint(anchor.X + normalX * normalOffset * direction,
                        anchor.Y + normalY * normalOffset * direction), width, height, chart, options);
                    var box = LabelBox.FromCenter(center.X, center.Y, width, height);
                    if (!monitoring && !box.Intersects(a, b)) continue;
                    // Viewport fitting may reduce normal clearance, but must not slide the anchor past a run end.
                    if (monitoring && Math.Abs((center.X - anchor.X) * normalY - (center.Y - anchor.Y) * normalX) > .01) continue;
                    var routeBox = monitoring ? box.Expand(MonitoringRouteLabelMargin) : box;
                    var score = (OverlapScore(box, obstacles) + OverlapScore(box, placedLabels, monitoring ? 8 : 3)) * 100
                        + OverlapScore(box, obstacles, RouteLabelCardMargin)
                        + Distance(middle, center) * 0.05
                        + (monitoring ? 0 : JunctionPenalty(box, points, edgeSegments));
                    var crossesOwnRoute = false;
                    foreach (var segment in edgeSegments) {
                        if (!routeBox.Intersects(segment.Start, segment.End)) continue;
                        if (!ReferenceEquals(segment.Edge, edge)) score += 150;
                        else if (monitoring) { crossesOwnRoute = true; break; }
                    }
                    if (crossesOwnRoute || score >= bestScore) continue;

                    best = (center, monitoring ? anchor : center);
                    bestScore = score;
                }
            }
        }

        // Association with the route cannot justify painting over another card or its caption.
        // The classic owner can place an off-route label with a leader when no clear run exists.
        if (best.HasValue && OverlapScore(LabelBox.FromCenter(best.Value.Center.X, best.Value.Center.Y, width, height), obstacles) > 0) best = null;
        if (best.HasValue) return best;
        // A short connected stair can carry a badge even when none of its individual runs can.
        // Monitoring labels retain their normal-clearance search rather than moving onto the route.
        return monitoring ? null : PlaceLabelOnPolyline(chart, options, edge, points, width, height, obstacles, placedLabels, edgeSegments);
    }

    // A label beside a bend or a split reads as belonging to the wrong branch, so each route vertex near the box costs.
    private const double RouteLabelJunctionClearance = 10;
    private const double RouteLabelJunctionCost = 30;

    private static double JunctionPenalty(LabelBox box, IReadOnlyList<ChartPoint> ownRoute, IReadOnlyList<EdgeSegment> edgeSegments) {
        var zone = box.Expand(RouteLabelJunctionClearance);
        var first = ownRoute[0];
        var last = ownRoute[ownRoute.Count - 1];
        var cost = 0.0;
        foreach (var segment in edgeSegments) {
            foreach (var point in new[] { segment.Start, segment.End }) {
                if (Distance(point, first) < .5 || Distance(point, last) < .5) continue;
                if (zone.Contains(point)) cost += RouteLabelJunctionCost;
            }
        }

        return cost;
    }

    private static (ChartPoint Center, ChartPoint Anchor)? PlaceLabelOnPolyline(TopologyChart chart, TopologyRenderOptions options,
        TopologyEdge edge, IReadOnlyList<ChartPoint> points, double width, double height, IReadOnlyList<LabelBox> obstacles,
        IReadOnlyList<LabelBox> placedLabels, IReadOnlyList<EdgeSegment> edgeSegments) {
        if (points.Count < 2) return null;
        var anchor = EdgeLabelPoint(points);
        var center = ClampLabel(anchor, width, height, chart, options);
        if (Distance(anchor, center) > .01) return null;
        var box = LabelBox.FromCenter(center.X, center.Y, width, height);
        if (OverlapScore(box, obstacles) > 0 || OverlapScore(box, placedLabels, 3) > 0) return null;
        var ends = box.Expand(RouteLabelEndMargin);
        if (ends.Contains(points[0]) || ends.Contains(points[points.Count - 1])) return null;
        var touchesOwnRoute = false;
        foreach (var segment in edgeSegments) {
            if (!box.Intersects(segment.Start, segment.End)) continue;
            if (!ReferenceEquals(segment.Edge, edge)) return null;
            touchesOwnRoute = true;
        }
        return touchesOwnRoute ? (center, anchor) : null;
    }
}

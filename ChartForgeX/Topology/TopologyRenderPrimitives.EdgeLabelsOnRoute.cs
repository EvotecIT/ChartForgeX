using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    // Distance between the positions tried along a run.
    private const double RouteLabelStep = 8;
    private const double RouteLabelEndMargin = 4;
    private const double RouteLabelCardMargin = 8;

    /// <summary>
    /// Returns true when an automatic label can use a straight run of its own resolved route.
    /// Labels the caller positioned (an offset, an anchor point, or an anchor node) keep the classic placement.
    /// </summary>
    private static bool PlacesLabelOnRoute(TopologyChart chart, TopologyRenderOptions options, TopologyEdge edge) =>
        Math.Abs(edge.LabelOffsetX) < 0.000001 && Math.Abs(edge.LabelOffsetY) < 0.000001 &&
        !edge.HasLabelAnchorOverride && string.IsNullOrWhiteSpace(edge.LabelAnchorNodeId) &&
        (TopologyDenseRoutePlanner.IsPlanned(chart, edge) ||
            !IsMonitoringDashboardStyle(options) && edge.Waypoints.Count == 0 && edge.Routing != TopologyEdgeRouting.Curved);

    /// <summary>The cards, tile captions, and group headers a label on a planned route must not cover.</summary>
    private static List<LabelBox> RouteLabelObstacles(TopologyChart chart, TopologyRenderOptions options) {
        var boxes = new List<LabelBox>(chart.Nodes.Count * 2 + chart.Groups.Count);
        foreach (var node in chart.Nodes) {
            if (EffectiveNodeDisplayMode(node, options) == TopologyNodeDisplayMode.Hidden) continue;
            // The card and its caption count as one block as wide as the wider of the two.
            var caption = TopologyNodeFootprint.Caption(chart, node);
            var center = CenterX(node);
            var half = Math.Max(node.Width, caption.Width) / 2;
            boxes.Add(LabelBox.FromBounds(center - half - 1, node.Y - 1, center + half + 1, node.Y + node.Height + caption.Height + 1));
        }

        if (!TopologyGroupHeader.IsDrawn(options)) return boxes;
        foreach (var group in chart.Groups) {
            var header = TopologyGroupHeader.Bounds(group, options, chart.TextMeasurement);
            boxes.Add(LabelBox.FromBounds(header.Left - 2, header.Top - 2, header.Right + 2, header.Bottom + 2));
        }

        return boxes;
    }

    /// <summary>
    /// Centres a label on a run of its own route that is long enough to carry it. Covering a card, caption, group
    /// header, or another label costs most, then hiding another route, then crowding a card (direction markers need
    /// the room), then distance from the route midpoint. A label near the edge of the content area is moved inside it
    /// only as far as it still sits on its run. Returns null when no run is long enough.
    /// </summary>
    private static ChartPoint? PlaceLabelOnRoute(TopologyChart chart, TopologyRenderOptions options, TopologyEdge edge, IReadOnlyList<ChartPoint> points, double width, double height, IReadOnlyList<LabelBox> obstacles, IReadOnlyList<LabelBox> placedLabels, IReadOnlyList<EdgeSegment> edgeSegments) {
        var middle = EdgeLabelPoint(points);
        ChartPoint? best = null;
        var bestScore = double.MaxValue;
        for (var i = 0; i + 1 < points.Count; i++) {
            var a = points[i];
            var b = points[i + 1];
            var horizontal = Math.Abs(a.Y - b.Y) < 0.01;
            if (!horizontal && Math.Abs(a.X - b.X) >= 0.01) continue;
            var length = horizontal ? Math.Abs(b.X - a.X) : Math.Abs(b.Y - a.Y);
            var room = length - (horizontal ? width : height) - RouteLabelEndMargin * 2;
            if (room < 0) continue;
            var start = (horizontal ? Math.Min(a.X, b.X) : Math.Min(a.Y, b.Y)) + (horizontal ? width : height) / 2 + RouteLabelEndMargin;
            var steps = (int)Math.Ceiling(room / RouteLabelStep);
            for (var step = 0; step <= steps; step++) {
                var along = start + (steps == 0 ? 0 : room * step / steps);
                var center = horizontal ? new ChartPoint(along, a.Y) : new ChartPoint(a.X, along);
                center = ClampLabel(center, width, height, chart, options);
                var box = LabelBox.FromCenter(center.X, center.Y, width, height);
                if (!box.Intersects(a, b)) continue;
                var score = (OverlapScore(box, obstacles) + OverlapScore(box, placedLabels, 3)) * 100
                    + OverlapScore(box, obstacles, RouteLabelCardMargin)
                    + Distance(middle, center) * 0.05;
                foreach (var segment in edgeSegments) {
                    if (!ReferenceEquals(segment.Edge, edge) && box.Intersects(segment.Start, segment.End)) score += 150;
                }

                if (score >= bestScore) continue;
                best = center;
                bestScore = score;
            }
        }

        return best;
    }
}

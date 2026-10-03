using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

public static partial class TopologyLayoutDiagnostics {
    /// <summary>A crossing counts when a route runs at least this far inside the obstacle, so a touch on the border is ignored.</summary>
    private const double CrossingInset = 1;
    /// <summary>Parallel route segments closer than this are drawn on top of each other.</summary>
    private const double OverlapDistance = 2;
    /// <summary>Two routes sharing less than this in total only touch at corners or junctions.</summary>
    private const double MinimumOverlapLength = 6;
    /// <summary>How far past its end a route is extended when testing that it points at its node.</summary>
    private const double AttachmentReach = 14;

    private static void AnalyzeRoutes(TopologyLayoutDiagnosticReport report, TopologyChart chart, TopologyRenderOptions options) {
        var nodeBounds = new Dictionary<string, TopologyLayoutNodeDiagnostic>(StringComparer.Ordinal);
        foreach (var node in report.Nodes) {
            if (!nodeBounds.ContainsKey(node.Id)) nodeBounds.Add(node.Id, node);
        }

        // Hidden nodes draw nothing, and artwork nodes are backdrops that routes are allowed to pass over.
        var hidden = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in chart.Nodes) {
            if (EffectiveNodeDisplayMode(node, options) is TopologyNodeDisplayMode.Hidden or TopologyNodeDisplayMode.Artwork) hidden.Add(node.Id);
        }

        foreach (var edge in report.Edges) {
            foreach (var node in report.Nodes) {
                if (hidden.Contains(node.Id)) continue;
                var own = node.Id == edge.SourceNodeId || node.Id == edge.TargetNodeId;
                if (!own && Crosses(edge.Points, node.Bounds)) report.RouteCrossings.Add(new TopologyLayoutRouteCrossingDiagnostic(TopologyLayoutRouteCrossingKind.Node, edge.Id, node.Id));
                if (node.CaptionBounds.HasValue && Crosses(edge.Points, node.CaptionBounds.Value)) report.RouteCrossings.Add(new TopologyLayoutRouteCrossingDiagnostic(TopologyLayoutRouteCrossingKind.NodeCaption, edge.Id, node.Id));
            }

            foreach (var group in report.Groups) {
                if (group.HeaderBounds.HasValue && Crosses(edge.Points, group.HeaderBounds.Value)) report.RouteCrossings.Add(new TopologyLayoutRouteCrossingDiagnostic(TopologyLayoutRouteCrossingKind.GroupHeader, edge.Id, group.Id));
            }

            edge.SourceAttached = nodeBounds.TryGetValue(edge.SourceNodeId, out var source) && RouteEndPointsAtNode(edge.Points, fromStart: true, source.Bounds);
            edge.TargetAttached = nodeBounds.TryGetValue(edge.TargetNodeId, out var target) && RouteEndPointsAtNode(edge.Points, fromStart: false, target.Bounds);
        }

        for (var i = 0; i < report.Edges.Count; i++) {
            for (var j = i + 1; j < report.Edges.Count; j++) {
                var length = SharedLength(report.Edges[i].Points, report.Edges[j].Points);
                if (length >= MinimumOverlapLength) report.RouteOverlaps.Add(new TopologyLayoutRouteOverlapDiagnostic(report.Edges[i].Id, report.Edges[j].Id, length,
                    length <= TopologyDenseRoutePlanner.SharedTrunkLength(chart, chart.Edges[i], chart.Edges[j]) + 0.01));
            }
        }

        if (!options.IncludeEdgeLabels) return;
        var routes = new Dictionary<TopologyEdge, IReadOnlyList<ChartPoint>>();
        for (var i = 0; i < chart.Edges.Count && i < report.Edges.Count; i++) {
            if (!routes.ContainsKey(chart.Edges[i])) routes.Add(chart.Edges[i], report.Edges[i].Points);
        }

        foreach (var layout in EdgeLabelLayouts(chart, options)) {
            var bounds = new ChartRect(layout.CenterX - layout.Width / 2, layout.CenterY - layout.Height / 2, layout.Width, layout.Height);
            var distance = routes.TryGetValue(layout.Edge, out var points) ? DistanceToRoute(bounds, points) : 0;
            report.EdgeLabels.Add(new TopologyLayoutEdgeLabelDiagnostic(layout.Edge.Id, bounds, distance));
        }
    }

    private static bool Crosses(IReadOnlyList<ChartPoint> points, ChartRect bounds) {
        var left = bounds.Left + CrossingInset;
        var top = bounds.Top + CrossingInset;
        var right = bounds.Right - CrossingInset;
        var bottom = bounds.Bottom - CrossingInset;
        if (right <= left || bottom <= top) return false;
        for (var i = 0; i + 1 < points.Count; i++) {
            if (SegmentCrossesRect(points[i], points[i + 1], left, top, right, bottom)) return true;
        }

        return false;
    }

    // Liang-Barsky clipping: the segment crosses the open rectangle when a non-empty parameter interval remains.
    private static bool SegmentCrossesRect(ChartPoint a, ChartPoint b, double left, double top, double right, double bottom) {
        double t0 = 0, t1 = 1;
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        if (Math.Abs(dx) < 1e-9 && Math.Abs(dy) < 1e-9) return false;
        bool Clip(double p, double q) {
            if (Math.Abs(p) < 1e-12) return q > 0;
            var r = q / p;
            if (p < 0) {
                if (r > t1) return false;
                if (r > t0) t0 = r;
            } else {
                if (r < t0) return false;
                if (r < t1) t1 = r;
            }

            return true;
        }

        return Clip(-dx, a.X - left) && Clip(dx, right - a.X) && Clip(-dy, a.Y - top) && Clip(dy, bottom - a.Y) && t1 - t0 > 1e-9;
    }

    /// <summary>
    /// A horizontal or vertical route end is attached when its last leg, continued a little further, runs into the node;
    /// a leg that stops beside the node and runs along its side is not. Diagonal and curved ends arrive at any angle, so
    /// they are attached when they stop within reach of the node.
    /// </summary>
    internal static bool RouteEndPointsAtNode(IReadOnlyList<ChartPoint> points, bool fromStart, ChartRect node) {
        if (points.Count < 2) return false;
        var end = fromStart ? points[0] : points[points.Count - 1];
        var step = fromStart ? 1 : -1;
        var index = fromStart ? 1 : points.Count - 2;
        var previous = points[index];
        // Skip sample points that coincide with the end so the leg has a direction.
        while (Distance(previous, end) < 0.01 && index + step >= 0 && index + step < points.Count) {
            index += step;
            previous = points[index];
        }

        var length = Distance(previous, end);
        if (length < 0.01) return false;
        if (Math.Abs(end.X - previous.X) > 0.01 && Math.Abs(end.Y - previous.Y) > 0.01) return PointToRectDistance(end, node) <= AttachmentReach;
        var reach = new ChartPoint(end.X + (end.X - previous.X) / length * AttachmentReach, end.Y + (end.Y - previous.Y) / length * AttachmentReach);
        return SegmentCrossesRect(end, reach, node.Left - 1, node.Top - 1, node.Right + 1, node.Bottom + 1);
    }

    private static double SharedLength(IReadOnlyList<ChartPoint> first, IReadOnlyList<ChartPoint> second) {
        var total = 0.0;
        for (var i = 0; i + 1 < first.Count; i++) {
            for (var j = 0; j + 1 < second.Count; j++) total += SharedLength(first[i], first[i + 1], second[j], second[j + 1]);
        }

        return total;
    }

    private static double SharedLength(ChartPoint a, ChartPoint b, ChartPoint c, ChartPoint d) {
        if (Math.Abs(a.Y - b.Y) < 0.01 && Math.Abs(c.Y - d.Y) < 0.01 && Math.Abs(a.Y - c.Y) < OverlapDistance)
            return Math.Max(0, Math.Min(Math.Max(a.X, b.X), Math.Max(c.X, d.X)) - Math.Max(Math.Min(a.X, b.X), Math.Min(c.X, d.X)));
        if (Math.Abs(a.X - b.X) < 0.01 && Math.Abs(c.X - d.X) < 0.01 && Math.Abs(a.X - c.X) < OverlapDistance)
            return Math.Max(0, Math.Min(Math.Max(a.Y, b.Y), Math.Max(c.Y, d.Y)) - Math.Max(Math.Min(a.Y, b.Y), Math.Min(c.Y, d.Y)));
        return 0;
    }

    private static double DistanceToRoute(ChartRect bounds, IReadOnlyList<ChartPoint> points) {
        var best = double.PositiveInfinity;
        for (var i = 0; i + 1 < points.Count; i++) {
            if (SegmentCrossesRect(points[i], points[i + 1], bounds.Left, bounds.Top, bounds.Right, bounds.Bottom)) return 0;
            best = Math.Min(best, SegmentToRectDistance(points[i], points[i + 1], bounds));
        }

        return double.IsPositiveInfinity(best) ? 0 : best;
    }

    // The closest approach between a segment and a rectangle it does not cross is at a segment end or a rectangle corner.
    private static double SegmentToRectDistance(ChartPoint a, ChartPoint b, ChartRect rect) {
        var best = Math.Min(PointToRectDistance(a, rect), PointToRectDistance(b, rect));
        best = Math.Min(best, PointToSegmentDistance(new ChartPoint(rect.Left, rect.Top), a, b));
        best = Math.Min(best, PointToSegmentDistance(new ChartPoint(rect.Right, rect.Top), a, b));
        best = Math.Min(best, PointToSegmentDistance(new ChartPoint(rect.Left, rect.Bottom), a, b));
        return Math.Min(best, PointToSegmentDistance(new ChartPoint(rect.Right, rect.Bottom), a, b));
    }

    private static double PointToRectDistance(ChartPoint point, ChartRect rect) {
        var dx = Math.Max(0, Math.Max(rect.Left - point.X, point.X - rect.Right));
        var dy = Math.Max(0, Math.Max(rect.Top - point.Y, point.Y - rect.Bottom));
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static double PointToSegmentDistance(ChartPoint point, ChartPoint a, ChartPoint b) {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        var lengthSquared = dx * dx + dy * dy;
        var t = lengthSquared < 1e-12 ? 0 : Math.Max(0, Math.Min(1, ((point.X - a.X) * dx + (point.Y - a.Y) * dy) / lengthSquared));
        return Distance(point, new ChartPoint(a.X + dx * t, a.Y + dy * t));
    }

    private static double Distance(ChartPoint a, ChartPoint b) {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}

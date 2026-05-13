using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    public static bool ShouldRoundEdgeCorners(TopologyEdge edge, IReadOnlyList<ChartPoint> points, TopologyRenderOptions options) {
        if (options.EdgeCornerStyle != TopologyEdgeCornerStyle.Rounded || options.EdgeCornerRadius <= 0 || points.Count < 3) return false;
        return edge.Routing is TopologyEdgeRouting.Orthogonal or TopologyEdgeRouting.ObstacleAvoidingOrthogonal || edge.Waypoints.Count > 0;
    }

    public static List<ChartPoint> RoundedOrthogonalRoutePoints(IReadOnlyList<ChartPoint> points, double radius) {
        if (points.Count < 3 || radius <= 0) return new List<ChartPoint>(points);
        var result = new List<ChartPoint> { points[0] };
        for (var i = 1; i < points.Count - 1; i++) {
            var previous = points[i - 1];
            var corner = points[i];
            var next = points[i + 1];
            var incoming = Unit(corner.X - previous.X, corner.Y - previous.Y);
            var outgoing = Unit(next.X - corner.X, next.Y - corner.Y);
            var inLength = Distance(previous, corner);
            var outLength = Distance(corner, next);
            var bend = Math.Min(radius, Math.Min(inLength, outLength) / 2);
            if (bend <= 0.5 || !IsOrthogonalBend(incoming, outgoing)) {
                result.Add(corner);
                continue;
            }

            var before = new ChartPoint(corner.X - incoming.X * bend, corner.Y - incoming.Y * bend);
            var after = new ChartPoint(corner.X + outgoing.X * bend, corner.Y + outgoing.Y * bend);
            ReplaceOrAdd(result, before);
            for (var step = 1; step <= 4; step++) result.Add(QuadraticPoint(before, corner, after, step / 4.0));
        }

        ReplaceOrAdd(result, points[points.Count - 1]);
        return result;
    }

    private static bool IsOrthogonalBend(ChartPoint incoming, ChartPoint outgoing) =>
        Math.Abs(incoming.X * outgoing.X + incoming.Y * outgoing.Y) < 0.0001;

    private static ChartPoint Unit(double dx, double dy) {
        var length = Math.Sqrt(dx * dx + dy * dy);
        return length < 0.0001 ? new ChartPoint(0, 0) : new ChartPoint(dx / length, dy / length);
    }

    private static void ReplaceOrAdd(List<ChartPoint> points, ChartPoint point) {
        if (points.Count > 0 && Distance(points[points.Count - 1], point) < 0.001) points[points.Count - 1] = point;
        else points.Add(point);
    }
}

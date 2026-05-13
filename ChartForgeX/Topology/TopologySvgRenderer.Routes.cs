using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;

namespace ChartForgeX.Topology;

public sealed partial class TopologySvgRenderer {
    private static string RoundedEdgePath(IReadOnlyList<ChartPoint> points, double radius) {
        var path = new SvgPathDataBuilder(points.Count * 20).MoveTo(points[0]);
        for (var i = 1; i < points.Count - 1; i++) {
            var previous = points[i - 1];
            var corner = points[i];
            var next = points[i + 1];
            var inLength = Distance(previous, corner);
            var outLength = Distance(corner, next);
            if (inLength <= 0.0001 || outLength <= 0.0001) {
                path.LineTo(corner);
                continue;
            }

            var bend = Math.Min(radius, Math.Min(inLength, outLength) / 2);
            if (bend <= 0.5 || !IsOrthogonalCorner(previous, corner, next)) {
                path.LineTo(corner);
                continue;
            }

            var incomingX = (corner.X - previous.X) / inLength;
            var incomingY = (corner.Y - previous.Y) / inLength;
            var outgoingX = (next.X - corner.X) / outLength;
            var outgoingY = (next.Y - corner.Y) / outLength;
            path.LineTo(corner.X - incomingX * bend, corner.Y - incomingY * bend);
            path.QuadraticTo(corner.X, corner.Y, corner.X + outgoingX * bend, corner.Y + outgoingY * bend);
        }

        return path.LineTo(points[points.Count - 1]).Build();
    }

    private static bool IsOrthogonalCorner(ChartPoint previous, ChartPoint corner, ChartPoint next) {
        var dx1 = corner.X - previous.X;
        var dy1 = corner.Y - previous.Y;
        var dx2 = next.X - corner.X;
        var dy2 = next.Y - corner.Y;
        return Math.Abs(dx1 * dx2 + dy1 * dy2) < 0.0001;
    }

    private static double Distance(ChartPoint first, ChartPoint second) {
        var dx = second.X - first.X;
        var dy = second.Y - first.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}

using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal sealed partial class ChartPath {
    /// <summary>Tests exact line/cubic centerline contact with an inclusive rectangle before the shared native rectangular clip paints it.</summary>
    /// <remarks>Curve boundary parameters reuse the bounded shared path polynomial owner; this does not replace curves with flattened lines.</remarks>
    internal bool IntersectsRectangle(ChartRect rectangle) {
        bool Contains(ChartPoint point) => point.X >= rectangle.Left && point.X <= rectangle.Right && point.Y >= rectangle.Top && point.Y <= rectangle.Bottom;
        var current = default(ChartPoint); var hasCurrent = false;
        foreach (var command in Commands) {
            var end = new ChartPoint(command.X, command.Y);
            if (command.Kind == ChartPathCommandKind.MoveTo) { current = end; hasCurrent = true; if (Contains(end)) return true; continue; }
            if (!hasCurrent) throw new InvalidOperationException("Rectangle intersection requires a starting point.");
            if (command.Kind == ChartPathCommandKind.LineTo) {
                var x1 = current.X; var y1 = current.Y; var x2 = end.X; var y2 = end.Y;
                if (ChartPatternLineGeometry.ClipLineToRect(ref x1, ref y1, ref x2, ref y2, rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom)) return true;
            } else {
                var start = current; var first = new ChartPoint(command.Control1X, command.Control1Y); var second = new ChartPoint(command.Control2X, command.Control2Y);
                var roots = RectangleParameters(new EndpointSegment(start, first, second, end, true), rectangle);
                foreach (var t in roots) if (Contains(CubicPoint(start, first, second, end, t))) return true;
                for (var index = 1; index < roots.Count; index++)
                    if (Contains(CubicPoint(start, first, second, end, roots[index - 1] + (roots[index] - roots[index - 1]) / 2))) return true;
            }
            current = end;
        }
        return false;
    }

    /// <summary>Finds exact native line/cubic boundary parameters for contact tests and lossless rectangle slices.</summary>
    private static List<double> RectangleParameters(EndpointSegment segment, ChartRect rectangle) {
        var roots = new List<double> { 0, 1 };
        void Crossings(double p0, double p1, double p2, double p3, double boundary) => roots.AddRange(EndpointPolynomialRoots(segment.Cubic
            ? new[] { p0 - boundary, 3 * (p1 - p0), 3 * (p0 - 2 * p1 + p2), p3 - p0 + 3 * (p1 - p2) }
            : new[] { p0 - boundary, p3 - p0 }));
        Crossings(segment.Start.X, segment.First.X, segment.Second.X, segment.End.X, rectangle.Left);
        Crossings(segment.Start.X, segment.First.X, segment.Second.X, segment.End.X, rectangle.Right);
        Crossings(segment.Start.Y, segment.First.Y, segment.Second.Y, segment.End.Y, rectangle.Top);
        Crossings(segment.Start.Y, segment.First.Y, segment.Second.Y, segment.End.Y, rectangle.Bottom);
        roots.Sort(); return roots;
    }
}

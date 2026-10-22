using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal sealed partial class ChartPath {
    /// <summary>Measures the actual straight, step and cubic centerline inside an inclusive category strip.</summary>
    /// <remarks>Cubic crossings are isolated on derivative-monotone spans, not sampled or connected across authored gaps.</remarks>
    internal ChartRect? BoundsInXRange(double minimum, double maximum) {
        var left = double.PositiveInfinity; var right = double.NegativeInfinity;
        var top = double.PositiveInfinity; var bottom = double.NegativeInfinity;
        void Include(ChartPoint point) {
            left = Math.Min(left, point.X); right = Math.Max(right, point.X);
            top = Math.Min(top, point.Y); bottom = Math.Max(bottom, point.Y);
        }
        void IfInside(ChartPoint point) { if (point.X >= minimum && point.X <= maximum) Include(point); }
        var current = default(ChartPoint);
        foreach (var command in Commands) {
            var end = new ChartPoint(command.X, command.Y);
            if (command.Kind == ChartPathCommandKind.MoveTo) IfInside(end);
            else if (command.Kind == ChartPathCommandKind.LineTo) {
                if (Math.Max(current.X, end.X) >= minimum && Math.Min(current.X, end.X) <= maximum) {
                    if (current.X == end.X) { Include(current); Include(end); }
                    else {
                        var first = Math.Max(minimum, Math.Min(current.X, end.X));
                        var last = Math.Min(maximum, Math.Max(current.X, end.X));
                        Include(new ChartPoint(first, ChartMath.InterpolateOrExtrapolateRange(current.Y, end.Y,
                            current.X < end.X ? ChartMath.Normalize(first, current.X, end.X) : 1 - ChartMath.Normalize(first, end.X, current.X))));
                        Include(new ChartPoint(last, ChartMath.InterpolateOrExtrapolateRange(current.Y, end.Y,
                            current.X < end.X ? ChartMath.Normalize(last, current.X, end.X) : 1 - ChartMath.Normalize(last, end.X, current.X))));
                    }
                }
            } else {
                var start = current; var first = new ChartPoint(command.Control1X, command.Control1Y);
                var second = new ChartPoint(command.Control2X, command.Control2Y);
                IfInside(start); IfInside(end);
                var spans = new List<double> { 0, 1 };
                CubicExtrema(start.X, first.X, second.X, end.X, t => { if (t > 0 && t < 1) spans.Add(t); });
                spans.Sort();
                for (var index = 1; index < spans.Count; index++) {
                    var from = spans[index - 1]; var to = spans[index];
                    var a = CubicPoint(start, first, second, end, from); var b = CubicPoint(start, first, second, end, to);
                    IfInside(a); IfInside(b);
                    Crossing(minimum); Crossing(maximum);
                    void Crossing(double x) {
                        if (x < Math.Min(a.X, b.X) || x > Math.Max(a.X, b.X) || a.X == b.X) return;
                        var low = from; var high = to; var increasing = b.X > a.X;
                        for (var iteration = 0; iteration < 56; iteration++) {
                            var middle = low / 2 + high / 2;
                            if ((CubicPoint(start, first, second, end, middle).X < x) == increasing) low = middle; else high = middle;
                        }
                        Include(CubicPoint(start, first, second, end, low / 2 + high / 2));
                    }
                }
                CubicExtrema(start.Y, first.Y, second.Y, end.Y,
                    t => { if (t > 0 && t < 1) IfInside(CubicPoint(start, first, second, end, t)); });
            }
            current = end;
        }
        return double.IsInfinity(left) ? null : new ChartRect(left, top, right - left, bottom - top);
    }
}

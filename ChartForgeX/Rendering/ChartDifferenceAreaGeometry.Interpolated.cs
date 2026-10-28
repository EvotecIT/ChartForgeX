using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static partial class ChartDifferenceAreaGeometry {
    /// <summary>Splits the same flattened boundaries painted by both backends, including independent step transitions and smooth crossings.</summary>
    private static IReadOnlyList<(bool Positive, ChartPath Path, int Start, int Count)> InterpolatedRegions(ChartSeries series, ChartPairedBoundaryGeometry mapped) {
        var result = new List<(bool, ChartPath, int, int)>();
        var start = 0;
        foreach (var segment in ChartPointSegments.Split(mapped.First)) {
            if (segment.Count < 2) { start += segment.Count; continue; }
            var first = ChartPairedBoundaryGeometry.BoundaryPath(series, segment, true).Flatten(12);
            var second = ChartPairedBoundaryGeometry.BoundaryPath(series, mapped.Second.GetRange(start, segment.Count), false).Flatten(12);
            if (first[0].X > first[first.Count - 1].X) first.Reverse();
            if (second[0].X > second[second.Count - 1].X) second.Reverse();
            var positions = first.Select(point => point.X).Concat(second.Select(point => point.X)).Distinct().OrderBy(x => x).ToArray();
            var a = new List<ChartPoint>(); var b = new List<ChartPoint>(); var sign = 0;
            var firstIndex = 0; var secondIndex = 0;
            for (var item = 1; item < positions.Length; item++) {
                var left = positions[item - 1]; var right = positions[item];
                var a0 = Sample(first, ref firstIndex, left, right, false); var a1 = Sample(first, ref firstIndex, left, right, true);
                var b0 = Sample(second, ref secondIndex, left, right, false); var b1 = Sample(second, ref secondIndex, left, right, true);
                var leftSign = Polarity(a0, b0); var rightSign = Polarity(a1, b1);
                if (leftSign == 0 && rightSign == 0) { Finish(); continue; }
                if (leftSign != 0 && rightSign != 0 && leftSign != rightSign) {
                    var fraction = CrossingFraction(a0.Y, b0.Y, a1.Y, b1.Y);
                    var x = Blend(left, right, fraction);
                    var cross = new ChartPoint(x, Blend(a0.Y, a1.Y, fraction) / 2 + Blend(b0.Y, b1.Y, fraction) / 2);
                    Span(a0, b0, cross, cross, leftSign); Finish(); Span(cross, cross, a1, b1, rightSign);
                } else {
                    Span(a0, b0, a1, b1, leftSign == 0 ? rightSign : leftSign);
                    if (rightSign == 0) Finish();
                }
            }
            Finish(); start += segment.Count;

            int Polarity(ChartPoint firstPoint, ChartPoint secondPoint) =>
                firstPoint.Y.CompareTo(secondPoint.Y) * (mapped.YReversed ? 1 : -1);
            void Span(ChartPoint first0, ChartPoint second0, ChartPoint first1, ChartPoint second1, int polarity) {
                if (sign != polarity) Finish();
                // Retain both sides of a held jump without introducing a seam between same-sign regions.
                if (a.Count == 0 || a[a.Count - 1].Y != first0.Y || b[b.Count - 1].Y != second0.Y) { a.Add(first0); b.Add(second0); }
                a.Add(first1); b.Add(second1); sign = polarity;
            }
            void Finish() {
                if (a.Count > 1) result.Add((sign > 0, ChartPairedBoundaryGeometry.Envelope(a, b), start, segment.Count));
                a.Clear(); b.Clear(); sign = 0;
            }
        }
        return result;
    }

    /// <summary>Evaluates a flattened edge covering this positive-width X interval; vertical step edges belong to its adjacent holds.</summary>
    private static ChartPoint Sample(IReadOnlyList<ChartPoint> points, ref int index, double left, double right, bool atRight) {
        while (index + 1 < points.Count - 1 && points[index + 1].X <= left) index++;
        var first = points[index]; var second = points[index + 1];
        var x = atRight ? right : left;
        var fraction = (x - first.X) / (second.X - first.X);
        return new ChartPoint(x, Blend(first.Y, second.Y, fraction));
    }
}

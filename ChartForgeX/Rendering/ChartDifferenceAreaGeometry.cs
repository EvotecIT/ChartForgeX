using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>Splits linear paired boundaries at ties and crossings, coalescing each signed run before either backend paints it.</summary>
internal static class ChartDifferenceAreaGeometry {
    internal static IReadOnlyList<(bool Positive, ChartPath Path)> Regions(ChartSeries series, ChartPairedBoundaryGeometry mapped) {
        var result = new List<(bool, ChartPath)>();
        var first = new List<ChartPoint>(); var second = new List<ChartPoint>();
        var sign = 0;
        for (var item = 1; item < mapped.First.Count; item++) {
            if (mapped.First[item].BreakBefore) { Finish(); continue; }
            var a = item - 1; var b = item;
            var left = series.Points[a * 2].Y.CompareTo(series.Points[a * 2 + 1].Y);
            var right = series.Points[b * 2].Y.CompareTo(series.Points[b * 2 + 1].Y);
            if (left == 0 && right == 0) { Finish(); continue; }
            if (left != 0 && right != 0 && left != right) {
                var t = CrossingFraction(series.Points[a * 2].Y, series.Points[a * 2 + 1].Y,
                    series.Points[b * 2].Y, series.Points[b * 2 + 1].Y);
                var x = Blend(mapped.First[a].X, mapped.First[b].X, t);
                var y = Blend(mapped.First[a].Y, mapped.First[b].Y, t) / 2
                    + Blend(mapped.Second[a].Y, mapped.Second[b].Y, t) / 2;
                var cross = new ChartPoint(x, y);
                Span(mapped.First[a], mapped.Second[a], cross, cross, left);
                Finish();
                Span(cross, cross, mapped.First[b], mapped.Second[b], right);
            } else {
                Span(mapped.First[a], mapped.Second[a], mapped.First[b], mapped.Second[b], left == 0 ? right : left);
                if (right == 0) Finish();
            }
        }
        Finish();
        return result;

        void Span(ChartPoint a1, ChartPoint a2, ChartPoint b1, ChartPoint b2, int polarity) {
            if (sign != polarity) Finish();
            if (first.Count == 0) { first.Add(a1); second.Add(a2); sign = polarity; }
            first.Add(b1); second.Add(b2);
        }
        void Finish() {
            if (first.Count > 1) result.Add((sign > 0, ChartPairedBoundaryGeometry.Envelope(first, second)));
            first.Clear(); second.Clear(); sign = 0;
        }
    }

    private static double CrossingFraction(double first0, double second0, double first1, double second1) {
        // Scale before subtraction: two finite observations can have an infinite raw difference.
        var scale = Math.Max(Math.Max(Math.Abs(first0), Math.Abs(second0)), Math.Max(Math.Abs(first1), Math.Abs(second1)));
        var left = Math.Abs(first0 / scale - second0 / scale);
        var right = Math.Abs(first1 / scale - second1 / scale);
        return left / (left + right);
    }

    private static double Blend(double first, double second, double fraction) => first * (1 - fraction) + second * fraction;
}

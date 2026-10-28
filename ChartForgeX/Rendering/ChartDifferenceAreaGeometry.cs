using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>Splits paired boundaries at linear crossings or step transitions, coalescing each signed run before either backend paints it.</summary>
internal static partial class ChartDifferenceAreaGeometry {
    internal static IReadOnlyList<(bool Positive, ChartPath Path, int Start, int Count)> Regions(ChartSeries series, ChartPairedBoundaryGeometry mapped) {
        if ((mapped.NonlinearValueScale && series.Interpolation == ChartInterpolation.Linear) || series.Interpolation == ChartInterpolation.Smooth || series.FirstBoundaryInterpolation.HasValue || series.SecondBoundaryInterpolation.HasValue)
            return InterpolatedRegions(series, mapped);
        if (series.Interpolation == ChartInterpolation.Step) return StepRegions(series, mapped);
        var result = new List<(bool, ChartPath, int, int)>();
        var first = new List<ChartPoint>(); var second = new List<ChartPoint>();
        var segmentStart = 0; var segmentEnd = ConnectedEnd(mapped.First, 0);
        var sign = 0;
        for (var item = 1; item < mapped.First.Count; item++) {
            if (mapped.First[item].BreakBefore) {
                Finish(); segmentStart = item; segmentEnd = ConnectedEnd(mapped.First, item); continue;
            }
            var a = item - 1; var b = item;
            var left = series.RenderPoints[a * 2].Y.CompareTo(series.RenderPoints[a * 2 + 1].Y);
            var right = series.RenderPoints[b * 2].Y.CompareTo(series.RenderPoints[b * 2 + 1].Y);
            if (left == 0 && right == 0) { Finish(); continue; }
            if (left != 0 && right != 0 && left != right) {
                var t = CrossingFraction(series.RenderPoints[a * 2].Y, series.RenderPoints[a * 2 + 1].Y,
                    series.RenderPoints[b * 2].Y, series.RenderPoints[b * 2 + 1].Y);
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
            if (first.Count > 1) result.Add((sign > 0, ChartPairedBoundaryGeometry.Envelope(first, second), segmentStart, segmentEnd - segmentStart));
            first.Clear(); second.Clear(); sign = 0;
        }
    }

    private static IReadOnlyList<(bool Positive, ChartPath Path, int Start, int Count)> StepRegions(ChartSeries series, ChartPairedBoundaryGeometry mapped) {
        var result = new List<(bool, ChartPath, int, int)>();
        var first = new List<ChartPoint>(); var second = new List<ChartPoint>();
        var segmentStart = 0; var segmentEnd = ConnectedEnd(mapped.First, 0);
        var sign = 0;
        for (var item = 1; item < mapped.First.Count; item++) {
            if (mapped.First[item].BreakBefore) {
                Finish(); segmentStart = item; segmentEnd = ConnectedEnd(mapped.First, item); continue;
            }
            var previous = item - 1;
            var transition = ChartPathBuilder.StepTransitionX(mapped.First[previous].X, mapped.First[item].X, series.StepPosition);
            Hold(mapped.First[previous].X, transition, previous);
            Hold(transition, mapped.First[item].X, item);
        }
        Finish();
        return result;

        void Hold(double startX, double endX, int item) {
            // Start and End have a zero-width endpoint hold; it must not split or create a filled region.
            if (startX == endX) return;
            var polarity = series.RenderPoints[item * 2].Y.CompareTo(series.RenderPoints[item * 2 + 1].Y);
            if (polarity == 0) { Finish(); return; }
            if (sign != polarity) Finish();
            var firstY = mapped.First[item].Y; var secondY = mapped.Second[item].Y;
            // A same-sign transition still needs both vertical boundary vertices, even when their Y ranges do not overlap.
            if (first.Count == 0 || first[first.Count - 1].Y != firstY || second[second.Count - 1].Y != secondY) {
                first.Add(new ChartPoint(startX, firstY)); second.Add(new ChartPoint(startX, secondY));
            }
            sign = polarity;
            first.Add(new ChartPoint(endX, firstY)); second.Add(new ChartPoint(endX, secondY));
        }
        void Finish() {
            if (first.Count > 1) result.Add((sign > 0, ChartPairedBoundaryGeometry.Envelope(first, second), segmentStart, segmentEnd - segmentStart));
            first.Clear(); second.Clear(); sign = 0;
        }
    }

    // Retain connected-source identity when polarity crossings split a component into several fills.
    private static int ConnectedEnd(IReadOnlyList<ChartPoint> points, int start) {
        for (var item = start + 1; item < points.Count; item++) if (points[item].BreakBefore) return item;
        return points.Count;
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

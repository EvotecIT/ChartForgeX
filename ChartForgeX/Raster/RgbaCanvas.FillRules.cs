using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal enum RasterFillRule {
    EvenOdd,
    NonZero
}

internal sealed partial class RgbaCanvas {
    private void ScanFillSpans(IReadOnlyList<List<ChartPoint>> contours, RasterFillRule fillRule, Action<int, double, double, double> span) {
        var minY = double.PositiveInfinity;
        var maxY = double.NegativeInfinity;
        foreach (var contour in contours) foreach (var point in contour) {
            minY = Math.Min(minY, point.Y);
            maxY = Math.Max(maxY, point.Y);
        }

        if (double.IsInfinity(minY) || double.IsInfinity(maxY)) return;
        var yStart = Math.Max(0, (int)Math.Floor(minY));
        var yEnd = Math.Min(_pixelHeight - 1, (int)Math.Ceiling(maxY));
        var intersections = new List<FillIntersection>();

        for (var y = yStart; y <= yEnd; y++) {
            var scanY = y + 0.5;
            intersections.Clear();
            AddFillIntersections(contours, scanY, fillRule, intersections);
            intersections.Sort(FillIntersectionComparer.Instance);
            if (fillRule == RasterFillRule.EvenOdd) {
                for (var i = 0; i + 1 < intersections.Count; i += 2) span(y, scanY, intersections[i].X, intersections[i + 1].X);
            } else {
                EmitNonZeroSpans(y, scanY, intersections, span);
            }
        }
    }

    private static void EmitNonZeroSpans(int y, double scanY, IReadOnlyList<FillIntersection> intersections, Action<int, double, double, double> span) {
        var winding = 0;
        var left = 0.0;
        foreach (var intersection in intersections) {
            var previous = winding;
            winding += intersection.Winding;
            if (previous == 0 && winding != 0) {
                left = intersection.X;
            } else if (previous != 0 && winding == 0) {
                span(y, scanY, left, intersection.X);
            }
        }
    }

    private static void AddFillIntersections(IReadOnlyList<List<ChartPoint>> contours, double scanY, RasterFillRule fillRule, List<FillIntersection> intersections) {
        foreach (var contour in contours) {
            for (var i = 0; i < contour.Count; i++) {
                var a = contour[i];
                var b = contour[(i + 1) % contour.Count];
                if ((a.Y <= scanY && b.Y > scanY) || (b.Y <= scanY && a.Y > scanY)) {
                    var x = a.X + (scanY - a.Y) * (b.X - a.X) / (b.Y - a.Y);
                    var winding = fillRule == RasterFillRule.NonZero ? b.Y > a.Y ? 1 : -1 : 0;
                    intersections.Add(new FillIntersection(x, winding));
                }
            }
        }
    }

    private readonly struct FillIntersection {
        public readonly double X;
        public readonly int Winding;

        public FillIntersection(double x, int winding) {
            X = x;
            Winding = winding;
        }
    }

    private sealed class FillIntersectionComparer : IComparer<FillIntersection> {
        public static readonly FillIntersectionComparer Instance = new();

        public int Compare(FillIntersection left, FillIntersection right) =>
            left.X < right.X ? -1 : left.X > right.X ? 1 : left.Winding.CompareTo(right.Winding);
    }
}

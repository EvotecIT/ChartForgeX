using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal enum RasterFillRule {
    EvenOdd,
    NonZero
}

internal sealed partial class RgbaCanvas {
    // A canvas without supersampling takes this many scanlines per pixel row, so a horizontal
    // edge or a stem thinner than a pixel is weighted by the rows it covers instead of being
    // kept or dropped whole. Supersampled canvases already average several rows per output pixel.
    private const int UnscaledFillSubScanlines = 8;
    private const double FullCoverage = 0.999999;
    // One row of coverage, reused by every fill on this canvas and left zeroed between them.
    private double[]? _fillCoverage;

    /// <summary>Receives one pixel row of accumulated coverage for the pixels from <paramref name="xStart"/> through <paramref name="xEnd"/>.</summary>
    private delegate void FillCoverageRow(int y, int xStart, int xEnd, double[] coverage);

    /// <summary>
    /// Scan-converts closed contours into per-pixel coverage. Horizontal coverage is exact on
    /// every scanline; vertical coverage comes from the sub-scanlines of each pixel row.
    /// </summary>
    private void ScanFillCoverage(IReadOnlyList<List<ChartPoint>> contours, RasterFillRule fillRule, FillCoverageRow row) {
        var edges = BuildFillEdges(contours, fillRule, out var minY, out var maxY);
        if (edges.Length == 0) return;
        var yStart = Math.Max(0, (int)Math.Floor(minY));
        var yEnd = Math.Min(_pixelHeight - 1, (int)Math.Ceiling(maxY));
        if (yStart > yEnd) return;

        var samples = _scale == 1 ? UnscaledFillSubScanlines : 1;
        var coverage = _fillCoverage ??= new double[_pixelWidth];
        var active = new List<int>();
        var intersections = new List<FillIntersection>();
        var next = 0;
        for (var y = yStart; y <= yEnd; y++) {
            var rowMin = int.MaxValue;
            var rowMax = int.MinValue;
            var rowSamples = samples > 1 && RowIsUniform(edges, active, next, y) ? 1 : samples;
            var weight = 1.0 / rowSamples;
            for (var sample = 0; sample < rowSamples; sample++) {
                var scanY = y + (sample + 0.5) / rowSamples;
                while (next < edges.Length && edges[next].Top <= scanY) active.Add(next++);
                intersections.Clear();
                for (var index = active.Count - 1; index >= 0; index--) {
                    var edge = edges[active[index]];
                    if (edge.Bottom <= scanY) {
                        active[index] = active[active.Count - 1];
                        active.RemoveAt(active.Count - 1);
                        continue;
                    }

                    intersections.Add(new FillIntersection(edge.X + (scanY - edge.Top) * edge.Slope, edge.Winding));
                }

                if (intersections.Count < 2) continue;
                intersections.Sort(FillIntersectionComparer.Instance);
                if (fillRule == RasterFillRule.EvenOdd) {
                    for (var i = 0; i + 1 < intersections.Count; i += 2) AddSpanCoverage(coverage, intersections[i].X, intersections[i + 1].X, weight, ref rowMin, ref rowMax);
                } else {
                    var winding = 0;
                    var left = 0.0;
                    for (var i = 0; i < intersections.Count; i++) {
                        var previous = winding;
                        winding += intersections[i].Winding;
                        if (previous == 0 && winding != 0) left = intersections[i].X;
                        else if (previous != 0 && winding == 0) AddSpanCoverage(coverage, left, intersections[i].X, weight, ref rowMin, ref rowMax);
                    }
                }
            }

            if (rowMin > rowMax) continue;
            row(y, rowMin, rowMax, coverage);
            Array.Clear(coverage, rowMin, rowMax - rowMin + 1);
        }
    }

    /// <summary>True when every edge crossing the row is vertical and spans it, so one scanline describes the whole row.</summary>
    private static bool RowIsUniform(FillEdge[] edges, List<int> active, int next, int y) {
        if (next < edges.Length && edges[next].Top < y + 1) return false;
        for (var index = 0; index < active.Count; index++) {
            var edge = edges[active[index]];
            if (edge.Slope != 0 || edge.Top > y || edge.Bottom < y + 1) return false;
        }

        return true;
    }

    private void AddSpanCoverage(double[] coverage, double left, double right, double weight, ref int rowMin, ref int rowMax) {
        if (left < 0) left = 0;
        if (right > _pixelWidth) right = _pixelWidth;
        if (right <= left) return;
        var first = (int)left;
        var last = Math.Min(_pixelWidth - 1, (int)Math.Ceiling(right) - 1);
        if (first >= last) {
            coverage[first] += (right - left) * weight;
            last = first;
        } else {
            coverage[first] += (first + 1 - left) * weight;
            for (var x = first + 1; x < last; x++) coverage[x] += weight;
            coverage[last] += (right - last) * weight;
        }

        if (first < rowMin) rowMin = first;
        if (last > rowMax) rowMax = last;
    }

    private static FillEdge[] BuildFillEdges(IReadOnlyList<List<ChartPoint>> contours, RasterFillRule fillRule, out double minY, out double maxY) {
        minY = double.PositiveInfinity;
        maxY = double.NegativeInfinity;
        var count = 0;
        foreach (var contour in contours) count += contour.Count;
        var edges = new List<FillEdge>(count);
        foreach (var contour in contours) {
            for (var i = 0; i < contour.Count; i++) {
                var a = contour[i];
                var b = contour[(i + 1) % contour.Count];
                var sum = a.X + a.Y + b.X + b.Y;
                if (a.Y == b.Y || double.IsNaN(sum) || double.IsInfinity(sum)) continue;
                var downward = b.Y > a.Y;
                var top = downward ? a : b;
                var bottom = downward ? b : a;
                edges.Add(new FillEdge(top.Y, bottom.Y, top.X, (bottom.X - top.X) / (bottom.Y - top.Y), fillRule == RasterFillRule.NonZero ? downward ? 1 : -1 : 0));
                minY = Math.Min(minY, top.Y);
                maxY = Math.Max(maxY, bottom.Y);
            }
        }

        var sorted = edges.ToArray();
        Array.Sort(sorted, FillEdgeComparer.Instance);
        return sorted;
    }

    private readonly struct FillEdge {
        public readonly double Top;
        public readonly double Bottom;
        public readonly double X;
        public readonly double Slope;
        public readonly int Winding;

        public FillEdge(double top, double bottom, double x, double slope, int winding) {
            Top = top;
            Bottom = bottom;
            X = x;
            Slope = slope;
            Winding = winding;
        }
    }

    private sealed class FillEdgeComparer : IComparer<FillEdge> {
        public static readonly FillEdgeComparer Instance = new();

        public int Compare(FillEdge left, FillEdge right) => left.Top.CompareTo(right.Top);
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

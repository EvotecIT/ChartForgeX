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
    // Strokes take at least eight sub-scanlines per output pixel row at any supersampling, so a
    // line keeps its exact width whether it runs across the rows or along them.
    private int StrokeSubScanlines => Math.Max(1, (UnscaledFillSubScanlines + _supersamplingScale - 1) / _supersamplingScale);
    private const double FullCoverage = 0.999999;
    // One row of coverage, reused by every fill on this canvas and left zeroed between them.
    private double[]? _fillCoverage;

    /// <summary>Receives one pixel row of accumulated coverage for the pixels from <paramref name="xStart"/> through <paramref name="xEnd"/>.</summary>
    private delegate void FillCoverageRow(int y, int xStart, int xEnd, double[] coverage);

    /// <summary>
    /// Scan-converts closed contours into per-pixel coverage. Horizontal coverage is exact on
    /// every scanline; vertical coverage comes from the sub-scanlines of each pixel row.
    /// </summary>
    private void ScanFillCoverage(IReadOnlyList<List<ChartPoint>> contours, RasterFillRule fillRule, FillCoverageRow row, int subScanlines = 0, int secondContourStart = int.MaxValue) {
        var edges = BuildFillEdges(contours, fillRule, out var minY, out var maxY, secondContourStart);
        if (edges.Length == 0) return;
        var yStart = (int)Math.Max(0, Math.Min(_pixelHeight, Math.Floor(minY)));
        var yEnd = (int)Math.Max(-1, Math.Min(_pixelHeight - 1, Math.Ceiling(maxY)));
        if (yStart > yEnd) return;

        var samples = subScanlines > 0 ? subScanlines : _scale == 1 ? UnscaledFillSubScanlines : 1;
        var coverage = _fillCoverage ??= new double[_pixelWidth];
        // The active edges and their crossings stay sorted by x from one scanline to the next, so
        // each scanline only re-sorts the few edges that crossed and inserts the edges that start.
        var active = new List<int>();
        var intersections = new List<FillIntersection>();
        var next = 0;
        var spans = new FillSpans();
        var rowBoundaries = new List<double>();
        for (var y = yStart; y <= yEnd; y++) {
            var rowSamples = samples > 1 && RowIsUniform(edges, active, next, y) ? 1 : samples;
            rowBoundaries.Clear();
            for (int sample = 0; sample <= rowSamples; sample++) rowBoundaries.Add(y + sample / (double)rowSamples);
            foreach (int edgeIndex in active) {
                if (edges[edgeIndex].Bottom > y && edges[edgeIndex].Bottom < y + 1) rowBoundaries.Add(edges[edgeIndex].Bottom);
            }
            for (int edgeIndex = next; edgeIndex < edges.Length && edges[edgeIndex].Top < y + 1; edgeIndex++) {
                if (edges[edgeIndex].Top > y) rowBoundaries.Add(edges[edgeIndex].Top);
                if (edges[edgeIndex].Bottom > y && edges[edgeIndex].Bottom < y + 1) rowBoundaries.Add(edges[edgeIndex].Bottom);
            }
            rowBoundaries.Sort();
            for (var sample = 1; sample < rowBoundaries.Count; sample++) {
                double weight = rowBoundaries[sample] - rowBoundaries[sample - 1];
                if (weight <= 0) continue;
                var scanY = (rowBoundaries[sample] + rowBoundaries[sample - 1]) / 2;
                AdvanceActiveEdges(edges, active, intersections, ref next, scanY);
                if (intersections.Count < 2) continue;
                if (fillRule == RasterFillRule.EvenOdd) {
                    for (var i = 0; i + 1 < intersections.Count; i += 2) AddSpanCoverage(coverage, intersections[i].X, intersections[i + 1].X, weight, spans);
                } else {
                    var winding = 0;
                    var secondWinding = 0;
                    var left = 0.0;
                    for (var i = 0; i < intersections.Count; i++) {
                        bool previous = winding != 0 || secondWinding != 0;
                        if (intersections[i].SecondShape) secondWinding += intersections[i].Winding;
                        else winding += intersections[i].Winding;
                        bool inside = winding != 0 || secondWinding != 0;
                        if (!previous && inside) left = intersections[i].X;
                        else if (previous && !inside) AddSpanCoverage(coverage, left, intersections[i].X, weight, spans);
                    }
                }
            }

            FlushRowSpans(y, spans, coverage, row);
        }
    }

    /// <summary>Hands each run of covered pixels to <paramref name="row"/> and clears it, skipping the gaps between runs.</summary>
    private static void FlushRowSpans(int y, FillSpans spans, double[] coverage, FillCoverageRow row) {
        if (spans.Count == 0) return;
        Array.Sort(spans.Firsts, spans.Lasts, 0, spans.Count);
        var first = spans.Firsts[0];
        var last = spans.Lasts[0];
        for (var i = 1; i <= spans.Count; i++) {
            if (i < spans.Count && spans.Firsts[i] <= last + 1) {
                if (spans.Lasts[i] > last) last = spans.Lasts[i];
                continue;
            }

            row(y, first, last, coverage);
            Array.Clear(coverage, first, last - first + 1);
            if (i == spans.Count) break;
            first = spans.Firsts[i];
            last = spans.Lasts[i];
        }

        spans.Count = 0;
    }

    /// <summary>
    /// Moves the active edges to <paramref name="scanY"/>: drops the edges that ended, recomputes the
    /// crossings in their previous order and restores it with an insertion sort, then inserts the edges
    /// that start. The result is the crossings sorted by x and winding, as a full sort would give.
    /// </summary>
    private static void AdvanceActiveEdges(FillEdge[] edges, List<int> active, List<FillIntersection> crossings, ref int next, double scanY) {
        var kept = 0;
        for (var index = 0; index < active.Count; index++) {
            var edge = edges[active[index]];
            if (edge.Bottom <= scanY) continue;
            active[kept] = active[index];
            crossings[kept] = new FillIntersection(edge.X + (scanY - edge.Top) * edge.Slope, edge.Winding, edge.SecondShape);
            kept++;
        }

        active.RemoveRange(kept, active.Count - kept);
        crossings.RemoveRange(kept, crossings.Count - kept);
        var comparer = FillIntersectionComparer.Instance;
        for (var i = 1; i < crossings.Count; i++) {
            var crossing = crossings[i];
            if (comparer.Compare(crossings[i - 1], crossing) <= 0) continue;
            var edgeIndex = active[i];
            var j = i - 1;
            while (j >= 0 && comparer.Compare(crossings[j], crossing) > 0) {
                crossings[j + 1] = crossings[j];
                active[j + 1] = active[j];
                j--;
            }

            crossings[j + 1] = crossing;
            active[j + 1] = edgeIndex;
        }

        // The edges that start are sorted on their own and merged in from the back, one pass for all of them.
        var existing = crossings.Count;
        for (; next < edges.Length && edges[next].Top <= scanY; next++) {
            var edge = edges[next];
            if (edge.Bottom <= scanY) continue;
            var crossing = new FillIntersection(edge.X + (scanY - edge.Top) * edge.Slope, edge.Winding, edge.SecondShape);
            var j = crossings.Count - 1;
            crossings.Add(crossing);
            active.Add(next);
            while (j >= existing && comparer.Compare(crossings[j], crossing) > 0) {
                crossings[j + 1] = crossings[j];
                active[j + 1] = active[j];
                j--;
            }

            crossings[j + 1] = crossing;
            active[j + 1] = next;
        }

        var added = crossings.Count - existing;
        if (added == 0 || existing == 0) return;
        var startedCrossings = crossings.GetRange(existing, added);
        var startedEdges = active.GetRange(existing, added);
        var left = existing - 1;
        var right = added - 1;
        for (var write = crossings.Count - 1; right >= 0; write--) {
            if (left >= 0 && comparer.Compare(crossings[left], startedCrossings[right]) > 0) {
                crossings[write] = crossings[left];
                active[write] = active[left];
                left--;
            } else {
                crossings[write] = startedCrossings[right];
                active[write] = startedEdges[right];
                right--;
            }
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

    private void AddSpanCoverage(double[] coverage, double left, double right, double weight, FillSpans spans) {
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

        spans.Add(first, last);
    }

    private static FillEdge[] BuildFillEdges(IReadOnlyList<List<ChartPoint>> contours, RasterFillRule fillRule, out double minY, out double maxY, int secondContourStart) {
        minY = double.PositiveInfinity;
        maxY = double.NegativeInfinity;
        var count = 0;
        foreach (var contour in contours) count += contour.Count;
        var edges = new List<FillEdge>(count);
        int contourIndex = 0;
        foreach (var contour in contours) {
            bool secondShape = contourIndex++ >= secondContourStart;
            for (var i = 0; i < contour.Count; i++) {
                var a = contour[i];
                var b = contour[(i + 1) % contour.Count];
                var sum = a.X + a.Y + b.X + b.Y;
                if (a.Y == b.Y || double.IsNaN(sum) || double.IsInfinity(sum)) continue;
                var downward = b.Y > a.Y;
                var top = downward ? a : b;
                var bottom = downward ? b : a;
                edges.Add(new FillEdge(top.Y, bottom.Y, top.X, (bottom.X - top.X) / (bottom.Y - top.Y), fillRule == RasterFillRule.NonZero ? downward ? 1 : -1 : 0, secondShape));
                minY = Math.Min(minY, top.Y);
                maxY = Math.Max(maxY, bottom.Y);
            }
        }

        // Sorting primitive keys with an index array is several times faster than a comparer over the structs.
        var tops = new double[edges.Count];
        var order = new int[edges.Count];
        for (var i = 0; i < tops.Length; i++) {
            tops[i] = edges[i].Top;
            order[i] = i;
        }

        Array.Sort(tops, order);
        var sorted = new FillEdge[order.Length];
        for (var i = 0; i < order.Length; i++) sorted[i] = edges[order[i]];
        return sorted;
    }

    private readonly struct FillEdge {
        public readonly double Top;
        public readonly double Bottom;
        public readonly double X;
        public readonly double Slope;
        public readonly int Winding;
        public readonly bool SecondShape;

        public FillEdge(double top, double bottom, double x, double slope, int winding, bool secondShape) {
            Top = top;
            Bottom = bottom;
            X = x;
            Slope = slope;
            Winding = winding;
            SecondShape = secondShape;
        }
    }

    private readonly struct FillIntersection {
        public readonly double X;
        public readonly int Winding;
        public readonly bool SecondShape;

        public FillIntersection(double x, int winding, bool secondShape) {
            X = x;
            Winding = winding;
            SecondShape = secondShape;
        }
    }

    /// <summary>The first and last pixels of the spans one pixel row added coverage to, kept as key arrays so they sort quickly.</summary>
    private sealed class FillSpans {
        public int[] Firsts = new int[16];
        public int[] Lasts = new int[16];
        public int Count;

        public void Add(int first, int last) {
            if (Count == Firsts.Length) {
                Array.Resize(ref Firsts, Count * 2);
                Array.Resize(ref Lasts, Count * 2);
            }

            Firsts[Count] = first;
            Lasts[Count] = last;
            Count++;
        }
    }

    private sealed class FillIntersectionComparer : IComparer<FillIntersection> {
        public static readonly FillIntersectionComparer Instance = new();

        public int Compare(FillIntersection left, FillIntersection right) =>
            left.X < right.X ? -1 : left.X > right.X ? 1 : left.Winding.CompareTo(right.Winding);
    }
}

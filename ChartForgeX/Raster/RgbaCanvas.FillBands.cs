using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal sealed partial class RgbaCanvas {
    // A fill with this many outline points, such as a stroke of a long series, is scanned in
    // vertical bands. Each sub-scanline of a scan visits every active edge, and a row takes a
    // sub-scanline at every edge end inside it, so one scan of a dense outline grows with the
    // square of its size. A band keeps only the contours that reach it, which bounds both.
    // A band also takes sub-scanlines only at its own edge ends, so its coverage can differ from
    // a whole-canvas scan by rounding, at most a couple of alpha levels on a few edge pixels.
    private const int BandedFillPoints = 4096;
    // Only outlines this crowded, in points per pixel row they span, take far more sub-scanlines than the fixed samples.
    private const int BandedRowPoints = 64;
    private const int FillBandWidth = 4;
    // Keeps a contour in a band its computed crossings could reach through rounding.
    private const double FillBandMargin = 0.01;
    // Banding stops paying off once wide contours repeat across many bands.
    private const int MaximumBandRepeats = 4;

    /// <summary>
    /// Splits a fill into bands of device-pixel columns. A closed contour adds no winding to the
    /// points beside its bounds, so a band keeps the contours whose bounds reach it and clips its coverage to its own columns. Small fills, fills of two winding shapes and
    /// fills of wide contours keep one band across the canvas.
    /// </summary>
    private List<FillBand> CreateFillBands(IReadOnlyList<List<ChartPoint>> contours, RasterFillRule fillRule, int secondContourStart, out double minY, out double maxY) {
        var bands = new List<FillBand>();
        var members = SplitIntoBands(contours, secondContourStart);
        if (members == null) {
            AddFillBand(bands, contours, fillRule, secondContourStart, 0, _pixelWidth, out minY, out maxY);
            return bands;
        }

        minY = double.PositiveInfinity;
        maxY = double.NegativeInfinity;
        for (var index = 0; index < members.Length; index++) {
            if (members[index] == null) continue;
            AddFillBand(bands, members[index]!, fillRule, int.MaxValue, index * FillBandWidth, Math.Min(_pixelWidth, (index + 1) * FillBandWidth), out var bandMinY, out var bandMaxY);
            minY = Math.Min(minY, bandMinY);
            maxY = Math.Max(maxY, bandMaxY);
        }

        return bands;
    }

    private List<List<ChartPoint>>?[]? SplitIntoBands(IReadOnlyList<List<ChartPoint>> contours, int secondContourStart) {
        if (secondContourStart != int.MaxValue || _pixelWidth < FillBandWidth * 2) return null;
        long points = 0;
        var top = double.PositiveInfinity;
        var bottom = double.NegativeInfinity;
        foreach (var contour in contours) {
            points += contour.Count;
            foreach (var point in contour) {
                top = Math.Min(top, point.Y);
                bottom = Math.Max(bottom, point.Y);
            }
        }

        if (points < BandedFillPoints || !(points > BandedRowPoints * Math.Max(1, bottom - top))) return null;

        var bandCount = (_pixelWidth + FillBandWidth - 1) / FillBandWidth;
        var members = new List<List<ChartPoint>>?[bandCount];
        long assigned = 0;
        foreach (var contour in contours) {
            if (contour.Count == 0) continue;
            var minX = double.PositiveInfinity;
            var maxX = double.NegativeInfinity;
            foreach (var point in contour) {
                minX = Math.Min(minX, point.X);
                maxX = Math.Max(maxX, point.X);
            }

            // A contour with an unusable point loses edges and no longer closes, so every band keeps it.
            var finite = !double.IsNaN(minX) && !double.IsNaN(maxX) && !double.IsInfinity(minX) && !double.IsInfinity(maxX);
            var firstBand = finite ? Math.Floor((minX - FillBandMargin) / FillBandWidth) : 0;
            var lastBand = finite ? Math.Floor((maxX + FillBandMargin) / FillBandWidth) : bandCount - 1;
            // A contour wholly beside the canvas adds no winding inside it.
            if (lastBand < 0 || firstBand >= bandCount) continue;
            var first = (int)Math.Max(0, firstBand);
            var last = (int)Math.Min(bandCount - 1, lastBand);
            for (var index = first; index <= last; index++) (members[index] ??= new List<List<ChartPoint>>()).Add(contour);
            assigned += (long)contour.Count * (last - first + 1);
            if (assigned > points * MaximumBandRepeats) return null;
        }

        return members;
    }

    private static void AddFillBand(List<FillBand> bands, IReadOnlyList<List<ChartPoint>> contours, RasterFillRule fillRule, int secondContourStart, int left, int right, out double minY, out double maxY) {
        var edges = BuildFillEdges(contours, fillRule, out minY, out maxY, secondContourStart);
        if (edges.Length > 0) bands.Add(new FillBand(edges, left, right));
    }

    /// <summary>One band's sorted edges and the scan state carried from one sub-scanline to the next.</summary>
    private sealed class FillBand {
        public readonly FillEdge[] Edges;
        public readonly int Left;
        public readonly int Right;
        // The active edges and their crossings stay sorted by x from one scanline to the next, so
        // each scanline only re-sorts the few edges that crossed and inserts the edges that start.
        public int[] Active = new int[16];
        public FillIntersection[] Crossings = new FillIntersection[16];
        public int Count;
        public int Next;
        // The edges that start on one sub-scanline, sorted on their own before they are merged in.
        public FillIntersection[] StartedCrossings = new FillIntersection[16];
        public int[] StartedEdges = new int[16];

        public FillBand(FillEdge[] edges, int left, int right) {
            Edges = edges;
            Left = left;
            Right = right;
        }

        public void EnsureCapacity(int count) {
            if (Active.Length >= count) return;
            var capacity = Math.Max(count, Active.Length * 2);
            Array.Resize(ref Active, capacity);
            Array.Resize(ref Crossings, capacity);
        }

        public void GrowStarted(out FillIntersection[] crossings, out int[] edges) {
            Array.Resize(ref StartedCrossings, StartedCrossings.Length * 2);
            Array.Resize(ref StartedEdges, StartedCrossings.Length);
            crossings = StartedCrossings;
            edges = StartedEdges;
        }
    }
}

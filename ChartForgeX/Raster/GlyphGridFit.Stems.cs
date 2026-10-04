using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal sealed partial class GlyphGridFit {
    // Optional geometry analysis stays small even for an unusually complex glyph. Larger outlines
    // retain vertical fitting. Straight edges only: diagonals and flattened curve fragments are not stems.
    private const int MaximumStemPoints = 4096;
    private const int MaximumStemEdges = 64;

    /// <summary>Fits mutually nearest opposite vertical edges enclosing non-zero ink, then interpolates
    /// all x coordinates through their ordered anchors. Layout coordinates and advances are untouched.</summary>
    private void FitStems(List<List<ChartPoint>> contours) {
        var pointCount = 0;
        foreach (var contour in contours) {
            pointCount += contour.Count;
            if (pointCount > MaximumStemPoints) return;
        }
        var edges = new List<StemEdge>();
        foreach (var contour in contours) for (var i = 0; i < contour.Count; i++) {
            var a = contour[i]; var b = contour[(i + 1) % contour.Count];
            var height = Math.Abs(b.Y - a.Y) * _outputScale;
            if (Math.Abs(a.X - b.X) * _outputScale > 0.001 || height < 1) continue;
            edges.Add(new StemEdge(a.X * _outputScale, Math.Min(a.Y, b.Y), Math.Max(a.Y, b.Y), b.Y > a.Y));
            if (edges.Count > MaximumStemEdges) return;
        }
        if (edges.Count < 2) return;
        var partners = new int[edges.Count];
        for (var i = 0; i < edges.Count; i++) {
            partners[i] = -1;
            var closest = double.MaxValue;
            for (var j = 0; j < edges.Count; j++) {
                var a = edges[i]; var b = edges[j];
                var width = Math.Abs(a.X - b.X);
                var overlap = Math.Min(a.Bottom, b.Bottom) - Math.Max(a.Top, b.Top);
                if (a.Down == b.Down || width < 0.15 || width > 2 || overlap * _outputScale < 1 || width >= closest) continue;
                closest = width; partners[i] = j;
            }
        }
        var anchors = new List<StemAnchor>();
        for (var i = 0; i < edges.Count; i++) {
            var j = partners[i];
            if (j <= i || partners[j] != i) continue;
            var a = edges[i]; var b = edges[j];
            var left = Math.Min(a.X, b.X); var right = Math.Max(a.X, b.X);
            var middleY = (Math.Max(a.Top, b.Top) + Math.Min(a.Bottom, b.Bottom)) / 2;
            // Opposite segments may bound a counter instead of a stroke. Probe using the same non-zero
            // rule as painting, so CFF/TrueType winding, holes and overlapping components share one policy.
            if (!HasInk(contours, (left + right) / (2 * _outputScale), middleY)) continue;
            var width = right - left;
            var fittedWidth = Math.Max(1, Math.Round(width, MidpointRounding.AwayFromZero));
            fittedWidth = Math.Max(width - 0.35, Math.Min(width + 0.35, fittedWidth));
            var fittedLeft = Math.Round((left + right - fittedWidth) / 2, MidpointRounding.AwayFromZero);
            anchors.Add(new StemAnchor(left, fittedLeft));
            anchors.Add(new StemAnchor(right, fittedLeft + fittedWidth));
        }
        if (anchors.Count == 0) return;
        anchors.Sort((a, b) => a.Source.CompareTo(b.Source));
        for (var i = anchors.Count - 1; i > 0; i--) {
            var previous = anchors[i - 1]; var current = anchors[i];
            var gap = current.Source - previous.Source;
            if (gap < 0.001) {
                // Conflicting stems on the same x coordinate are ambiguous; retain the original glyph.
                if (Math.Abs(current.Target - previous.Target) > 0.001) return;
                anchors.RemoveAt(i);
            } else if (current.Target - previous.Target < gap * 0.5) return;
        }
        foreach (var contour in contours) for (var i = 0; i < contour.Count; i++) {
            var point = contour[i];
            contour[i] = new ChartPoint(MapStemX(point.X * _outputScale, anchors) / _outputScale, point.Y);
        }
    }

    private static double MapStemX(double x, List<StemAnchor> anchors) {
        var first = anchors[0];
        if (x <= first.Source) return x + first.Target - first.Source;
        for (var i = 1; i < anchors.Count; i++) {
            var a = anchors[i - 1]; var b = anchors[i];
            if (x <= b.Source) return a.Target + (x - a.Source) * (b.Target - a.Target) / (b.Source - a.Source);
        }
        var last = anchors[anchors.Count - 1];
        return x + last.Target - last.Source;
    }

    private static bool HasInk(List<List<ChartPoint>> contours, double x, double y) {
        var winding = 0;
        foreach (var contour in contours) for (var i = 0; i < contour.Count; i++) {
            var a = contour[i]; var b = contour[(i + 1) % contour.Count];
            if ((a.Y <= y && b.Y > y) || (b.Y <= y && a.Y > y)) {
                var crossing = a.X + (y - a.Y) * (b.X - a.X) / (b.Y - a.Y);
                if (crossing > x) winding += b.Y > a.Y ? 1 : -1;
            }
        }
        return winding != 0;
    }

    private readonly struct StemEdge {
        internal StemEdge(double x, double top, double bottom, bool down) { X = x; Top = top; Bottom = bottom; Down = down; }
        internal double X { get; }
        internal double Top { get; }
        internal double Bottom { get; }
        internal bool Down { get; }
    }

    private readonly struct StemAnchor {
        internal StemAnchor(double source, double target) { Source = source; Target = target; }
        internal double Source { get; }
        internal double Target { get; }
    }
}

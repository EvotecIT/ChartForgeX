using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal enum RasterLineCap {
    Butt,
    Round,
    Square
}

internal enum RasterLineJoin {
    Miter,
    Round,
    Bevel
}

/// <summary>
/// Straight lines and polylines. Every one is outlined by <see cref="RasterStroker"/> and painted
/// as a single shape, so a stroke keeps its exact width, joins and caps follow the SVG attributes
/// they mirror, and a translucent stroke never darkens where its own segments overlap.
/// </summary>
internal sealed partial class RgbaCanvas {
    private const double DefaultMiterLimit = 4.0;

    /// <summary>Strokes a polyline with round caps and joins, as a chart series path is drawn in SVG.</summary>
    public void DrawPolyline(IReadOnlyList<ChartPoint> points, ChartColor color, double thickness) {
        DrawPolyline(points, color, thickness, RasterLineCap.Round, RasterLineJoin.Round, null);
    }

    /// <summary>Strokes a polyline with explicit SVG stroke attributes; a dash array is in canvas units.</summary>
    internal void DrawPolyline(IReadOnlyList<ChartPoint> points, ChartColor color, double thickness, RasterLineCap lineCap, RasterLineJoin lineJoin, IReadOnlyList<double>? dashArray, double miterLimit = DefaultMiterLimit) {
        if (points == null) throw new ArgumentNullException(nameof(points));
        if (points.Count < 2) return;
        StrokePolylines(new[] { points }, color, thickness, lineCap, lineJoin, dashArray, miterLimit);
    }

    /// <summary>Strokes a line segment with round caps.</summary>
    public void DrawLine(double x0, double y0, double x1, double y1, ChartColor color, double thickness) {
        DrawLine(x0, y0, x1, y1, color, thickness, RasterLineCap.Round);
    }

    /// <summary>Strokes a line segment; <see cref="RasterLineCap.Butt"/> matches an SVG <c>line</c> without <c>stroke-linecap</c>.</summary>
    internal void DrawLine(double x0, double y0, double x1, double y1, ChartColor color, double thickness, RasterLineCap lineCap) {
        StrokePolylines(new[] { Segment(x0, y0, x1, y1) }, color, thickness, lineCap, RasterLineJoin.Miter);
    }

    /// <summary>Strokes a dashed line segment whose dashes are <paramref name="dash"/> long with butt ends, as <c>stroke-dasharray</c> draws them.</summary>
    public void DrawDashedLine(double x0, double y0, double x1, double y1, ChartColor color, double thickness, double dash = 6, double gap = 5) {
        DrawDashedLine(x0, y0, x1, y1, color, thickness, dash, gap, RasterLineCap.Butt);
    }

    /// <summary>Strokes a dashed line segment with an explicit cap for every dash.</summary>
    internal void DrawDashedLine(double x0, double y0, double x1, double y1, ChartColor color, double thickness, double dash, double gap, RasterLineCap lineCap) {
        StrokePolylines(new[] { Segment(x0, y0, x1, y1) }, color, thickness, lineCap, RasterLineJoin.Miter, DashPattern(dash, gap));
    }

    /// <summary>Strokes a rectangle outline with mitred corners, as an SVG <c>rect</c> stroke.</summary>
    public void StrokeRect(double x, double y, double width, double height, ChartColor color, double thickness = 1) {
        var ring = new[] { new ChartPoint(x, y), new ChartPoint(x + width, y), new ChartPoint(x + width, y + height), new ChartPoint(x, y + height), new ChartPoint(x, y) };
        StrokePolylines(new[] { ring }, color, thickness, RasterLineCap.Butt, RasterLineJoin.Miter);
    }

    /// <summary>Strokes polygon outlines as one shape, joined at every vertex including the first, as an SVG <c>polygon</c> or a path ending in <c>Z</c>.</summary>
    internal void StrokeClosedPolylines(IEnumerable<IReadOnlyList<ChartPoint>> rings, ChartColor color, double thickness, RasterLineJoin lineJoin) {
        if (rings == null) throw new ArgumentNullException(nameof(rings));
        var closed = new List<IReadOnlyList<ChartPoint>>();
        foreach (var ring in rings) {
            if (ring == null || ring.Count < 2) continue;
            var points = new List<ChartPoint>(ring.Count + 1);
            points.AddRange(ring);
            var first = ring[0];
            var last = ring[ring.Count - 1];
            if (first.X != last.X || first.Y != last.Y) points.Add(first);
            closed.Add(points);
        }

        StrokePolylines(closed, color, thickness, RasterLineCap.Butt, lineJoin);
    }

    /// <summary>Strokes one polygon outline; see <see cref="StrokeClosedPolylines"/>.</summary>
    internal void StrokeClosedPolyline(IReadOnlyList<ChartPoint> ring, ChartColor color, double thickness, RasterLineJoin lineJoin) =>
        StrokeClosedPolylines(new[] { ring }, color, thickness, lineJoin);

    /// <summary>A dash array for one dash and one gap, or none when either is not a positive length.</summary>
    internal static double[]? DashPattern(double dash, double gap) => dash > 0 && gap > 0 ? new[] { dash, gap } : null;

    private static ChartPoint[] Segment(double x0, double y0, double x1, double y1) => new[] { new ChartPoint(x0, y0), new ChartPoint(x1, y1) };
}

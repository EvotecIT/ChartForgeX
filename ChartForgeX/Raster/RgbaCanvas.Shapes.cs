using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

/// <summary>
/// Shapes painted through the contour fill, so curved fills and strokes share one coverage model.
/// </summary>
internal sealed partial class RgbaCanvas {
    private void FillRoundedRectContours(double x, double y, double width, double height, double radius, ChartColor color) {
        if (!(width > 0) || !(height > 0)) return;
        FillContours(new[] { ChartCurveFlattening.RoundedRectangle(x, y, width, height, radius, radius, _scale) }, color, RasterFillRule.NonZero);
    }

    private void StrokeRoundedRectContours(double x, double y, double width, double height, double radius, ChartColor color,
        double thickness, IReadOnlyList<double>? dashArray = null) {
        if (!(width > 0) || !(height > 0) || !(thickness > 0) || double.IsInfinity(thickness)) return;
        if (thickness >= Math.Min(width, height)) {
            if (dashArray == null) FillRoundedRectContours(x, y, width, height, radius, color);
            return;
        }
        double half = thickness / 2;
        var ring = ChartCurveFlattening.RoundedRectangle(x + half, y + half, width - thickness, height - thickness,
            Math.Max(0, radius - half), Math.Max(0, radius - half), _scale);
        // SVG rectangle dash phase starts at the top edge after the top-left corner.
        var first = ring[ring.Count - 1];
        ring.RemoveAt(ring.Count - 1); ring.Insert(0, first); ring.Add(first);
        StrokePolylines(new[] { ring }, color, thickness, RasterLineCap.Butt, RasterLineJoin.Miter, dashArray);
    }

    /// <summary>
    /// Strokes polylines as one shape: overlapping segments, joins, and caps are united before
    /// painting, so a translucent stroke keeps an even tone. A polyline whose last point repeats
    /// its first is closed.
    /// </summary>
    internal void StrokePolylines(IReadOnlyList<IReadOnlyList<ChartPoint>> polylines, ChartColor color, double thickness, RasterLineCap lineCap, RasterLineJoin lineJoin, IReadOnlyList<double>? dashArray = null, double miterLimit = DefaultMiterLimit) {
        if (polylines == null) throw new ArgumentNullException(nameof(polylines));
        if (color.A == 0 || !(thickness > 0)) return;
        // Outline in device pixels, so the outline is built once at the resolution it is filled at.
        var width = thickness * _scale;
        double[]? pattern = null;
        if (dashArray != null && dashArray.Count > 0) {
            pattern = new double[dashArray.Count];
            for (var i = 0; i < pattern.Length; i++) pattern[i] = dashArray[i] * _scale;
        }

        var outline = new List<List<ChartPoint>>();
        foreach (var polyline in polylines) {
            if (polyline == null || polyline.Count == 0) continue;
            var device = new List<ChartPoint>(polyline.Count);
            foreach (var point in polyline) device.Add(new ChartPoint(point.X * _scale, point.Y * _scale));
            if (pattern == null) {
                RasterStroker.AppendOutline(device, width, lineCap, lineJoin, miterLimit, 1, outline);
                continue;
            }

            double padding = width * Math.Max(1, miterLimit);
            foreach (var dash in RasterStroker.Dash(device, pattern, -padding, -padding, _pixelWidth + padding, _pixelHeight + padding))
                RasterStroker.AppendOutline(dash, width, lineCap, lineJoin, miterLimit, 1, outline);
        }

        FillContoursPixels(outline, color, RasterFillRule.NonZero, StrokeSubScanlines);
    }

    /// <summary>
    /// Strokes SVG path data as one shape, so a renderer can draw the same path its SVG counterpart
    /// writes. Curves are flattened at the canvas resolution; a closed subpath gets a join where it closes.
    /// </summary>
    internal void StrokePathData(string pathData, ChartColor color, double thickness, RasterLineCap lineCap, RasterLineJoin lineJoin) {
        var polylines = new List<IReadOnlyList<ChartPoint>>();
        foreach (var subpath in ChartMapPathParser.ParseSubpaths(pathData, _scale)) {
            var points = subpath.Points;
            if (subpath.IsClosed && points.Count > 1 && (points[0].X != points[points.Count - 1].X || points[0].Y != points[points.Count - 1].Y)) points.Add(points[0]);
            polylines.Add(points);
        }

        StrokePolylines(polylines, color, thickness, lineCap, lineJoin);
    }

    /// <summary>Device pixels per canvas unit, the resolution callers flatten curves at before stroking them.</summary>
    internal int PixelsPerUnit => _scale;

    /// <summary>Fills an ellipse.</summary>
    internal void FillEllipse(double cx, double cy, double rx, double ry, ChartColor color) {
        if (!(rx > 0) || !(ry > 0) || color.A == 0) return;
        FillContours(new[] { ChartCurveFlattening.Ellipse(cx, cy, rx, ry, _scale) }, color, RasterFillRule.NonZero);
    }

    /// <summary>Strokes an ellipse outline centered on the ellipse.</summary>
    internal void StrokeEllipse(double cx, double cy, double rx, double ry, ChartColor color, double thickness) {
        if (!(rx > 0) || !(ry > 0)) return;
        var ring = ChartCurveFlattening.Ellipse(cx, cy, rx, ry, _scale);
        ring.Add(ring[0]);
        StrokePolylines(new[] { ring }, color, thickness, RasterLineCap.Butt, RasterLineJoin.Round);
    }

    /// <summary>Strokes a circular arc from <paramref name="startAngle"/> through <paramref name="sweep"/> radians; a sweep of a full turn or more closes the ring.</summary>
    internal void StrokeArc(double cx, double cy, double radius, double startAngle, double sweep, ChartColor color, double thickness, RasterLineCap lineCap) {
        if (!(radius > 0) || sweep == 0) return;
        if (Math.Abs(sweep) >= Math.PI * 2 - 0.000001) {
            StrokeEllipse(cx, cy, radius, radius, color, thickness);
            return;
        }

        StrokePolylines(new[] { ChartCurveFlattening.Arc(cx, cy, radius, startAngle, sweep, _scale) }, color, thickness, lineCap, RasterLineJoin.Round);
    }

    /// <summary>Fills a rectangle, optionally rounded, with a linear gradient between two points.</summary>
    internal void FillRectLinearGradient(double x, double y, double width, double height, double radius, ChartPoint start, ChartPoint end, IReadOnlyList<RasterGradientStop> stops) {
        FillContoursLinearGradient(new[] { ChartCurveFlattening.RoundedRectangle(x, y, width, height, radius, radius, _scale) }, start, end, stops, RasterGradientSpreadMethod.Pad, RasterFillRule.NonZero);
    }

    /// <summary>Fills a rectangle, optionally rounded, with a radial gradient around a center.</summary>
    internal void FillRectRadialGradient(double x, double y, double width, double height, double radius, ChartPoint center, double gradientRadius, IReadOnlyList<RasterGradientStop> stops) {
        FillContoursRadialGradient(new[] { ChartCurveFlattening.RoundedRectangle(x, y, width, height, radius, radius, _scale) }, center, new ChartPoint(center.X + gradientRadius, center.Y), new ChartPoint(center.X, center.Y + gradientRadius), stops, RasterGradientSpreadMethod.Pad, RasterFillRule.NonZero);
    }
}

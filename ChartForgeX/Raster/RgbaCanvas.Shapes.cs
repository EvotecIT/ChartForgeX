using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

/// <summary>
/// Shapes painted through the contour fill, so curved fills and strokes share one coverage model.
/// </summary>
internal sealed partial class RgbaCanvas {
    /// <summary>
    /// Strokes polylines as one shape: overlapping segments, joins, and caps are united before
    /// painting, so a translucent stroke keeps an even tone. A polyline whose last point repeats
    /// its first is closed.
    /// </summary>
    internal void StrokePolylines(IReadOnlyList<IReadOnlyList<ChartPoint>> polylines, ChartColor color, double thickness, RasterLineCap lineCap, RasterLineJoin lineJoin, IReadOnlyList<double>? dashArray = null, double miterLimit = DefaultMiterLimit) {
        if (polylines == null) throw new ArgumentNullException(nameof(polylines));
        if (color.A == 0 || !(thickness > 0)) return;
        var outline = new List<List<ChartPoint>>();
        foreach (var polyline in polylines) {
            if (polyline == null || polyline.Count == 0) continue;
            if (dashArray == null || dashArray.Count == 0) {
                RasterStroker.AppendOutline(polyline, thickness, lineCap, lineJoin, miterLimit, _scale, outline);
                continue;
            }

            foreach (var dash in RasterStroker.Dash(polyline, dashArray)) RasterStroker.AppendOutline(dash, thickness, lineCap, lineJoin, miterLimit, _scale, outline);
        }

        FillContours(outline, color, RasterFillRule.NonZero);
    }

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

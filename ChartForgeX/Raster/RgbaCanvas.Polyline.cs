using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal enum RasterLineCap {
    Butt,
    Round
}

internal enum RasterLineJoin {
    Miter,
    Round,
    Bevel
}

internal sealed partial class RgbaCanvas {
    public void DrawPolyline(IReadOnlyList<ChartPoint> points, ChartColor color, double thickness) {
        DrawPolyline(points, color, thickness, RasterLineCap.Round, RasterLineJoin.Round, null);
    }

    internal void DrawLine(double x0, double y0, double x1, double y1, ChartColor color, double thickness, RasterLineCap lineCap) {
        if (lineCap == RasterLineCap.Round) {
            DrawLine(x0, y0, x1, y1, color, thickness);
            return;
        }

        DrawLinePixelsButt(x0 * _scale, y0 * _scale, x1 * _scale, y1 * _scale, Math.Max(1, thickness * _scale), color);
    }

    internal void DrawPolyline(IReadOnlyList<ChartPoint> points, ChartColor color, double thickness, RasterLineCap lineCap, RasterLineJoin lineJoin, IReadOnlyList<double>? dashArray) {
        if (points == null) throw new ArgumentNullException(nameof(points));
        if (points.Count < 2) return;
        var scaledThickness = Math.Max(1, thickness * _scale);
        if (dashArray != null && dashArray.Count > 0) {
            DrawDashedPolyline(points, color, scaledThickness, lineCap, dashArray);
            return;
        }

        for (var i = 1; i < points.Count; i++) {
            DrawLinePixelsButt(points[i - 1].X * _scale, points[i - 1].Y * _scale, points[i].X * _scale, points[i].Y * _scale, scaledThickness, color);
        }

        if (lineCap != RasterLineCap.Round && lineJoin != RasterLineJoin.Round) return;
        var radius = Math.Max(0.5, scaledThickness / 2.0);
        if (lineCap == RasterLineCap.Round) DrawSoftCirclePixels(points[0].X * _scale, points[0].Y * _scale, radius, color);
        for (var i = 1; i < points.Count - 1; i++) {
            if (lineJoin == RasterLineJoin.Round && ShouldRoundPolylineJoin(points, i, _scale)) DrawSoftCirclePixels(points[i].X * _scale, points[i].Y * _scale, radius, color);
        }

        var last = points[points.Count - 1];
        if (lineCap == RasterLineCap.Round) DrawSoftCirclePixels(last.X * _scale, last.Y * _scale, radius, color);
    }

    private void DrawDashedPolyline(IReadOnlyList<ChartPoint> points, ChartColor color, double thickness, RasterLineCap lineCap, IReadOnlyList<double> dashArray) {
        var scaledDash = ScaleDashArray(dashArray);
        if (scaledDash.Count == 0) return;
        var dashIndex = 0;
        var dashRemaining = scaledDash[0];
        var drawDash = true;
        for (var i = 1; i < points.Count; i++) {
            var x0 = points[i - 1].X * _scale;
            var y0 = points[i - 1].Y * _scale;
            var x1 = points[i].X * _scale;
            var y1 = points[i].Y * _scale;
            var dx = x1 - x0;
            var dy = y1 - y0;
            var length = Math.Sqrt(dx * dx + dy * dy);
            if (length <= 0.000001) continue;
            var consumed = 0.0;
            while (consumed < length - 0.000001) {
                var step = Math.Min(dashRemaining, length - consumed);
                if (drawDash && step > 0.000001) {
                    var start = consumed / length;
                    var end = (consumed + step) / length;
                    var sx = x0 + dx * start;
                    var sy = y0 + dy * start;
                    var ex = x0 + dx * end;
                    var ey = y0 + dy * end;
                    DrawLinePixelsButt(sx, sy, ex, ey, thickness, color);
                    if (lineCap == RasterLineCap.Round) {
                        var radius = Math.Max(0.5, thickness / 2.0);
                        DrawSoftCirclePixels(sx, sy, radius, color);
                        DrawSoftCirclePixels(ex, ey, radius, color);
                    }
                }

                consumed += step;
                dashRemaining -= step;
                if (dashRemaining <= 0.000001) {
                    dashIndex = (dashIndex + 1) % scaledDash.Count;
                    dashRemaining = scaledDash[dashIndex];
                    drawDash = !drawDash;
                }
            }
        }
    }

    private List<double> ScaleDashArray(IReadOnlyList<double> dashArray) {
        var scaled = new List<double>(dashArray.Count * 2);
        foreach (var value in dashArray) if (value > 0) scaled.Add(value * _scale);
        if (scaled.Count % 2 == 1) {
            var count = scaled.Count;
            for (var i = 0; i < count; i++) scaled.Add(scaled[i]);
        }

        return scaled;
    }

    private void DrawLinePixelsButt(double x0, double y0, double x1, double y1, double thickness, ChartColor color) {
        if (thickness <= 0 || color.A == 0) return;

        var radius = Math.Max(0.5, thickness / 2.0);
        var feather = 1.0;
        var minX = Math.Max(0, (int)Math.Floor(Math.Min(x0, x1) - radius - feather));
        var minY = Math.Max(0, (int)Math.Floor(Math.Min(y0, y1) - radius - feather));
        var maxX = Math.Min(_pixelWidth - 1, (int)Math.Ceiling(Math.Max(x0, x1) + radius + feather));
        var maxY = Math.Min(_pixelHeight - 1, (int)Math.Ceiling(Math.Max(y0, y1) + radius + feather));
        var vx = x1 - x0;
        var vy = y1 - y0;
        var lengthSquared = vx * vx + vy * vy;

        if (lengthSquared <= 0.000001) {
            DrawSoftCirclePixels(x0, y0, radius, color);
            return;
        }

        for (var y = minY; y <= maxY; y++) for (var x = minX; x <= maxX; x++) {
            var px = x + 0.5;
            var py = y + 0.5;
            var t = ((px - x0) * vx + (py - y0) * vy) / lengthSquared;
            if (t < 0 || t > 1) continue;
            var closestX = x0 + vx * t;
            var closestY = y0 + vy * t;
            var dx = px - closestX;
            var dy = py - closestY;
            var distance = Math.Sqrt(dx * dx + dy * dy);
            if (distance <= radius) {
                BlendPixel(x, y, color);
            } else if (distance < radius + feather) {
                BlendPixel(x, y, WithOpacity(color, radius + feather - distance));
            }
        }
    }

    private static bool ShouldRoundPolylineJoin(IReadOnlyList<ChartPoint> points, int index, int scale) {
        var ax = (points[index].X - points[index - 1].X) * scale;
        var ay = (points[index].Y - points[index - 1].Y) * scale;
        var bx = (points[index + 1].X - points[index].X) * scale;
        var by = (points[index + 1].Y - points[index].Y) * scale;
        var aLength = Math.Sqrt(ax * ax + ay * ay);
        var bLength = Math.Sqrt(bx * bx + by * by);
        if (aLength <= 0.000001 || bLength <= 0.000001) return false;
        var cosine = (ax * bx + ay * by) / (aLength * bLength);
        return cosine < 0.985;
    }
}

using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal sealed partial class RgbaCanvas {
    public void DrawPolyline(IReadOnlyList<ChartPoint> points, ChartColor color, double thickness) {
        if (points == null) throw new ArgumentNullException(nameof(points));
        if (points.Count < 2) return;
        var scaledThickness = Math.Max(1, thickness * _scale);
        for (var i = 1; i < points.Count; i++) {
            DrawLinePixelsButt(points[i - 1].X * _scale, points[i - 1].Y * _scale, points[i].X * _scale, points[i].Y * _scale, scaledThickness, color);
        }

        var radius = Math.Max(0.5, scaledThickness / 2.0);
        DrawSoftCirclePixels(points[0].X * _scale, points[0].Y * _scale, radius, color);
        for (var i = 1; i < points.Count - 1; i++) {
            if (ShouldRoundPolylineJoin(points, i, _scale)) DrawSoftCirclePixels(points[i].X * _scale, points[i].Y * _scale, radius, color);
        }

        var last = points[points.Count - 1];
        DrawSoftCirclePixels(last.X * _scale, last.Y * _scale, radius, color);
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

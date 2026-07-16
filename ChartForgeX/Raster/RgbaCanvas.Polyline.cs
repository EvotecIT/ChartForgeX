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
    private const double DefaultMiterLimit = 4.0;

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

    internal void DrawPolyline(IReadOnlyList<ChartPoint> points, ChartColor color, double thickness, RasterLineCap lineCap, RasterLineJoin lineJoin, IReadOnlyList<double>? dashArray, double miterLimit = DefaultMiterLimit) {
        if (points == null) throw new ArgumentNullException(nameof(points));
        if (points.Count < 2) return;
        var scaledThickness = Math.Max(1, thickness * _scale);
        if (dashArray != null && dashArray.Count > 0) {
            DrawDashedPolyline(points, color, scaledThickness, lineCap, dashArray);
            return;
        }

        if (double.IsNaN(miterLimit) || double.IsInfinity(miterLimit) || miterLimit < 1) miterLimit = DefaultMiterLimit;
        var closed = IsClosedPolyline(points);

        for (var i = 1; i < points.Count; i++) {
            DrawLinePixelsButt(points[i - 1].X * _scale, points[i - 1].Y * _scale, points[i].X * _scale, points[i].Y * _scale, scaledThickness, color);
        }

        var radius = Math.Max(0.5, scaledThickness / 2.0);
        if (!closed && lineCap == RasterLineCap.Round) DrawSoftCirclePixels(points[0].X * _scale, points[0].Y * _scale, radius, color);
        for (var i = 1; i < points.Count - 1; i++) {
            if (lineJoin == RasterLineJoin.Round) {
                if (ShouldRoundPolylineJoin(points[i - 1], points[i], points[i + 1], _scale)) DrawSoftCirclePixels(points[i].X * _scale, points[i].Y * _scale, radius, color);
            } else {
                DrawPolylineJoin(points[i - 1], points[i], points[i + 1], color, thickness, lineJoin, miterLimit);
            }
        }

        var last = points[points.Count - 1];
        if (closed) {
            var previous = points[points.Count - 2];
            var first = points[0];
            var next = points[1];
            if (lineJoin == RasterLineJoin.Round) {
                if (ShouldRoundPolylineJoin(previous, first, next, _scale)) DrawSoftCirclePixels(first.X * _scale, first.Y * _scale, radius, color);
            } else {
                DrawPolylineJoin(previous, first, next, color, thickness, lineJoin, miterLimit);
            }
        } else if (lineCap == RasterLineCap.Round) {
            DrawSoftCirclePixels(last.X * _scale, last.Y * _scale, radius, color);
        }
    }

    private void DrawPolylineJoin(ChartPoint previous, ChartPoint current, ChartPoint next, ChartColor color, double thickness, RasterLineJoin lineJoin, double miterLimit) {
        var incomingX = current.X - previous.X;
        var incomingY = current.Y - previous.Y;
        var outgoingX = next.X - current.X;
        var outgoingY = next.Y - current.Y;
        var incomingLength = Math.Sqrt(incomingX * incomingX + incomingY * incomingY);
        var outgoingLength = Math.Sqrt(outgoingX * outgoingX + outgoingY * outgoingY);
        if (incomingLength <= 0.000001 || outgoingLength <= 0.000001) return;

        incomingX /= incomingLength;
        incomingY /= incomingLength;
        outgoingX /= outgoingLength;
        outgoingY /= outgoingLength;
        var cross = incomingX * outgoingY - incomingY * outgoingX;
        if (Math.Abs(cross) <= 0.000001) return;

        var outerSide = cross > 0 ? -1.0 : 1.0;
        var radius = Math.Max(0.5 / _scale, thickness / 2.0);
        var incomingOuter = new ChartPoint(
            current.X - incomingY * outerSide * radius,
            current.Y + incomingX * outerSide * radius);
        var outgoingOuter = new ChartPoint(
            current.X - outgoingY * outerSide * radius,
            current.Y + outgoingX * outerSide * radius);

        if (lineJoin == RasterLineJoin.Miter && TryMiterPoint(current, incomingOuter, outgoingOuter, incomingX, incomingY, outgoingX, outgoingY, radius, miterLimit, out var miter)) {
            FillPolygon(new[] { current, incomingOuter, miter, outgoingOuter }, color);
            return;
        }

        FillPolygon(new[] { current, incomingOuter, outgoingOuter }, color);
    }

    private static bool TryMiterPoint(ChartPoint current, ChartPoint incomingOuter, ChartPoint outgoingOuter, double incomingX, double incomingY, double outgoingX, double outgoingY, double radius, double miterLimit, out ChartPoint miter) {
        var denominator = incomingX * outgoingY - incomingY * outgoingX;
        var deltaX = outgoingOuter.X - incomingOuter.X;
        var deltaY = outgoingOuter.Y - incomingOuter.Y;
        var distance = (deltaX * outgoingY - deltaY * outgoingX) / denominator;
        miter = new ChartPoint(incomingOuter.X + incomingX * distance, incomingOuter.Y + incomingY * distance);
        var miterX = miter.X - current.X;
        var miterY = miter.Y - current.Y;
        var miterLength = Math.Sqrt(miterX * miterX + miterY * miterY);
        return !double.IsNaN(miterLength) && !double.IsInfinity(miterLength) && miterLength <= radius * miterLimit;
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

    private static bool IsClosedPolyline(IReadOnlyList<ChartPoint> points) {
        if (points.Count < 3) return false;
        var first = points[0];
        var last = points[points.Count - 1];
        var dx = first.X - last.X;
        var dy = first.Y - last.Y;
        return dx * dx + dy * dy <= 0.000001;
    }

    private static bool ShouldRoundPolylineJoin(ChartPoint previous, ChartPoint current, ChartPoint next, int scale) {
        var ax = (current.X - previous.X) * scale;
        var ay = (current.Y - previous.Y) * scale;
        var bx = (next.X - current.X) * scale;
        var by = (next.Y - current.Y) * scale;
        var aLength = Math.Sqrt(ax * ax + ay * ay);
        var bLength = Math.Sqrt(bx * bx + by * by);
        if (aLength <= 0.000001 || bLength <= 0.000001) return false;
        var cosine = (ax * bx + ay * by) / (aLength * bLength);
        return cosine < 0.985;
    }
}

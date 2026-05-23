using System;
using System.Collections.Generic;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal static class ChartPatternLineGeometry {
    public static IReadOnlyList<ChartPatternLine> Build(ChartFillPattern pattern, double x, double y, double width, double height, double radius, double spacing) {
        var lines = new List<ChartPatternLine>();
        if (pattern == ChartFillPattern.None || width <= 1 || height <= 1) return lines;
        if (pattern == ChartFillPattern.DiagonalForward || pattern == ChartFillPattern.Crosshatch) AddDirection(lines, x, y, width, height, radius, spacing, true);
        if (pattern == ChartFillPattern.DiagonalBackward || pattern == ChartFillPattern.Crosshatch) AddDirection(lines, x, y, width, height, radius, spacing, false);
        return lines;
    }

    private static void AddDirection(List<ChartPatternLine> lines, double x, double y, double width, double height, double radius, double spacing, bool forward) {
        spacing = Math.Max(4, spacing);
        for (var offset = -height; offset < width + height; offset += spacing) {
            var x0 = x + offset;
            var y0 = forward ? y + height : y;
            var x1 = x + offset + height;
            var y1 = forward ? y : y + height;
            if (ClipLineToRect(ref x0, ref y0, ref x1, ref y1, x, y, x + width, y + height)) AddRoundedClippedLine(lines, x0, y0, x1, y1, x, y, width, height, radius);
        }
    }

    private static void AddRoundedClippedLine(List<ChartPatternLine> lines, double x0, double y0, double x1, double y1, double rectX, double rectY, double width, double height, double radius) {
        radius = Math.Max(0, Math.Min(radius, Math.Min(width, height) / 2.0));
        if (radius <= 0.000001) {
            lines.Add(new ChartPatternLine(x0, y0, x1, y1));
            return;
        }

        var dx = x1 - x0;
        var dy = y1 - y0;
        var steps = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(dx * dx + dy * dy) / 1.5));
        var active = false;
        var startX = x0;
        var startY = y0;
        var previousX = x0;
        var previousY = y0;
        for (var i = 0; i <= steps; i++) {
            var t = i / (double)steps;
            var currentX = x0 + dx * t;
            var currentY = y0 + dy * t;
            var inside = IsInsideRoundedRect(currentX, currentY, rectX, rectY, width, height, radius);
            if (inside && !active) {
                startX = currentX;
                startY = currentY;
                active = true;
            } else if (!inside && active) {
                lines.Add(new ChartPatternLine(startX, startY, previousX, previousY));
                active = false;
            }

            previousX = currentX;
            previousY = currentY;
        }

        if (active) lines.Add(new ChartPatternLine(startX, startY, x1, y1));
    }

    private static bool IsInsideRoundedRect(double px, double py, double x, double y, double width, double height, double radius) {
        if (px < x || px > x + width || py < y || py > y + height) return false;
        var closestX = Math.Max(x + radius, Math.Min(x + width - radius, px));
        var closestY = Math.Max(y + radius, Math.Min(y + height - radius, py));
        var dx = px - closestX;
        var dy = py - closestY;
        return dx * dx + dy * dy <= radius * radius + 0.000001;
    }

    private static bool ClipLineToRect(ref double x0, ref double y0, ref double x1, ref double y1, double minX, double minY, double maxX, double maxY) {
        var dx = x1 - x0;
        var dy = y1 - y0;
        var t0 = 0.0;
        var t1 = 1.0;
        if (!ClipTest(-dx, x0 - minX, ref t0, ref t1)) return false;
        if (!ClipTest(dx, maxX - x0, ref t0, ref t1)) return false;
        if (!ClipTest(-dy, y0 - minY, ref t0, ref t1)) return false;
        if (!ClipTest(dy, maxY - y0, ref t0, ref t1)) return false;
        if (t1 < 1) {
            x1 = x0 + t1 * dx;
            y1 = y0 + t1 * dy;
        }

        if (t0 > 0) {
            x0 += t0 * dx;
            y0 += t0 * dy;
        }

        return true;
    }

    private static bool ClipTest(double p, double q, ref double t0, ref double t1) {
        if (Math.Abs(p) < 0.000001) return q >= 0;
        var r = q / p;
        if (p < 0) {
            if (r > t1) return false;
            if (r > t0) t0 = r;
        } else {
            if (r < t0) return false;
            if (r < t1) t1 = r;
        }

        return true;
    }
}

internal readonly struct ChartPatternLine {
    public ChartPatternLine(double x1, double y1, double x2, double y2) {
        X1 = x1;
        Y1 = y1;
        X2 = x2;
        Y2 = y2;
    }

    public double X1 { get; }

    public double Y1 { get; }

    public double X2 { get; }

    public double Y2 { get; }
}

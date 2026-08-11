using System;

namespace ChartForgeX.Raster;

internal sealed partial class RgbaCanvas {
    internal void DrawImageTransformed(
        int sourceWidth,
        int sourceHeight,
        byte[] rgba,
        double a,
        double b,
        double c,
        double d,
        double e,
        double f) {
        if (rgba == null) throw new ArgumentNullException(nameof(rgba));
        if (sourceWidth <= 0 || sourceHeight <= 0) return;
        if (rgba.Length < sourceWidth * sourceHeight * 4) throw new ArgumentException("RGBA buffer is smaller than the requested source dimensions.", nameof(rgba));
        var determinant = a * d - b * c;
        if (Math.Abs(determinant) < 0.000000001) return;

        TransformImagePoint(0, 0, a, b, c, d, e, f, out var x0, out var y0);
        TransformImagePoint(sourceWidth, 0, a, b, c, d, e, f, out var x1, out var y1);
        TransformImagePoint(0, sourceHeight, a, b, c, d, e, f, out var x2, out var y2);
        TransformImagePoint(sourceWidth, sourceHeight, a, b, c, d, e, f, out var x3, out var y3);
        var left = Math.Max(0, (int)Math.Floor(Math.Min(Math.Min(x0, x1), Math.Min(x2, x3)) * _scale));
        var top = Math.Max(0, (int)Math.Floor(Math.Min(Math.Min(y0, y1), Math.Min(y2, y3)) * _scale));
        var right = Math.Min(_pixelWidth, (int)Math.Ceiling(Math.Max(Math.Max(x0, x1), Math.Max(x2, x3)) * _scale));
        var bottom = Math.Min(_pixelHeight, (int)Math.Ceiling(Math.Max(Math.Max(y0, y1), Math.Max(y2, y3)) * _scale));

        for (var targetY = top; targetY < bottom; targetY++) for (var targetX = left; targetX < right; targetX++) {
            var canvasX = (targetX + 0.5) / _scale;
            var canvasY = (targetY + 0.5) / _scale;
            var translatedX = canvasX - e;
            var translatedY = canvasY - f;
            var sourceX = (d * translatedX - c * translatedY) / determinant;
            var sourceY = (-b * translatedX + a * translatedY) / determinant;
            if (sourceX < 0 || sourceY < 0 || sourceX >= sourceWidth || sourceY >= sourceHeight) continue;
            var color = SampleImageBilinear(rgba, sourceWidth, sourceHeight, sourceX - 0.5, sourceY - 0.5);
            if (color.A > 0) BlendPixel(targetX, targetY, color);
        }
    }

    private static void TransformImagePoint(double x, double y, double a, double b, double c, double d, double e, double f, out double targetX, out double targetY) {
        targetX = x * a + y * c + e;
        targetY = x * b + y * d + f;
    }
}

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
        double f,
        double opacity = 1, double canvasOriginX = 0, double canvasOriginY = 0) {
        DrawImageTransformedCore(sourceWidth, sourceHeight, rgba, a, b, c, d, e, f, opacity, false, canvasOriginX, canvasOriginY);
    }

    /// <summary>Composites completed native objects with the typed effect channel conversion.</summary>
    /// <remarks>The legacy image entrypoint keeps its arithmetic; both paths share this sampler and clipping.</remarks>
    internal void DrawCapturedImageTransformed(int sourceWidth, int sourceHeight, byte[] rgba,
        double a, double b, double c, double d, double e, double f, double opacity = 1) =>
        DrawImageTransformedCore(sourceWidth, sourceHeight, rgba, a, b, c, d, e, f, opacity, true);

    private void DrawImageTransformedCore(int sourceWidth, int sourceHeight, byte[] rgba,
        double a, double b, double c, double d, double e, double f, double opacity, bool captured, double canvasOriginX = 0, double canvasOriginY = 0) {
        if (rgba == null) throw new ArgumentNullException(nameof(rgba));
        if (sourceWidth <= 0 || sourceHeight <= 0 || opacity <= 0) return;
        if (rgba.Length < sourceWidth * sourceHeight * 4) throw new ArgumentException("RGBA buffer is smaller than the requested source dimensions.", nameof(rgba));
        var determinant = a * d - b * c;
        if (double.IsNaN(determinant) || double.IsInfinity(determinant) || Math.Abs(determinant) < 0.000000001) return;
        double footprintX = Math.Sqrt(d * d + c * c) / Math.Abs(determinant) / _scale;
        double footprintY = Math.Sqrt(b * b + a * a) / Math.Abs(determinant) / _scale;

        TransformImagePoint(0, 0, a, b, c, d, e, f, out var x0, out var y0);
        TransformImagePoint(sourceWidth, 0, a, b, c, d, e, f, out var x1, out var y1);
        TransformImagePoint(0, sourceHeight, a, b, c, d, e, f, out var x2, out var y2);
        TransformImagePoint(sourceWidth, sourceHeight, a, b, c, d, e, f, out var x3, out var y3);
        var left = Math.Max(0, (int)Math.Floor((Math.Min(Math.Min(x0, x1), Math.Min(x2, x3)) - canvasOriginX) * _scale));
        var top = Math.Max(0, (int)Math.Floor((Math.Min(Math.Min(y0, y1), Math.Min(y2, y3)) - canvasOriginY) * _scale));
        var right = Math.Min(_pixelWidth, (int)Math.Ceiling((Math.Max(Math.Max(x0, x1), Math.Max(x2, x3)) - canvasOriginX) * _scale));
        var bottom = Math.Min(_pixelHeight, (int)Math.Ceiling((Math.Max(Math.Max(y0, y1), Math.Max(y2, y3)) - canvasOriginY) * _scale));

        for (var targetY = top; targetY < bottom; targetY++) for (var targetX = left; targetX < right; targetX++) {
            // A cropped native capture retains the original pixel index BEFORE division. Rebasing
            // a fractional text/stroke origin would change interpolation rounding by a few ULPs.
            var canvasX = (targetX + canvasOriginX * _scale + 0.5) / _scale;
            var canvasY = (targetY + canvasOriginY * _scale + 0.5) / _scale;
            var translatedX = canvasX - e;
            var translatedY = canvasY - f;
            var sourceX = (d * translatedX - c * translatedY) / determinant;
            var sourceY = (-b * translatedX + a * translatedY) / determinant;
            if (sourceX < 0 || sourceY < 0 || sourceX >= sourceWidth || sourceY >= sourceHeight) continue;
            var color = SampleImageFiltered(rgba, sourceWidth, sourceHeight, sourceX - 0.5, sourceY - 0.5, footprintX, footprintY);
            if (opacity < 1) color = color.WithAlpha((byte)Math.Round(color.A * opacity));
            if (color.A > 0) {
                if (captured) BlendCapturedPixel(targetX, targetY, color);
                else BlendPixel(targetX, targetY, color);
            }
        }
        if (captured) HasCapturedEffectPixels = true;
    }

    private static void TransformImagePoint(double x, double y, double a, double b, double c, double d, double e, double f, out double targetX, out double targetY) {
        targetX = x * a + y * c + e;
        targetY = x * b + y * d + f;
    }
}

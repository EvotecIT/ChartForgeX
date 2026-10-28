using System;
using System.Threading;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal sealed partial class RgbaCanvas {
    /// <summary>SVG luminance is unassociated RGB in the mask's computed space, followed by mask alpha once.</summary>
    internal void DrawImageSvgMasked(int x, int y, int width, int height, byte[] rgba, byte[] mask,
        bool alphaOnly, bool linearRgb, CancellationToken cancellation, bool captured = false) {
        if (width <= 0 || height <= 0) return;
        if (rgba == null || mask == null || rgba.LongLength < checked((long)width * height * 4) || mask.LongLength < checked((long)width * height * 4))
            throw new ArgumentException("SVG masking requires complete source and mask RGBA surfaces.");
        for (var row = 0; row < height; row++) {
            cancellation.ThrowIfCancellationRequested();
            for (var column = 0; column < width; column++) {
                var dx = (long)x * _scale + column; var dy = (long)y * _scale + row;
                if (dx < 0 || dy < 0 || dx >= _pixelWidth || dy >= _pixelHeight) continue;
                var offset = (row * width + column) * 4;
                var coverage = RasterMaskCoverage.Coverage16(mask, offset, alphaOnly ? VisualMaskMode.Alpha : VisualMaskMode.Luminance, linearRgb);
                var alpha16 = RasterEffectPixels.Multiply(rgba[offset + 3] * 257, coverage);
                var alpha = (byte)((alpha16 + 128) / 257);
                if (alpha != 0) {
                    var color = ChartColor.FromRgba(rgba[offset], rgba[offset + 1], rgba[offset + 2], alpha);
                    if (captured) BlendCapturedPixel((int)dx, (int)dy, color); else BlendPixel((int)dx, (int)dy, color);
                }
            }
        }
        if (captured) HasCapturedEffectPixels = true;
    }
}

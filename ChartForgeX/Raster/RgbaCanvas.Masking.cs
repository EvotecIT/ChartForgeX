using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal sealed partial class RgbaCanvas {
    internal void DrawImageMasked(int x, int y, int width, int height, byte[] rgba, byte[] maskRgba) {
        if (rgba == null) throw new ArgumentNullException(nameof(rgba));
        if (maskRgba == null) throw new ArgumentNullException(nameof(maskRgba));
        if (width <= 0 || height <= 0) return;
        if (rgba.Length < width * height * 4) throw new ArgumentException("RGBA buffer is smaller than the requested image dimensions.", nameof(rgba));
        if (maskRgba.Length < width * height * 4) throw new ArgumentException("Mask RGBA buffer is smaller than the requested image dimensions.", nameof(maskRgba));
        var targetX0 = x * _scale;
        var targetY0 = y * _scale;
        for (var yy = 0; yy < height; yy++) for (var xx = 0; xx < width; xx++) {
            var dx = targetX0 + xx;
            var dy = targetY0 + yy;
            if (dx < 0 || dy < 0 || dx >= _pixelWidth || dy >= _pixelHeight) continue;
            var source = (yy * width + xx) * 4;
            var alpha = rgba[source + 3] * maskRgba[source + 3] / 255;
            if (alpha == 0) continue;
            BlendPixel(dx, dy, ChartColor.FromRgba(rgba[source], rgba[source + 1], rgba[source + 2], (byte)alpha));
        }
    }
}

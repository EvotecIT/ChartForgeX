using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

/// <summary>Glyph coverage strokes shared by native scenes and imported SVG text.</summary>
internal static class RasterTextStroke {
    internal const long MaximumIntermediatePixels = 8_000_000;

    internal static void Paint(byte[] destination, byte[] glyphPixels, int width, int height, int radius, ChartColor color) {
        if (width <= 0 || height <= 0 || radius < 0) throw new ArgumentOutOfRangeException(nameof(width));
        var pixels = (long)width * height;
        if (pixels > MaximumIntermediatePixels || destination.LongLength < pixels * 4 || glyphPixels.LongLength < pixels * 4)
            throw new ArgumentOutOfRangeException(nameof(width), "Text stroke exceeds the supported intermediate raster budget.");
        if (radius == 0 || color.A == 0) return;
        radius = Math.Min(radius, Math.Max(width, height));
        var dilated = RasterAlphaExtrema.Filter(glyphPixels, width, height, 4, 3, radius, radius, maximize: true);
        var eroded = RasterAlphaExtrema.Filter(glyphPixels, width, height, 4, 3, radius, radius, maximize: false);
        for (var pixel = 0; pixel < dilated.Length; pixel++) {
            var coverage = Math.Max(0, dilated[pixel] - eroded[pixel]);
            if (coverage == 0) continue;
            var index = pixel * 4;
            BlendTextPixel(destination, index, color, (byte)Math.Round(color.A * coverage / 255.0));
        }
    }

    private static void BlendTextPixel(byte[] destination, int index, ChartColor color, byte alpha) {
        if (alpha == 0) return;
        if (alpha == 255) {
            destination[index] = color.R;
            destination[index + 1] = color.G;
            destination[index + 2] = color.B;
            destination[index + 3] = 255;
            return;
        }
        var sourceAlpha = alpha / 255.0;
        var destinationAlpha = destination[index + 3] / 255.0;
        var outputAlpha = sourceAlpha + destinationAlpha * (1 - sourceAlpha);
        destination[index] = (byte)((color.R * sourceAlpha + destination[index] * destinationAlpha * (1 - sourceAlpha)) / outputAlpha);
        destination[index + 1] = (byte)((color.G * sourceAlpha + destination[index + 1] * destinationAlpha * (1 - sourceAlpha)) / outputAlpha);
        destination[index + 2] = (byte)((color.B * sourceAlpha + destination[index + 2] * destinationAlpha * (1 - sourceAlpha)) / outputAlpha);
        destination[index + 3] = (byte)(outputAlpha * 255);
    }
}

using System;
using System.IO;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal static class PpmWriter {
    public static byte[] WriteRgba(RgbaImage image, RasterImageOptions? options = null) {
        var header = GetHeader(image);
        var pixelBytes = checked(image.Width * image.Height * 3);
        using var stream = new MemoryStream(checked(header.Length + pixelBytes));
        stream.Write(header, 0, header.Length);
        WritePixels(stream, image, options);
        return stream.ToArray();
    }

    public static void WriteRgba(Stream stream, RgbaImage image, RasterImageOptions? options = null) {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        var header = GetHeader(image);
        stream.Write(header, 0, header.Length);
        WritePixels(stream, image, options);
    }

    private static byte[] GetHeader(RgbaImage image) {
        return Encoding.ASCII.GetBytes("P6\n" + image.Width + " " + image.Height + "\n255\n");
    }

    private static void WritePixels(Stream stream, RgbaImage image, RasterImageOptions? options) {
        var background = options?.Background ?? ChartColors.White;
        var row = new byte[checked(image.Width * 3)];
        var source = 0;
        for (var y = 0; y < image.Height; y++) {
            var target = 0;
            for (var x = 0; x < image.Width; x++) {
                RasterColorWriter.FillRgb(row, target, image.Pixels[source], image.Pixels[source + 1], image.Pixels[source + 2], image.Pixels[source + 3], background);
                source += 4;
                target += 3;
            }

            stream.Write(row, 0, row.Length);
        }
    }
}

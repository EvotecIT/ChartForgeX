using System;
using System.IO;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal static class BmpWriter {
    private const int FileHeaderSize = 14;
    private const int InfoHeaderSize = 40;
    private const int BitsPerPixel = 24;

    public static byte[] WriteRgba(RgbaImage image, RasterImageOptions? options = null) {
        var rowStride = checked(((image.Width * 3) + 3) / 4 * 4);
        var pixelBytes = checked(rowStride * image.Height);
        var fileSize = checked(FileHeaderSize + InfoHeaderSize + pixelBytes);
        using var stream = new MemoryStream(fileSize);
        WriteRgba(stream, image, options);
        return stream.ToArray();
    }

    public static void WriteRgba(Stream stream, RgbaImage image, RasterImageOptions? options = null) {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        var background = options?.Background ?? ChartColors.White;
        var rowStride = checked(((image.Width * 3) + 3) / 4 * 4);
        var pixelBytes = checked(rowStride * image.Height);
        var pixelOffset = FileHeaderSize + InfoHeaderSize;
        var fileSize = checked(pixelOffset + pixelBytes);
        stream.WriteByte((byte)'B');
        stream.WriteByte((byte)'M');
        WriteInt32(stream, fileSize);
        WriteInt16(stream, 0);
        WriteInt16(stream, 0);
        WriteInt32(stream, pixelOffset);
        WriteInt32(stream, InfoHeaderSize);
        WriteInt32(stream, image.Width);
        WriteInt32(stream, image.Height);
        WriteInt16(stream, 1);
        WriteInt16(stream, BitsPerPixel);
        WriteInt32(stream, 0);
        WriteInt32(stream, pixelBytes);
        WriteInt32(stream, 3780);
        WriteInt32(stream, 3780);
        WriteInt32(stream, 0);
        WriteInt32(stream, 0);

        var row = new byte[rowStride];
        for (var y = image.Height - 1; y >= 0; y--) {
            var source = y * image.Width * 4;
            var target = 0;
            for (var x = 0; x < image.Width; x++) {
                RasterColorWriter.FillBgr(row, target, image.Pixels[source], image.Pixels[source + 1], image.Pixels[source + 2], image.Pixels[source + 3], background);
                source += 4;
                target += 3;
            }

            stream.Write(row, 0, row.Length);
        }
    }

    private static void WriteInt16(Stream stream, int value) {
        stream.WriteByte((byte)(value & 255));
        stream.WriteByte((byte)((value >> 8) & 255));
    }

    private static void WriteInt32(Stream stream, int value) {
        stream.WriteByte((byte)(value & 255));
        stream.WriteByte((byte)((value >> 8) & 255));
        stream.WriteByte((byte)((value >> 16) & 255));
        stream.WriteByte((byte)((value >> 24) & 255));
    }
}

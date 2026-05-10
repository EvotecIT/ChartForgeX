using System;
using System.IO;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal static class TiffWriter {
    private const int IfdOffset = 8;
    private const int EntryCount = 13;
    private const int EntrySize = 12;

    public static byte[] WriteRgba(RgbaImage image, RasterImageOptions? options = null) {
        var bitsPerSampleOffset = IfdOffset + 2 + EntryCount * EntrySize + 4;
        var xResolutionOffset = bitsPerSampleOffset + 6;
        var yResolutionOffset = xResolutionOffset + 8;
        var pixelOffset = yResolutionOffset + 8;
        var pixelBytes = checked(image.Width * image.Height * 3);
        using var stream = new MemoryStream(checked(pixelOffset + pixelBytes));
        WriteRgba(stream, image, options);
        return stream.ToArray();
    }

    public static void WriteRgba(Stream stream, RgbaImage image, RasterImageOptions? options = null) {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        var background = options?.Background ?? ChartColors.White;
        var bitsPerSampleOffset = IfdOffset + 2 + EntryCount * EntrySize + 4;
        var xResolutionOffset = bitsPerSampleOffset + 6;
        var yResolutionOffset = xResolutionOffset + 8;
        var pixelOffset = yResolutionOffset + 8;
        var pixelBytes = checked(image.Width * image.Height * 3);
        stream.WriteByte((byte)'I');
        stream.WriteByte((byte)'I');
        WriteUInt16(stream, 42);
        WriteUInt32(stream, IfdOffset);
        WriteUInt16(stream, EntryCount);

        WriteEntry(stream, 256, 4, 1, image.Width);
        WriteEntry(stream, 257, 4, 1, image.Height);
        WriteEntry(stream, 258, 3, 3, bitsPerSampleOffset);
        WriteEntry(stream, 259, 3, 1, 1);
        WriteEntry(stream, 262, 3, 1, 2);
        WriteEntry(stream, 273, 4, 1, pixelOffset);
        WriteEntry(stream, 274, 3, 1, 1);
        WriteEntry(stream, 277, 3, 1, 3);
        WriteEntry(stream, 278, 4, 1, image.Height);
        WriteEntry(stream, 279, 4, 1, pixelBytes);
        WriteEntry(stream, 282, 5, 1, xResolutionOffset);
        WriteEntry(stream, 283, 5, 1, yResolutionOffset);
        WriteEntry(stream, 296, 3, 1, 2);
        WriteUInt32(stream, 0);

        WriteUInt16(stream, 8);
        WriteUInt16(stream, 8);
        WriteUInt16(stream, 8);
        WriteRational(stream, 96, 1);
        WriteRational(stream, 96, 1);

        WritePixels(stream, image, background);
    }

    private static void WritePixels(Stream stream, RgbaImage image, ChartColor background) {
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

    private static void WriteEntry(Stream stream, int tag, int type, int count, int valueOrOffset) {
        WriteUInt16(stream, tag);
        WriteUInt16(stream, type);
        WriteUInt32(stream, count);
        WriteUInt32(stream, valueOrOffset);
    }

    private static void WriteRational(Stream stream, int numerator, int denominator) {
        WriteUInt32(stream, numerator);
        WriteUInt32(stream, denominator);
    }

    private static void WriteUInt16(Stream stream, int value) {
        stream.WriteByte((byte)(value & 255));
        stream.WriteByte((byte)((value >> 8) & 255));
    }

    private static void WriteUInt32(Stream stream, int value) {
        stream.WriteByte((byte)(value & 255));
        stream.WriteByte((byte)((value >> 8) & 255));
        stream.WriteByte((byte)((value >> 16) & 255));
        stream.WriteByte((byte)((value >> 24) & 255));
    }
}

using System;
using System.IO;

namespace ChartForgeX.Raster;

internal static class BmpReader {
    public static bool IsBmp(byte[] data) => data != null && data.Length >= 2 && data[0] == (byte)'B' && data[1] == (byte)'M';

    public static RgbaImage Decode(byte[] data) {
        if (!IsBmp(data)) throw new NotSupportedException("Input is not a BMP image.");
        if (data.Length < 54) throw new InvalidDataException("BMP image is too short.");
        var pixelOffset = ReadInt32(data, 10);
        var dibSize = ReadInt32(data, 14);
        if (dibSize < 40) throw new NotSupportedException("Only BITMAPINFOHEADER BMP images are supported.");
        var width = ReadInt32(data, 18);
        var rawHeight = ReadInt32(data, 22);
        var planes = ReadUInt16(data, 26);
        var bitsPerPixel = ReadUInt16(data, 28);
        var compression = ReadInt32(data, 30);
        var colorsUsed = ReadInt32(data, 46);
        if (width <= 0 || rawHeight == 0) throw new InvalidDataException("BMP dimensions must be positive.");
        if (planes != 1) throw new InvalidDataException("BMP image has an invalid plane count.");
        if (compression != 0) throw new NotSupportedException("Compressed BMP images are not supported.");
        if (bitsPerPixel != 8 && bitsPerPixel != 24 && bitsPerPixel != 32) throw new NotSupportedException("Only 8-bit indexed, 24-bit, and 32-bit BMP images are supported.");

        var height = Math.Abs(rawHeight);
        var topDown = rawHeight < 0;
        var stride = checked(((width * bitsPerPixel) + 31) / 32 * 4);
        if (pixelOffset < 0 || pixelOffset + stride * height > data.Length) throw new InvalidDataException("BMP pixel data exceeds the input size.");
        var rgba = new byte[checked(width * height * 4)];
        if (bitsPerPixel == 8) {
            DecodeIndexed(data, pixelOffset, dibSize, colorsUsed, width, height, topDown, stride, rgba);
            return new RgbaImage(width, height, rgba);
        }

        var bytesPerPixel = bitsPerPixel / 8;
        for (var y = 0; y < height; y++) {
            var sourceY = topDown ? y : height - 1 - y;
            var source = pixelOffset + sourceY * stride;
            var target = y * width * 4;
            for (var x = 0; x < width; x++) {
                rgba[target++] = data[source + 2];
                rgba[target++] = data[source + 1];
                rgba[target++] = data[source];
                rgba[target++] = bytesPerPixel == 4 ? data[source + 3] : (byte)255;
                source += bytesPerPixel;
            }
        }

        return new RgbaImage(width, height, rgba);
    }

    private static void DecodeIndexed(byte[] data, int pixelOffset, int dibSize, int colorsUsed, int width, int height, bool topDown, int stride, byte[] rgba) {
        var paletteOffset = 14 + dibSize;
        var paletteEntries = colorsUsed > 0 ? colorsUsed : 256;
        if (paletteOffset < 0 || paletteOffset + paletteEntries * 4 > data.Length || paletteOffset + paletteEntries * 4 > pixelOffset) throw new InvalidDataException("BMP palette exceeds the input size.");
        for (var y = 0; y < height; y++) {
            var sourceY = topDown ? y : height - 1 - y;
            var source = pixelOffset + sourceY * stride;
            var target = y * width * 4;
            for (var x = 0; x < width; x++) {
                var index = data[source++];
                if (index >= paletteEntries) throw new InvalidDataException("BMP palette index is out of range.");
                var entry = paletteOffset + index * 4;
                rgba[target++] = data[entry + 2];
                rgba[target++] = data[entry + 1];
                rgba[target++] = data[entry];
                rgba[target++] = 255;
            }
        }
    }

    private static ushort ReadUInt16(byte[] data, int offset) => (ushort)(data[offset] | (data[offset + 1] << 8));

    private static int ReadInt32(byte[] data, int offset) =>
        data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24);
}

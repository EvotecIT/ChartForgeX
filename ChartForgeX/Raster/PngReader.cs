using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace ChartForgeX.Raster;

internal static class PngReader {
    private static readonly byte[] Signature = { 137, 80, 78, 71, 13, 10, 26, 10 };

    public static bool IsPng(byte[] data) {
        if (data == null || data.Length < Signature.Length) return false;
        for (var i = 0; i < Signature.Length; i++) {
            if (data[i] != Signature[i]) return false;
        }

        return true;
    }

    public static RgbaImage Decode(byte[] data) {
        if (!IsPng(data)) throw new NotSupportedException("Input is not a PNG image.");
        var offset = Signature.Length;
        var width = 0;
        var height = 0;
        var bitDepth = 0;
        var colorType = 0;
        byte[]? palette = null;
        byte[]? transparency = null;
        using var idat = new MemoryStream();

        while (offset + 12 <= data.Length) {
            var length = checked((int)ReadUInt32(data, offset));
            offset += 4;
            var type = Encoding.ASCII.GetString(data, offset, 4);
            var typeOffset = offset;
            offset += 4;
            if (length < 0 || offset + length + 4 > data.Length) throw new InvalidDataException("PNG chunk length exceeds the input size.");
            var expectedCrc = ReadUInt32(data, offset + length);
            var actualCrc = Crc32(data, typeOffset, checked(4 + length));
            if (expectedCrc != actualCrc) throw new InvalidDataException("PNG chunk CRC does not match.");
            if (type == "IHDR") {
                width = checked((int)ReadUInt32(data, offset));
                height = checked((int)ReadUInt32(data, offset + 4));
                bitDepth = data[offset + 8];
                colorType = data[offset + 9];
                if (data[offset + 12] != 0) throw new NotSupportedException("Interlaced PNG images are not supported.");
            } else if (type == "PLTE") {
                palette = Slice(data, offset, length);
            } else if (type == "tRNS") {
                transparency = Slice(data, offset, length);
            } else if (type == "IDAT") {
                idat.Write(data, offset, length);
            } else if (type == "IEND") {
                break;
            }

            offset += length + 4;
        }

        if (width <= 0 || height <= 0) throw new InvalidDataException("PNG image is missing a valid IHDR chunk.");
        if (bitDepth != 8) throw new NotSupportedException("Only 8-bit PNG images are supported.");
        var components = ComponentsFor(colorType);
        if (colorType == 3 && palette == null) throw new InvalidDataException("Indexed PNG image is missing a palette.");
        var raw = InflateZlib(idat.ToArray());
        var stride = checked(width * components);
        var expected = checked(height * (stride + 1));
        if (raw.Length < expected) throw new InvalidDataException("PNG image data is shorter than expected.");
        var unfiltered = Unfilter(raw, width, height, components, stride);
        return ToRgba(unfiltered, width, height, colorType, palette, transparency);
    }

    private static int ComponentsFor(int colorType) {
        switch (colorType) {
            case 0: return 1;
            case 2: return 3;
            case 3: return 1;
            case 4: return 2;
            case 6: return 4;
            default: throw new NotSupportedException("Unsupported PNG color type: " + colorType + ".");
        }
    }

    private static byte[] InflateZlib(byte[] data) {
        if (data.Length < 6) throw new InvalidDataException("PNG zlib stream is too short.");
        if ((data[0] & 0x0F) != 8) throw new NotSupportedException("Only deflate-compressed PNG zlib streams are supported.");
        if (((data[0] << 8) + data[1]) % 31 != 0) throw new InvalidDataException("PNG zlib header checksum is invalid.");
        if ((data[1] & 0x20) != 0) throw new NotSupportedException("PNG zlib streams with preset dictionaries are not supported.");
        using var input = new MemoryStream(data, 2, data.Length - 6);
        using var deflate = new DeflateStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        deflate.CopyTo(output);
        var inflated = output.ToArray();
        if (ReadUInt32(data, data.Length - 4) != Adler32(inflated)) throw new InvalidDataException("PNG zlib Adler-32 checksum does not match.");
        return inflated;
    }

    private static byte[] Unfilter(byte[] raw, int width, int height, int bpp, int stride) {
        var output = new byte[checked(height * stride)];
        var source = 0;
        for (var y = 0; y < height; y++) {
            var filter = raw[source++];
            var row = y * stride;
            for (var x = 0; x < stride; x++) {
                var value = raw[source++];
                var left = x >= bpp ? output[row + x - bpp] : 0;
                var up = y > 0 ? output[row - stride + x] : 0;
                var upLeft = y > 0 && x >= bpp ? output[row - stride + x - bpp] : 0;
                output[row + x] = (byte)(value + Predictor(filter, left, up, upLeft));
            }
        }

        return output;
    }

    private static int Predictor(int filter, int left, int up, int upLeft) {
        switch (filter) {
            case 0: return 0;
            case 1: return left;
            case 2: return up;
            case 3: return (left + up) / 2;
            case 4: return Paeth(left, up, upLeft);
            default: throw new InvalidDataException("Unknown PNG scanline filter: " + filter + ".");
        }
    }

    private static int Paeth(int left, int up, int upLeft) {
        var p = left + up - upLeft;
        var pa = Math.Abs(p - left);
        var pb = Math.Abs(p - up);
        var pc = Math.Abs(p - upLeft);
        if (pa <= pb && pa <= pc) return left;
        return pb <= pc ? up : upLeft;
    }

    private static RgbaImage ToRgba(byte[] pixels, int width, int height, int colorType, byte[]? palette, byte[]? transparency) {
        var rgba = new byte[checked(width * height * 4)];
        var source = 0;
        var target = 0;
        for (var i = 0; i < width * height; i++) {
            switch (colorType) {
                case 0:
                    var gray = pixels[source++];
                    rgba[target++] = gray; rgba[target++] = gray; rgba[target++] = gray; rgba[target++] = 255;
                    if (transparency != null && transparency.Length >= 2 && gray == ReadUInt16LowByte(transparency, 0)) rgba[target - 1] = 0;
                    break;
                case 2:
                    var r = pixels[source++];
                    var green = pixels[source++];
                    var b = pixels[source++];
                    rgba[target++] = r; rgba[target++] = green; rgba[target++] = b; rgba[target++] = 255;
                    if (transparency != null && transparency.Length >= 6 && r == ReadUInt16LowByte(transparency, 0) && green == ReadUInt16LowByte(transparency, 2) && b == ReadUInt16LowByte(transparency, 4)) rgba[target - 1] = 0;
                    break;
                case 3:
                    var index = pixels[source++];
                    var paletteOffset = index * 3;
                    if (palette == null || paletteOffset + 2 >= palette.Length) throw new InvalidDataException("PNG palette index is out of range.");
                    rgba[target++] = palette[paletteOffset]; rgba[target++] = palette[paletteOffset + 1]; rgba[target++] = palette[paletteOffset + 2];
                    rgba[target++] = transparency != null && index < transparency.Length ? transparency[index] : (byte)255;
                    break;
                case 4:
                    var g = pixels[source++];
                    rgba[target++] = g; rgba[target++] = g; rgba[target++] = g; rgba[target++] = pixels[source++];
                    break;
                case 6:
                    rgba[target++] = pixels[source++]; rgba[target++] = pixels[source++]; rgba[target++] = pixels[source++]; rgba[target++] = pixels[source++];
                    break;
            }
        }

        return new RgbaImage(width, height, rgba);
    }

    private static uint ReadUInt32(byte[] data, int offset) =>
        ((uint)data[offset] << 24) | ((uint)data[offset + 1] << 16) | ((uint)data[offset + 2] << 8) | data[offset + 3];

    private static byte ReadUInt16LowByte(byte[] data, int offset) => data[offset + 1];

    private static uint Adler32(byte[] data) {
        const uint mod = 65521;
        uint a = 1;
        uint b = 0;
        foreach (var value in data) {
            a = (a + value) % mod;
            b = (b + a) % mod;
        }

        return (b << 16) | a;
    }

    private static uint Crc32(byte[] data, int offset, int length) {
        uint crc = 0xffffffff;
        for (var i = 0; i < length; i++) {
            crc ^= data[offset + i];
            for (var bit = 0; bit < 8; bit++) crc = (crc & 1) == 1 ? 0xedb88320 ^ (crc >> 1) : crc >> 1;
        }

        return ~crc;
    }

    private static byte[] Slice(byte[] data, int offset, int length) {
        var copy = new byte[length];
        Buffer.BlockCopy(data, offset, copy, 0, length);
        return copy;
    }
}

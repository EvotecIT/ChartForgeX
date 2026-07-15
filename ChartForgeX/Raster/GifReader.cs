using System;
using System.Collections.Generic;
using System.IO;

namespace ChartForgeX.Raster;

internal static class GifReader {
    public static bool IsGif(byte[] data) => data != null && data.Length >= 6 && data[0] == (byte)'G' && data[1] == (byte)'I' && data[2] == (byte)'F' && data[3] == (byte)'8' && (data[4] == (byte)'7' || data[4] == (byte)'9') && data[5] == (byte)'a';

    public static RgbaImage Decode(byte[] data) {
        if (!IsGif(data)) throw new InvalidDataException("Input is not a GIF image.");
        var reader = new Reader(data);
        reader.Skip(6);
        var canvasWidth = reader.UInt16();
        var canvasHeight = reader.UInt16();
        RasterAllocationGuard.Calculate(canvasWidth, canvasHeight, 1, 1);
        var screenFlags = reader.Byte();
        reader.Byte();
        reader.Byte();
        var globalPalette = (screenFlags & 0x80) != 0 ? reader.Palette(1 << ((screenFlags & 0x07) + 1)) : null;
        var transparentIndex = -1;

        while (!reader.End) {
            var marker = reader.Byte();
            if (marker == 0x3B) break;
            if (marker == 0x21) {
                var label = reader.Byte();
                if (label == 0xF9) {
                    if (reader.Byte() != 4) throw new InvalidDataException("Invalid GIF graphic control extension.");
                    var flags = reader.Byte();
                    reader.UInt16();
                    var index = reader.Byte();
                    if (reader.Byte() != 0) throw new InvalidDataException("Invalid GIF graphic control terminator.");
                    transparentIndex = (flags & 0x01) != 0 ? index : -1;
                } else {
                    reader.SkipSubBlocks();
                }
                continue;
            }

            if (marker != 0x2C) throw new InvalidDataException("Unsupported GIF block marker.");
            var left = reader.UInt16();
            var top = reader.UInt16();
            var width = reader.UInt16();
            var height = reader.UInt16();
            if (width == 0 || height == 0 || left + width > canvasWidth || top + height > canvasHeight) throw new InvalidDataException("GIF image bounds exceed the logical screen.");
            var imageFlags = reader.Byte();
            var palette = (imageFlags & 0x80) != 0 ? reader.Palette(1 << ((imageFlags & 0x07) + 1)) : globalPalette;
            if (palette == null) throw new InvalidDataException("GIF image does not define a color table.");
            var minimumCodeSize = reader.Byte();
            var compressed = reader.ReadSubBlocks();
            var indices = DecodeLzw(compressed, minimumCodeSize, checked(width * height));
            if ((imageFlags & 0x40) != 0) indices = Deinterlace(indices, width, height);

            var pixels = new byte[checked(canvasWidth * canvasHeight * 4)];
            for (var y = 0; y < height; y++) {
                for (var x = 0; x < width; x++) {
                    var colorIndex = indices[y * width + x];
                    if (colorIndex == transparentIndex) continue;
                    var paletteOffset = colorIndex * 3;
                    if (paletteOffset + 2 >= palette.Length) throw new InvalidDataException("GIF pixel references a color outside its table.");
                    var offset = ((top + y) * canvasWidth + left + x) * 4;
                    pixels[offset] = palette[paletteOffset];
                    pixels[offset + 1] = palette[paletteOffset + 1];
                    pixels[offset + 2] = palette[paletteOffset + 2];
                    pixels[offset + 3] = 255;
                }
            }
            return new RgbaImage(canvasWidth, canvasHeight, pixels);
        }

        throw new InvalidDataException("GIF image does not contain an image frame.");
    }

    private static byte[] DecodeLzw(byte[] data, int minimumCodeSize, int expectedCount) {
        if (minimumCodeSize < 2 || minimumCodeSize > 8) throw new InvalidDataException("Unsupported GIF LZW minimum code size.");
        var clearCode = 1 << minimumCodeSize;
        var endCode = clearCode + 1;
        var prefix = new short[4096];
        var suffix = new byte[4096];
        var stack = new byte[4097];
        for (var i = 0; i < clearCode; i++) suffix[i] = (byte)i;
        var output = new byte[expectedCount];
        var outputOffset = 0;
        var bitOffset = 0;
        var codeSize = minimumCodeSize + 1;
        var nextCode = endCode + 1;
        var oldCode = -1;
        byte first = 0;

        while (outputOffset < expectedCount) {
            var code = ReadCode(data, ref bitOffset, codeSize);
            if (code < 0) break;
            if (code == clearCode) {
                codeSize = minimumCodeSize + 1;
                nextCode = endCode + 1;
                oldCode = -1;
                continue;
            }
            if (code == endCode) break;
            if (code > nextCode || code >= 4096) throw new InvalidDataException("Invalid GIF LZW code stream.");

            if (oldCode < 0) {
                if (code >= clearCode) throw new InvalidDataException("Invalid GIF LZW first code.");
                output[outputOffset++] = suffix[code];
                first = suffix[code];
                oldCode = code;
                continue;
            }

            var inputCode = code;
            var stackOffset = 0;
            if (code == nextCode) {
                stack[stackOffset++] = first;
                code = oldCode;
            }
            while (code >= clearCode) {
                if (code >= nextCode || stackOffset >= stack.Length - 1) throw new InvalidDataException("Invalid GIF LZW dictionary reference.");
                stack[stackOffset++] = suffix[code];
                code = prefix[code];
            }
            first = suffix[code];
            stack[stackOffset++] = first;
            while (stackOffset > 0 && outputOffset < expectedCount) output[outputOffset++] = stack[--stackOffset];

            if (nextCode < 4096) {
                prefix[nextCode] = (short)oldCode;
                suffix[nextCode] = first;
                nextCode++;
                if (nextCode == (1 << codeSize) && codeSize < 12) codeSize++;
            }
            oldCode = inputCode;
        }

        if (outputOffset != expectedCount) throw new InvalidDataException("GIF LZW stream ended before the image was complete.");
        return output;
    }

    private static int ReadCode(byte[] data, ref int bitOffset, int bitCount) {
        if (bitOffset + bitCount > data.Length * 8) return -1;
        var value = 0;
        for (var bit = 0; bit < bitCount; bit++) value |= ((data[(bitOffset + bit) >> 3] >> ((bitOffset + bit) & 7)) & 1) << bit;
        bitOffset += bitCount;
        return value;
    }

    private static byte[] Deinterlace(byte[] source, int width, int height) {
        var result = new byte[source.Length];
        var offset = 0;
        var starts = new[] { 0, 4, 2, 1 };
        var steps = new[] { 8, 8, 4, 2 };
        for (var pass = 0; pass < starts.Length; pass++) {
            for (var y = starts[pass]; y < height; y += steps[pass]) {
                Buffer.BlockCopy(source, offset, result, y * width, width);
                offset += width;
            }
        }
        return result;
    }

    private sealed class Reader {
        private readonly byte[] _data;
        private int _offset;
        public Reader(byte[] data) { _data = data; }
        public bool End => _offset >= _data.Length;
        public byte Byte() { Require(1); return _data[_offset++]; }
        public int UInt16() { Require(2); var value = _data[_offset] | (_data[_offset + 1] << 8); _offset += 2; return value; }
        public void Skip(int count) { Require(count); _offset += count; }
        public byte[] Palette(int entries) { var count = checked(entries * 3); Require(count); var result = new byte[count]; Buffer.BlockCopy(_data, _offset, result, 0, count); _offset += count; return result; }
        public void SkipSubBlocks() { while (true) { var count = Byte(); if (count == 0) return; Skip(count); } }
        public byte[] ReadSubBlocks() { var bytes = new List<byte>(); while (true) { var count = Byte(); if (count == 0) return bytes.ToArray(); Require(count); for (var i = 0; i < count; i++) bytes.Add(_data[_offset + i]); _offset += count; } }
        private void Require(int count) { if (count < 0 || _offset > _data.Length - count) throw new InvalidDataException("Unexpected end of GIF data."); }
    }
}

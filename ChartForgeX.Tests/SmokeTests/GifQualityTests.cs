using System;
using ChartForgeX.Raster;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void GifWriterUsesAdaptivePaletteForBrandColors() {
        var pixels = new byte[16 * 16 * 4];
        for (var i = 0; i < 16 * 16; i++) {
            var offset = i * 4;
            pixels[offset] = i % 2 == 0 ? (byte)37 : (byte)249;
            pixels[offset + 1] = i % 2 == 0 ? (byte)99 : (byte)115;
            pixels[offset + 2] = i % 2 == 0 ? (byte)235 : (byte)22;
            pixels[offset + 3] = 255;
        }

        var gif = GifWriter.WriteRgba(new[] { new RgbaImage(16, 16, pixels) }, 10, loop: false);
        Assert(gif.Length > 800, "Adaptive GIF encoding should produce a complete GIF with a global palette and image data.");
        Assert(PaletteContains(gif, 37, 99, 235, 2), "Adaptive GIF palette should preserve non-RGB332 blue brand colors.");
        Assert(PaletteContains(gif, 249, 115, 22, 2), "Adaptive GIF palette should preserve non-RGB332 orange brand colors.");
        Assert(!System.Text.Encoding.ASCII.GetString(gif).Contains("NETSCAPE2.0", StringComparison.Ordinal), "Non-looping GIFs should not include the looping extension.");
        Assert(ReadDecodedFramePixelCounts(gif)[0] == 16 * 16, "Adaptive GIF image data should decode to the full frame pixel count.");
    }

    private static void GifWriterUsesDeltaFramesForSmallMotion() {
        var first = SolidFrame(16, 16, 12, 18, 24);
        var second = SolidFrame(16, 16, 12, 18, 24);
        var changed = (5 * 16 + 4) * 4;
        second.Pixels[changed] = 249;
        second.Pixels[changed + 1] = 115;
        second.Pixels[changed + 2] = 22;

        var gif = GifWriter.WriteRgba(new[] { first, second }, 10, loop: true);
        var frames = ReadImageDescriptors(gif);
        Assert(frames.Length == 2, "Animated GIF encoding should preserve the requested frame count.");
        Assert(frames[0].Left == 0 && frames[0].Top == 0 && frames[0].Width == 16 && frames[0].Height == 16, "The first GIF frame should establish the full canvas.");
        Assert(frames[1].Left == 4 && frames[1].Top == 5 && frames[1].Width == 1 && frames[1].Height == 1, "Small motion should encode as a cropped delta frame instead of a full frame.");
        var decodedCounts = ReadDecodedFramePixelCounts(gif);
        Assert(decodedCounts.Length == 2 && decodedCounts[0] == 16 * 16 && decodedCounts[1] == 1, "Animated GIF image data should decode to each frame rectangle size.");
        AssertThrows<ArgumentException>(() => GifWriter.WriteRgba(new[] { first, SolidFrame(8, 16, 12, 18, 24) }, 10, loop: false), "GIF export should reject mismatched animated raster frame dimensions.");
    }

    private static void GifWriterClearsBeforeLzwWidthExpansion() {
        const int width = 80;
        const int height = 16;
        var pixels = new byte[width * height * 4];
        for (var i = 0; i < width * height; i++) {
            var value = (byte)((i * 37 + i / 7) & 255);
            var offset = i * 4;
            pixels[offset] = value;
            pixels[offset + 1] = (byte)(255 - value);
            pixels[offset + 2] = (byte)((value * 17) & 255);
            pixels[offset + 3] = 255;
        }

        var gif = GifWriter.WriteRgba(new[] { new RgbaImage(width, height, pixels) }, 10, loop: false);
        var decodedCounts = ReadDecodedFramePixelCounts(gif);
        Assert(decodedCounts.Length == 1 && decodedCounts[0] == width * height, "GIF LZW should clear before decoders switch to the next code width.");
    }

    private static RgbaImage SolidFrame(int width, int height, byte red, byte green, byte blue) {
        var pixels = new byte[width * height * 4];
        for (var i = 0; i < width * height; i++) {
            var offset = i * 4;
            pixels[offset] = red;
            pixels[offset + 1] = green;
            pixels[offset + 2] = blue;
            pixels[offset + 3] = 255;
        }

        return new RgbaImage(width, height, pixels);
    }

    private static bool PaletteContains(byte[] gif, byte red, byte green, byte blue, int tolerance) {
        const int paletteStart = 13;
        const int paletteLength = 256 * 3;
        for (var i = paletteStart; i < paletteStart + paletteLength; i += 3) {
            if (Math.Abs(gif[i] - red) > tolerance) continue;
            if (Math.Abs(gif[i + 1] - green) > tolerance) continue;
            if (Math.Abs(gif[i + 2] - blue) > tolerance) continue;
            return true;
        }

        return false;
    }

    private static GifImageDescriptor[] ReadImageDescriptors(byte[] gif) {
        var frames = new System.Collections.Generic.List<GifImageDescriptor>();
        var offset = 13 + 768;
        while (offset < gif.Length && gif[offset] != 0x3B) {
            if (gif[offset] == 0x21) {
                offset += 2;
                SkipSubBlocks(gif, ref offset);
                continue;
            }

            if (gif[offset] != 0x2C) throw new InvalidOperationException("Unexpected GIF block while reading image descriptors.");
            var left = ReadUInt16(gif, offset + 1);
            var top = ReadUInt16(gif, offset + 3);
            var width = ReadUInt16(gif, offset + 5);
            var height = ReadUInt16(gif, offset + 7);
            frames.Add(new GifImageDescriptor(left, top, width, height));
            var packed = gif[offset + 9];
            offset += 10;
            if ((packed & 0x80) != 0) offset += 3 * (1 << ((packed & 0x07) + 1));
            offset++;
            SkipSubBlocks(gif, ref offset);
        }

        return frames.ToArray();
    }

    private static int[] ReadDecodedFramePixelCounts(byte[] gif) {
        var counts = new System.Collections.Generic.List<int>();
        var offset = 13 + 768;
        while (offset < gif.Length && gif[offset] != 0x3B) {
            if (gif[offset] == 0x21) {
                offset += 2;
                SkipSubBlocks(gif, ref offset);
                continue;
            }

            if (gif[offset] != 0x2C) throw new InvalidOperationException("Unexpected GIF block while decoding image data.");
            var packed = gif[offset + 9];
            offset += 10;
            if ((packed & 0x80) != 0) offset += 3 * (1 << ((packed & 0x07) + 1));
            var minimumCodeSize = gif[offset++];
            var data = ReadSubBlocks(gif, ref offset);
            counts.Add(DecodeLzwPixelCount(data, minimumCodeSize));
        }

        return counts.ToArray();
    }

    private static byte[] ReadSubBlocks(byte[] gif, ref int offset) {
        var bytes = new System.Collections.Generic.List<byte>();
        while (offset < gif.Length) {
            var length = gif[offset++];
            if (length == 0) return bytes.ToArray();
            for (var i = 0; i < length; i++) bytes.Add(gif[offset + i]);
            offset += length;
        }

        throw new InvalidOperationException("GIF sub-block data did not terminate.");
    }

    private static int DecodeLzwPixelCount(byte[] data, int minimumCodeSize) {
        var clearCode = 1 << minimumCodeSize;
        var endCode = clearCode + 1;
        var dictionary = CreateLzwDictionary(clearCode);
        var codeSize = minimumCodeSize + 1;
        var nextCode = endCode + 1;
        byte[]? previous = null;
        var count = 0;
        var reader = new GifBitReader(data);
        while (reader.TryRead(codeSize, out var code)) {
            if (code == clearCode) {
                dictionary = CreateLzwDictionary(clearCode);
                codeSize = minimumCodeSize + 1;
                nextCode = endCode + 1;
                previous = null;
                continue;
            }

            if (code == endCode) return count;
            byte[] entry;
            if (code < dictionary.Count) entry = dictionary[code];
            else if (code == nextCode && previous != null) entry = Append(previous, previous[0]);
            else throw new InvalidOperationException("GIF LZW stream referenced an invalid code.");
            count += entry.Length;
            if (previous != null && nextCode < 4096) {
                dictionary.Add(Append(previous, entry[0]));
                nextCode++;
                if (nextCode == (1 << codeSize) && codeSize < 12) codeSize++;
            }

            previous = entry;
        }

        throw new InvalidOperationException("GIF LZW stream ended before the end code.");
    }

    private static System.Collections.Generic.List<byte[]> CreateLzwDictionary(int clearCode) {
        var dictionary = new System.Collections.Generic.List<byte[]>(clearCode + 2);
        for (var i = 0; i < clearCode; i++) dictionary.Add(new[] { (byte)i });
        dictionary.Add(System.Array.Empty<byte>());
        dictionary.Add(System.Array.Empty<byte>());
        return dictionary;
    }

    private static byte[] Append(byte[] bytes, byte value) {
        var result = new byte[bytes.Length + 1];
        System.Buffer.BlockCopy(bytes, 0, result, 0, bytes.Length);
        result[bytes.Length] = value;
        return result;
    }

    private static void SkipSubBlocks(byte[] gif, ref int offset) {
        while (offset < gif.Length) {
            var length = gif[offset++];
            if (length == 0) return;
            offset += length;
        }
    }

    private static int ReadUInt16(byte[] bytes, int offset) => bytes[offset] | (bytes[offset + 1] << 8);

    private readonly struct GifImageDescriptor {
        public GifImageDescriptor(int left, int top, int width, int height) {
            Left = left;
            Top = top;
            Width = width;
            Height = height;
        }

        public readonly int Left;
        public readonly int Top;
        public readonly int Width;
        public readonly int Height;
    }

    private ref struct GifBitReader {
        private readonly byte[] _bytes;
        private int _bitOffset;

        public GifBitReader(byte[] bytes) {
            _bytes = bytes;
            _bitOffset = 0;
        }

        public bool TryRead(int bitCount, out int value) {
            if (_bitOffset + bitCount > _bytes.Length * 8) {
                value = 0;
                return false;
            }

            value = 0;
            for (var bit = 0; bit < bitCount; bit++) {
                var absolute = _bitOffset + bit;
                if ((_bytes[absolute / 8] & (1 << (absolute % 8))) != 0) value |= 1 << bit;
            }

            _bitOffset += bitCount;
            return true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ChartForgeX.Raster;

internal static class GifWriter {
    public static byte[] WriteRgba(IReadOnlyList<RgbaImage> frames, int delayCentiseconds, bool loop) {
        using var stream = new MemoryStream();
        WriteRgba(stream, frames, delayCentiseconds, loop);
        return stream.ToArray();
    }

    public static void WriteRgba(Stream stream, IReadOnlyList<RgbaImage> frames, int delayCentiseconds, bool loop) {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        var animation = AnimatedRasterFrames.Create(frames, delayCentiseconds, loop, "GIF");
        WriteRgba(stream, animation);
    }

    public static void WriteRgba(Stream stream, AnimatedRasterFrames animation) {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (animation == null) throw new ArgumentNullException(nameof(animation));
        WriteAscii(stream, "GIF89a");
        WriteUInt16(stream, animation.Width);
        WriteUInt16(stream, animation.Height);
        var palette = GifPaletteQuantizer.BuildPalette(animation.Frames);
        stream.WriteByte(0xF7);
        stream.WriteByte(0);
        stream.WriteByte(0);
        WritePalette(stream, palette);
        if (animation.Loop) WriteLoopExtension(stream);
        var indexedFrames = GifFrameOptimizer.BuildFrames(animation.Frames, palette);
        foreach (var frame in indexedFrames) WriteFrame(stream, frame, animation.DelayCentiseconds);
        stream.WriteByte(0x3B);
    }

    private static void WriteFrame(Stream stream, GifIndexedFrame frame, int delayCentiseconds) {
        stream.WriteByte(0x21);
        stream.WriteByte(0xF9);
        stream.WriteByte(4);
        stream.WriteByte(0);
        WriteUInt16(stream, delayCentiseconds);
        stream.WriteByte(0);
        stream.WriteByte(0);
        stream.WriteByte(0x2C);
        WriteUInt16(stream, frame.Left);
        WriteUInt16(stream, frame.Top);
        WriteUInt16(stream, frame.Width);
        WriteUInt16(stream, frame.Height);
        stream.WriteByte(0);
        stream.WriteByte(8);
        WriteSubBlocks(stream, LzwEncode(frame.Pixels, 8));
    }

    private static byte[] LzwEncode(byte[] indices, int minimumCodeSize) {
        var clearCode = 1 << minimumCodeSize;
        var endCode = clearCode + 1;
        var nextCode = endCode + 1;
        var codeSize = minimumCodeSize + 1;
        var resetBeforeCode = (1 << codeSize) - 1;
        var dictionary = CreateDictionary();
        var writer = new BitWriter();
        writer.Write(clearCode, codeSize);
        var prefix = indices.Length == 0 ? string.Empty : ((char)indices[0]).ToString();
        for (var i = 1; i < indices.Length; i++) {
            var value = (char)indices[i];
            var candidate = prefix + value;
            if (dictionary.ContainsKey(candidate)) {
                prefix = candidate;
                continue;
            }

            writer.Write(dictionary[prefix], codeSize);
            if (nextCode < resetBeforeCode) {
                dictionary[candidate] = nextCode++;
            } else {
                writer.Write(clearCode, codeSize);
                dictionary = CreateDictionary();
                nextCode = endCode + 1;
            }

            prefix = value.ToString();
        }

        if (prefix.Length > 0) writer.Write(dictionary[prefix], codeSize);
        writer.Write(endCode, codeSize);
        return writer.ToArray();
    }

    private static Dictionary<string, int> CreateDictionary() {
        var dictionary = new Dictionary<string, int>(256, StringComparer.Ordinal);
        for (var i = 0; i < 256; i++) dictionary[((char)i).ToString()] = i;
        return dictionary;
    }

    private static void WritePalette(Stream stream, GifPalette palette) {
        for (var i = 0; i < 256; i++) {
            var offset = i * 3;
            stream.WriteByte(palette.Colors[offset]);
            stream.WriteByte(palette.Colors[offset + 1]);
            stream.WriteByte(palette.Colors[offset + 2]);
        }
    }

    private static void WriteLoopExtension(Stream stream) {
        stream.WriteByte(0x21);
        stream.WriteByte(0xFF);
        stream.WriteByte(11);
        WriteAscii(stream, "NETSCAPE2.0");
        stream.WriteByte(3);
        stream.WriteByte(1);
        WriteUInt16(stream, 0);
        stream.WriteByte(0);
    }

    private static void WriteSubBlocks(Stream stream, byte[] bytes) {
        var offset = 0;
        while (offset < bytes.Length) {
            var count = Math.Min(255, bytes.Length - offset);
            stream.WriteByte((byte)count);
            stream.Write(bytes, offset, count);
            offset += count;
        }

        stream.WriteByte(0);
    }

    private static void WriteAscii(Stream stream, string value) {
        var bytes = Encoding.ASCII.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static void WriteUInt16(Stream stream, int value) {
        stream.WriteByte((byte)(value & 255));
        stream.WriteByte((byte)((value >> 8) & 255));
    }

    private sealed class BitWriter {
        private readonly List<byte> _bytes = new();
        private int _current;
        private int _bits;

        public void Write(int code, int bitCount) {
            _current |= code << _bits;
            _bits += bitCount;
            while (_bits >= 8) {
                _bytes.Add((byte)(_current & 255));
                _current >>= 8;
                _bits -= 8;
            }
        }

        public byte[] ToArray() {
            if (_bits > 0) {
                _bytes.Add((byte)(_current & 255));
                _current = 0;
                _bits = 0;
            }

            return _bytes.ToArray();
        }
    }
}

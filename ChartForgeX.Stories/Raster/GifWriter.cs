using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace ChartForgeX.Raster;

internal static partial class GifWriter {
    private const int MaximumGifFieldValue = 65535;

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

    public static void WriteRgba(Stream stream, AnimatedRasterFrames animation, CancellationToken cancellationToken = default) {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (animation == null) throw new ArgumentNullException(nameof(animation));
        ValidateLogicalScreen(animation);
        var source = new RasterAnimationSource(animation.Width, animation.Height, animation.Frames.Count,
            (index, _) => new RasterAnimationFrame(animation.Frames[index], TimeSpan.FromMilliseconds(animation.DelayForFrame(index) * 10d)));
        WriteSource(stream, source, animation.PlayCount, cancellationToken);
    }

    private static void WriteFrame(Stream stream, GifIndexedFrame frame, int delayCentiseconds, GifPalette palette, CancellationToken cancellationToken, bool localPalette = false) {
        ValidateFrame(frame);
        stream.WriteByte(0x21);
        stream.WriteByte(0xF9);
        stream.WriteByte(4);
        stream.WriteByte(palette.HasTransparency ? (byte)0x09 : (byte)0x04);
        WriteUInt16(stream, delayCentiseconds, "frame delay");
        stream.WriteByte(palette.HasTransparency ? (byte)palette.TransparentIndex : (byte)0);
        stream.WriteByte(0);
        stream.WriteByte(0x2C);
        WriteUInt16(stream, frame.Left, "frame left offset");
        WriteUInt16(stream, frame.Top, "frame top offset");
        WriteUInt16(stream, frame.Width, "frame width");
        WriteUInt16(stream, frame.Height, "frame height");
        stream.WriteByte(localPalette ? (byte)0x87 : (byte)0);
        if (localPalette) WritePalette(stream, palette);
        stream.WriteByte(8);
        WriteSubBlocks(stream, LzwEncode(frame.Pixels, 8, cancellationToken));
    }

    private static byte[] LzwEncode(byte[] indices, int minimumCodeSize, CancellationToken cancellationToken) {
        var clearCode = 1 << minimumCodeSize;
        var endCode = clearCode + 1;
        var nextCode = endCode + 1;
        var codeSize = minimumCodeSize + 1;
        var dictionary = new Dictionary<int, int>(4096);
        var writer = new BitWriter();
        writer.Write(clearCode, codeSize);
        if (indices.Length == 0) { writer.Write(endCode, codeSize); return writer.ToArray(); }
        var prefix = (int)indices[0];
        for (var i = 1; i < indices.Length; i++) {
            if ((i & 4095) == 0) cancellationToken.ThrowIfCancellationRequested();
            var value = indices[i];
            var key = (prefix << 8) | value;
            if (dictionary.TryGetValue(key, out var code)) { prefix = code; continue; }
            writer.Write(prefix, codeSize);
            // The decoder builds its dictionary one emitted code behind the encoder.
            if (nextCode == 1 << codeSize && codeSize < 12) codeSize++;
            if (nextCode < 4096) dictionary.Add(key, nextCode++);
            else {
                writer.Write(clearCode, codeSize);
                dictionary.Clear(); nextCode = endCode + 1; codeSize = minimumCodeSize + 1;
            }
            prefix = value;
        }
        writer.Write(prefix, codeSize);
        if (nextCode == 1 << codeSize && codeSize < 12) codeSize++;
        writer.Write(endCode, codeSize);
        return writer.ToArray();
    }

    private static void WritePalette(Stream stream, GifPalette palette) {
        for (var i = 0; i < 256; i++) {
            var offset = i * 3;
            stream.WriteByte(palette.Colors[offset]);
            stream.WriteByte(palette.Colors[offset + 1]);
            stream.WriteByte(palette.Colors[offset + 2]);
        }
    }

    private static void WriteLoopExtension(Stream stream, int repeatCount) {
        stream.WriteByte(0x21);
        stream.WriteByte(0xFF);
        stream.WriteByte(11);
        WriteAscii(stream, "NETSCAPE2.0");
        stream.WriteByte(3);
        stream.WriteByte(1);
        WriteUInt16(stream, repeatCount, "loop count");
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

    private static void ValidateLogicalScreen(AnimatedRasterFrames animation) {
        if (animation.Width > MaximumGifFieldValue) throw new ArgumentOutOfRangeException(nameof(animation.Width), animation.Width, "GIF logical screen width must fit in an unsigned 16-bit field.");
        if (animation.Height > MaximumGifFieldValue) throw new ArgumentOutOfRangeException(nameof(animation.Height), animation.Height, "GIF logical screen height must fit in an unsigned 16-bit field.");
    }

    private static void ValidateFrame(GifIndexedFrame frame) {
        if (frame.Left > MaximumGifFieldValue) throw new ArgumentOutOfRangeException(nameof(frame.Left), frame.Left, "GIF frame left offset must fit in an unsigned 16-bit field.");
        if (frame.Top > MaximumGifFieldValue) throw new ArgumentOutOfRangeException(nameof(frame.Top), frame.Top, "GIF frame top offset must fit in an unsigned 16-bit field.");
        if (frame.Width > MaximumGifFieldValue) throw new ArgumentOutOfRangeException(nameof(frame.Width), frame.Width, "GIF frame width must fit in an unsigned 16-bit field.");
        if (frame.Height > MaximumGifFieldValue) throw new ArgumentOutOfRangeException(nameof(frame.Height), frame.Height, "GIF frame height must fit in an unsigned 16-bit field.");
    }

    private static void WriteUInt16(Stream stream, int value, string fieldName) {
        if (value < 0 || value > MaximumGifFieldValue) throw new ArgumentOutOfRangeException(fieldName, value, "GIF " + fieldName + " must fit in an unsigned 16-bit field.");
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

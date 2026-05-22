using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace ChartForgeX.Raster;

internal static class ApngWriter {
    private static readonly byte[] Signature = { 137, 80, 78, 71, 13, 10, 26, 10 };

    public static byte[] WriteRgba(IReadOnlyList<RgbaImage> frames, int delayCentiseconds, bool loop) {
        using var stream = new MemoryStream();
        WriteRgba(stream, frames, delayCentiseconds, loop);
        return stream.ToArray();
    }

    public static void WriteRgba(Stream stream, IReadOnlyList<RgbaImage> frames, int delayCentiseconds, bool loop) {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        var animation = AnimatedRasterFrames.Create(frames, delayCentiseconds, loop, "PNG");
        WriteRgba(stream, animation);
    }

    public static void WriteRgba(Stream stream, AnimatedRasterFrames animation) {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (animation == null) throw new ArgumentNullException(nameof(animation));
        stream.Write(Signature, 0, Signature.Length);
        WriteIhdr(stream, animation.Width, animation.Height);
        WriteActl(stream, animation.Frames.Count, animation.Loop ? 0 : 1);
        var optimizedFrames = RgbaFrameOptimizer.BuildFrames(animation.Frames);
        var sequence = 0u;
        for (var i = 0; i < optimizedFrames.Count; i++) {
            var frame = optimizedFrames[i];
            WriteFctl(stream, sequence++, frame.Width, frame.Height, frame.Left, frame.Top, animation.DelayCentiseconds, 100);
            var compressed = ZlibDeflate(RawFrame(frame));
            if (i == 0) WriteChunk(stream, "IDAT", compressed);
            else WriteFdat(stream, sequence++, compressed);
        }

        WriteChunk(stream, "IEND", Array.Empty<byte>());
    }

    private static void WriteIhdr(Stream stream, int width, int height) {
        var data = new List<byte>(13);
        WriteUInt(data, (uint)width);
        WriteUInt(data, (uint)height);
        data.Add(8);
        data.Add(6);
        data.Add(0);
        data.Add(0);
        data.Add(0);
        WriteChunk(stream, "IHDR", data.ToArray());
    }

    private static void WriteActl(Stream stream, int frameCount, int plays) {
        var data = new List<byte>(8);
        WriteUInt(data, (uint)frameCount);
        WriteUInt(data, (uint)plays);
        WriteChunk(stream, "acTL", data.ToArray());
    }

    private static void WriteFctl(Stream stream, uint sequence, int width, int height, int x, int y, int delayNumerator, int delayDenominator) {
        var data = new List<byte>(26);
        WriteUInt(data, sequence);
        WriteUInt(data, (uint)width);
        WriteUInt(data, (uint)height);
        WriteUInt(data, (uint)x);
        WriteUInt(data, (uint)y);
        WriteUInt16(data, delayNumerator);
        WriteUInt16(data, delayDenominator);
        data.Add(0);
        data.Add(0);
        WriteChunk(stream, "fcTL", data.ToArray());
    }

    private static void WriteFdat(Stream stream, uint sequence, byte[] compressed) {
        var data = new byte[compressed.Length + 4];
        data[0] = (byte)((sequence >> 24) & 255);
        data[1] = (byte)((sequence >> 16) & 255);
        data[2] = (byte)((sequence >> 8) & 255);
        data[3] = (byte)(sequence & 255);
        Buffer.BlockCopy(compressed, 0, data, 4, compressed.Length);
        WriteChunk(stream, "fdAT", data);
    }

    private static byte[] RawFrame(RgbaFrameRect frame) {
        var raw = new byte[frame.Height * (frame.Width * 4 + 1)];
        var source = 0;
        var destination = 0;
        for (var y = 0; y < frame.Height; y++) {
            raw[destination++] = 0;
            Buffer.BlockCopy(frame.Pixels, source, raw, destination, frame.Width * 4);
            source += frame.Width * 4;
            destination += frame.Width * 4;
        }

        return raw;
    }

    private static byte[] ZlibDeflate(byte[] data) {
        using var stream = new MemoryStream();
        stream.WriteByte(0x78);
        stream.WriteByte(0x9C);
        using (var deflate = new DeflateStream(stream, CompressionLevel.Optimal, true)) {
            deflate.Write(data, 0, data.Length);
        }

        WriteUInt(stream, Adler32(data));
        return stream.ToArray();
    }

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

    private static void WriteChunk(Stream stream, string type, byte[] data) {
        WriteUInt(stream, (uint)data.Length);
        var typeBytes = Encoding.ASCII.GetBytes(type);
        stream.Write(typeBytes, 0, typeBytes.Length);
        stream.Write(data, 0, data.Length);
        var crcInput = new byte[typeBytes.Length + data.Length];
        Buffer.BlockCopy(typeBytes, 0, crcInput, 0, typeBytes.Length);
        Buffer.BlockCopy(data, 0, crcInput, typeBytes.Length, data.Length);
        WriteUInt(stream, Crc32(crcInput));
    }

    private static uint Crc32(byte[] data) {
        uint crc = 0xffffffff;
        foreach (var b in data) {
            crc ^= b;
            for (var i = 0; i < 8; i++) crc = (crc & 1) == 1 ? 0xedb88320 ^ (crc >> 1) : crc >> 1;
        }

        return ~crc;
    }

    private static void WriteUInt(Stream stream, uint value) {
        stream.WriteByte((byte)((value >> 24) & 255));
        stream.WriteByte((byte)((value >> 16) & 255));
        stream.WriteByte((byte)((value >> 8) & 255));
        stream.WriteByte((byte)(value & 255));
    }

    private static void WriteUInt(List<byte> bytes, uint value) {
        bytes.Add((byte)((value >> 24) & 255));
        bytes.Add((byte)((value >> 16) & 255));
        bytes.Add((byte)((value >> 8) & 255));
        bytes.Add((byte)(value & 255));
    }

    private static void WriteUInt16(List<byte> bytes, int value) {
        bytes.Add((byte)((value >> 8) & 255));
        bytes.Add((byte)(value & 255));
    }
}

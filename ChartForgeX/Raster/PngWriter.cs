using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace ChartForgeX.Raster;

internal static class PngWriter {
    public static byte[] WriteRgba(RgbaImage image) => WriteRgba(image.Width, image.Height, image.Pixels);

    public static byte[] WriteRgba(int width, int height, byte[] rgba) {
        using var ms = new MemoryStream();
        ms.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, 0, 8);
        var ihdr = new List<byte>();
        WriteUInt(ihdr, (uint)width); WriteUInt(ihdr, (uint)height);
        ihdr.Add(8); ihdr.Add(6); ihdr.Add(0); ihdr.Add(0); ihdr.Add(0);
        WriteChunk(ms, "IHDR", ihdr.ToArray());
        var raw = new byte[height * (width * 4 + 1)];
        var src = 0; var dst = 0;
        for (var y = 0; y < height; y++) {
            raw[dst++] = 0;
            Buffer.BlockCopy(rgba, src, raw, dst, width * 4);
            src += width * 4; dst += width * 4;
        }
        WriteChunk(ms, "IDAT", ZlibDeflate(raw));
        WriteChunk(ms, "IEND", Array.Empty<byte>());
        return ms.ToArray();
    }

    private static byte[] ZlibDeflate(byte[] data) {
        using var ms = new MemoryStream();
        ms.WriteByte(0x78); ms.WriteByte(0x9C);
        using (var deflate = new DeflateStream(ms, CompressionLevel.Optimal, true)) {
            deflate.Write(data, 0, data.Length);
        }

        WriteUInt(ms, Adler32(data));
        return ms.ToArray();
    }

    private static uint Adler32(byte[] data) {
        const uint mod = 65521; uint a = 1, b = 0;
        foreach (var value in data) { a = (a + value) % mod; b = (b + a) % mod; }
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

    private static void WriteUInt(Stream s, uint value) {
        s.WriteByte((byte)((value >> 24) & 255)); s.WriteByte((byte)((value >> 16) & 255)); s.WriteByte((byte)((value >> 8) & 255)); s.WriteByte((byte)(value & 255));
    }

    private static void WriteUInt(List<byte> bytes, uint value) {
        bytes.Add((byte)((value >> 24) & 255)); bytes.Add((byte)((value >> 16) & 255)); bytes.Add((byte)((value >> 8) & 255)); bytes.Add((byte)(value & 255));
    }
}

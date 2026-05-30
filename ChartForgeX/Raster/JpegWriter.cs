using System;
using System.IO;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal static class JpegWriter {
    private static readonly byte[] LuminanceQuantization = {
        16, 11, 10, 16, 24, 40, 51, 61,
        12, 12, 14, 19, 26, 58, 60, 55,
        14, 13, 16, 24, 40, 57, 69, 56,
        14, 17, 22, 29, 51, 87, 80, 62,
        18, 22, 37, 56, 68, 109, 103, 77,
        24, 35, 55, 64, 81, 104, 113, 92,
        49, 64, 78, 87, 103, 121, 120, 101,
        72, 92, 95, 98, 112, 100, 103, 99
    };

    private static readonly byte[] ChrominanceQuantization = {
        17, 18, 24, 47, 99, 99, 99, 99,
        18, 21, 26, 66, 99, 99, 99, 99,
        24, 26, 56, 99, 99, 99, 99, 99,
        47, 66, 99, 99, 99, 99, 99, 99,
        99, 99, 99, 99, 99, 99, 99, 99,
        99, 99, 99, 99, 99, 99, 99, 99,
        99, 99, 99, 99, 99, 99, 99, 99,
        99, 99, 99, 99, 99, 99, 99, 99
    };

    private static readonly int[] ZigZag = {
        0, 1, 8, 16, 9, 2, 3, 10,
        17, 24, 32, 25, 18, 11, 4, 5,
        12, 19, 26, 33, 40, 48, 41, 34,
        27, 20, 13, 6, 7, 14, 21, 28,
        35, 42, 49, 56, 57, 50, 43, 36,
        29, 22, 15, 23, 30, 37, 44, 51,
        58, 59, 52, 45, 38, 31, 39, 46,
        53, 60, 61, 54, 47, 55, 62, 63
    };

    private static readonly double[] DctBasis = BuildDctBasis();

    private static readonly byte[] DcLuminanceBits = { 0, 0, 1, 5, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0 };
    private static readonly byte[] DcLuminanceValues = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
    private static readonly byte[] DcChrominanceBits = { 0, 0, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0 };
    private static readonly byte[] DcChrominanceValues = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
    private static readonly byte[] AcLuminanceBits = { 0, 0, 2, 1, 3, 3, 2, 4, 3, 5, 5, 4, 4, 0, 0, 1, 125 };
    private static readonly byte[] AcChrominanceBits = { 0, 0, 2, 1, 2, 4, 4, 3, 4, 7, 5, 4, 4, 0, 1, 2, 119 };

    private static readonly byte[] AcLuminanceValues = {
        0x01, 0x02, 0x03, 0x00, 0x04, 0x11, 0x05, 0x12,
        0x21, 0x31, 0x41, 0x06, 0x13, 0x51, 0x61, 0x07,
        0x22, 0x71, 0x14, 0x32, 0x81, 0x91, 0xA1, 0x08,
        0x23, 0x42, 0xB1, 0xC1, 0x15, 0x52, 0xD1, 0xF0,
        0x24, 0x33, 0x62, 0x72, 0x82, 0x09, 0x0A, 0x16,
        0x17, 0x18, 0x19, 0x1A, 0x25, 0x26, 0x27, 0x28,
        0x29, 0x2A, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
        0x3A, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49,
        0x4A, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59,
        0x5A, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69,
        0x6A, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79,
        0x7A, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89,
        0x8A, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98,
        0x99, 0x9A, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7,
        0xA8, 0xA9, 0xAA, 0xB2, 0xB3, 0xB4, 0xB5, 0xB6,
        0xB7, 0xB8, 0xB9, 0xBA, 0xC2, 0xC3, 0xC4, 0xC5,
        0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xD2, 0xD3, 0xD4,
        0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xE1, 0xE2,
        0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA,
        0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8,
        0xF9, 0xFA
    };

    private static readonly byte[] AcChrominanceValues = {
        0x00, 0x01, 0x02, 0x03, 0x11, 0x04, 0x05, 0x21,
        0x31, 0x06, 0x12, 0x41, 0x51, 0x07, 0x61, 0x71,
        0x13, 0x22, 0x32, 0x81, 0x08, 0x14, 0x42, 0x91,
        0xA1, 0xB1, 0xC1, 0x09, 0x23, 0x33, 0x52, 0xF0,
        0x15, 0x62, 0x72, 0xD1, 0x0A, 0x16, 0x24, 0x34,
        0xE1, 0x25, 0xF1, 0x17, 0x18, 0x19, 0x1A, 0x26,
        0x27, 0x28, 0x29, 0x2A, 0x35, 0x36, 0x37, 0x38,
        0x39, 0x3A, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
        0x49, 0x4A, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
        0x59, 0x5A, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68,
        0x69, 0x6A, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78,
        0x79, 0x7A, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87,
        0x88, 0x89, 0x8A, 0x92, 0x93, 0x94, 0x95, 0x96,
        0x97, 0x98, 0x99, 0x9A, 0xA2, 0xA3, 0xA4, 0xA5,
        0xA6, 0xA7, 0xA8, 0xA9, 0xAA, 0xB2, 0xB3, 0xB4,
        0xB5, 0xB6, 0xB7, 0xB8, 0xB9, 0xBA, 0xC2, 0xC3,
        0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xD2,
        0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA,
        0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9,
        0xEA, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8,
        0xF9, 0xFA
    };

    public static byte[] WriteRgba(RgbaImage image, RasterImageOptions? options = null) {
        using var stream = new MemoryStream();
        WriteRgba(stream, image, options);
        return stream.ToArray();
    }

    public static void WriteRgba(Stream stream, RgbaImage image, RasterImageOptions? options = null) {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (image.Width > 65535 || image.Height > 65535) throw new NotSupportedException("JPEG dimensions must be 65535 pixels or less.");
        var background = options?.Background ?? ChartColors.White;
        var quality = options?.JpegQuality ?? 90;
        var yQuant = ScaleQuantization(LuminanceQuantization, quality);
        var cQuant = ScaleQuantization(ChrominanceQuantization, quality);
        var dcY = BuildCodes(DcLuminanceBits, DcLuminanceValues);
        var acY = BuildCodes(AcLuminanceBits, AcLuminanceValues);
        var dcC = BuildCodes(DcChrominanceBits, DcChrominanceValues);
        var acC = BuildCodes(AcChrominanceBits, AcChrominanceValues);

        WriteMarker(stream, 0xD8);
        WriteJfif(stream);
        WriteQuantization(stream, 0, yQuant);
        WriteQuantization(stream, 1, cQuant);
        WriteFrame(stream, image.Width, image.Height);
        WriteHuffman(stream, 0, false, DcLuminanceBits, DcLuminanceValues);
        WriteHuffman(stream, 1, false, DcChrominanceBits, DcChrominanceValues);
        WriteHuffman(stream, 0, true, AcLuminanceBits, AcLuminanceValues);
        WriteHuffman(stream, 1, true, AcChrominanceBits, AcChrominanceValues);
        WriteScanHeader(stream);

        var writer = new BitWriter(stream);
        var previousY = 0;
        var previousCb = 0;
        var previousCr = 0;
        var block = new double[64];
        var coefficients = new int[64];
        for (var by = 0; by < image.Height; by += 8) {
            for (var bx = 0; bx < image.Width; bx += 8) {
                FillBlock(image, background, bx, by, 0, block);
                ForwardDct(block, coefficients, yQuant);
                previousY = WriteBlock(writer, coefficients, previousY, dcY, acY);
                FillBlock(image, background, bx, by, 1, block);
                ForwardDct(block, coefficients, cQuant);
                previousCb = WriteBlock(writer, coefficients, previousCb, dcC, acC);
                FillBlock(image, background, bx, by, 2, block);
                ForwardDct(block, coefficients, cQuant);
                previousCr = WriteBlock(writer, coefficients, previousCr, dcC, acC);
            }
        }

        writer.Flush();
        WriteMarker(stream, 0xD9);
    }

    private static byte[] ScaleQuantization(byte[] source, int quality) {
        var scale = quality < 50 ? 5000 / quality : 200 - quality * 2;
        var result = new byte[64];
        for (var i = 0; i < source.Length; i++) {
            var value = (source[i] * scale + 50) / 100;
            result[i] = (byte)Math.Max(1, Math.Min(255, value));
        }

        return result;
    }

    private static HuffmanCode[] BuildCodes(byte[] bits, byte[] values) {
        var table = new HuffmanCode[256];
        var code = 0;
        var index = 0;
        for (var length = 1; length <= 16; length++) {
            for (var i = 0; i < bits[length]; i++) {
                table[values[index++]] = new HuffmanCode(code, length);
                code++;
            }

            code <<= 1;
        }

        return table;
    }

    private static void FillBlock(RgbaImage image, ChartColor background, int blockX, int blockY, int component, double[] block) {
        for (var y = 0; y < 8; y++) {
            var py = Math.Min(image.Height - 1, blockY + y);
            for (var x = 0; x < 8; x++) {
                var px = Math.Min(image.Width - 1, blockX + x);
                var offset = (py * image.Width + px) * 4;
                var r = image.Pixels[offset];
                var g = image.Pixels[offset + 1];
                var b = image.Pixels[offset + 2];
                RasterColorWriter.Flatten(ref r, ref g, ref b, image.Pixels[offset + 3], background);
                var value = component == 0
                    ? 0.299 * r + 0.587 * g + 0.114 * b
                    : component == 1
                        ? -0.168736 * r - 0.331264 * g + 0.5 * b + 128
                        : 0.5 * r - 0.418688 * g - 0.081312 * b + 128;
                block[y * 8 + x] = value - 128;
            }
        }
    }

    private static void ForwardDct(double[] input, int[] output, byte[] quantization) {
        for (var i = 0; i < ZigZag.Length; i++) {
            var natural = ZigZag[i];
            var basisOffset = natural * 64;
            var sum = 0.0;
            for (var sample = 0; sample < 64; sample++) sum += input[sample] * DctBasis[basisOffset + sample];
            output[i] = (int)Math.Round(sum / quantization[natural]);
        }
    }

    private static double[] BuildDctBasis() {
        var basis = new double[64 * 64];
        for (var v = 0; v < 8; v++) {
            for (var u = 0; u < 8; u++) {
                var cu = u == 0 ? 1 / Math.Sqrt(2) : 1;
                var cv = v == 0 ? 1 / Math.Sqrt(2) : 1;
                var natural = v * 8 + u;
                for (var y = 0; y < 8; y++) {
                    for (var x = 0; x < 8; x++) {
                        basis[natural * 64 + y * 8 + x] =
                            0.25 * cu * cv *
                            Math.Cos(((2 * x + 1) * u * Math.PI) / 16.0) *
                            Math.Cos(((2 * y + 1) * v * Math.PI) / 16.0);
                    }
                }
            }
        }

        return basis;
    }

    private static int WriteBlock(BitWriter writer, int[] coefficients, int previousDc, HuffmanCode[] dcTable, HuffmanCode[] acTable) {
        var dc = coefficients[0];
        var diff = dc - previousDc;
        var dcSize = MagnitudeSize(diff);
        writer.Write(dcTable[dcSize]);
        if (dcSize > 0) writer.Write(AmplitudeBits(diff, dcSize), dcSize);

        var zeroRun = 0;
        for (var i = 1; i < 64; i++) {
            var value = coefficients[i];
            if (value == 0) {
                zeroRun++;
                continue;
            }

            while (zeroRun > 15) {
                writer.Write(acTable[0xF0]);
                zeroRun -= 16;
            }

            var size = MagnitudeSize(value);
            writer.Write(acTable[(zeroRun << 4) | size]);
            writer.Write(AmplitudeBits(value, size), size);
            zeroRun = 0;
        }

        if (zeroRun > 0) writer.Write(acTable[0]);
        return dc;
    }

    private static int MagnitudeSize(int value) {
        var absolute = Math.Abs(value);
        var size = 0;
        while (absolute > 0) {
            size++;
            absolute >>= 1;
        }

        return size;
    }

    private static int AmplitudeBits(int value, int size) => value >= 0 ? value : value + ((1 << size) - 1);

    private static void WriteJfif(Stream stream) {
        WriteMarker(stream, 0xE0);
        WriteUInt16(stream, 16);
        stream.Write(new byte[] { 0x4A, 0x46, 0x49, 0x46, 0, 1, 1, 0 }, 0, 8);
        WriteUInt16(stream, 1);
        WriteUInt16(stream, 1);
        stream.WriteByte(0);
        stream.WriteByte(0);
    }

    private static void WriteQuantization(Stream stream, int id, byte[] table) {
        WriteMarker(stream, 0xDB);
        WriteUInt16(stream, 67);
        stream.WriteByte((byte)id);
        for (var i = 0; i < ZigZag.Length; i++) stream.WriteByte(table[ZigZag[i]]);
    }

    private static void WriteFrame(Stream stream, int width, int height) {
        WriteMarker(stream, 0xC0);
        WriteUInt16(stream, 17);
        stream.WriteByte(8);
        WriteUInt16(stream, height);
        WriteUInt16(stream, width);
        stream.WriteByte(3);
        WriteComponent(stream, 1, 0);
        WriteComponent(stream, 2, 1);
        WriteComponent(stream, 3, 1);
    }

    private static void WriteComponent(Stream stream, int id, int quantizationId) {
        stream.WriteByte((byte)id);
        stream.WriteByte(0x11);
        stream.WriteByte((byte)quantizationId);
    }

    private static void WriteHuffman(Stream stream, int id, bool ac, byte[] bits, byte[] values) {
        WriteMarker(stream, 0xC4);
        WriteUInt16(stream, 3 + 16 + values.Length);
        stream.WriteByte((byte)((ac ? 0x10 : 0) | id));
        for (var i = 1; i <= 16; i++) stream.WriteByte(bits[i]);
        stream.Write(values, 0, values.Length);
    }

    private static void WriteScanHeader(Stream stream) {
        WriteMarker(stream, 0xDA);
        WriteUInt16(stream, 12);
        stream.WriteByte(3);
        WriteScanComponent(stream, 1, 0, 0);
        WriteScanComponent(stream, 2, 1, 1);
        WriteScanComponent(stream, 3, 1, 1);
        stream.WriteByte(0);
        stream.WriteByte(63);
        stream.WriteByte(0);
    }

    private static void WriteScanComponent(Stream stream, int id, int dcTable, int acTable) {
        stream.WriteByte((byte)id);
        stream.WriteByte((byte)((dcTable << 4) | acTable));
    }

    private static void WriteMarker(Stream stream, int marker) {
        stream.WriteByte(0xFF);
        stream.WriteByte((byte)marker);
    }

    private static void WriteUInt16(Stream stream, int value) {
        stream.WriteByte((byte)((value >> 8) & 255));
        stream.WriteByte((byte)(value & 255));
    }

    private readonly struct HuffmanCode {
        public readonly int Bits;
        public readonly int Length;

        public HuffmanCode(int bits, int length) {
            Bits = bits;
            Length = length;
        }
    }

    private sealed class BitWriter {
        private readonly Stream _stream;
        private int _buffer;
        private int _count;

        public BitWriter(Stream stream) {
            _stream = stream;
        }

        public void Write(HuffmanCode code) => Write(code.Bits, code.Length);

        public void Write(int bits, int count) {
            for (var i = count - 1; i >= 0; i--) {
                _buffer = (_buffer << 1) | ((bits >> i) & 1);
                _count++;
                if (_count == 8) FlushByte();
            }
        }

        public void Flush() {
            if (_count == 0) return;
            _buffer <<= 8 - _count;
            _buffer |= (1 << (8 - _count)) - 1;
            FlushByte();
        }

        private void FlushByte() {
            _stream.WriteByte((byte)_buffer);
            if (_buffer == 0xFF) _stream.WriteByte(0);
            _buffer = 0;
            _count = 0;
        }
    }
}

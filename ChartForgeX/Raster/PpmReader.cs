using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace ChartForgeX.Raster;

internal static class PpmReader {
    public static bool IsPpm(byte[] data) => data != null && data.Length >= 2 && data[0] == (byte)'P' && (data[1] == (byte)'3' || data[1] == (byte)'6');

    public static RgbaImage Decode(byte[] data) {
        if (!IsPpm(data)) throw new NotSupportedException("Input is not a PPM image.");
        var reader = new PpmTokenReader(data);
        var magic = reader.NextToken();
        var width = ParsePositive(reader.NextToken(), "width");
        var height = ParsePositive(reader.NextToken(), "height");
        var maxValue = ParsePositive(reader.NextToken(), "max value");
        if (maxValue > 255) throw new NotSupportedException("Only PPM images with max value up to 255 are supported.");
        return magic == "P6" ? DecodeBinary(data, reader.Position, width, height, maxValue) : DecodeAscii(reader, width, height, maxValue);
    }

    private static RgbaImage DecodeBinary(byte[] data, int offset, int width, int height, int maxValue) {
        SkipBinaryHeaderSeparator(data, ref offset);
        var expected = checked(width * height * 3);
        if (offset + expected > data.Length) throw new InvalidDataException("PPM pixel data is shorter than expected.");
        var rgba = new byte[checked(width * height * 4)];
        var source = offset;
        var target = 0;
        for (var i = 0; i < width * height; i++) {
            rgba[target++] = Scale(data[source++], maxValue);
            rgba[target++] = Scale(data[source++], maxValue);
            rgba[target++] = Scale(data[source++], maxValue);
            rgba[target++] = 255;
        }

        return new RgbaImage(width, height, rgba);
    }

    private static RgbaImage DecodeAscii(PpmTokenReader reader, int width, int height, int maxValue) {
        var rgba = new byte[checked(width * height * 4)];
        var target = 0;
        for (var i = 0; i < width * height; i++) {
            rgba[target++] = Scale(ParseNonNegative(reader.NextToken()), maxValue);
            rgba[target++] = Scale(ParseNonNegative(reader.NextToken()), maxValue);
            rgba[target++] = Scale(ParseNonNegative(reader.NextToken()), maxValue);
            rgba[target++] = 255;
        }

        return new RgbaImage(width, height, rgba);
    }

    private static byte Scale(int value, int maxValue) {
        if (value > maxValue) throw new InvalidDataException("PPM sample value exceeds the max value.");
        return maxValue == 255 ? (byte)value : (byte)Math.Round(value * 255.0 / maxValue);
    }

    private static int ParsePositive(string token, string name) {
        var value = ParseNonNegative(token);
        if (value <= 0) throw new InvalidDataException("PPM " + name + " must be positive.");
        return value;
    }

    private static int ParseNonNegative(string token) {
        if (!int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out var value) || value < 0) throw new InvalidDataException("Invalid PPM numeric token.");
        return value;
    }

    private static void SkipBinaryHeaderSeparator(byte[] data, ref int offset) {
        if (offset >= data.Length || !IsWhitespace(data[offset])) return;
        if (data[offset] == 13 && offset + 1 < data.Length && data[offset + 1] == 10) {
            offset += 2;
            return;
        }

        offset++;
    }

    private static bool IsWhitespace(byte value) => value == 9 || value == 10 || value == 12 || value == 13 || value == 32;

    private sealed class PpmTokenReader {
        private readonly byte[] _data;

        public PpmTokenReader(byte[] data) {
            _data = data;
        }

        public int Position { get; private set; }

        public string NextToken() {
            SkipWhitespaceAndComments();
            if (Position >= _data.Length) throw new InvalidDataException("Unexpected end of PPM image.");
            var start = Position;
            while (Position < _data.Length && !IsWhitespace(_data[Position]) && _data[Position] != (byte)'#') Position++;
            return Encoding.ASCII.GetString(_data, start, Position - start);
        }

        private void SkipWhitespaceAndComments() {
            while (Position < _data.Length) {
                if (IsWhitespace(_data[Position])) {
                    Position++;
                    continue;
                }

                if (_data[Position] == (byte)'#') {
                    while (Position < _data.Length && _data[Position] != 10 && _data[Position] != 13) Position++;
                    continue;
                }

                break;
            }
        }
    }
}

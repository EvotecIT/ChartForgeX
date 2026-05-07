using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static Chart SampleChart() {
        return Chart.Create()
            .WithTitle("A < B & C")
            .WithSubtitle("No JS, no CDN")
            .WithXAxis("Run")
            .WithYAxis("Checks")
            .WithTheme(ChartTheme.Dark())
            .WithSize(640, 360)
            .WithXLabels("Mon", "Tue", "Wed")
            .AddSmoothArea("Passed", Points(100, 180, 260))
            .AddSmoothLine("Failed", Points(20, 14, 9), ChartColor.FromRgb(248, 113, 113));
    }

    private static IEnumerable<ChartPoint> Points(params double[] y) {
        for (var i = 0; i < y.Length; i++) yield return new ChartPoint(i + 1, y[i]);
    }

    private static IEnumerable<ChartPoint> DatePoints(DateTime[] dates, params double[] y) {
        for (var i = 0; i < dates.Length; i++) yield return new ChartPoint(dates[i], y[i]);
    }

    private static int ReadBigEndianInt32(byte[] bytes, int offset) {
        return (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
    }

    private static byte[] ReadPngRgba(byte[] png, out int width, out int height) {
        width = ReadBigEndianInt32(png, 16);
        height = ReadBigEndianInt32(png, 20);
        using var idat = new MemoryStream();
        var offset = 8;
        while (offset < png.Length) {
            var length = ReadBigEndianInt32(png, offset);
            var type = Encoding.ASCII.GetString(png, offset + 4, 4);
            if (type == "IDAT") idat.Write(png, offset + 8, length);
            offset += length + 12;
        }

        var compressed = idat.ToArray();
        using var compressedStream = new MemoryStream(compressed, 2, compressed.Length - 6);
        using var deflate = new DeflateStream(compressedStream, CompressionMode.Decompress);
        using var rawStream = new MemoryStream();
        deflate.CopyTo(rawStream);
        var raw = rawStream.ToArray();
        var rgba = new byte[width * height * 4];
        var stride = width * 4;
        var previous = new byte[stride];
        var current = new byte[stride];
        var src = 0;
        var dst = 0;
        for (var y = 0; y < height; y++) {
            var filter = raw[src++];
            Assert(src + stride <= raw.Length, "PNG smoke decoder expects complete scanlines.");
            Assert(UnfilterPngRow(raw, src, current, previous, stride, 4, filter), "PNG smoke decoder expects standard RGBA scanline filters.");
            Buffer.BlockCopy(current, 0, rgba, dst, stride);
            src += stride;
            dst += stride;

            var swap = previous;
            previous = current;
            current = swap;
        }

        return rgba;
    }

    private static bool UnfilterPngRow(byte[] raw, int rawOffset, byte[] output, byte[] previous, int stride, int bytesPerPixel, int filter) {
        switch (filter) {
            case 0:
                Buffer.BlockCopy(raw, rawOffset, output, 0, stride);
                return true;
            case 1:
                for (var i = 0; i < stride; i++) {
                    var left = i >= bytesPerPixel ? output[i - bytesPerPixel] : 0;
                    output[i] = unchecked((byte)(raw[rawOffset + i] + left));
                }

                return true;
            case 2:
                for (var i = 0; i < stride; i++) {
                    output[i] = unchecked((byte)(raw[rawOffset + i] + previous[i]));
                }

                return true;
            case 3:
                for (var i = 0; i < stride; i++) {
                    var left = i >= bytesPerPixel ? output[i - bytesPerPixel] : 0;
                    var up = previous[i];
                    output[i] = unchecked((byte)(raw[rawOffset + i] + ((left + up) >> 1)));
                }

                return true;
            case 4:
                for (var i = 0; i < stride; i++) {
                    var left = i >= bytesPerPixel ? output[i - bytesPerPixel] : 0;
                    var up = previous[i];
                    var upperLeft = i >= bytesPerPixel ? previous[i - bytesPerPixel] : 0;
                    output[i] = unchecked((byte)(raw[rawOffset + i] + PaethPredictor(left, up, upperLeft)));
                }

                return true;
            default:
                return false;
        }
    }

    private static int PaethPredictor(int left, int up, int upperLeft) {
        var estimate = left + up - upperLeft;
        var leftDistance = Math.Abs(estimate - left);
        var upDistance = Math.Abs(estimate - up);
        var upperLeftDistance = Math.Abs(estimate - upperLeft);
        if (leftDistance <= upDistance && leftDistance <= upperLeftDistance) return left;
        return upDistance <= upperLeftDistance ? up : upperLeft;
    }

    private static int CountOccurrences(string value, string pattern) {
        var count = 0;
        var index = 0;
        while ((index = value.IndexOf(pattern, index, StringComparison.Ordinal)) >= 0) {
            count++;
            index += pattern.Length;
        }

        return count;
    }

    private static string[] ExtractAttributeValues(string value, string prefix) {
        var values = new List<string>();
        var index = 0;
        while ((index = value.IndexOf(prefix, index, StringComparison.Ordinal)) >= 0) {
            index += prefix.Length;
            var end = value.IndexOf("\"", index, StringComparison.Ordinal);
            if (end < 0) break;
            values.Add(value.Substring(index, end - index));
            index = end + 1;
        }

        return values.ToArray();
    }

    private static void AssertNoDuplicateIds(string markup, string context) {
        var ids = ExtractAttributeValues(markup, "id=\"");
        var duplicates = ids
            .GroupBy(id => id, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        Assert(duplicates.Length == 0, context + " should not contain duplicate id attributes: " + string.Join(", ", duplicates));
    }

    private static int CountAlphaInRect(byte[] rgba, int width, int x, int y, int rectWidth, int rectHeight) {
        var count = 0;
        for (var yy = y; yy < y + rectHeight; yy++) for (var xx = x; xx < x + rectWidth; xx++) {
            if (rgba[(yy * width + xx) * 4 + 3] > 0) count++;
        }

        return count;
    }

    private static int CountTransparentSamplesOnRow(byte[] rgba, int width, int y, int x, int sampleWidth) {
        var count = 0;
        for (var xx = x; xx < x + sampleWidth; xx++) {
            if (rgba[(y * width + xx) * 4 + 3] == 0) count++;
        }

        return count;
    }

    private static int CountNearColor(byte[] rgba, byte red, byte green, byte blue, byte tolerance = 8) {
        var count = 0;
        for (var i = 0; i < rgba.Length; i += 4) {
            if (rgba[i + 3] == 0) continue;
            if (Math.Abs(rgba[i] - red) <= tolerance && Math.Abs(rgba[i + 1] - green) <= tolerance && Math.Abs(rgba[i + 2] - blue) <= tolerance) count++;
        }

        return count;
    }

    private static int CountNearColorInRect(byte[] rgba, int width, int x, int y, int sampleWidth, int sampleHeight, byte red, byte green, byte blue, byte tolerance = 8) {
        var count = 0;
        for (var yy = y; yy < y + sampleHeight; yy++) {
            for (var xx = x; xx < x + sampleWidth; xx++) {
                var i = (yy * width + xx) * 4;
                if (rgba[i + 3] == 0) continue;
                if (Math.Abs(rgba[i] - red) <= tolerance && Math.Abs(rgba[i + 1] - green) <= tolerance && Math.Abs(rgba[i + 2] - blue) <= tolerance) count++;
            }
        }

        return count;
    }

    private static ColorBounds FindNearColorBounds(byte[] rgba, int width, byte red, byte green, byte blue, byte tolerance = 8) {
        var height = rgba.Length / width / 4;
        var left = width;
        var top = height;
        var right = -1;
        var bottom = -1;
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                var i = (y * width + x) * 4;
                if (rgba[i + 3] == 0) continue;
                if (Math.Abs(rgba[i] - red) > tolerance || Math.Abs(rgba[i + 1] - green) > tolerance || Math.Abs(rgba[i + 2] - blue) > tolerance) continue;
                left = Math.Min(left, x);
                top = Math.Min(top, y);
                right = Math.Max(right, x);
                bottom = Math.Max(bottom, y);
            }
        }

        return new ColorBounds(left, top, right, bottom);
    }

    private readonly struct ColorBounds {
        public ColorBounds(int left, int top, int right, int bottom) {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public int Left { get; }
        public int Top { get; }
        public int Right { get; }
        public int Bottom { get; }
        public int Width => IsEmpty ? 0 : Right - Left + 1;
        public int Height => IsEmpty ? 0 : Bottom - Top + 1;
        public bool IsEmpty => Right < Left || Bottom < Top;
    }

    private static string FindRepositoryRoot() {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null) {
            if (File.Exists(Path.Combine(directory.FullName, "ChartForgeX.sln"))) return directory.FullName;
            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }

    private static bool IsGeneratedPath(string file) {
        var normalized = file.Replace(Path.DirectorySeparatorChar, '/');
        return normalized.Contains("/bin/", StringComparison.Ordinal) || normalized.Contains("/obj/", StringComparison.Ordinal);
    }

    private static bool IsProjectSettingFile(string file) {
        return file.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ||
            file.EndsWith(".props", StringComparison.OrdinalIgnoreCase) ||
            file.EndsWith(".targets", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasXmlProperty(string file, string name, string expectedValue) {
        return GetXmlElements(file, name).Any(element => string.Equals((element.Value ?? string.Empty).Trim(), expectedValue, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetXmlValue(string file, string localName) {
        return (GetXmlElements(file, localName).FirstOrDefault()?.Value ?? string.Empty).Trim();
    }

    private static IEnumerable<System.Xml.Linq.XElement> GetXmlElements(string file, string localName) {
        var document = System.Xml.Linq.XDocument.Load(file);
        return document.Descendants().Where(element => string.Equals(element.Name.LocalName, localName, StringComparison.Ordinal));
    }

    private static void AssertSelfContainedMarkup(string markup, string name) {
        Assert(!markup.Contains("<script", StringComparison.OrdinalIgnoreCase), name + " should not contain script elements.");
        Assert(!markup.Contains("<link", StringComparison.OrdinalIgnoreCase), name + " should not contain external link elements.");
        Assert(!markup.Contains("@import", StringComparison.OrdinalIgnoreCase), name + " should not import stylesheets.");
        Assert(!markup.Contains("<object", StringComparison.OrdinalIgnoreCase), name + " should not contain object embeds.");
        Assert(!markup.Contains("<embed", StringComparison.OrdinalIgnoreCase), name + " should not contain embed elements.");
        Assert(!markup.Contains("<foreignObject", StringComparison.OrdinalIgnoreCase), name + " should not contain embedded foreign HTML.");
        Assert(!ContainsAny(markup, " onload=", " onclick=", " onerror=", " onmouseover=", " onfocus="), name + " should not contain inline event handlers.");
        var withoutSvgNamespace = markup.Replace("http://www.w3.org/2000/svg", string.Empty);
        Assert(!withoutSvgNamespace.Contains("http://", StringComparison.OrdinalIgnoreCase), name + " should not reference external HTTP resources.");
        Assert(!withoutSvgNamespace.Contains("https://", StringComparison.OrdinalIgnoreCase), name + " should not reference external HTTPS resources.");
        Assert(!withoutSvgNamespace.Contains("url(http", StringComparison.OrdinalIgnoreCase), name + " should not reference external CSS URLs.");
    }

    private static bool ContainsAny(string value, params string[] needles) {
        foreach (var needle in needles) {
            if (value.Contains(needle, StringComparison.OrdinalIgnoreCase)) return true;
        }

        return false;
    }

    private static double GetAttribute(string text, string marker, string attribute) {
        var start = text.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0) throw new InvalidOperationException("Missing marker: " + marker);
        var attributeMarker = " " + attribute + "=\"";
        start = text.IndexOf(attributeMarker, start, StringComparison.Ordinal);
        if (start < 0) throw new InvalidOperationException("Missing attribute: " + attribute);
        start += attributeMarker.Length;
        var end = text.IndexOf("\"", start, StringComparison.Ordinal);
        return double.Parse(text.Substring(start, end - start), CultureInfo.InvariantCulture);
    }

    private static DecodedPng DecodePng(byte[] png) {
        if (png.Length < 33 || png[0] != 137 || png[1] != 80 || png[2] != 78 || png[3] != 71) throw new InvalidOperationException("Invalid PNG signature.");
        var index = 8;
        var width = 0;
        var height = 0;
        using var idat = new MemoryStream();
        while (index + 12 <= png.Length) {
            var length = checked((int)ReadPngUInt(png, index));
            index += 4;
            var type = Encoding.ASCII.GetString(png, index, 4);
            index += 4;
            if (index + length + 4 > png.Length) throw new InvalidOperationException("Invalid PNG chunk length.");
            if (type == "IHDR") {
                width = (int)ReadPngUInt(png, index);
                height = (int)ReadPngUInt(png, index + 4);
                if (png[index + 8] != 8 || png[index + 9] != 6) throw new InvalidOperationException("Only 8-bit RGBA PNGs are supported by the smoke decoder.");
            } else if (type == "IDAT") {
                idat.Write(png, index, length);
            } else if (type == "IEND") {
                break;
            }

            index += length + 4;
        }

        var compressed = idat.ToArray();
        if (width <= 0 || height <= 0 || compressed.Length <= 6) throw new InvalidOperationException("PNG is missing image data.");
        using var compressedStream = new MemoryStream(compressed, 2, compressed.Length - 6);
        using var deflate = new DeflateStream(compressedStream, CompressionMode.Decompress);
        using var rawStream = new MemoryStream();
        deflate.CopyTo(rawStream);
        var raw = rawStream.ToArray();
        var stride = width * 4;
        if (raw.Length != height * (stride + 1)) throw new InvalidOperationException("Unexpected PNG scanline length.");
        var pixels = new byte[width * height * 4];
        var source = 0;
        var target = 0;
        for (var y = 0; y < height; y++) {
            if (raw[source++] != 0) throw new InvalidOperationException("Only unfiltered PNG scanlines are supported by the smoke decoder.");
            Buffer.BlockCopy(raw, source, pixels, target, stride);
            source += stride;
            target += stride;
        }

        return new DecodedPng(width, height, pixels);
    }

    private static uint ReadPngUInt(byte[] png, int index) {
        return ((uint)png[index] << 24) | ((uint)png[index + 1] << 16) | ((uint)png[index + 2] << 8) | png[index + 3];
    }

    private readonly struct DecodedPng {
        public readonly int Width;
        public readonly int Height;
        private readonly byte[] _pixels;

        public DecodedPng(int width, int height, byte[] pixels) {
            Width = width;
            Height = height;
            _pixels = pixels;
        }

        public (byte R, byte G, byte B, byte A) Pixel(int x, int y) {
            if (x < 0 || x >= Width || y < 0 || y >= Height) throw new ArgumentOutOfRangeException(nameof(x));
            var index = (y * Width + x) * 4;
            return (_pixels[index], _pixels[index + 1], _pixels[index + 2], _pixels[index + 3]);
        }
    }

    private static void Assert(bool condition, string message) {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void AssertThrows<TException>(Action action, string message) where TException : Exception {
        try {
            action();
        } catch (TException) {
            return;
        }

        throw new InvalidOperationException(message);
    }
}

using System;
using System.Globalization;

namespace ChartForgeX.SvgRaster;

internal readonly struct SvgRasterViewBox {
    public readonly double X;
    public readonly double Y;
    public readonly double Width;
    public readonly double Height;

    public SvgRasterViewBox(double x, double y, double width, double height) {
        if (double.IsNaN(x) || double.IsInfinity(x)) throw new ArgumentOutOfRangeException(nameof(x), "SVG viewBox coordinates must be finite.");
        if (double.IsNaN(y) || double.IsInfinity(y)) throw new ArgumentOutOfRangeException(nameof(y), "SVG viewBox coordinates must be finite.");
        if (double.IsNaN(width) || double.IsInfinity(width) || width <= 0) throw new ArgumentOutOfRangeException(nameof(width), "SVG viewBox width must be finite and positive.");
        if (double.IsNaN(height) || double.IsInfinity(height) || height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "SVG viewBox height must be finite and positive.");
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public static SvgRasterViewBox Parse(string? value) {
        if (string.IsNullOrWhiteSpace(value)) return new SvgRasterViewBox(0, 0, 24, 24);
        var parts = value!.Split(new[] { ' ', '\t', '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4) throw new FormatException("SVG viewBox must contain four numbers.");
        return new SvgRasterViewBox(ParseNumber(parts[0]), ParseNumber(parts[1]), ParseNumber(parts[2]), ParseNumber(parts[3]));
    }

    public static SvgRasterViewBox FromDimensions(string? width, string? height) {
        var parsedWidth = ParseLength(width, 24);
        var parsedHeight = ParseLength(height, 24);
        return new SvgRasterViewBox(0, 0, parsedWidth, parsedHeight);
    }

    public static bool TryFromDimensions(string? width, string? height, out SvgRasterViewBox viewBox) {
        if (!TryParseLength(width, out var parsedWidth) ||
            !TryParseLength(height, out var parsedHeight)) {
            viewBox = default;
            return false;
        }
        viewBox = new SvgRasterViewBox(0, 0, parsedWidth, parsedHeight);
        return true;
    }

    private static double ParseLength(string? value, double fallback) {
        return TryParseLength(value, out var parsed) ? parsed : fallback;
    }

    internal static bool TryParseLength(string? value, out double parsed) {
        parsed = 0;
        if (string.IsNullOrWhiteSpace(value)) return false;
        var trimmed = value!.Trim();
        var multiplier = 1d;
        if (trimmed.EndsWith("px", StringComparison.OrdinalIgnoreCase)) trimmed = trimmed.Substring(0, trimmed.Length - 2);
        else if (trimmed.EndsWith("pt", StringComparison.OrdinalIgnoreCase)) { trimmed = trimmed.Substring(0, trimmed.Length - 2); multiplier = 96d / 72d; }
        else if (trimmed.EndsWith("pc", StringComparison.OrdinalIgnoreCase)) { trimmed = trimmed.Substring(0, trimmed.Length - 2); multiplier = 16d; }
        else if (trimmed.EndsWith("in", StringComparison.OrdinalIgnoreCase)) { trimmed = trimmed.Substring(0, trimmed.Length - 2); multiplier = 96d; }
        else if (trimmed.EndsWith("cm", StringComparison.OrdinalIgnoreCase)) { trimmed = trimmed.Substring(0, trimmed.Length - 2); multiplier = 96d / 2.54d; }
        else if (trimmed.EndsWith("mm", StringComparison.OrdinalIgnoreCase)) { trimmed = trimmed.Substring(0, trimmed.Length - 2); multiplier = 96d / 25.4d; }
        else if (trimmed.EndsWith("q", StringComparison.OrdinalIgnoreCase)) { trimmed = trimmed.Substring(0, trimmed.Length - 1); multiplier = 96d / 101.6d; }
        if (!double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var scalar) ||
            scalar <= 0 ||
            double.IsNaN(scalar) ||
            double.IsInfinity(scalar)) return false;
        parsed = scalar * multiplier;
        return !double.IsNaN(parsed) && !double.IsInfinity(parsed) && parsed > 0;
    }

    private static double ParseNumber(string value) =>
        double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
}

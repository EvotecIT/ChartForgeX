using System;
using System.Globalization;

namespace ChartForgeX.SvgRaster;

internal readonly struct SvgRasterViewBox {
    public readonly double X;
    public readonly double Y;
    public readonly double Width;
    public readonly double Height;

    public SvgRasterViewBox(double x, double y, double width, double height) {
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width), "SVG viewBox width and height must be positive.");
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

    private static double ParseLength(string? value, double fallback) {
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        var trimmed = value!.Trim();
        var multiplier = 1d;
        if (trimmed.EndsWith("px", StringComparison.OrdinalIgnoreCase)) trimmed = trimmed.Substring(0, trimmed.Length - 2);
        else if (trimmed.EndsWith("pt", StringComparison.OrdinalIgnoreCase)) { trimmed = trimmed.Substring(0, trimmed.Length - 2); multiplier = 96d / 72d; }
        else if (trimmed.EndsWith("pc", StringComparison.OrdinalIgnoreCase)) { trimmed = trimmed.Substring(0, trimmed.Length - 2); multiplier = 16d; }
        else if (trimmed.EndsWith("in", StringComparison.OrdinalIgnoreCase)) { trimmed = trimmed.Substring(0, trimmed.Length - 2); multiplier = 96d; }
        else if (trimmed.EndsWith("cm", StringComparison.OrdinalIgnoreCase)) { trimmed = trimmed.Substring(0, trimmed.Length - 2); multiplier = 96d / 2.54d; }
        else if (trimmed.EndsWith("mm", StringComparison.OrdinalIgnoreCase)) { trimmed = trimmed.Substring(0, trimmed.Length - 2); multiplier = 96d / 25.4d; }
        else if (trimmed.EndsWith("q", StringComparison.OrdinalIgnoreCase)) { trimmed = trimmed.Substring(0, trimmed.Length - 1); multiplier = 96d / 101.6d; }
        if (!double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) || parsed <= 0) return fallback;
        return parsed * multiplier;
    }

    private static double ParseNumber(string value) =>
        double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
}

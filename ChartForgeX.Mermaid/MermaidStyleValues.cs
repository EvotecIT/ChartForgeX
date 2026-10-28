using System;
using System.Globalization;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.SvgRaster;

namespace ChartForgeX.Mermaid;

/// <summary>Shares the native paint boundary between Mermaid source diagnostics and conversion.</summary>
internal static class MermaidStyleValues {
    internal static bool IsColor(string value) => SvgRasterColor.TryParse(value, out _)
        || SvgPaint.TryCssVariable(value, ChartColor.Black, out _, out _);

    internal static bool TryDash(string value, out bool dashed) {
        dashed = false;
        if (string.Equals(value.Trim(), "none", StringComparison.OrdinalIgnoreCase)) return true;
        var parts = value.Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return false;
        foreach (var part in parts) {
            if (!double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out var length)
                || double.IsNaN(length) || double.IsInfinity(length) || length < 0) return false;
            if (length > 0) dashed = true;
        }
        return true;
    }
    /// <summary>Reads a finite logical-pixel scalar without evaluating CSS expressions.</summary>
    internal static bool TryPixels(string value, out double number, bool allowUnit = true) {
        value = value.Trim();
        if (allowUnit && value.EndsWith("px", StringComparison.OrdinalIgnoreCase)) value = value.Substring(0, value.Length - 2).TrimEnd();
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number)
            && !double.IsNaN(number) && !double.IsInfinity(number);
    }

    /// <summary>Materializes native dash pairs; an odd positive CSS list repeats once per CSS stroke semantics.</summary>
    internal static bool TryNativeDash(string value, out double[] pattern) {
        pattern = Array.Empty<double>();
        if (string.Equals(value.Trim(), "none", StringComparison.OrdinalIgnoreCase)) return true;
        var parts = value.Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0 || parts.Length > 256) return false;
        var count = parts.Length % 2 == 0 ? parts.Length : parts.Length * 2;
        var values = new double[count];
        for (var index = 0; index < parts.Length; index++) {
            if (!TryPixels(parts[index], out var length, allowUnit: false) || length <= 0) return false;
            values[index] = length;
        }
        for (var index = parts.Length; index < count; index++) values[index] = values[index - parts.Length];
        pattern = values; return true;
    }

}

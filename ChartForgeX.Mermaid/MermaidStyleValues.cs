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
}

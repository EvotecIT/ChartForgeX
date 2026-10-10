using System;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Mermaid;

/// <summary>Maps bounded today-marker stroke declarations to the native reference-line contract.</summary>
internal static class MermaidGanttTodayStyle {
    internal static ChartStrokeStyle? Parse(string? text, out bool supported) {
        supported = true;
        if (string.IsNullOrWhiteSpace(text) || string.Equals(text, "off", StringComparison.OrdinalIgnoreCase)) return null;
        if (text!.Length > 4096) { supported = false; return null; }
        ChartColor? color = null; double? width = null; double[]? dash = null;
        var opacity = 1d; var strokeOpacity = 1d; var any = false;
        var declarations = text.Split(',');
        if (declarations.Length > 64) { supported = false; return null; }
        foreach (var declaration in declarations) {
            var colon = declaration.IndexOf(':');
            if (colon <= 0) { supported = false; continue; }
            var name = declaration.Substring(0, colon).Trim().ToLowerInvariant();
            var value = declaration.Substring(colon + 1).Trim();
            if (name == "stroke" && string.Equals(value, "none", StringComparison.OrdinalIgnoreCase)) { color = ChartColor.Transparent; any = true; }
            else if (name == "stroke" && ChartColor.TryParse(value, out var parsedColor)) { color = parsedColor; any = true; }
            else if (name == "stroke-width" && Pixels(value, out var pixels)) { width = pixels; any = true; }
            else if ((name == "opacity" || name == "stroke-opacity") && Opacity(value, out var alpha)) {
                if (name == "opacity") opacity = alpha; else strokeOpacity = alpha;
                any = true;
            } else if (name == "stroke-dasharray" && Dashes(value, out var lengths)) { dash = lengths; any = true; }
            else supported = false;
        }
        return any ? new ChartStrokeStyle(color, width, opacity * strokeOpacity, dash) : null;
    }

    private static bool Pixels(string text, out double value) {
        if (text.EndsWith("px", StringComparison.OrdinalIgnoreCase)) text = text.Substring(0, text.Length - 2);
        return Number(text, out value) && value >= 0 && value <= 1024;
    }

    private static bool Opacity(string text, out double value) {
        var percent = text.EndsWith("%", StringComparison.Ordinal);
        if (percent) text = text.Substring(0, text.Length - 1);
        if (!Number(text, out value)) return false;
        if (percent) value /= 100;
        return value >= 0 && value <= 1;
    }

    private static bool Dashes(string text, out double[] values) {
        values = Array.Empty<double>();
        if (string.Equals(text, "none", StringComparison.OrdinalIgnoreCase)) return true;
        var tokens = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0 || tokens.Length > 32) return false;
        var result = new double[tokens.Length];
        for (var index = 0; index < tokens.Length; index++)
            if (!Pixels(tokens[index], out result[index]) || result[index] <= 0) return false;
        values = result; return true;
    }

    private static bool Number(string text, out double value) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && !double.IsNaN(value) && !double.IsInfinity(value);
}

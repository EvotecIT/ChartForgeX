using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Primitives;

namespace ChartForgeX.SvgRaster;

internal static partial class SvgRasterColor {
    public static bool TryParse(string? value, out ChartColor color) {
        if (ChartColor.TryParse(value, out color)) return true;
        if (string.IsNullOrWhiteSpace(value)) return false;
        var trimmed = value!.Trim();
        return TryParseRgb(trimmed, out color) || TryParseHsl(trimmed, out color);
    }

    private static bool TryParseRgb(string value, out ChartColor color) {
        color = default;
        var open = value.IndexOf('(');
        if (open <= 0 || value[value.Length - 1] != ')') return false;
        var function = value.Substring(0, open).Trim();
        if (!string.Equals(function, "rgb", StringComparison.OrdinalIgnoreCase) && !string.Equals(function, "rgba", StringComparison.OrdinalIgnoreCase)) return false;
        var parts = SplitFunctionArguments(value.Substring(open + 1, value.Length - open - 2));
        if (parts.Count < 3 || parts.Count > 4) return false;
        if (!TryParseChannel(parts[0], out var red) || !TryParseChannel(parts[1], out var green) || !TryParseChannel(parts[2], out var blue)) return false;
        var alpha = (byte)255;
        if (parts.Count == 4 && !TryParseAlpha(parts[3], out alpha)) return false;
        color = ChartColor.FromRgba(red, green, blue, alpha);
        return true;
    }

    private static IReadOnlyList<string> SplitFunctionArguments(string value) {
        var parts = new List<string>();
        var start = 0;
        for (var i = 0; i < value.Length; i++) {
            if (char.IsWhiteSpace(value[i]) || value[i] == ',' || value[i] == '/') {
                AddPart(value, start, i, parts);
                start = i + 1;
            }
        }

        AddPart(value, start, value.Length, parts);
        return parts;
    }

    private static void AddPart(string value, int start, int end, List<string> parts) {
        if (end <= start) return;
        var part = value.Substring(start, end - start).Trim();
        if (part.Length > 0) parts.Add(part);
    }

    private static bool TryParseChannel(string value, out byte channel) {
        channel = 0;
        if (TryParsePercent(value, out var percent)) {
            channel = ClampByte(percent * 255.0);
            return true;
        }

        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)) return false;
        channel = ClampByte(parsed);
        return true;
    }

    private static bool TryParseAlpha(string value, out byte alpha) {
        alpha = 255;
        if (TryParsePercent(value, out var percent)) {
            alpha = ClampByte(percent * 255.0);
            return true;
        }

        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)) return false;
        alpha = ClampByte(parsed <= 1 ? parsed * 255.0 : parsed);
        return true;
    }

    private static bool TryParsePercent(string value, out double percent) {
        percent = 0;
        var trimmed = value.Trim();
        if (!trimmed.EndsWith("%", StringComparison.Ordinal)) return false;
        trimmed = trimmed.Substring(0, trimmed.Length - 1);
        if (!double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)) return false;
        percent = Math.Max(0, Math.Min(1, parsed / 100.0));
        return true;
    }

    private static byte ClampByte(double value) {
        return ChartForgeX.Rendering.ChartColorMath.QuantizeSrgb(value);
    }
}

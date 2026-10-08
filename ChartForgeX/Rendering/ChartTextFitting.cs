using System;

namespace ChartForgeX.Rendering;

internal static class ChartTextFitting {
    public static string TrimEnd(string value, double fontSize, double maxWidth, Func<string, double, double> measure) {
        return FitEnd(value, maxWidth, text => measure(text, fontSize), "...");
    }

    /// <summary>Fits a prefix without first shaping an arbitrarily long source value.</summary>
    internal static string FitEnd(string value, double maxWidth, Func<string, double> measure, string suffix) {
        if (string.IsNullOrEmpty(value)) return value;
        var length = PrefixLength(value, maxWidth, measure);
        if (length == value.Length) return value;
        if (suffix.Length == 0) return value.Substring(0, length);
        if (measure(suffix) > maxWidth) return string.Empty;
        length = PrefixLength(value, maxWidth, text => measure(text + suffix));
        return value.Substring(0, length) + suffix;
    }

    /// <summary>Returns a whole-text-element prefix using bounded exponential then binary measurement.</summary>
    internal static int PrefixLength(string value, double maxWidth, Func<string, double> measure) {
        if (value.Length == 0) return 0;
        var low = 0;
        var high = Math.Min(16, value.Length);
        while (true) {
            high = Typography.TextElementBoundary.Snap(value, high);
            if (high <= low) high = Typography.TextElementBoundary.Next(value, low);
            if (measure(value.Substring(0, high)) > maxWidth) break;
            low = high;
            if (low == value.Length) return low;
            high = (int)Math.Min(value.Length, (long)high * 2);
        }
        while (Typography.TextElementBoundary.Next(value, low) < high) {
            var mid = Typography.TextElementBoundary.Snap(value, low + (high - low) / 2);
            if (mid <= low) mid = Typography.TextElementBoundary.Next(value, low);
            if (measure(value.Substring(0, mid)) <= maxWidth) low = mid;
            else high = mid;
        }
        return low;
    }
}

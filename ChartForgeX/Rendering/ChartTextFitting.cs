using System;

namespace ChartForgeX.Rendering;

internal static class ChartTextFitting {
    public static string TrimEnd(string value, double fontSize, double maxWidth, Func<string, double, double> measure) {
        return FitEnd(value, maxWidth, text => measure(text, fontSize), "...");
    }

    /// <summary>Fits a prefix without first shaping an arbitrarily long source value.</summary>
    internal static string FitEnd(string value, double maxWidth, Func<string, double> measure, string suffix) {
        return FitEnd(value, text => measure(text) <= maxWidth, suffix);
    }

    /// <summary>Fits a whole-text-element prefix in a measured shape. The predicate must accept nested, increasing prefixes.</summary>
    internal static string FitEnd(string value, Func<string, bool> fits, string suffix) {
        if (string.IsNullOrEmpty(value)) return value;
        var length = PrefixLength(value, fits);
        if (length == value.Length) return value;
        if (suffix.Length == 0) return value.Substring(0, length);
        if (!fits(suffix)) return string.Empty;
        length = PrefixLength(value, text => fits(text + suffix));
        return value.Substring(0, length) + suffix;
    }

    /// <summary>Returns a whole-text-element prefix using bounded exponential then binary measurement.</summary>
    internal static int PrefixLength(string value, double maxWidth, Func<string, double> measure) {
        return PrefixLength(value, text => measure(text) <= maxWidth);
    }

    private static int PrefixLength(string value, Func<string, bool> fits) {
        if (value.Length == 0) return 0;
        var low = 0;
        var high = Math.Min(16, value.Length);
        while (true) {
            high = Typography.TextElementBoundary.Snap(value, high);
            if (high <= low) high = Typography.TextElementBoundary.Next(value, low);
            if (!fits(value.Substring(0, high))) break;
            low = high;
            if (low == value.Length) return low;
            high = (int)Math.Min(value.Length, (long)high * 2);
        }
        while (Typography.TextElementBoundary.Next(value, low) < high) {
            var mid = Typography.TextElementBoundary.Snap(value, low + (high - low) / 2);
            if (mid <= low) mid = Typography.TextElementBoundary.Next(value, low);
            if (fits(value.Substring(0, mid))) low = mid;
            else high = mid;
        }
        return low;
    }
}

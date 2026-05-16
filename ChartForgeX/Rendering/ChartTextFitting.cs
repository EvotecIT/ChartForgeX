using System;

namespace ChartForgeX.Rendering;

internal static class ChartTextFitting {
    public static string TrimEnd(string value, double fontSize, double maxWidth, Func<string, double, double> measure) {
        if (string.IsNullOrEmpty(value) || measure(value, fontSize) <= maxWidth) return value;
        const string suffix = "...";
        if (measure(suffix, fontSize) > maxWidth) return string.Empty;
        var low = 0;
        var high = value.Length;
        while (low < high) {
            var mid = (low + high + 1) / 2;
            if (measure(value.Substring(0, mid) + suffix, fontSize) <= maxWidth) low = mid;
            else high = mid - 1;
        }

        return value.Substring(0, low) + suffix;
    }
}

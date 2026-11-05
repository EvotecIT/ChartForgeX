using System;

namespace ChartForgeX.Rendering;

internal static class ChartTextFitting {
    // Evaluate candidates with the selected face's measured advances.
    internal static double FitFontSize(double minimum, double preferred, Func<double, bool> fits) {
        if (fits(preferred)) return preferred;
        var upper = preferred;
        for (var iteration = 0; iteration < 18; iteration++) {
            var candidate = (minimum + upper) / 2;
            if (fits(candidate)) minimum = candidate;
            else upper = candidate;
        }
        return Math.Floor(minimum * 1000) / 1000;
    }

    /// <summary>Shortens chart text with an ellipsis, keeping at least <see cref="MinimumEllipsizedTextElements"/> visible text elements or hiding it.</summary>
    public static string TrimEnd(string value, double fontSize, double maxWidth, Func<string, double, double> measure) {
        return FitEnd(value, maxWidth, text => measure(text, fontSize), "…", MinimumEllipsizedTextElements);
    }

    /// <summary>Fits a prefix without first shaping an arbitrarily long source value.</summary>
    internal static string FitEnd(string value, double maxWidth, Func<string, double> measure, string suffix, int minimumElements = 0) {
        return FitEnd(value, text => measure(text) <= maxWidth, suffix, minimumElements);
    }

    /// <summary>An ellipsized caption keeps at least this many visible text elements; anything shorter cannot identify its source and is hidden.</summary>
    internal const int MinimumEllipsizedTextElements = 6;

    /// <summary>Whether a shortened prefix keeps enough visible text elements to stand for its source.</summary>
    internal static bool KeepsShortenedMinimum(string prefix, int minimumElements = MinimumEllipsizedTextElements) {
        prefix = prefix.TrimEnd();
        var count = 0;
        for (var index = 0; index < prefix.Length && count < minimumElements; index = Typography.TextElementBoundary.Next(prefix, index)) count++;
        return count >= minimumElements;
    }

    /// <summary>Fits a whole-text-element prefix in a measured shape. The predicate must accept nested, increasing prefixes.</summary>
    /// <remarks>With a suffix, a prefix shorter than <paramref name="minimumElements"/> returns an empty string: the caller hides the text.
    /// Chart text passes <see cref="MinimumEllipsizedTextElements"/>; general text blocks keep any whole-element prefix.</remarks>
    internal static string FitEnd(string value, Func<string, bool> fits, string suffix, int minimumElements = 0) {
        if (string.IsNullOrEmpty(value)) return value;
        var length = PrefixLength(value, fits);
        if (length == value.Length) return value;
        if (suffix.Length == 0) return value.Substring(0, length);
        if (!fits(suffix)) return string.Empty;
        length = PrefixLength(value, text => fits(text + suffix));
        var prefix = value.Substring(0, length);
        return minimumElements <= 0 || KeepsShortenedMinimum(prefix, minimumElements) ? prefix + suffix : string.Empty;
    }

    /// <summary>
    /// Returns where the distinguishing words of <paramref name="value"/> start when a sibling shares at least one of
    /// its leading words, or -1. An end ellipsis would keep only that shared head, so callers keep this tail instead.
    /// </summary>
    internal static int DistinguishingTailStart(string value, System.Collections.Generic.IEnumerable<string> siblings) {
        var shared = 0;
        foreach (var sibling in siblings) {
            if (ReferenceEquals(sibling, value) || string.Equals(sibling, value, StringComparison.Ordinal)) continue;
            var length = 0;
            while (length < value.Length && length < sibling.Length && value[length] == sibling[length]) length++;
            shared = Math.Max(shared, length);
        }
        if (shared == 0 || shared >= value.Length) return -1;
        var wordStart = value.LastIndexOf(' ', Math.Max(0, shared - 1));
        // The shared head must include a whole word; otherwise ordinary end fitting already differs.
        if (wordStart <= 0) return -1;
        return wordStart + 1 < value.Length ? wordStart + 1 : -1;
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

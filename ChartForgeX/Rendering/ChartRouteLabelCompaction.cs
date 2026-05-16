using System;
using System.Text;

namespace ChartForgeX.Rendering;

internal static class ChartRouteLabelCompaction {
    public static string Compact(string label, int maximumLength = 24) {
        if (label == null) throw new ArgumentNullException(nameof(label));
        var ms = label.LastIndexOf(" ms", StringComparison.OrdinalIgnoreCase);
        if (ms > 0) {
            var start = label.LastIndexOf(' ', ms - 1);
            if (start >= 0 && ms + 3 <= label.Length) return label.Substring(start + 1, ms - start + 2);
        }

        var routeSeparator = label.IndexOf(" to ", StringComparison.OrdinalIgnoreCase);
        if (routeSeparator > 0) {
            var route = CompactEndpoint(label.Substring(0, routeSeparator)) + " to " + CompactEndpoint(label.Substring(routeSeparator + 4));
            if (route.Length <= maximumLength) return route;
        }

        return label.Length <= maximumLength ? label : label.Substring(0, maximumLength - 3) + "...";
    }

    private static string CompactEndpoint(string label) {
        label = label.Trim();
        if (label.Length <= 12 || label.IndexOf(' ') < 0) return label;
        var words = label.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 2) return label;
        var abbreviation = new StringBuilder(words.Length);
        foreach (var word in words) if (word.Length > 0 && char.IsLetterOrDigit(word[0])) abbreviation.Append(char.ToUpperInvariant(word[0]));
        return abbreviation.Length >= 2 ? abbreviation.ToString() : label;
    }
}

using System;
using System.Globalization;
using System.Text;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidGanttInputDateFormat {
    private static readonly string[] CompleteDateFallbacks = {
        "yyyy-MM-dd", "yyyy-MM-dd HH:mm", "yyyy-MM-dd H:mm", "yyyy-MM-ddTHH:mm", "yyyy-MM-ddTH:mm",
        "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ss.FFFFFFFK", "yyyy-M-d", "yyyy-M-d H:mm", "yyyy-M-d H:mm:ss",
        "MMMM d yyyy", "MMM d yyyy", "MMMM d, yyyy", "MMM d, yyyy", "d MMMM yyyy", "d MMM yyyy",
        "M/d/yyyy", "M-d-yyyy", "yyyy/M/d", "o", "r"
    };

    internal static bool TryParseCompleteFallback(string input, out DateTime value) {
        foreach (var format in CompleteDateFallbacks)
            if (TryParseNative(input, format, out value, null, allowWhiteSpaces: true)) return true;
        value = default;
        return false;
    }

    private static bool TryParseNative(string input, string format, out DateTime value, DateTime? anchor, bool allowWhiteSpaces = false) {
        var formats = IsNativeStandardFormat(format)
            ? CultureInfo.InvariantCulture.DateTimeFormat.GetAllDateTimePatterns(format[0]) : new[] { format };
        var whiteSpaces = allowWhiteSpaces ? DateTimeStyles.AllowWhiteSpaces : DateTimeStyles.None;
        foreach (var candidate in formats) {
            var normalized = new StringBuilder(input.Trim()); var nativeFormat = new StringBuilder(candidate);
            DateFields(candidate, out _, out _, out _, out var offset);
            if (!ApplyDateDefaults(normalized, nativeFormat, anchor)) continue;
            if (offset) {
                // Optional K fields without an authored offset also remain independent of the host.
                if (!DateTimeOffset.TryParseExact(normalized.ToString(), nativeFormat.ToString(), CultureInfo.InvariantCulture,
                    whiteSpaces | DateTimeStyles.AssumeUniversal, out var instant)) continue;
                value = DateTime.SpecifyKind(instant.UtcDateTime, DateTimeKind.Unspecified);
                return true;
            }
            if (!DateTime.TryParseExact(normalized.ToString(), nativeFormat.ToString(), CultureInfo.InvariantCulture,
                whiteSpaces | DateTimeStyles.NoCurrentDateDefault, out value)) continue;
            value = DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
            return true;
        }
        value = default;
        return false;
    }
}

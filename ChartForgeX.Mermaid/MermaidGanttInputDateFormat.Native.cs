using System;
using System.Globalization;
using System.Text;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidGanttInputDateFormat {
    private static readonly string[] CompleteDateFallbacks = {
        "yyyy-MM-dd", "yyyy-MM-dd HH:mm", "yyyy-MM-dd H:mm", "yyyy-MM-ddTHH:mm", "yyyy-MM-ddTH:mm",
        "yyyy-MM-dd HH:mmK", "yyyy-MM-dd H:mmK", "yyyy-MM-ddTHH:mmK", "yyyy-MM-ddTH:mmK",
        "yyyy-MM-dd HH:mm:ss.FFFFFFFK", "yyyy-MM-dd H:mm:ss.FFFFFFFK",
        "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ss.FFFFFFFK", "yyyy-MM-ddTH:mm:ss.FFFFFFFK", "yyyy-M-d", "yyyy-M-d H:mm", "yyyy-M-d H:mm:ss",
        "yyyy-M-dTH:mm", "yyyy-M-dTH:mmK", "yyyy-M-dTH:mm:ss", "yyyy-M-dTH:mm:ss.FFFFFFFK",
        "yyyy-M-d H:mmK", "yyyy-M-d H:mm:ss.FFFFFFFK",
        "MMMM d yyyy", "MMM d yyyy", "MMMM d, yyyy", "MMM d, yyyy", "d MMMM yyyy", "d MMM yyyy",
        "M/d/yyyy", "M-d-yyyy", "yyyy/M/d", "o", "r"
    };

    private static readonly string[] CompleteNonIsoDates = {
        "MMMM d yyyy", "MMM d yyyy", "MMMM d, yyyy", "MMM d, yyyy", "d MMMM yyyy", "d MMM yyyy",
        "M/d/yyyy", "M-d-yyyy", "yyyy/M/d",
        "dddd, d MMMM yyyy", "dddd, d MMM yyyy", "ddd, d MMMM yyyy", "ddd, d MMM yyyy",
        "dddd, MMMM d, yyyy", "dddd, MMM d, yyyy", "ddd, MMMM d, yyyy", "ddd, MMM d, yyyy",
        "dddd d MMMM yyyy", "dddd d MMM yyyy", "ddd d MMMM yyyy", "ddd d MMM yyyy",
        "dddd MMMM d yyyy", "dddd MMM d yyyy", "ddd MMMM d yyyy", "ddd MMM d yyyy"
    };
    private static readonly string[] CompleteTimeSuffixes = {
        " H:mm", " H:mm:ss", " H:mm:ss.FFFFFFF", " h:mm tt", " h:mm:ss tt", " h:mm:ss.FFFFFFF tt",
        " H:mmK", " H:mm:ss.FFFFFFFK", " H:mm zzz", " H:mm:ss.FFFFFFF zzz",
        " h:mm ttK", " h:mm:ss ttK", " h:mm:ss.FFFFFFF ttK",
        " h:mm tt zzz", " h:mm:ss tt zzz", " h:mm:ss.FFFFFFF tt zzz"
    };

    internal static bool TryParseCompleteFallback(string input, out DateTime value) {
        foreach (var format in CompleteDateFallbacks)
            if (TryParseNative(input, format, out value, null, allowWhiteSpaces: true)) return true;
        foreach (var date in CompleteNonIsoDates) {
            if (TryParseNative(input, date, out value, null, allowWhiteSpaces: true)) return true;
            foreach (var time in CompleteTimeSuffixes)
                if (TryParseNative(input, date + time, out value, null, allowWhiteSpaces: true)
                    || TryParseNative(input, date + "," + time, out value, null, allowWhiteSpaces: true)) return true;
        }
        value = default;
        return false;
    }

    internal static bool IsWeekdayName(string input) {
        var format = CultureInfo.InvariantCulture.DateTimeFormat;
        for (var day = 0; day < 7; day++)
            if (string.Equals(input, format.GetDayName((DayOfWeek)day), StringComparison.OrdinalIgnoreCase)
                || string.Equals(input, format.GetAbbreviatedDayName((DayOfWeek)day), StringComparison.OrdinalIgnoreCase)) return true;
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

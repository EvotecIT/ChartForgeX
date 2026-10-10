using System;
using System.Globalization;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidGanttParser {
    private static bool TryParseDuration(string text, out double amount, out string unit) {
        text = text.Trim();
        amount = 0;
        unit = string.Empty;
        var index = 0;
        while (index < text.Length && (char.IsDigit(text[index]) || text[index] == '.')) index++;
        if (index == 0 || index == text.Length ||
            !double.TryParse(text.Substring(0, index), NumberStyles.Float, CultureInfo.InvariantCulture, out amount) ||
            amount < 0 || double.IsInfinity(amount) || double.IsNaN(amount)) return false;

        var authoredUnit = text.Substring(index).Trim();
        // Mermaid shorthand is case-sensitive: M means calendar months, m means minutes.
        unit = authoredUnit == "M" ? "M" : authoredUnit.ToLowerInvariant();
        switch (unit) {
            case "ms": case "millisecond": case "milliseconds":
            case "s": case "second": case "seconds":
            case "m": case "minute": case "minutes":
            case "h": case "hour": case "hours":
            case "d": case "day": case "days":
            case "w": case "week": case "weeks":
            case "M": case "month": case "months":
            case "y": case "year": case "years":
                return true;
            default:
                return false;
        }
    }

    // Match Mermaid's Day.js addition: calendar month/year clamping, rounded days/weeks,
    // and millisecond clock precision. DateTime keeps the schedule independent of the host timezone.
    private static DateTime AddDuration(DateTime start, double amount, string unit) {
        switch (unit) {
            case "M": case "month": case "months":
                return start.AddMonths(checked((int)amount));
            case "y": case "year": case "years":
                return start.AddYears(checked((int)amount));
            case "w": case "week": case "weeks":
                return start.AddDays(Math.Floor(amount * 7 + 0.5));
            case "d": case "day": case "days":
                return start.AddDays(Math.Floor(amount + 0.5));
            case "h": case "hour": case "hours":
                return start.Add(TimeSpan.FromMilliseconds(Math.Truncate(amount * 3600000)));
            case "m": case "minute": case "minutes":
                return start.Add(TimeSpan.FromMilliseconds(Math.Truncate(amount * 60000)));
            case "s": case "second": case "seconds":
                return start.Add(TimeSpan.FromMilliseconds(Math.Truncate(amount * 1000)));
            default:
                return start.Add(TimeSpan.FromMilliseconds(Math.Truncate(amount)));
        }
    }
}

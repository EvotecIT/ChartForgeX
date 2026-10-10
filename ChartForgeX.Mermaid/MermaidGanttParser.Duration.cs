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
        // Mermaid distinguishes M (months) from m (minutes). Other existing clock-unit
        // aliases remain case-insensitive for ChartForgeX source compatibility.
        unit = authoredUnit == "M" ? "M" : authoredUnit.ToLowerInvariant();
        switch (unit) {
            case "ms": case "millisecond": case "milliseconds":
            case "s": case "second": case "seconds":
            case "m": case "minute": case "minutes":
            case "h": case "hour": case "hours":
            case "d": case "day": case "days":
            case "w": case "week": case "weeks":
                return true;
            case "M":
            case "y":
                // New calendar units follow Mermaid's adjacent suffix and decimal grammar.
                // Preserve existing clock-unit aliases without extending calendar syntax.
                return authoredUnit == unit && index == text.Length - 1 &&
                    text[0] != '.' && text[index - 1] != '.';
            default:
                return false;
        }
    }

    // Match Mermaid's Day.js addition: calendar month/year clamping, rounded days/weeks,
    // and millisecond clock precision. DateTime keeps the schedule independent of the host timezone.
    private static DateTime AddDuration(DateTime start, double amount, string unit) {
        switch (unit) {
            case "M":
                return start.AddMonths(checked((int)amount));
            case "y":
                return start.AddYears(checked((int)amount));
            case "w": case "week": case "weeks":
                return start.AddDays(Math.Floor(amount * 7 + 0.5));
            case "d": case "day": case "days":
                return start.AddDays(Math.Floor(amount + 0.5));
            case "h": case "hour": case "hours":
                return AddClockDuration(start, amount * 3600000);
            case "m": case "minute": case "minutes":
                return AddClockDuration(start, amount * 60000);
            case "s": case "second": case "seconds":
                return AddClockDuration(start, amount * 1000);
            default:
                return AddClockDuration(start, amount);
        }
    }

    private static DateTime AddClockDuration(DateTime start, double milliseconds) {
        // Day.js adds before JavaScript Date truncates the complete timestamp to milliseconds.
        // Truncating a scaled duration first loses precision (for example, 1.001 seconds).
        var epochTicks = new DateTime(1970, 1, 1).Ticks;
        var endMilliseconds = Math.Truncate((start.Ticks - epochTicks) / (double)TimeSpan.TicksPerMillisecond + milliseconds);
        var endTicks = checked((long)endMilliseconds * TimeSpan.TicksPerMillisecond + epochTicks);
        // ChartForgeX also accepts legacy DateTime inputs and milestone midpoints finer
        // than one millisecond. A nonnegative duration must never move those backwards.
        return new DateTime(Math.Max(start.Ticks, endTicks), start.Kind);
    }
}

using System;

namespace ChartForgeX.Rendering;

internal static partial class ChartTimeScale {
    // Calendar boundaries in a skipped local interval move to its first valid instant.
    // Keep the original calendar cursor for subsequent days, weeks and months.
    private static DateTime NormalizeCalendarBoundary(DateTime value, TimeZoneInfo zone) {
        if (!zone.IsInvalidTime(value)) return value;
        var lower = value;
        var upper = value;
        for (var minutes = 1; minutes <= 4096; minutes *= 2) {
            upper = value.AddMinutes(minutes);
            if (!zone.IsInvalidTime(upper)) break;
            lower = upper;
        }
        if (zone.IsInvalidTime(upper)) throw new ArgumentException("The calendar boundary cannot be resolved in the supported window.");
        while (upper.Ticks - lower.Ticks > 1) {
            var middle = lower.AddTicks((upper.Ticks - lower.Ticks) / 2);
            if (zone.IsInvalidTime(middle)) lower = middle;
            else upper = middle;
        }
        return upper;
    }
}

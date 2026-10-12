using System;
using System.Collections.Generic;

namespace ChartForgeX.Rendering;

internal static partial class ChartTimeScale {
    // D3 hour ranges offset UTC instants, then floor in the display zone. A fractional
    // DST shift can therefore produce a half-hour boundary. Counts filter local hours.
    private static IReadOnlyList<double>? GenerateFixedHours(TimeZoneInfo zone, double min, double max, int count) {
        var steps = 0;
        var start = DateTime.SpecifyKind(DateTime.FromOADate(min), DateTimeKind.Utc);
        var stop = DateTime.SpecifyKind(DateTime.FromOADate(max), DateTimeKind.Utc);
        var candidate = Floor(start.AddMilliseconds(-1));
        candidate = Floor(Offset(candidate));
        var ticks = new List<double>();
        while (candidate <= stop) {
            if (++steps > MaximumSteps || ticks.Count >= MaximumTicks) return null;
            if (candidate >= start) ticks.Add(candidate.ToOADate());
            var next = Floor(Offset(candidate));
            if (next <= candidate) break;
            candidate = next;
        }
        return ticks;

        DateTime Floor(DateTime instant) {
            while (true) {
                if (++steps > MaximumSteps) throw new ArgumentException("The hourly interval exceeds the step budget.");
                var local = TimeZoneInfo.ConvertTimeFromUtc(instant, zone);
                instant = instant.AddTicks(-(local.Ticks % TimeSpan.TicksPerHour));
                if (TimeZoneInfo.ConvertTimeFromUtc(instant, zone).Hour % count == 0) return instant;
                instant = instant.AddMilliseconds(-1);
            }
        }

        DateTime Offset(DateTime instant) {
            do {
                if (++steps > MaximumSteps) throw new ArgumentException("The hourly interval exceeds the step budget.");
                instant = instant.AddHours(1);
            } while (TimeZoneInfo.ConvertTimeFromUtc(instant, zone).Hour % count != 0);
            return instant;
        }
    }
}

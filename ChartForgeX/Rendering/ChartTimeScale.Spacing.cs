using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static partial class ChartTimeScale {
    /// <summary>Enumerates the existing calendar schedule, retaining missing bands and actual DST boundaries.</summary>
    internal static double[] UnitBoundaries(ChartAxis axis, double minimum, double maximum, ChartTimeTickInterval interval) {
        var zone = interval.Unit == ChartTimeTickUnit.Millisecond ? TimeZoneInfo.Utc : axis.TimeZone ?? TimeZoneInfo.Utc;
        var localMin = ToLocal(minimum, zone);
        var count = interval.Count;
        DateTime floor;
        switch (interval.Unit) {
            case ChartTimeTickUnit.Millisecond:
                var step = (long)count * TimeSpan.TicksPerMillisecond;
                floor = localMin.AddTicks(-PositiveRemainder(localMin.Ticks - TickEpoch.Ticks, step)); break;
            case ChartTimeTickUnit.Second: floor = new DateTime(localMin.Year, localMin.Month, localMin.Day, localMin.Hour, localMin.Minute, localMin.Second / count * count); break;
            case ChartTimeTickUnit.Minute: floor = new DateTime(localMin.Year, localMin.Month, localMin.Day, localMin.Hour, localMin.Minute / count * count, 0); break;
            case ChartTimeTickUnit.Hour: floor = new DateTime(localMin.Year, localMin.Month, localMin.Day, localMin.Hour / count * count, 0, 0); break;
            case ChartTimeTickUnit.Day: floor = new DateTime(localMin.Year, localMin.Month, (localMin.Day - 1) / count * count + 1); break;
            case ChartTimeTickUnit.Week:
                floor = localMin.Date.AddDays(-(((int)localMin.DayOfWeek - (int)interval.WeekStart + 7) % 7));
                var epoch = ToLocal(TickEpoch.ToOADate(), zone).Date;
                var epochWeek = epoch.AddDays(-(((int)epoch.DayOfWeek - (int)interval.WeekStart + 7) % 7));
                var weeks = (floor.Ticks - epochWeek.Ticks) / (TimeSpan.TicksPerDay * 7);
                floor = floor.AddDays(-PositiveRemainder(weeks, count) * 7); break;
            case ChartTimeTickUnit.Month: floor = new DateTime(localMin.Year, (localMin.Month - 1) / count * count + 1, 1); break;
            default: floor = new DateTime((localMin.Year - 1) / count * count + 1, 1, 1); break;
        }
        var boundaries = new List<double>(); var instants = new List<DateTime>(2);
        var resolution = new TimeInterval(interval.Unit <= ChartTimeTickUnit.Hour ? TimeUnit.Second : TimeUnit.Day, 1);
        var current = (DateTime?)floor;
        while (current.HasValue) {
            ResolveInstants(current.Value, zone, resolution, instants);
            foreach (var instant in instants) {
                var value = ChartDateTime.ToOADate(instant);
                if (value < 0) throw new InvalidOperationException("Discrete time bands require ordered instants on or after 1899-12-30 UTC.");
                boundaries.Add(value);
                if (boundaries.Count > ChartTimeSpacingDomain.MaximumEntries + 1)
                    throw new InvalidOperationException("The calendar time domain exceeds the 100,000-band limit; select a coarser calendar unit or narrower authored bounds.");
            }
            if (boundaries.Count > 0 && boundaries[boundaries.Count - 1] > maximum) break;
            current = FixedNext(current.Value, interval);
        }
        boundaries.Sort();
        var ordered = boundaries.Distinct().ToArray(); var first = 0;
        while (first + 1 < ordered.Length && ordered[first + 1] <= minimum) first++;
        var result = ordered.Skip(first).ToArray();
        if (result.Length < 2 || result[0] > minimum || result[result.Length - 1] <= maximum)
            throw new InvalidOperationException("The calendar time domain requires a representable enclosing calendar boundary.");
        return result;
    }
}

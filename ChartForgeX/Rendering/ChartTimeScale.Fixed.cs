using System;
using System.Collections.Generic;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal static partial class ChartTimeScale {
    private static readonly DateTime TickEpoch = new(1970, 1, 1);

    /// <summary>Returns fixed calendar boundaries inside a window, including an empty result when none occur.
    /// Null requests automatic ticks for an unsupported date window or an interval exceeding the tick budget.</summary>
    internal static IReadOnlyList<double>? GenerateFixed(ChartAxis axis, double min, double max, ChartTimeTickInterval interval) {
        if (axis == null) throw new ArgumentNullException(nameof(axis));
        if (interval == null) throw new ArgumentNullException(nameof(interval));
        if (min < 0 || !IsRepresentable(max) || min > max) return null;
        // Milliseconds are epoch intervals, so display-zone offsets must not move their boundaries.
        var zone = interval.Unit == ChartTimeTickUnit.Millisecond ? TimeZoneInfo.Utc : axis.TimeZone ?? TimeZoneInfo.Utc;
        try {
            var localMin = ToLocal(min, zone); var localMax = ToLocal(max, zone);
            var walkMin = localMin - RepeatedHourWidth(localMin, zone);
            var walkMax = localMax + RepeatedHourWidth(localMax, zone);
            var candidate = FixedCeiling(walkMin, interval);
            var ticks = new List<double>(); var instants = new List<DateTime>(2);
            var resolution = new TimeInterval(interval.Unit <= ChartTimeTickUnit.Hour ? TimeUnit.Second : TimeUnit.Day, 1);
            var steps = 0;
            while (candidate.HasValue && candidate.Value <= walkMax) {
                if (++steps > MaximumSteps) return null;
                ResolveInstants(candidate.Value, zone, resolution, instants);
                foreach (var instant in instants) {
                    var value = instant.ToOADate();
                    if (value >= min && value <= max) ticks.Add(value);
                    if (ticks.Count > MaximumTicks) return null;
                }
                candidate = FixedNext(candidate.Value, interval);
            }
            ticks.Sort();
            for (var index = ticks.Count - 1; index > 0; index--) if (ticks[index] == ticks[index - 1]) ticks.RemoveAt(index);
            return ticks;
        } catch (Exception exception) when (exception is ArgumentException || exception is OverflowException) { return null; }
    }

    private static DateTime? FixedCeiling(DateTime value, ChartTimeTickInterval interval) {
        var count = interval.Count;
        DateTime floor;
        switch (interval.Unit) {
            case ChartTimeTickUnit.Millisecond:
                var step = (long)count * TimeSpan.TicksPerMillisecond;
                var remainder = PositiveRemainder(value.Ticks - TickEpoch.Ticks, step);
                return FixedAddTicks(value, remainder == 0 ? 0 : step - remainder);
            case ChartTimeTickUnit.Second: floor = new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second / count * count); break;
            case ChartTimeTickUnit.Minute: floor = new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute / count * count, 0); break;
            case ChartTimeTickUnit.Hour: floor = new DateTime(value.Year, value.Month, value.Day, value.Hour / count * count, 0, 0); break;
            case ChartTimeTickUnit.Day: floor = new DateTime(value.Year, value.Month, (value.Day - 1) / count * count + 1); break;
            case ChartTimeTickUnit.Week:
                floor = value.Date.AddDays(-(((int)value.DayOfWeek - (int)interval.WeekStart + 7) % 7));
                var epochWeek = TickEpoch.AddDays(-(((int)TickEpoch.DayOfWeek - (int)interval.WeekStart + 7) % 7));
                var weeks = (floor.Ticks - epochWeek.Ticks) / (TimeSpan.TicksPerDay * 7);
                var remaining = PositiveRemainder(weeks, count);
                if (remaining != 0) return FixedAddDays(floor, (count - remaining) * 7);
                break;
            default: floor = new DateTime(value.Year, (value.Month - 1) / count * count + 1, 1); break;
        }
        return floor >= value ? floor : FixedNext(floor, interval);
    }

    private static DateTime? FixedNext(DateTime value, ChartTimeTickInterval interval) {
        var count = (long)interval.Count;
        try {
            switch (interval.Unit) {
                case ChartTimeTickUnit.Millisecond: return FixedAddTicks(value, count * TimeSpan.TicksPerMillisecond);
                case ChartTimeTickUnit.Second: return value.Second + count < 60 ? value.AddSeconds(count) : new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0).AddMinutes(1);
                case ChartTimeTickUnit.Minute: return value.Minute + count < 60 ? value.AddMinutes(count) : new DateTime(value.Year, value.Month, value.Day, value.Hour, 0, 0).AddHours(1);
                case ChartTimeTickUnit.Hour: return value.Hour + count < 24 ? value.AddHours(count) : value.Date.AddDays(1);
                case ChartTimeTickUnit.Day: return value.Day - 1 + count < DateTime.DaysInMonth(value.Year, value.Month) ? value.AddDays(count) : new DateTime(value.Year, value.Month, 1).AddMonths(1);
                case ChartTimeTickUnit.Week: return FixedAddDays(value, count * 7);
                default: return value.Month - 1 + count < 12 ? value.AddMonths((int)count) : new DateTime(value.Year, 1, 1).AddYears(1);
            }
        } catch (ArgumentOutOfRangeException) { return null; }
    }

    private static long PositiveRemainder(long value, long divisor) => (value % divisor + divisor) % divisor;
    private static DateTime? FixedAddTicks(DateTime value, long ticks) => ticks > DateTime.MaxValue.Ticks - value.Ticks ? null : value.AddTicks(ticks);
    private static DateTime? FixedAddDays(DateTime value, long days) {
        try { return value.AddDays(days); } catch (ArgumentOutOfRangeException) { return null; }
    }
}

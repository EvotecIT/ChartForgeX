using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>
/// Generates calendar-aligned ticks and default labels for <see cref="ChartScaleKind.Time"/> axes.
/// Axis values are OLE Automation dates holding UTC instants; alignment and labels use the axis display zone.
/// </summary>
internal static class ChartTimeScale {
    private const int MaximumTicks = 2_000;
    private const int MaximumSteps = 100_000;
    private const double MinimumOaDate = -657434.0;
    private const double MaximumOaDate = 2958465.0;
    private const double DaysPerSecond = 1.0 / 86400.0;
    private const int MinimumYear = 100;

    private static readonly TimeInterval[] Intervals = {
        new(TimeUnit.Second, 1), new(TimeUnit.Second, 5), new(TimeUnit.Second, 15), new(TimeUnit.Second, 30),
        new(TimeUnit.Minute, 1), new(TimeUnit.Minute, 2), new(TimeUnit.Minute, 5), new(TimeUnit.Minute, 10), new(TimeUnit.Minute, 15), new(TimeUnit.Minute, 30),
        new(TimeUnit.Hour, 1), new(TimeUnit.Hour, 2), new(TimeUnit.Hour, 3), new(TimeUnit.Hour, 6), new(TimeUnit.Hour, 12),
        new(TimeUnit.Day, 1), new(TimeUnit.Day, 2), new(TimeUnit.Week, 1),
        new(TimeUnit.Month, 1), new(TimeUnit.Month, 3), new(TimeUnit.Month, 6),
        new(TimeUnit.Year, 1)
    };

    /// <summary>Returns calendar-aligned ticks, or null when the range cannot be represented as dates.</summary>
    public static IReadOnlyList<double>? Generate(ChartAxis axis, double min, double max, bool inside) {
        if (axis == null) throw new ArgumentNullException(nameof(axis));
        if (min > max) (min, max) = (max, min);
        // OLE dates before 1899-12-30 store the time of day with an inverted sign, so they are not ordered within a day.
        if (min < 0 || !IsRepresentable(max) || max - min < DaysPerSecond) return null;
        var zone = axis.TimeZone ?? TimeZoneInfo.Utc;
        var interval = ChooseInterval((max - min) / (Math.Max(2, axis.TickCount) - 1));
        try {
            var localMin = ToLocal(min, zone);
            var localMax = ToLocal(max, zone);
            // A repeated wall-clock hour at either end hides instants on the other side of the bound, so walk past it.
            var walkMin = localMin - RepeatedHourWidth(localMin, zone);
            var walkMax = localMax + RepeatedHourWidth(localMax, zone);
            var start = Floor(walkMin, interval);
            // Outer ticks start one interval early so a skipped daylight-saving floor still leaves a tick at or before min.
            if (!inside && start.Year > MinimumYear) start = Floor(start.AddTicks(-1), interval);
            var windowMin = min - interval.ApproximateDays * 1.5;
            var windowMax = max + interval.ApproximateDays * 1.5;
            var candidates = new List<double>();
            var instants = new List<DateTime>(2);
            var latest = double.NegativeInfinity;
            for (var (local, steps) = (start, 0); candidates.Count < MaximumTicks && steps < MaximumSteps; local = Next(local, interval), steps++) {
                if (local > walkMax && (inside || latest >= max)) break;
                ResolveInstants(local, zone, interval, instants);
                foreach (var instant in instants) {
                    var value = instant.ToOADate();
                    if (value < windowMin || value > windowMax) continue;
                    candidates.Add(value);
                    latest = Math.Max(latest, value);
                }

                if (local.Ticks > DateTime.MaxValue.Ticks - IntervalTicksUpperBound(interval)) break;
            }

            // Repeated daylight-saving hours interleave instants from consecutive wall-clock steps, so order them by instant.
            candidates.Sort();
            var ticks = new List<double>(candidates.Count);
            foreach (var value in candidates) {
                if (ticks.Count > 0 && value <= ticks[ticks.Count - 1]) continue;
                if (!inside || (value >= min && value <= max)) ticks.Add(value);
            }

            if (inside) return ticks.Count >= 2 ? ticks : new[] { min, max };
            while (ticks.Count > 1 && ticks[1] <= min) ticks.RemoveAt(0);
            while (ticks.Count > 1 && ticks[ticks.Count - 2] >= max) ticks.RemoveAt(ticks.Count - 1);
            if (ticks.Count == 0 || ticks[0] > min) ticks.Insert(0, min);
            if (ticks[ticks.Count - 1] < max) ticks.Add(max);
            return ticks;
        } catch (Exception exception) when (exception is ArgumentException || exception is OverflowException) {
            return null;
        }
    }

    /// <summary>Formats a time-axis value: dates at midnight, otherwise wall-clock time in the display zone.</summary>
    public static string Format(ChartAxis axis, double value) {
        if (axis == null) throw new ArgumentNullException(nameof(axis));
        if (ToDisplayTime(axis, value) is not { } rounded) return ChartNumericFormatter.FormatCompact(value);
        if (rounded.TimeOfDay == TimeSpan.Zero) return rounded.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return rounded.ToString(rounded.Second == 0 ? "HH:mm" : "HH:mm:ss", CultureInfo.InvariantCulture);
    }

    /// <summary>Returns the axis title with the time-zone designator appended when the axis requests it.</summary>
    public static string DecorateTitle(ChartAxis axis, string title) {
        if (axis == null) throw new ArgumentNullException(nameof(axis));
        if (axis.Scale != ChartScaleKind.Time || !axis.ShowTimeZone) return title ?? string.Empty;
        var label = ZoneDesignator(axis);
        return string.IsNullOrWhiteSpace(title) ? label : title + " (" + label + ")";
    }

    /// <summary>Returns the time-zone designator for an axis: the explicit label, <c>UTC</c>, or the zone identifier.</summary>
    public static string ZoneDesignator(ChartAxis axis) {
        if (axis == null) throw new ArgumentNullException(nameof(axis));
        return !string.IsNullOrWhiteSpace(axis.TimeZoneLabel) ? axis.TimeZoneLabel!.Trim() : axis.TimeZone == null ? "UTC" : axis.TimeZone.Id;
    }

    /// <summary>Converts an axis value to wall-clock time in the display zone, rounded to whole seconds, or null when not a date.</summary>
    public static DateTime? ToDisplayTime(ChartAxis axis, double value) {
        if (axis == null) throw new ArgumentNullException(nameof(axis));
        if (!IsRepresentable(value)) return null;
        var local = ToLocal(value, axis.TimeZone ?? TimeZoneInfo.Utc);
        return new DateTime((local.Ticks + TimeSpan.TicksPerSecond / 2) / TimeSpan.TicksPerSecond * TimeSpan.TicksPerSecond);
    }

    private static void ResolveInstants(DateTime local, TimeZoneInfo zone, TimeInterval interval, List<DateTime> instants) {
        instants.Clear();
        if (local.Year < MinimumYear || zone.IsInvalidTime(local)) return;
        if (!zone.IsAmbiguousTime(local)) {
            instants.Add(TimeZoneInfo.ConvertTimeToUtc(local, zone));
            return;
        }

        // A repeated wall-clock time maps to two instants; sub-day ticks keep both so the repeated hour is not left empty.
        var offsets = zone.GetAmbiguousTimeOffsets(local);
        Array.Sort(offsets);
        for (var i = offsets.Length - 1; i >= 0; i--) {
            instants.Add(DateTime.SpecifyKind(local - offsets[i], DateTimeKind.Utc));
            if (interval.Unit > TimeUnit.Hour) break;
        }
    }

    private static TimeSpan RepeatedHourWidth(DateTime local, TimeZoneInfo zone) {
        if (!zone.IsAmbiguousTime(local)) return TimeSpan.Zero;
        var offsets = zone.GetAmbiguousTimeOffsets(local);
        Array.Sort(offsets);
        return offsets[offsets.Length - 1] - offsets[0];
    }

    private static bool IsRepresentable(double value) => value >= MinimumOaDate && value <= MaximumOaDate;

    private static DateTime ToLocal(double value, TimeZoneInfo zone) {
        var utc = DateTime.SpecifyKind(DateTime.FromOADate(value), DateTimeKind.Utc);
        return DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(utc, zone), DateTimeKind.Unspecified);
    }

    private static TimeInterval ChooseInterval(double targetDays) {
        var best = Intervals[0];
        var bestScore = double.PositiveInfinity;
        foreach (var candidate in Intervals) {
            var score = Math.Abs(Math.Log(candidate.ApproximateDays / targetDays));
            if (score < bestScore) {
                best = candidate;
                bestScore = score;
            }
        }

        if (best.Unit != TimeUnit.Year) return best;
        return new TimeInterval(TimeUnit.Year, NiceYearStep(targetDays / 365.25));
    }

    private static int NiceYearStep(double years) {
        if (years <= 1.5) return 1;
        var power = Math.Pow(10, Math.Floor(Math.Log10(years)));
        var fraction = years / power;
        var nice = fraction < 1.5 ? 1 : fraction < 3 ? 2 : fraction < 7 ? 5 : 10;
        return (int)Math.Min(1000, nice * power);
    }

    private static DateTime Floor(DateTime value, TimeInterval interval) {
        var step = interval.Step;
        switch (interval.Unit) {
            case TimeUnit.Second: return new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second / step * step);
            case TimeUnit.Minute: return new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute / step * step, 0);
            case TimeUnit.Hour: return new DateTime(value.Year, value.Month, value.Day, value.Hour / step * step, 0, 0);
            case TimeUnit.Day: return new DateTime(value.Year, value.Month, (value.Day - 1) / step * step + 1);
            case TimeUnit.Week: return value.Date.AddDays(-(((int)value.DayOfWeek + 6) % 7));
            case TimeUnit.Month: return new DateTime(value.Year, (value.Month - 1) / step * step + 1, 1);
            default: return new DateTime(Math.Max(1, value.Year / step * step), 1, 1);
        }
    }

    private static DateTime Next(DateTime value, TimeInterval interval) {
        var step = interval.Step;
        switch (interval.Unit) {
            case TimeUnit.Second: return value.AddSeconds(step);
            case TimeUnit.Minute: return value.AddMinutes(step);
            case TimeUnit.Hour: return value.AddHours(step);
            case TimeUnit.Day:
                // Day steps restart on the first of each month so labels stay on predictable days (1, 3, 5, ...).
                var next = value.AddDays(step);
                return next.Month == value.Month ? next : new DateTime(next.Year, next.Month, 1);
            case TimeUnit.Week: return value.AddDays(7);
            case TimeUnit.Month: return value.AddMonths(step);
            default: return value.AddYears(step);
        }
    }

    private static long IntervalTicksUpperBound(TimeInterval interval) => (long)Math.Ceiling(interval.ApproximateDays * 1.1 * TimeSpan.TicksPerDay);

    private enum TimeUnit {
        Second,
        Minute,
        Hour,
        Day,
        Week,
        Month,
        Year
    }

    private readonly struct TimeInterval {
        public TimeInterval(TimeUnit unit, int step) {
            Unit = unit;
            Step = step;
        }

        public TimeUnit Unit { get; }

        public int Step { get; }

        public double ApproximateDays => Step * (Unit switch {
            TimeUnit.Second => DaysPerSecond,
            TimeUnit.Minute => 1.0 / 1440.0,
            TimeUnit.Hour => 1.0 / 24.0,
            TimeUnit.Day => 1.0,
            TimeUnit.Week => 7.0,
            TimeUnit.Month => 30.44,
            _ => 365.25
        });
    }
}

using System;
using System.Collections.Generic;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal static partial class ChartTimeScale {
    /// <summary>Returns the display-zone day boundary for an instant, including midnight transition handling.</summary>
    internal static double? DisplayDayStart(ChartAxis axis, double value) {
        if (ToDisplayTime(axis, value, roundToSeconds: false) is not { } local) return null;
        var instants = new List<DateTime>(2);
        ResolveInstants(local.Date, axis.TimeZone ?? TimeZoneInfo.Utc, new TimeInterval(TimeUnit.Day, 1), instants);
        return instants.Count == 0 ? null : instants[0].ToOADate();
    }

    /// <summary>Recognizes date captions through their actual formatter without interpreting authored format strings.</summary>
    internal static bool HasDayCaptionPrecision(ChartAxis axis, Func<double, string> format, double value) {
        if (DisplayDayStart(axis, value) is not { } start) return false;
        var text = format(start);
        // Six-hour boundaries alone alias minute, second and subsecond clock captions.
        // Inspect their finer units as well as the actual project instant before using a day boundary.
        return text == format(value) && text == format(start + 123d / 86400000) && text == format(start + 1d / 86400)
            && text == format(start + 1d / 1440) && text == format(start + .25) && text == format(start + .5) && text == format(start + .75);
    }

    /// <summary>Uses the project's first display day as one phase for every automatic day/week step.</summary>
    private static IReadOnlyList<double> GenerateAnchoredCalendarInterval(ChartAxis axis, double min, double max,
        TimeInterval interval, double anchor) {
        var zone = axis.TimeZone ?? TimeZoneInfo.Utc;
        var first = ToLocal(anchor, zone).Date;
        var last = ToLocal(max, zone);
        var step = interval.Unit == TimeUnit.Week ? interval.Step * 7 : interval.Step;
        var offset = (ToLocal(min, zone).Date - first).Days;
        first = first.AddDays(Math.Floor((double)offset / step) * step);
        var ticks = new List<double>(); var instants = new List<DateTime>(2);
        for (var (local, count) = (first, 0); local <= last && count < MaximumTicks; local = local.AddDays(step), count++) {
            ResolveInstants(local, zone, interval, instants);
            foreach (var instant in instants) {
                var value = instant.ToOADate();
                if (value >= min && value <= max) ticks.Add(value);
            }
            if (local.Ticks > DateTime.MaxValue.Ticks - step * TimeSpan.TicksPerDay) break;
        }
        return ticks;
    }
}

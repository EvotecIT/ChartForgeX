using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static partial class ChartTimeScale {
    internal static double BucketEnd(ChartAxis axis, double start, ChartTimeTickInterval interval) {
        if (axis.Scale != ChartScaleKind.Time) throw new InvalidOperationException("Time buckets require a time X axis.");
        try {
            var utc = start >= 0 ? ChartDateTime.FromOrderedOaDate(start)
                : DateTime.SpecifyKind(DateTime.FromOADate(start), DateTimeKind.Utc);
            var count = interval.Count;
            if (interval.Unit < ChartTimeTickUnit.Day) {
                var end = interval.Unit switch {
                    ChartTimeTickUnit.Millisecond => utc.AddMilliseconds(count),
                    ChartTimeTickUnit.Second => utc.AddSeconds(count),
                    ChartTimeTickUnit.Minute => utc.AddMinutes(count),
                    _ => utc.AddHours(count)
                };
                return RepresentableBucketEnd(start, utc, end);
            }
            var zone = axis.TimeZone ?? TimeZoneInfo.Utc;
            var local = DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(utc, zone), DateTimeKind.Unspecified);
            var boundary = interval.Unit == ChartTimeTickUnit.Year ? local.AddYears(count)
                : interval.Unit == ChartTimeTickUnit.Month ? local.AddMonths(count)
                : local.AddDays(interval.Unit == ChartTimeTickUnit.Week ? (double)count * 7 : count);
            var instants = new List<DateTime>(2);
            ResolveInstants(boundary, zone, new TimeInterval(TimeUnit.Day, 1), instants);
            if (instants.Count == 0) throw new InvalidOperationException("The time bucket has no representable end instant.");
            return RepresentableBucketEnd(start, utc, instants[0]);
        } catch (ArgumentException exception) {
            throw new InvalidOperationException("The time bucket end must be a representable calendar instant.", exception);
        }
    }

    private static double RepresentableBucketEnd(double start, DateTime utc, DateTime end) {
        var value = ChartDateTime.ToOADate(end);
        // Negative OLE dates decrease within a UTC day and jump at midnight. A
        // single numeric interval can represent only one continuous day branch.
        // Negative aliases for the epoch day also jump when encoded canonically.
        if (start < 0 && (end.Date != utc.Date || value >= 0))
            throw new InvalidOperationException("A time bucket with a negative OLE date must remain within one UTC day and cannot use an epoch-day alias.");
        return value;
    }
}

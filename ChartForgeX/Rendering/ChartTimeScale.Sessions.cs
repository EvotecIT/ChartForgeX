using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal static partial class ChartTimeScale {
    internal static IReadOnlyList<double> UniqueSessionTicks(ChartAxis axis, IReadOnlyList<double> ticks) {
        if (axis.TimeSessions == null) return ticks;
        var selected = new List<double>();
        foreach (var value in ticks.OrderBy(value => value)) {
            if (selected.Count > 0 && axis.TimeSessions.Forward(value) == axis.TimeSessions.Forward(selected[selected.Count - 1]))
                selected[selected.Count - 1] = value;
            else selected.Add(value);
        }
        return PreserveFormatting(ticks, selected);
    }
    private static double VisibleTimeSpan(ChartAxis axis, double minimum, double maximum) => axis.TimeSessions is { } domain
        ? domain.Forward(maximum) - domain.Forward(minimum) : maximum - minimum;

    private static IReadOnlyList<double> GenerateInterval(ChartAxis axis, double minimum, double maximum, bool inside, TimeInterval interval) =>
        axis.TimeSessions == null ? GenerateContinuousInterval(axis, minimum, maximum, inside, interval)
            : GenerateSessionInterval(axis, minimum, maximum, interval);

    private static IReadOnlyList<double> GenerateSessionInterval(ChartAxis axis, double minimum, double maximum, TimeInterval interval) {
        var domain = axis.TimeSessions!;
        var ranges = SessionTickRanges(domain, minimum, maximum);
        var candidates = new List<double>();
        if (interval.Unit >= TimeUnit.Day) {
            // A calendar boundary may fall during a closure. Its next declared opening is
            // a real source instant and keeps the calendar cadence without inventing a mark.
            foreach (var value in GenerateContinuousInterval(axis, minimum, maximum, true, interval)) {
                var candidate = domain.IsClosed(value) ? domain.NextOpening(value) : value;
                if (candidate >= minimum && candidate <= maximum) candidates.Add(candidate);
            }
        } else {
            foreach (var range in ranges) {
                candidates.AddRange(GenerateContinuousInterval(axis, range.Minimum, range.Maximum, true, interval));
                if (candidates.Count >= MaximumTicks) break;
            }
        }
        if (candidates.Count == 0 && ranges.Count > 0) {
            candidates.Add(ranges[0].Minimum);
            if (ranges[ranges.Count - 1].Maximum > candidates[0]) candidates.Add(ranges[ranges.Count - 1].Maximum);
        }
        candidates.Sort();
        var selected = new List<double>();
        foreach (var value in candidates) {
            if (selected.Count > 0 && value == selected[selected.Count - 1]) continue;
            if (selected.Count > 0 && domain.Forward(value) == domain.Forward(selected[selected.Count - 1]))
                selected[selected.Count - 1] = value;
            else selected.Add(value);
        }
        var first = ToDisplayTime(axis, minimum); var last = ToDisplayTime(axis, maximum);
        var crossesDate = first.HasValue && last.HasValue && first.Value.Date != last.Value.Date;
        if (!crossesDate) return selected;
        var date = DayPattern(axis, first!.Value.Year != last!.Value.Year);
        var format = interval.Unit >= TimeUnit.Day ? date : date + " " + TimePattern(axis, interval.Unit == TimeUnit.Second);
        return new CompactDateTicks(selected, format);
    }

    private static List<(double Minimum, double Maximum)> SessionTickRanges(ChartTimeSessionDomain domain, double minimum, double maximum) {
        var ranges = new List<(double Minimum, double Maximum)>();
        var first = domain.Sessions[0]; var last = domain.Sessions[domain.Sessions.Count - 1];
        if (minimum < first.Minimum) ranges.Add((minimum, Math.Min(maximum, first.Minimum)));
        for (var index = Math.Max(0, domain.IndexAtOrBefore(minimum)); index < domain.Sessions.Count; index++) {
            var session = domain.Sessions[index];
            if (session.Minimum > maximum) break;
            var start = Math.Max(minimum, session.Minimum); var end = Math.Min(maximum, session.Maximum);
            if (end > start) ranges.Add((start, end));
        }
        if (maximum > last.Maximum) ranges.Add((Math.Max(minimum, last.Maximum), maximum));
        return ranges;
    }
}

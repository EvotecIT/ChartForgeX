using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal static partial class ChartTimeScale {
    /// <summary>Walks actual calendar intervals, measuring complete captions before choosing a coarser scale.</summary>
    internal static IReadOnlyList<double>? GenerateFitting(ChartAxis axis, double min, double max,
        Func<IReadOnlyList<double>, bool> fits, bool allowCompactDates, IReadOnlyList<double>? regularObservations = null, double minimumStepDays = 0,
        int automaticTickCount = 8, double? calendarAnchor = null, Func<IReadOnlyList<double>, IReadOnlyList<double>>? selectTicks = null) {
        if (min < 0 || !IsRepresentable(max) || max - min < DaysPerSecond) return null;
        var limit = axis.HasExplicitTickCount ? axis.TickCount : automaticTickCount;
        var interval = FittingStartInterval(axis, min, max, limit, minimumStepDays);
        IReadOnlyList<double>? sparse = null;
        IReadOnlyList<double>? compactFit = null;
        IReadOnlyList<double>? cadenceFit = null;
        if (regularObservations != null && FitObservationCadence(regularObservations, axis, min, max, limit, fits, false) is { Count: > 1 } cadenceTicks)
            return cadenceTicks;
        for (var attempt = 0; attempt < 64; attempt++) {
            if (regularObservations != null && interval.Unit > TimeUnit.Week) break;
            var ticks = regularObservations == null ? TryGenerateFittingInterval(axis, min, max, interval, calendarAnchor)
                : ObservationInterval(regularObservations, min, max, interval);
            if (ticks == null) {
                if (regularObservations == null) return null;
                interval = ChooseInterval(interval.ApproximateDays * 1.01); continue;
            }
            if (axis.TimeSessions != null && ticks.Count >= MaximumTicks) {
                interval = ChooseInterval(interval.ApproximateDays * 1.01);
                continue;
            }
            var intervalTicks = ticks;
            if (selectTicks != null) ticks = selectTicks(ticks);
            var tryFiveDays = false;
            if (ticks.Count > 0) {
                var ordinaryInterval = axis.HasExplicitTickCount || interval.ApproximateDays + 1e-10 >= Math.Max(minimumStepDays, VisibleTimeSpan(axis, min, max) / (limit - 1));
                var completeFits = ordinaryInterval && ticks.Count <= limit && fits(ticks);
                // Keep the broad weekly rhythm. A measured four-day failure may
                // still fit five-day captions before jumping straight to a week.
                tryFiveDays = regularObservations == null && interval.Unit == TimeUnit.Day && interval.Step == 4
                    && ordinaryInterval && ticks.Count > 1 && ticks.Count <= limit && !completeFits;
                if (completeFits) {
                    // A sparse coarse ladder step need not replace a readable
                    // stride of the preceding real calendar boundaries.
                    if (ticks.Count > 1) return !axis.HasExplicitTickCount && ticks.Count < 4 && cadenceFit is { Count: >= 4 }
                        ? cadenceFit : ticks;
                    sparse = ticks;
                }
                if (compactFit == null && ordinaryInterval && ticks.Count <= limit && ticks.Count > 1 && allowCompactDates && interval.Unit >= TimeUnit.Day
                    && TryCompactDates(ticks, axis, min, max) is { } compact && fits(compact)) compactFit = compact;
                // A periodic formatter can alias every coarser calendar boundary
                // after its last useful interval becomes too crowded. Preserve a
                // measured regular stride of real boundaries before accepting one caption.
                if (cadenceFit == null && intervalTicks.Count > 2)
                    cadenceFit = FitCalendarStride(intervalTicks, limit, fits, selectTicks);
                // A coarse authored formatter may not distinguish this interval.
                // Preserve a real accepted caption, never the rejected sequence.
                if (sparse == null) {
                    var first = PreserveFormatting(ticks, new[] { ticks[0] });
                    if (fits(first)) sparse = first;
                }
            }
            // No boundary at this interval means coarser steps cannot restore a useful
            // sequence. Keep the last real calendar boundary; never invent padded instants.
            if (ticks.Count == 0) break;
            interval = tryFiveDays ? new TimeInterval(TimeUnit.Day, 5) : ChooseInterval(interval.ApproximateDays * 1.01);
        }
        if (regularObservations != null)
            return FitObservationCadence(regularObservations, axis, min, max, limit, fits, allowCompactDates)
                ?? GenerateFitting(axis, min, max, fits, allowCompactDates, minimumStepDays: minimumStepDays,
                    automaticTickCount: automaticTickCount, calendarAnchor: calendarAnchor, selectTicks: selectTicks);
        return cadenceFit ?? compactFit ?? sparse;
    }

    /// <summary>Retains a measured stride of real calendar boundaries when all ordinary coarser choices lose caption precision.</summary>
    private static IReadOnlyList<double>? FitCalendarStride(IReadOnlyList<double> ticks, int limit,
        Func<IReadOnlyList<double>, bool> fits, Func<IReadOnlyList<double>, IReadOnlyList<double>>? selectTicks) {
        for (var stride = 2; stride < ticks.Count; stride++) {
            var values = new List<double>();
            for (var index = 0; index < ticks.Count; index += stride) values.Add(ticks[index]);
            var retained = PreserveFormatting(ticks, values);
            IReadOnlyList<double> selected = selectTicks != null ? selectTicks(retained) : retained;
            if (selected.Count > 1 && selected.Count <= limit && fits(selected)) return selected;
        }
        return null;
    }

    /// <summary>Retains the broad calendar rhythm ceiling while measured spacing chooses the compact pitch.</summary>
    internal static int AutomaticTickCount(ChartAxis axis) => axis.LabelDensity == ChartLabelDensity.Dense ? 100 : 8;

    /// <summary>Leaves room between automatic calendar captions rather than merely avoiding overlap.</summary>
    internal static double ComfortableLabelGap(double firstExtent, double secondExtent, double ordinaryGap, double length, double fontSize,
        bool compactDefault) {
        var mean = (firstExtent + secondExtent) / 2;
        // Compact schedules have short captions and a useful six-tick rhythm.
        // Measure their actual pitch rather than rounding width to a count.
        // Ordinary plots retain a generous gap; short plots may use the existing
        // collision gap when six complete captions already occupy the space.
        var preferredPitch = Math.Min(80, length / 5);
        var gap = Math.Min(mean, preferredPitch - mean);
        // Longer captions normally retain 24 px of clear space. A compact
        // uniform date sequence may use one em, while a broad date beside a
        // shorter clock caption keeps the mixed-caption breathing room.
        var longCaption = Math.Max(firstExtent, secondExtent) >= fontSize * 4;
        var uniformLong = Math.Min(firstExtent, secondExtent) >= fontSize * 4;
        if (longCaption) gap = Math.Max(compactDefault && length < 400 && uniformLong ? Math.Min(24, fontSize) : 24, gap);
        else if (compactDefault && length < 400) ordinaryGap /= 2;
        return Math.Max(ordinaryGap, gap);
    }

    // A calendar phase can put just one boundary inside a nominal two-tick
    // window. Try a finer actual calendar interval before accepting that loss.
    private static TimeInterval FittingStartInterval(ChartAxis axis, double min, double max, int limit, double minimumStepDays) {
        var interval = ChooseInterval(Math.Max(minimumStepDays, VisibleTimeSpan(axis, min, max) / (limit - 1)));
        if (axis.HasExplicitTickCount && TryGenerateFittingInterval(axis, min, max, interval) is { Count: >= 2 }) return interval;
        // Inspect the preceding real step as well. Its measured stride can
        // preserve clock precision when ordinary day boundaries all read 00:00.
        for (var index = Intervals.Length - 1; index >= 0; index--) {
            var finer = Intervals[index];
            if (finer.ApproximateDays >= interval.ApproximateDays || finer.ApproximateDays < minimumStepDays) continue;
            if (!axis.HasExplicitTickCount) return finer;
            var ticks = TryGenerateFittingInterval(axis, min, max, finer);
            if (ticks == null) continue;
            if (ticks.Count > limit) break;
            if (ticks.Count >= 2) return finer;
        }
        return interval;
    }

    // Calendar arithmetic may be outside DateTime's supported range. Authored
    // fitting/formatting callbacks run outside these catches and retain their errors.
    private static IReadOnlyList<double>? TryGenerateFittingInterval(ChartAxis axis, double min, double max, TimeInterval interval, double? calendarAnchor = null) {
        try { return calendarAnchor.HasValue && interval.Unit is TimeUnit.Day or TimeUnit.Week
            ? GenerateAnchoredCalendarInterval(axis, min, max, interval, calendarAnchor.Value)
            : GenerateInterval(axis, min, max, true, interval); }
        catch (Exception exception) when (exception is ArgumentException || exception is OverflowException) { return null; }
    }

    private static CompactDateTicks? TryCompactDates(IReadOnlyList<double> ticks, ChartAxis axis, double min, double max) {
        try { return new CompactDateTicks(ticks, CompactDateFormat(axis, min, max)); }
        catch (Exception exception) when (exception is ArgumentException || exception is OverflowException) { return null; }
    }

    /// <summary>Rejects a finer automatic time step when its formatter cannot distinguish neighbouring instants.</summary>
    internal static bool HasDistinctAdjacentLabels(IEnumerable<string> labels) {
        string? previous = null;
        foreach (var text in labels) {
            if (text.Length == 0) continue;
            if (string.Equals(previous, text, StringComparison.Ordinal)) return false;
            previous = text;
        }
        return true;
    }

    // A three-day cadence has no coarser multiple in the ordinary day/week
    // ladder. Fit a complete regular source stride before painting either grid
    // or captions; returning its failed dense set would defer thinning to paint.
    private static IReadOnlyList<double>? FitObservationCadence(IReadOnlyList<double> observations, ChartAxis axis, double min, double max,
        int limit, Func<IReadOnlyList<double>, bool> fits, bool allowCompactDates) {
        var visible = new List<double>();
        foreach (var value in observations) if (value >= min && value <= max) visible.Add(value);
        if (visible.Count == 0) return null;
        var cadence = observations[1] - observations[0];
        var previousStride = 0;
        IReadOnlyList<double>? compactFit = null; IReadOnlyList<double>? single = null;
        for (var count = Math.Min(limit, visible.Count); count >= 1; count--) {
            var stride = count == 1 ? visible.Count : (int)Math.Ceiling((visible.Count - 1d) / (count - 1));
            if (stride == previousStride) continue;
            previousStride = stride;
            var ticks = new List<double>();
            for (var index = 0; index < visible.Count; index += stride) ticks.Add(visible[index]);
            if (fits(ticks)) { if (ticks.Count > 1) return ticks; single = ticks; }
            if (compactFit == null && ticks.Count > 1 && allowCompactDates && cadence * stride >= 1
                && TryCompactDates(ticks, axis, min, max) is { } compact && fits(compact)) compactFit = compact;
        }
        return compactFit ?? single;
    }

    // Regular time bars have a source cadence. Retain calendar-sized steps which
    // are exact multiples of that cadence, with their phase anchored to the bars.
    // Irregular series continue to use ordinary calendar boundaries.
    private static IReadOnlyList<double>? ObservationInterval(IReadOnlyList<double> observations, double min, double max, TimeInterval interval) {
        if (interval.Unit > TimeUnit.Week) return null;
        var ratio = interval.ApproximateDays / (observations[1] - observations[0]);
        var stride = Math.Round(ratio);
        if (stride < 1 || stride > int.MaxValue || Math.Abs(ratio - stride) > 1e-6) return null;
        var ticks = new List<double>();
        for (var index = 0; index < observations.Count; index += (int)stride) {
            var value = observations[index];
            if (value >= min && value <= max) ticks.Add(value);
        }
        return ticks;
    }

    // Only pathological, multi-millennial count budgets need this last resort.
    // The returned ends are unit boundaries in the display zone, never padded
    // domain instants that happen to carry arbitrary minutes or seconds.
    private static IReadOnlyList<double> CalendarEndpoints(ChartAxis axis, double min, double max, TimeInterval interval) {
        var zone = axis.TimeZone ?? TimeZoneInfo.Utc;
        var unit = new TimeInterval(interval.Unit, 1);
        var first = Floor(ToLocal(min, zone), unit);
        var last = Floor(ToLocal(max, zone), unit);
        var instants = new List<DateTime>(2);
        ResolveInstants(first, zone, unit, instants);
        if (instants.Count == 0 || instants[instants.Count - 1].ToOADate() < min) first = Next(first, unit);
        var result = new List<double>(2);
        foreach (var boundary in new[] { first, last }) {
            ResolveInstants(boundary, zone, unit, instants);
            foreach (var instant in instants) {
                var value = instant.ToOADate();
                if (value >= min && value <= max && (result.Count == 0 || value > result[result.Count - 1])) result.Add(value);
            }
        }
        return result;
    }

    private static string CompactDateFormat(ChartAxis axis, double min, double max) {
        var first = ToDisplayTime(axis, min);
        var last = ToDisplayTime(axis, max);
        return first?.Year == last?.Year ? "MM-dd" : "yyyy-MM-dd";
    }

    internal static string FormatTick(ChartAxis axis, double value, IReadOnlyList<double>? ticks, bool retainDateContext = false) {
        var local = ToDisplayTime(axis, value);
        if (ticks is CompactDateTicks compact && local.HasValue) return local.Value.ToString(compact.Format, CultureInfo.InvariantCulture);
        var text = Format(axis, value);
        if (!retainDateContext || !local.HasValue || local.Value.TimeOfDay == TimeSpan.Zero || ticks == null) return text;
        // A detached observation cannot borrow the calendar context of neighbouring axis
        // captions. Keep its display-zone date when that axis includes calendar days.
        foreach (var tick in ticks) {
            var tickLocal = ToDisplayTime(axis, tick);
            if (tickLocal.HasValue && (tickLocal.Value.TimeOfDay == TimeSpan.Zero || tickLocal.Value.Date != local.Value.Date))
                return local.Value.ToString(local.Value.Second == 0 ? "yyyy-MM-dd HH:mm" : "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }
        return text;
    }

    internal static IReadOnlyList<double> PreserveFormatting(IReadOnlyList<double> source, IReadOnlyList<double> selected) =>
        source is CompactDateTicks compact ? new CompactDateTicks(selected, compact.Format) : selected;

    internal static bool SameFormatting(IReadOnlyList<double> first, IReadOnlyList<double> second) =>
        (first as CompactDateTicks)?.Format == (second as CompactDateTicks)?.Format;

    private sealed class CompactDateTicks : List<double> {
        internal CompactDateTicks(IEnumerable<double> ticks, string format) : base(ticks) => Format = format;
        internal string Format { get; }
    }
}

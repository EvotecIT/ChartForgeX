using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal static partial class ChartTimeScale {
    /// <summary>Walks actual calendar intervals, measuring complete captions before choosing a coarser scale.</summary>
    internal static IReadOnlyList<double>? GenerateFitting(ChartAxis axis, double min, double max,
        Func<IReadOnlyList<double>, bool> fits, bool allowCompactDates, IReadOnlyList<double>? regularObservations = null, double minimumStepDays = 0) {
        if (min < 0 || !IsRepresentable(max) || max - min < DaysPerSecond) return null;
        var limit = axis.HasExplicitTickCount ? axis.TickCount : 100;
        // Alignment can leave room for another nominal interval at both ends.
        // Begin one density level finer, then enforce the actual authored count.
        var interval = ChooseInterval(Math.Max(minimumStepDays, (max - min) / (limit * 2)));
        IReadOnlyList<double>? sparse = null;
        for (var attempt = 0; attempt < 64; attempt++) {
            if (regularObservations != null && interval.Unit > TimeUnit.Week) break;
            var ticks = regularObservations == null ? TryGenerateFittingInterval(axis, min, max, interval)
                : ObservationInterval(regularObservations, min, max, interval);
            if (ticks == null) {
                if (regularObservations == null) return null;
                interval = ChooseInterval(interval.ApproximateDays * 1.01); continue;
            }
            if (ticks.Count > 0 && ticks.Count <= limit) {
                if (fits(ticks)) return ticks;
                if (allowCompactDates && interval.Unit >= TimeUnit.Day && TryCompactDates(ticks, axis, min, max) is { } compact
                    && fits(compact)) return compact;
                sparse = ticks;
            }
            // No boundary at this interval means coarser steps cannot restore a useful
            // sequence. Keep the last real calendar boundary; never invent padded instants.
            if (ticks.Count == 0) break;
            interval = ChooseInterval(interval.ApproximateDays * 1.01);
        }
        if (regularObservations != null)
            return FitObservationCadence(regularObservations, axis, min, max, limit, fits, allowCompactDates)
                ?? GenerateFitting(axis, min, max, fits, allowCompactDates, minimumStepDays: minimumStepDays);
        return sparse ?? TryGenerateFittingInterval(axis, min, max, ChooseInterval(max - min));
    }

    // Calendar arithmetic may be outside DateTime's supported range. Authored
    // fitting/formatting callbacks run outside these catches and retain their errors.
    private static IReadOnlyList<double>? TryGenerateFittingInterval(ChartAxis axis, double min, double max, TimeInterval interval) {
        try { return GenerateInterval(axis, min, max, true, interval); }
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
        for (var count = Math.Min(limit, visible.Count); count >= 1; count--) {
            var stride = count == 1 ? visible.Count : (int)Math.Ceiling((visible.Count - 1d) / (count - 1));
            if (stride == previousStride) continue;
            previousStride = stride;
            var ticks = new List<double>();
            for (var index = 0; index < visible.Count; index += stride) ticks.Add(visible[index]);
            if (fits(ticks)) return ticks;
            if (allowCompactDates && cadence * stride >= 1 && TryCompactDates(ticks, axis, min, max) is { } compact
                && fits(compact)) return compact;
        }
        return null;
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
        return first?.Year == last?.Year ? "MMM d" : "MMM d, yyyy";
    }

    internal static string FormatTick(ChartAxis axis, double value, IReadOnlyList<double>? ticks) =>
        ticks is CompactDateTicks compact && ToDisplayTime(axis, value) is { } local
            ? local.ToString(compact.Format, CultureInfo.InvariantCulture) : Format(axis, value);

    internal static IReadOnlyList<double> PreserveFormatting(IReadOnlyList<double> source, IReadOnlyList<double> selected) =>
        source is CompactDateTicks compact ? new CompactDateTicks(selected, compact.Format) : selected;

    internal static bool SameFormatting(IReadOnlyList<double> first, IReadOnlyList<double> second) =>
        (first as CompactDateTicks)?.Format == (second as CompactDateTicks)?.Format;

    private sealed class CompactDateTicks : List<double> {
        internal CompactDateTicks(IEnumerable<double> ticks, string format) : base(ticks) => Format = format;
        internal string Format { get; }
    }
}

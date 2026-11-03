using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal static class ChartNumericFormatter {
    /// <summary>Formats displayed values with grouping below 10,000; an explicit chart formatter owns culture and units.</summary>
    public static string FormatValue(ChartOptions options, double value) =>
        (options ?? throw new ArgumentNullException(nameof(options))).ValueFormat.Format(value);

    /// <summary>
    /// Picks one compact unit for automatic captions, with optional shared precision for count-like stages.
    /// Returns null when an authored format owns the captions or no refinement is needed.
    /// </summary>
    internal static Func<double, string>? SeriesCaptionFormat(ChartOptions options, IEnumerable<double> values, bool sharePrecision = false) {
        var format = (options ?? throw new ArgumentNullException(nameof(options))).ValueFormat;
        return SeriesCaptionFormat(format, values, sharePrecision);
    }
    private static Func<double, string>? SeriesCaptionFormat(ChartValueFormat format, IEnumerable<double> values, bool sharePrecision = false) {
        if (!ReferenceEquals(format, ChartValueFormat.ExistingValue)) return null;
        var finite = new List<double>();
        foreach (var value in values) if (!double.IsNaN(value) && !double.IsInfinity(value)) finite.Add(value);
        var largest = 0d;
        foreach (var value in finite) largest = Math.Max(largest, Math.Abs(value));
        if (sharePrecision) {
            if (finite.Count < 2 || largest == 0) return null;
            // Three significant places at the chart's largest observation give counts one
            // common precision. Preserve small positive observations instead of rounding them to zero.
            var decimals = Math.Max(0, Math.Min(6, 2 - (int)Math.Floor(Math.Log10(largest))));
            while (decimals < 6 && finite.Exists(value => value != 0 && Math.Round(value, decimals) == 0)) decimals++;
            var precision = ChartValueFormat.ForTickStep(Math.Pow(10, -decimals), 0);
            return value => value != 0 && Math.Round(value, decimals) == 0 ? format.Format(value) : precision.Format(value);
        }
        if (largest < 10000) return null;
        var (divisor, suffix) = largest >= 1e9 ? (1e9, "B") : largest >= 1e6 ? (1e6, "M") : (1e3, "k");
        return value => {
            var scaled = Math.Round(value / divisor, 1);
            if (Math.Abs(value) < divisor || Math.Abs(scaled) >= 10000) return format.Format(value);
            return scaled.ToString("0.#", format.Culture) + suffix;
        };
    }

    /// <summary>Formats human-facing coordinates with a short round-trip caption; raw metadata retains its own serialization.</summary>
    internal static string FormatCoordinate(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    public static string FormatCompact(double value) {
        return ChartValueFormat.InvariantCompact.Format(value);
    }

    /// <summary>Preserves authored value formats and refines ambiguous default scale captions.</summary>
    internal static string[] FormatScaleValues(ChartOptions options, IReadOnlyList<double> values) => FormatScaleValues(options.ValueFormat, values);

    /// <summary>Formats a scale with one captured policy even when a callback changes its originating chart.</summary>
    internal static string[] FormatScaleValues(ChartValueFormat format, IReadOnlyList<double> values, IReadOnlyList<double>? ticks = null) {
        var captions = new string[values.Count];
        var shared = SeriesCaptionFormat(format, values);
        for (var index = 0; index < values.Count; index++) captions[index] = shared?.Invoke(values[index]) ?? format.Format(values[index]);
        if (ReferenceEquals(format, ChartValueFormat.ExistingValue)) {
            RefineDefaults(values, captions, format.Format(0));
            if (ticks != null && ticks.Count > 0) {
                var tickCaptions = FormatCompactScaleValues(ticks);
                var exactTicks = new Dictionary<double, string>();
                for (var index = 0; index < ticks.Count; index++) exactTicks[ticks[index]] = tickCaptions[index];
                for (var index = 0; index < values.Count; index++) if (exactTicks.TryGetValue(values[index], out var text)) captions[index] = text;
            }
        }
        return captions;
    }

    internal static string[] FormatCompactScaleValues(IReadOnlyList<double> values) {
        var captions = new string[values.Count];
        for (var index = 0; index < values.Count; index++) captions[index] = FormatCompact(values[index]);
        // A tick must name its own value: 1,250 written as "1.3k" labels a gridline that isn't 1,300 on a 250 step.
        // A caption may round within 1% of the tick step (a logarithmic 31.62 still reads "31.6"); a coarser one
        // takes the same plain-number fallback that ambiguous default scales use.
        var zero = FormatCompact(0);
        var tolerance = TickStep(values) * .01;
        if (!AllWithin(values, captions, tolerance)) {
            var stepFormat = ChartValueFormat.ForTickStep(TickStep(values), values.Count > 0 ? values[0] : 0);
            for (var index = 0; index < values.Count; index++) captions[index] = values[index] == 0 ? zero
                : Math.Abs(values[index]) < .001 ? ChartValueFormat.FormatSignificant(values[index]) : stepFormat.Format(values[index]);
            if (!AllWithin(values, captions, tolerance))
                for (var index = 0; index < values.Count; index++) captions[index] = values[index] == 0 ? zero : ChartValueFormat.FormatSignificant(values[index]);
            if (!AllWithin(values, captions, tolerance))
                for (var index = 0; index < values.Count; index++) captions[index] = values[index] == 0 ? zero : ChartValueFormat.ExactNumber(values[index]);
        }
        RefineDefaults(values, captions, zero);
        return captions;
    }

    // The smallest gap between distinct finite ticks; zero for a single tick, which then has to read back exactly.
    private static double TickStep(IReadOnlyList<double> values) {
        var finite = new List<double>();
        foreach (var value in values) if (!double.IsNaN(value) && !double.IsInfinity(value)) finite.Add(value);
        finite.Sort();
        var step = double.PositiveInfinity;
        for (var index = 1; index < finite.Count; index++) if (finite[index] > finite[index - 1]) step = Math.Min(step, finite[index] - finite[index - 1]);
        return double.IsInfinity(step) || double.IsNaN(step) ? 0 : step;
    }

    private static bool AllWithin(IReadOnlyList<double> values, string[] captions, double tolerance) {
        for (var index = 0; index < values.Count; index++) if (!ReadsBackAs(captions[index], values[index], tolerance)) return false;
        return true;
    }

    // Reads a default caption back as a number, k/M/B suffix included, in exact decimal arithmetic.
    private static bool ReadsBackAs(string caption, double value, double tolerance) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value == 0) return true;
        var unit = 1m; var number = caption;
        if (caption.EndsWith("k", StringComparison.Ordinal)) { unit = 1e3m; number = caption.Substring(0, caption.Length - 1); }
        else if (caption.EndsWith("M", StringComparison.Ordinal)) { unit = 1e6m; number = caption.Substring(0, caption.Length - 1); }
        else if (caption.EndsWith("B", StringComparison.Ordinal)) { unit = 1e9m; number = caption.Substring(0, caption.Length - 1); }
        // The round-trip text keeps every digit of the tick; a direct decimal conversion would round it to 15 digits.
        if (!decimal.TryParse(value.ToString("R", CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out var exact)
            || !decimal.TryParse(number, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var shown)) {
            // Beyond decimal's range, compare the doubles themselves.
            return double.TryParse(number, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var approximate)
                && Math.Abs(approximate * (double)unit - value) <= tolerance;
        }
        try { return Math.Abs(shown * unit - exact) <= (decimal)tolerance; }
        catch (OverflowException) { return false; }
    }
    // A lone value has no neighbouring tick to disambiguate, so it uses the bounded default precision
    // instead of the exact-digit fallback a scale needs when its own captions collide.
    internal static string FormatCompactAxisValue(double value) => ChartValueFormat.ExistingValue.Format(value);

    internal static string RefineCompactAxisValue(double value, string caption, string zero) =>
        value == 0 ? zero : NeedsPrecision(value, caption, zero) ? ChartValueFormat.FormatSignificant(value) : caption;

    // Caption comparison is linear in the number of values, including large numeric matrix columns.
    internal static void RefineDefaults(IReadOnlyList<double> values, string[] captions, string zero) {
        for (var index = 0; index < values.Count; index++) if (values[index] == 0) captions[index] = zero;
        if (!Ambiguous(values, captions, zero)) return;
        for (var index = 0; index < values.Count; index++) captions[index] = values[index] == 0 ? zero : ChartValueFormat.ExactNumber(values[index]);
    }

    private static bool Ambiguous(IReadOnlyList<double> values, string[] captions, string zero) {
        var seen = new Dictionary<string, double>(StringComparer.Ordinal);
        for (var index = 0; index < values.Count; index++) {
            if (NeedsPrecision(values[index], captions[index], zero)) return true;
            if (seen.TryGetValue(captions[index], out var value) && value != values[index]) return true;
            seen[captions[index]] = values[index];
        }
        return false;
    }

    private static bool IsZeroCaption(string caption, string zero) => caption == zero || caption == "-" + zero;

    private static bool NeedsPrecision(double value, string caption, string zero) => caption.Length > 24
        || value != 0 && IsZeroCaption(caption, zero);
}

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
    /// Picks one compact unit for a series' automatic value captions, so a series doesn't mix "9,800" with "12.5k".
    /// Returns null when the chart has an authored format or no caption would be compact.
    /// </summary>
    internal static Func<double, string>? SeriesCaptionFormat(ChartOptions options, IEnumerable<double> values) {
        var format = (options ?? throw new ArgumentNullException(nameof(options))).ValueFormat;
        if (!ReferenceEquals(format, ChartValueFormat.ExistingValue)) return null;
        var finite = new List<double>();
        foreach (var value in values) if (!double.IsNaN(value) && !double.IsInfinity(value)) finite.Add(value);
        var largest = 0d;
        foreach (var value in finite) largest = Math.Max(largest, Math.Abs(value));
        if (largest < 10000) return null;
        var (divisor, suffix) = largest >= 1e9 ? (1e9, "B") : largest >= 1e6 ? (1e6, "M") : (1e3, "k");
        // A value below one unit would round too far (149 as "0.1k", 999 as "1.0k"); it keeps its own caption
        // and doesn't decide whether the compact ones need a decimal.
        var decimals = false;
        foreach (var value in finite) if (Math.Abs(value) >= divisor && Math.Round(value / divisor, 1) != Math.Round(value / divisor)) decimals = true;
        var pattern = decimals ? "0.0" : "0";
        return value => {
            var scaled = Math.Round(value / divisor, 1);
            if (Math.Abs(value) < divisor || Math.Abs(scaled) >= 10000) return format.Format(value);
            return scaled.ToString(pattern, CultureInfo.InvariantCulture) + suffix;
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
    internal static string[] FormatScaleValues(ChartValueFormat format, IReadOnlyList<double> values) {
        var captions = new string[values.Count];
        for (var index = 0; index < values.Count; index++) captions[index] = format.Format(values[index]);
        if (ReferenceEquals(format, ChartValueFormat.ExistingValue)) RefineDefaults(values, captions, format.Format(0));
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
            for (var index = 0; index < values.Count; index++) captions[index] = values[index] == 0 ? zero : ChartValueFormat.FormatSignificant(values[index]);
            if (!AllWithin(values, captions, tolerance))
                for (var index = 0; index < values.Count; index++) captions[index] = values[index] == 0 ? zero : values[index].ToString("R", CultureInfo.InvariantCulture);
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
            || !decimal.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out var shown)) {
            // Beyond decimal's range, compare the doubles themselves.
            return double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out var approximate)
                && Math.Abs(approximate * (double)unit - value) <= tolerance;
        }
        try { return Math.Abs(shown * unit - exact) <= (decimal)tolerance; }
        catch (OverflowException) { return false; }
    }
    internal static string FormatCompactAxisValue(double value) {
        var caption = FormatCompact(value); var zero = FormatCompact(0);
        return RefineCompactAxisValue(value, caption, zero);
    }

    internal static string RefineCompactAxisValue(double value, string caption, string zero) =>
        value == 0 ? zero : NeedsPrecision(value, caption, zero) ? ChartValueFormat.FormatSignificant(value) : caption;

    // Caption comparison is linear in the number of values, including large numeric matrix columns.
    internal static void RefineDefaults(IReadOnlyList<double> values, string[] captions, string zero) {
        for (var index = 0; index < values.Count; index++) if (values[index] == 0) captions[index] = zero;
        if (!Ambiguous(values, captions, zero)) return;
        for (var index = 0; index < values.Count; index++) captions[index] = values[index] == 0 ? zero : ChartValueFormat.FormatSignificant(values[index]);
        if (!Ambiguous(values, captions, "0")) return;
        for (var index = 0; index < values.Count; index++) captions[index] = values[index] == 0 ? zero : values[index].ToString("R", CultureInfo.InvariantCulture);
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

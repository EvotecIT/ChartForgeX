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
        var decimals = false;
        foreach (var value in finite) if (Math.Round(value / divisor, 1) != Math.Round(value / divisor)) decimals = true;
        var pattern = decimals ? "0.0" : "0";
        return value => {
            var scaled = Math.Round(value / divisor, 1);
            // A value far below the series' unit would read as "0.0k"; it keeps its own caption.
            if (scaled == 0 && value != 0 || Math.Abs(scaled) >= 10000) return format.Format(value);
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
        // A tick must name its own value: 1,250 written as "1.3k" labels a gridline that isn't 1,300.
        // Such a scale takes the same plain-number fallback that ambiguous default scales use.
        var zero = FormatCompact(0);
        if (!AllExact(values, captions)) {
            for (var index = 0; index < values.Count; index++) captions[index] = values[index] == 0 ? zero : ChartValueFormat.FormatSignificant(values[index]);
            if (!AllExact(values, captions))
                for (var index = 0; index < values.Count; index++) captions[index] = values[index] == 0 ? zero : values[index].ToString("R", CultureInfo.InvariantCulture);
        }
        RefineDefaults(values, captions, zero);
        return captions;
    }

    private static bool AllExact(IReadOnlyList<double> values, string[] captions) {
        for (var index = 0; index < values.Count; index++) if (!ReadsBackAs(captions[index], values[index])) return false;
        return true;
    }

    // Reads a default caption back as a number, k/M/B suffix included, and compares it exactly.
    private static bool ReadsBackAs(string caption, double value) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value == 0) return true;
        var unit = 1m; var number = caption;
        if (caption.EndsWith("k", StringComparison.Ordinal)) { unit = 1e3m; number = caption.Substring(0, caption.Length - 1); }
        else if (caption.EndsWith("M", StringComparison.Ordinal)) { unit = 1e6m; number = caption.Substring(0, caption.Length - 1); }
        else if (caption.EndsWith("B", StringComparison.Ordinal)) { unit = 1e9m; number = caption.Substring(0, caption.Length - 1); }
        // Beyond decimal's range a caption is a significant-digit approximation; ambiguity refinement owns those.
        if (Math.Abs(value) >= 7.9e27) return true;
        if (!decimal.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out var shown)) return false;
        try { return shown * unit == (decimal)value; } catch (OverflowException) { return true; }
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

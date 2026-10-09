using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal static class ChartNumericFormatter {
    /// <summary>Formats displayed values with grouping below 10,000; an explicit chart formatter owns culture and units.</summary>
    public static string FormatValue(ChartOptions options, double value) =>
        (options ?? throw new ArgumentNullException(nameof(options))).ValueFormat.Format(value);

    public static string FormatCompact(double value) {
        return ChartValueFormat.InvariantCompact.Format(value);
    }

    /// <summary>Preserves authored value formats and refines ambiguous default scale captions.</summary>
    internal static string[] FormatScaleValues(ChartOptions options, IReadOnlyList<double> values) {
        var captions = new string[values.Count];
        for (var index = 0; index < values.Count; index++) captions[index] = FormatValue(options, values[index]);
        if (ReferenceEquals(options.ValueFormat, ChartValueFormat.ExistingValue)) RefineDefaults(values, captions, FormatValue(options, 0));
        return captions;
    }

    internal static string[] FormatCompactScaleValues(IReadOnlyList<double> values) {
        var captions = new string[values.Count];
        for (var index = 0; index < values.Count; index++) captions[index] = FormatCompact(values[index]);
        RefineDefaults(values, captions, FormatCompact(0));
        return captions;
    }

    internal static string FormatCompactAxisValue(double value) {
        var caption = FormatCompact(value); var zero = FormatCompact(0);
        return value == 0 ? zero : NeedsPrecision(value, caption, zero) ? ChartValueFormat.FormatSignificant(value) : caption;
    }

    // Caption comparison is linear in the number of values, including large numeric matrix columns.
    private static void RefineDefaults(IReadOnlyList<double> values, string[] captions, string zero) {
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

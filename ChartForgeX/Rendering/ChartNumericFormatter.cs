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

    /// <summary>Keeps default scale captions distinct for tiny dimensions and readable for extreme magnitudes; authored formatters stay authoritative.</summary>
    internal static string[] FormatScaleValues(ChartOptions options, IReadOnlyList<double> values) {
        var captions = new string[values.Count];
        for (var index = 0; index < values.Count; index++) captions[index] = FormatValue(options, values[index]);
        if (!ReferenceEquals(options.ValueFormat, ChartValueFormat.ExistingValue)) return captions;
        var needsPrecision = false;
        for (var index = 0; index < values.Count; index++) {
            if (captions[index].Length > 24 || values[index] != 0 && captions[index] == FormatValue(options, 0)) needsPrecision = true;
            for (var other = 0; other < index; other++)
                if (values[index] != values[other] && captions[index] == captions[other]) needsPrecision = true;
        }
        if (!needsPrecision) return captions;
        for (var index = 0; index < values.Count; index++) captions[index] = values[index].ToString("G6", CultureInfo.InvariantCulture);
        for (var index = 0; index < values.Count; index++) for (var other = 0; other < index; other++)
            if (values[index] != values[other] && captions[index] == captions[other]) {
                for (var value = 0; value < values.Count; value++) captions[value] = values[value].ToString("R", CultureInfo.InvariantCulture);
                return captions;
            }
        return captions;
    }
}

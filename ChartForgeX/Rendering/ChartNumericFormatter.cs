using System;
using System.Globalization;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal static class ChartNumericFormatter {
    /// <summary>Formats displayed values with grouping below 10,000; an explicit chart formatter owns culture and units.</summary>
    public static string FormatValue(ChartOptions options, double value) => options.ValueFormatter != null
        ? options.ValueFormatter(value) ?? string.Empty
        : Math.Abs(value) < 10000 ? value.ToString("#,0.##", CultureInfo.InvariantCulture) : FormatCompact(value);

    public static string FormatCompact(double value) {
        var abs = Math.Abs(value);
        if (abs >= 1000000000) return (value / 1000000000).ToString("0.#", CultureInfo.InvariantCulture) + "B";
        if (abs >= 1000000) return (value / 1000000).ToString("0.#", CultureInfo.InvariantCulture) + "M";
        if (abs >= 1000) return (value / 1000).ToString("0.#", CultureInfo.InvariantCulture) + "k";
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}

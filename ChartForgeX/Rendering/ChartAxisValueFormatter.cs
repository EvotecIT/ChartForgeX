using System;
using System.Globalization;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>Formats renderer-neutral axis values using explicit labels, configured formatters, and scale-aware defaults.</summary>
internal static class ChartAxisValueFormatter {
    public static string Format(ChartAxis axis, double value, Func<double, string>? fallbackFormatter = null) {
        if (axis == null) throw new ArgumentNullException(nameof(axis));
        foreach (var label in axis.Labels) {
            if (Math.Abs(label.Value - value) < 0.000001) return label.Text;
        }

        var formatter = axis.LabelFormatter ?? fallbackFormatter;
        if (formatter != null) return formatter(value) ?? string.Empty;
        if (axis.Scale == ChartScaleKind.Time) return DateTime.FromOADate(value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return ChartNumericFormatter.FormatCompact(value);
    }
}

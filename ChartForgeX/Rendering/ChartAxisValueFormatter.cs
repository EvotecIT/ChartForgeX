using System;
using System.Collections.Generic;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>Formats renderer-neutral axis values using explicit labels, configured formatters, and scale-aware defaults.</summary>
internal static class ChartAxisValueFormatter {
    // Exact mappings take precedence over tolerance matches, especially for closely spaced time values.
    internal static string? FindExplicitLabel(IReadOnlyList<ChartAxisLabel> labels, double value) {
        foreach (var label in labels) if (label.Value == value) return label.Text;
        foreach (var label in labels) if (Math.Abs(label.Value - value) < 0.000001) return label.Text;
        return null;
    }

    public static string Format(ChartAxis axis, double value, Func<double, string>? fallbackFormatter = null) {
        if (axis == null) throw new ArgumentNullException(nameof(axis));
        if (FindExplicitLabel(axis.Labels, value) is { } label) return label;

        var formatter = axis.LabelFormatter ?? fallbackFormatter;
        if (formatter != null) return formatter(value) ?? string.Empty;
        if (axis.Scale == ChartScaleKind.Time) return ChartTimeScale.Format(axis, value);
        return ChartNumericFormatter.FormatCompact(value);
    }
}

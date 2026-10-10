using System;
using System.Collections.Generic;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>Formats renderer-neutral axis values using explicit labels, configured formatters, and scale-aware defaults.</summary>
internal static class ChartAxisValueFormatter {
    /// <summary>Retains one result per axis/value while a scene's labels, layout and semantic facts are prepared.</summary>
    internal sealed class Cache {
        private readonly Dictionary<ChartAxis, Dictionary<double, string>> _labels = new();
        internal void Set(ChartAxis axis, double value, string text) {
            if (!_labels.TryGetValue(axis, out var labels)) _labels.Add(axis, labels = new Dictionary<double, string>());
            labels[value] = text;
        }
        internal string Format(ChartAxis axis, double value, Func<double, string> formatter, bool retainResult = true) {
            if (!_labels.TryGetValue(axis, out var labels)) _labels.Add(axis, labels = new Dictionary<double, string>());
            if (labels.TryGetValue(value, out var text)) return text;
            text = formatter(value);
            if (retainResult) labels.Add(value, text);
            return text;
        }
    }

    // Exact mappings take precedence over tolerance matches, especially for closely spaced time values.
    internal static string? FindExplicitLabel(IReadOnlyList<ChartAxisLabel> labels, double value) {
        foreach (var label in labels) if (label.Value == value) return label.Text;
        foreach (var label in labels) if (Math.Abs(label.Value - value) < 0.000001) return label.Text;
        return null;
    }

    public static string Format(ChartAxis axis, double value, Func<double, string>? fallbackFormatter = null, IReadOnlyList<double>? ticks = null) {
        if (axis == null) throw new ArgumentNullException(nameof(axis));
        if (FindExplicitLabel(axis.Labels, value) is { } label) return label;

        var formatter = axis.LabelFormatter ?? fallbackFormatter;
        if (formatter != null) return formatter(value) ?? string.Empty;
        if (axis.Scale == ChartScaleKind.Time && ticks != null && ChartTicks.IsNumericTimeFallback(ticks)) return value.ToString("G17", System.Globalization.CultureInfo.InvariantCulture);
        if (axis.Scale == ChartScaleKind.Time) return ChartTimeScale.Format(axis, value);
        return ChartNumericFormatter.FormatCompactAxisValue(value);
    }

    /// <summary>Resolves one automatic caption set per preparation while keeping authored formatters lazy.</summary>
    internal static Func<double, string> Create(ChartAxis axis, IReadOnlyList<double> ticks,
        Func<double, string>? fallbackFormatter = null, ChartOptions? valueDefaults = null) {
        if (axis == null) throw new ArgumentNullException(nameof(axis));
        if (ticks == null) throw new ArgumentNullException(nameof(ticks));
        if (axis.LabelFormatter != null || fallbackFormatter != null || axis.Scale == ChartScaleKind.Time)
            return value => Format(axis, value, fallbackFormatter, ticks);
        // Schedule axes retain their existing value notation; other numeric axes use compact notation.
        var captions = valueDefaults == null ? ChartNumericFormatter.FormatCompactScaleValues(ticks)
            : ChartNumericFormatter.FormatScaleValues(valueDefaults, ticks);
        var defaults = new Dictionary<double, string>();
        for (var index = 0; index < ticks.Count; index++) defaults[ticks[index]] = captions[index];
        return value => FindExplicitLabel(axis.Labels, value) ?? (defaults.TryGetValue(value, out var text) ? text
            : valueDefaults == null ? ChartNumericFormatter.FormatCompactAxisValue(value)
            : ChartNumericFormatter.FormatScaleValues(valueDefaults, new[] { value })[0]);
    }
}

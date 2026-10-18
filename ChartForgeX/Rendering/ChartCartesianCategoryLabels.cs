using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>Resolves category captions once for native axes, observations, legends and adapter readouts.</summary>
internal static class ChartCartesianCategoryLabels {
    /// <summary>Captures complete category bindings while numeric tick aliases remain a continuous domain.</summary>
    internal static IReadOnlyList<ChartAxisLabel>? CaptureBindings(Chart chart, ChartAxis axis, Func<double, string> format) {
        if (axis.Scale != ChartScaleKind.Linear || chart.Series.Any(series => series.HistogramBinLayout != null || series.TimeBucketInterval != null)) return null;
        var coordinates = chart.Series.SelectMany(ChartCartesianOrientation.Categories).Distinct().OrderBy(value => value).ToArray();
        var categorical = chart.Series.Any(series => IsCategoricalKind(series.Kind));
        // A named line can use categories too, but a few labels at numeric positions
        // cannot name observations at all the other coordinates between those ticks.
        if (!categorical && (coordinates.Length == 0 || axis.Labels.Count == 0
            || coordinates.Any(value => ChartAxisValueFormatter.FindExplicitLabel(axis.Labels, value) == null))) return null;
        return Array.AsReadOnly(coordinates.Select(value => new ChartAxisLabel(value, format(value))).ToArray());
    }

    /// <summary>Identifies native category families without treating numeric tick aliases as categorical observations.</summary>
    internal static bool IsCategoricalKind(ChartSeriesKind kind) => kind is ChartSeriesKind.Bar or ChartSeriesKind.HorizontalBar
        or ChartSeriesKind.RangeBar or ChartSeriesKind.BoxPlot or ChartSeriesKind.Waterfall;

    internal static IReadOnlyDictionary<double, string> Resolve(Chart chart, IEnumerable<double> categories,
        ChartAxisValueFormatter.Cache cache, Func<double, string?>? defaultCaption = null) {
        var axis = ChartCartesianOrientation.CategoryAxis(chart);
        var labels = ChartCartesianOrientation.CategoryLabels(chart);
        var coordinates = categories.Distinct().OrderBy(value => value).ToArray();
        var formatter = ChartAxisValueFormatter.Create(axis, coordinates);
        var captions = new Dictionary<double, string>();
        foreach (var category in coordinates)
            captions[category] = cache.Format(axis, category, value => ChartAxisValueFormatter.FindExplicitLabel(labels, value)
                ?? (axis.LabelFormatter == null ? defaultCaption?.Invoke(value) : null) ?? formatter(value));
        return captions;
    }
}

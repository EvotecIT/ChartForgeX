using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>Resolves category captions once for native axes, observations, legends and adapter readouts.</summary>
internal static class ChartCartesianCategoryLabels {
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

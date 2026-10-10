using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>Resolves category captions once for axes, marks, legends and adapter readouts during preparation.</summary>
internal static class ChartWaterfallCategoryLabels {
    internal static IReadOnlyDictionary<double, string> Resolve(Chart chart, ChartAxisValueFormatter.Cache cache) {
        var axis = ChartCartesianOrientation.CategoryAxis(chart);
        var labels = ChartCartesianOrientation.CategoryLabels(chart);
        var steps = ChartWaterfallSteps.Create(chart.Series[0]);
        var coordinates = steps.Select(step => step.X).Distinct().OrderBy(value => value).ToArray();
        var formatter = ChartAxisValueFormatter.Create(axis, coordinates);
        var captions = new Dictionary<double, string>();
        foreach (var step in steps) {
            captions[step.X] = cache.Format(axis, step.X, value => ChartAxisValueFormatter.FindExplicitLabel(labels, value)
                ?? (axis.LabelFormatter == null ? step.Kind switch {
                    ChartWaterfallItemKind.OpeningBalance => chart.Options.Labels.OpeningBalance,
                    ChartWaterfallItemKind.Subtotal => chart.Options.Labels.Subtotal,
                    ChartWaterfallItemKind.Total => chart.Options.Labels.Total,
                    _ => formatter(value)
                } : formatter(value)));
        }
        return captions;
    }
}

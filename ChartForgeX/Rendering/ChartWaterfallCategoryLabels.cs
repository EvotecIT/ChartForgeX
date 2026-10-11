using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>Resolves category captions once for axes, marks, legends and adapter readouts during preparation.</summary>
internal static class ChartWaterfallCategoryLabels {
    internal static IReadOnlyDictionary<double, string> Resolve(Chart chart, ChartAxisValueFormatter.Cache cache) {
        var steps = ChartWaterfallSteps.Create(chart.Series[0]);
        var kinds = steps.GroupBy(step => step.X).ToDictionary(group => group.Key, group => group.First().Kind);
        return ChartCartesianCategoryLabels.Resolve(chart, steps.Select(step => step.X), cache, value => kinds[value] switch {
            ChartWaterfallItemKind.OpeningBalance => chart.Options.Labels.OpeningBalance,
            ChartWaterfallItemKind.Subtotal => chart.Options.Labels.Subtotal,
            ChartWaterfallItemKind.Total => chart.Options.Labels.Total,
            _ => null
        });
    }
}

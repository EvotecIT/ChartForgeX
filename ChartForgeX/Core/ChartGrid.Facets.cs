using System;
using System.Collections.Generic;
using ChartForgeX.Data;

namespace ChartForgeX.Core;

public sealed partial class ChartGrid {
    /// <summary>Builds a small-multiple grid from deterministic dataset groups.</summary>
    public static ChartGrid FromFacets<T, TKey>(
        ChartDataset<T> dataset,
        Func<T, TKey> facet,
        Func<TKey, ChartDataset<T>, Chart> chartFactory,
        int columns = 2,
        IEqualityComparer<TKey>? comparer = null) {
        if (dataset == null) throw new ArgumentNullException(nameof(dataset));
        if (facet == null) throw new ArgumentNullException(nameof(facet));
        if (chartFactory == null) throw new ArgumentNullException(nameof(chartFactory));

        var grid = Create().WithColumns(columns);
        foreach (var group in dataset.GroupBy(facet, comparer)) {
            var chart = chartFactory(group.Key, group.Dataset);
            if (chart == null) throw new InvalidOperationException("Facet chart factories must return a chart.");
            grid.Add(chart);
        }

        return grid;
    }
}

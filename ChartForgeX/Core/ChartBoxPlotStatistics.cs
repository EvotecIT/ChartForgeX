using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Core;

internal static class ChartBoxPlotStatistics {
    internal static ChartBoxPlot FromValues(double x, IEnumerable<double> values) {
        if (values == null) throw new ArgumentNullException(nameof(values));
        var sorted = values.ToArray();
        if (sorted.Length == 0) throw new ArgumentException("Box plot values must contain at least one value.", nameof(values));
        foreach (var value in sorted) ChartGuards.Finite(value, nameof(values));
        Array.Sort(sorted);
        return new ChartBoxPlot(x, sorted[0], Quantile(sorted, .25), Quantile(sorted, .5), Quantile(sorted, .75), sorted[sorted.Length - 1]);
    }

    private static double Quantile(IReadOnlyList<double> sorted, double probability) {
        if (sorted.Count == 1) return sorted[0];
        var position = (sorted.Count - 1) * probability;
        var lower = (int)Math.Floor(position); var upper = (int)Math.Ceiling(position);
        return sorted[lower] + (sorted[upper] - sorted[lower]) * (position - lower);
    }
}

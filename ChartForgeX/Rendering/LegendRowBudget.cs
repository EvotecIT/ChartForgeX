using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal static class LegendRowBudget {
    internal static List<T> Apply<T>(List<T> rows, Chart chart, double rowHeight, Func<T, int> count, Func<int, T> summary) {
        var height = chart.Options.Size.Height * chart.Options.LegendMaximumHeightFraction;
        var maximumRows = Math.Max(1, (int)Math.Floor(Math.Max(0, height - 36) / Math.Max(1, rowHeight)));
        if (chart.Options.LegendMaximumRows.HasValue) maximumRows = Math.Min(maximumRows, chart.Options.LegendMaximumRows.Value);
        if (rows.Count <= maximumRows) return rows;
        var omitted = 0;
        for (var i = maximumRows - 1; i < rows.Count; i++) omitted += count(rows[i]);
        rows.RemoveRange(maximumRows - 1, rows.Count - maximumRows + 1);
        rows.Add(summary(omitted));
        return rows;
    }

    internal static string Summary(int omitted) => "+ " + omitted.ToString(CultureInfo.InvariantCulture) + " more entries";
}

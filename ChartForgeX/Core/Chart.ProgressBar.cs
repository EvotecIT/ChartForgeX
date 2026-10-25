using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds retained labeled progress rows against a shared maximum unless an item declares its own domain.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="items">The labeled values to render.</param>
    /// <param name="maximum">The shared maximum value.</param>
    /// <param name="color">An optional base color. When null, the theme palette colors each row.</param>
    /// <returns>The current chart. The added series owns ProgressItems; update/refill through UpdateProgressItem or ReplaceProgressItems.</returns>
    /// <remarks>Shared-maximum item output is unchanged. Typed progress handles expose an empty Points collection; direct numeric ProgressBar construction remains a shared-domain workflow.</remarks>
    public Chart AddProgressBars(string name, IEnumerable<ChartProgressItem> items, double maximum = 100, ChartColor? color = null) {
        EnsureCanAddSeries();
        if (items == null) throw new ArgumentNullException(nameof(items));
        ChartGuards.Finite(maximum, nameof(maximum));
        if (maximum <= 0) throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "Progress maximum must be greater than zero.");
        var series = ChartSeries.CreateProgressBars(name, items, color);
        Options.ProgressMaximum = maximum;
        Options.XAxisLabels.Clear();
        for (var index = 0; index < series.ProgressItems.Count; index++)
            Options.XAxisLabels.Add(new ChartAxisLabel(index + 1, series.ProgressItems[index].Label));
        AddSeries(series);
        return this;
    }
}

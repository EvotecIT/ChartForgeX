using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {

    /// <summary>Adds a contribution-style calendar heatmap chart.</summary>
    /// <param name="name">The series name.</param>
    /// <param name="items">The dated values to render. Duplicate dates are summed.</param>
    /// <param name="color">An optional high-intensity cell color.</param>
    /// <param name="id">Optional stable identity assigned before attachment.</param>
    /// <returns>The current chart.</returns>
    /// <remarks>
    /// Week layout, localized weekday and month names, and cell sizing come from <see cref="ChartOptions.Calendar"/>.
    /// Scale words and the accessible summary come from <see cref="ChartOptions.Labels"/>.
    /// </remarks>
    public Chart AddCalendarHeatmap(string name, IEnumerable<ChartCalendarHeatmapItem> items, ChartColor? color = null, ChartSeriesId? id = null) {
        var series = id.HasValue ? ChartSeries.CreateCalendarHeatmap(id.Value, name, items, color) : ChartSeries.CreateCalendarHeatmap(name, items, color);
        return AddSeries(series);
    }
}

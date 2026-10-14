using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Configures the shared week layout, localized names, and cell sizing of calendar heatmaps.</summary>
    /// <param name="configure">The configuration callback. Changes are installed together when the callback succeeds.</param>
    /// <returns>The current chart.</returns>
    public Chart ConfigureCalendar(Action<ChartCalendarOptions> configure) {
        Options.Calendar = ChartOptionConfiguration.Configure(Options.Calendar, configure, options => options.Clone());
        return this;
    }

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
        if (series.ObservationCount == 0) throw new ArgumentException("Calendar heatmaps must contain at least one dated value.", nameof(items));
        return AddSeries(series);
    }
}

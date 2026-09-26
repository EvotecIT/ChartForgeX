using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds a Gantt lane: an entity, such as a service or domain controller, with its time-bounded items (for example
    /// incidents). Items are coloured through <see cref="ChartOptions.StateCategories"/>; overlapping items stack into
    /// sub-rows. Lanes render in the order they are added, and consecutive lanes with the same <paramref name="group"/>
    /// are listed under one group header. Set <see cref="ChartOptions.GanttToday"/> (a UTC instant) to draw a "Now" line and
    /// end open items there.
    /// </summary>
    /// <param name="name">The lane label.</param>
    /// <param name="items">The lane items. They are ordered by start, then end, then input order.</param>
    /// <param name="group">Optional group header, for example a site.</param>
    /// <param name="summary">Optional text for the summary column, for example <c>3 incidents</c>.</param>
    /// <returns>The current chart.</returns>
    public Chart AddGanttLane(string name, IEnumerable<ChartGanttLaneItem> items, string? group = null, string? summary = null) {
        if (name == null) throw new ArgumentNullException(nameof(name));
        if (items == null) throw new ArgumentNullException(nameof(items));
        var ordered = new List<ChartGanttLaneItem>();
        foreach (var item in items) {
            if (item.Category == null) throw new ArgumentException("Gantt lane items must be created with a category.", nameof(items));
            ordered.Add(item);
        }

        ordered = ordered.Select((item, index) => (Item: item, Index: index))
            .OrderBy(entry => entry.Item.Start).ThenBy(entry => entry.Item.End ?? double.MaxValue).ThenBy(entry => entry.Index)
            .Select(entry => entry.Item).ToList();
        var series = new ChartSeries(name, ChartSeriesKind.GanttLane, ordered.ConvertAll(item => new ChartPoint(item.Start, item.End ?? item.Start))) {
            ShowInLegend = false,
            LaneSummary = summary,
            LaneGroup = string.IsNullOrWhiteSpace(group) ? null : group
        };
        series.GanttLaneItems.AddRange(ordered);
        Series.Add(series);
        return this;
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds a contribution-style calendar heatmap chart.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="items">The dated values to render. Duplicate dates are summed.</param>
    /// <param name="color">An optional high-intensity cell color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddCalendarHeatmap(string name, IEnumerable<ChartCalendarHeatmapItem> items, ChartColor? color = null) {
        EnsureCanAddSeries();
        if (items == null) throw new ArgumentNullException(nameof(items));
        var byDate = new SortedDictionary<DateTime, CalendarAggregate>();
        foreach (var item in items) {
            if (!byDate.TryGetValue(item.Date, out var aggregate)) aggregate = new CalendarAggregate();
            aggregate.Value += item.Value;
            if (item.Color.HasValue) aggregate.Color = item.Color;
            byDate[item.Date] = aggregate;
        }

        if (byDate.Count == 0) throw new ArgumentException("Calendar heatmaps must contain at least one dated value.", nameof(items));
        var points = new List<ChartPoint>(byDate.Count);
        var labels = new List<ChartAxisLabel>(byDate.Count);
        var colors = new List<ChartColor?>(byDate.Count);
        foreach (var entry in byDate) {
            points.Add(new ChartPoint(entry.Key, entry.Value.Value));
            labels.Add(new ChartAxisLabel(entry.Key, entry.Key.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
            colors.Add(entry.Value.Color);
        }

        Options.XAxisLabels.Clear();
        Options.XAxisLabels.AddRange(labels);
        Add(name, ChartSeriesKind.CalendarHeatmap, points, color);
        Series[Series.Count - 1].PointColors.AddRange(colors);
        return this;
    }

    private struct CalendarAggregate {
        public double Value;
        public ChartColor? Color;
    }
}

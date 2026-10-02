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
    /// <param name="firstDayOfWeek">The weekday shown in the top row; weeks run from it.</param>
    /// <param name="dayNames">Optional localized weekday names: seven entries indexed by <see cref="DayOfWeek"/> (Sunday
    /// first). Null uses the invariant abbreviations <c>Sun</c> through <c>Sat</c>.</param>
    /// <param name="monthNames">Optional localized month names: twelve entries, January first. Null uses the invariant
    /// abbreviations <c>Jan</c> through <c>Dec</c>.</param>
    /// <returns>The current chart.</returns>
    /// <remarks>The scale words and the accessible summary come from <see cref="ChartOptions.Labels"/>
    /// (<see cref="ChartLabels.Less"/>, <see cref="ChartLabels.More"/>, <see cref="ChartLabels.NoData"/>,
    /// <see cref="ChartLabels.AccessibleTextFormatter"/>); cell sizing comes from <see cref="WithCalendarHeatmapCells"/>.</remarks>
    public Chart AddCalendarHeatmap(string name, IEnumerable<ChartCalendarHeatmapItem> items, ChartColor? color = null, DayOfWeek firstDayOfWeek = DayOfWeek.Sunday, IReadOnlyList<string>? dayNames = null, IReadOnlyList<string>? monthNames = null) {
        EnsureCanAddSeries();
        if (items == null) throw new ArgumentNullException(nameof(items));
        if (!Enum.IsDefined(typeof(DayOfWeek), firstDayOfWeek)) throw new ArgumentOutOfRangeException(nameof(firstDayOfWeek), firstDayOfWeek, "Unknown weekday.");
        var days = Names(dayNames, 7, nameof(dayNames), "Day names must contain seven non-empty entries indexed by DayOfWeek.");
        var months = Names(monthNames, 12, nameof(monthNames), "Month names must contain twelve non-empty entries, January first.");
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
        var series = Series[Series.Count - 1];
        series.PointColors.AddRange(colors);
        series.CalendarFirstDay = firstDayOfWeek;
        series.CalendarDayNames = days;
        series.CalendarMonthNames = months;
        return this;
    }

    /// <summary>
    /// Sets how calendar heatmap cells are sized. By default cells fill the plot, limited by its width or height. All three
    /// values are set together: an omitted argument returns that setting to automatic, and a preferred size takes
    /// precedence over the maximum. Nothing changes when a value is out of range.
    /// </summary>
    /// <param name="size">A preferred cell size in pixels, used when it fits; the cells shrink when it does not. Null fills the plot.</param>
    /// <param name="maximumSize">The largest size cells grow to when they fill the plot, so a short window does not turn into a few
    /// huge squares. Null leaves them unlimited.</param>
    /// <param name="gap">The gap between cells in pixels. Null uses a gap chosen from the number of weeks.</param>
    /// <returns>The current chart.</returns>
    public Chart WithCalendarHeatmapCells(double? size = null, double? maximumSize = null, double? gap = null) {
        // Validate every value before changing any of them.
        _ = new ChartOptions { CalendarCellSize = size, CalendarMaximumCellSize = maximumSize, CalendarCellGap = gap };
        Options.CalendarCellSize = size;
        Options.CalendarMaximumCellSize = maximumSize;
        Options.CalendarCellGap = gap;
        return this;
    }

    private static string[]? Names(IReadOnlyList<string>? names, int count, string parameterName, string message) {
        if (names == null) return null;
        if (names.Count != count) throw new ArgumentException(message, parameterName);
        var copy = new string[count];
        for (var i = 0; i < count; i++) {
            if (string.IsNullOrWhiteSpace(names[i])) throw new ArgumentException(message, parameterName);
            copy[i] = names[i];
        }

        return copy;
    }

    private struct CalendarAggregate {
        public double Value;
        public ChartColor? Color;
    }
}

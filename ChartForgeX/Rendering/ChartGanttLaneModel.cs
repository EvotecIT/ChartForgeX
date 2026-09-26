using System;
using System.Collections.Generic;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>
/// Renderer-neutral rows, packed sub-rows, time ticks, and geometry for Gantt lanes so SVG and PNG stay in parity.
/// </summary>
internal sealed class ChartGanttLaneModel {
    public const double BandMaximum = 22;
    public const double BandMinimum = 4;
    public const double GroupRowUnits = 0.9;
    public const double SeparatorRowUnits = 0.4;
    public const double OpenEndExtension = 0.04;
    public const double BarRadius = 3;
    public const double LabelPadding = 6;

    private ChartGanttLaneModel(Chart chart, List<ChartGanttLaneRow> rows, double min, double max, double? now, IReadOnlyList<double> ticks, ChartStateCategoryLegend legend) {
        Chart = chart;
        Rows = rows;
        Min = min;
        Max = max;
        Now = now;
        Ticks = ticks;
        Legend = legend;
        foreach (var row in rows) {
            Units += RowUnits(row);
            HasSummary |= !row.IsGroup && !string.IsNullOrWhiteSpace(row.Summary);
        }

        HasSummary |= !string.IsNullOrWhiteSpace(chart.Options.LaneSummaryHeader);
    }

    public Chart Chart { get; }

    public IReadOnlyList<ChartGanttLaneRow> Rows { get; }

    public double Min { get; }

    public double Max { get; }

    /// <summary>Gets <see cref="ChartOptions.GanttToday"/>; the "Now" line is drawn only for an explicit current time.</summary>
    public double? Now { get; }

    /// <summary>Gets a value indicating whether the "Now" line falls inside the visible range.</summary>
    public bool NowVisible => Now.HasValue && Now.Value >= Min && Now.Value <= Max;

    public IReadOnlyList<double> Ticks { get; }

    public ChartStateCategoryLegend Legend { get; }

    public bool HasSummary { get; }

    public string? SummaryHeader => Chart.Options.LaneSummaryHeader;

    /// <summary>Gets the layout height in lane units: one per sub-row plus a smaller unit per group header.</summary>
    public double Units { get; }

    public static ChartGanttLaneModel Build(Chart chart) {
        var legend = new ChartStateCategoryLegend(chart);
        var min = double.PositiveInfinity;
        var max = double.NegativeInfinity;
        var hasOpen = false;
        foreach (var series in chart.Series) {
            if (series.Kind != ChartSeriesKind.GanttLane) continue;
            foreach (var item in series.GanttLaneItems) {
                min = Math.Min(min, item.Start);
                max = Math.Max(max, item.End ?? item.Start);
                hasOpen |= item.IsOpen;
            }
        }

        var today = chart.Options.GanttToday;
        if (today.HasValue) {
            min = Math.Min(min, today.Value);
            max = Math.Max(max, today.Value);
        }

        // Without an explicit current time, open items run slightly past the latest data so they still read as open.
        if (hasOpen && !today.HasValue && max > double.NegativeInfinity) max += Math.Max(1.0 / 1440.0, (max - min) * OpenEndExtension);
        var axis = chart.Options.XAxis;
        if (axis.Minimum.HasValue) min = axis.Minimum.Value;
        if (axis.Maximum.HasValue) max = axis.Maximum.Value;
        if (!(max > min)) max = min + 1.0 / 24.0;
        var ticks = ChartTimeScale.Generate(axis, min, max, true) ?? ChartTicks.GenerateInside(min, max, Math.Max(2, axis.TickCount));

        var rows = new List<ChartGanttLaneRow>();
        string? currentGroup = null;
        for (var seriesIndex = 0; seriesIndex < chart.Series.Count; seriesIndex++) {
            var series = chart.Series[seriesIndex];
            if (series.Kind != ChartSeriesKind.GanttLane) continue;
            if (series.LaneGroup != null && !string.Equals(series.LaneGroup, currentGroup, StringComparison.Ordinal)) rows.Add(ChartGanttLaneRow.Group(series.LaneGroup));
            else if (series.LaneGroup == null && currentGroup != null) rows.Add(ChartGanttLaneRow.Group(string.Empty));
            currentGroup = series.LaneGroup;
            rows.Add(ChartGanttLaneRow.Lane(seriesIndex, series.Name, series.LaneSummary, Pack(series, legend, today, min, max)));
        }

        return new ChartGanttLaneModel(chart, rows, min, max, today, ticks, legend);
    }

    /// <summary>
    /// Assigns each visible item to the first sub-row whose previous item has ended (interval partitioning). Items outside
    /// the axis range get sub-row -1 and take no space. Open items end at the current time, or at the axis end when the
    /// current time is unset or earlier than their start.
    /// </summary>
    private static List<ChartGanttLanePlacedItem> Pack(ChartSeries series, ChartStateCategoryLegend legend, double? now, double min, double max) {
        var placed = new List<ChartGanttLanePlacedItem>(series.GanttLaneItems.Count);
        var subRowEnds = new List<double>();
        for (var i = 0; i < series.GanttLaneItems.Count; i++) {
            var item = series.GanttLaneItems[i];
            var end = item.End ?? (now.HasValue && now.Value > item.Start ? now.Value : Math.Max(max, item.Start));
            if (end <= min || item.Start >= max) {
                placed.Add(new ChartGanttLanePlacedItem(i, item, end, -1, legend.Resolve(item.Category)));
                continue;
            }

            var subRow = subRowEnds.FindIndex(rowEnd => rowEnd <= item.Start);
            if (subRow < 0) {
                subRow = subRowEnds.Count;
                subRowEnds.Add(end);
            } else {
                subRowEnds[subRow] = end;
            }

            placed.Add(new ChartGanttLanePlacedItem(i, item, end, subRow, legend.Resolve(item.Category)));
        }

        return placed;
    }

    public double UnitHeight(ChartRect plot) => plot.Height / Math.Max(1, Units);

    public double Band(ChartRect plot) => Math.Min(UnitHeight(plot) * 0.9, Math.Max(BandMinimum, Math.Min(BandMaximum, UnitHeight(plot) * 0.72)));

    /// <summary>Returns the top of each row, in row order, followed by the plot bottom.</summary>
    public double[] RowTops(ChartRect plot) {
        var tops = new double[Rows.Count + 1];
        var y = plot.Top;
        var unit = UnitHeight(plot);
        for (var i = 0; i < Rows.Count; i++) {
            tops[i] = y;
            y += RowUnits(Rows[i]) * unit;
        }

        tops[Rows.Count] = plot.Bottom;
        return tops;
    }

    private static double RowUnits(ChartGanttLaneRow row) => row.IsGroup ? (row.Name.Length == 0 ? SeparatorRowUnits : GroupRowUnits) : row.SubRows;

    public double BarTop(ChartRect plot, double rowTop, int subRow) => rowTop + subRow * UnitHeight(plot) + (UnitHeight(plot) - Band(plot)) / 2;

    public double X(double value, ChartRect plot) => plot.Left + (Math.Max(Min, Math.Min(Max, value)) - Min) / (Max - Min) * plot.Width;

    /// <summary>Returns the pixel span of an item clipped to the axis, or false when it lies outside the visible range.</summary>
    public bool TrySpan(ChartGanttLanePlacedItem item, ChartRect plot, out double left, out double width) {
        left = 0;
        width = 0;
        if (item.SubRow < 0 || item.End <= Min || item.Item.Start >= Max) return false;
        left = Math.Min(X(item.Item.Start, plot), plot.Right - 2);
        width = Math.Max(2, X(item.End, plot) - left);
        return true;
    }

    public string FormatTick(double value) => ChartTimeScale.FormatTick(Chart.Options.XAxis, value);

    public string ItemSummary(ChartGanttLaneRow lane, ChartGanttLanePlacedItem placed) {
        var axis = Chart.Options.XAxis;
        var item = placed.Item;
        var text = lane.Name + " · " + placed.Category.Label + (string.IsNullOrWhiteSpace(item.Label) ? string.Empty : " · " + item.Label) + " · " +
            ChartTimeScale.FormatInstant(axis, item.Start) + " – " + (item.IsOpen ? "ongoing" : ChartTimeScale.FormatInstant(axis, placed.End)) +
            " (" + ChartStateTimelineModel.FormatDuration(placed.End - item.Start) + (item.IsOpen ? " so far" : string.Empty) + ")";
        return string.IsNullOrWhiteSpace(item.Detail) ? text : text + " · " + item.Detail;
    }
}

/// <summary>A Gantt lane row: either a group header or a lane with packed items.</summary>
internal sealed class ChartGanttLaneRow {
    private ChartGanttLaneRow(bool isGroup, int seriesIndex, string name, string? summary, IReadOnlyList<ChartGanttLanePlacedItem> items) {
        IsGroup = isGroup;
        SeriesIndex = seriesIndex;
        Name = name;
        Summary = summary;
        Items = items;
        var subRows = 1;
        foreach (var item in items) subRows = Math.Max(subRows, item.SubRow + 1);
        SubRows = subRows;
    }

    public bool IsGroup { get; }

    public int SeriesIndex { get; }

    public string Name { get; }

    public string? Summary { get; }

    public IReadOnlyList<ChartGanttLanePlacedItem> Items { get; }

    public int SubRows { get; }

    public static ChartGanttLaneRow Group(string name) => new(true, -1, name, null, Array.Empty<ChartGanttLanePlacedItem>());

    public static ChartGanttLaneRow Lane(int seriesIndex, string name, string? summary, IReadOnlyList<ChartGanttLanePlacedItem> items) => new(false, seriesIndex, name, summary, items);
}

/// <summary>A lane item with its resolved end, sub-row, and category.</summary>
internal readonly struct ChartGanttLanePlacedItem {
    public ChartGanttLanePlacedItem(int pointIndex, ChartGanttLaneItem item, double end, int subRow, ChartStateCategory category) {
        PointIndex = pointIndex;
        Item = item;
        End = end;
        SubRow = subRow;
        Category = category;
    }

    public int PointIndex { get; }

    public ChartGanttLaneItem Item { get; }

    public double End { get; }

    public int SubRow { get; }

    public ChartStateCategory Category { get; }
}

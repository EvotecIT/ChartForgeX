using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>
/// Renderer-neutral lanes, segments, time ticks, and geometry for state timelines so SVG and PNG stay in parity.
/// </summary>
internal sealed class ChartStateTimelineModel {
    public const double LaneBandMaximum = 26;
    public const double LaneBandMinimum = 4;
    public const double SegmentRadius = 1.5;
    public const double AxisReserve = 30;
    public const double AxisTitleReserve = 20;
    public const double LegendSwatch = 10;
    public const double LegendItemGap = 18;
    public const double ColumnGap = 14;
    public const double ContentInset = 12;
    public const double HatchSpacing = 6;
    public const double HatchOpacity = 0.45;

    private ChartStateTimelineModel(Chart chart, List<ChartStateTimelineLane> lanes, double min, double max, IReadOnlyList<double> ticks, List<ChartStateCategory> legendStates) {
        Chart = chart;
        Lanes = lanes;
        Min = min;
        Max = max;
        Ticks = ticks;
        LegendStates = legendStates;
        foreach (var lane in lanes) HasSummary |= !string.IsNullOrWhiteSpace(lane.Summary);
        HasSummary |= !string.IsNullOrWhiteSpace(chart.Options.StateTimelineSummaryHeader) && lanes.Count > 0;
    }

    public Chart Chart { get; }

    public IReadOnlyList<ChartStateTimelineLane> Lanes { get; }

    public double Min { get; }

    public double Max { get; }

    public IReadOnlyList<double> Ticks { get; }

    public IReadOnlyList<ChartStateCategory> LegendStates { get; }

    public bool HasSummary { get; }

    public string? SummaryHeader => Chart.Options.StateTimelineSummaryHeader;

    public static ChartStateTimelineModel Build(Chart chart) {
        var states = new Dictionary<string, ChartStateCategory>(StringComparer.Ordinal);
        foreach (var state in chart.Options.StateCategories) states[state.Key] = state;
        var lanes = new List<ChartStateTimelineLane>();
        var min = double.PositiveInfinity;
        var max = double.NegativeInfinity;
        for (var seriesIndex = 0; seriesIndex < chart.Series.Count; seriesIndex++) {
            var series = chart.Series[seriesIndex];
            if (series.Kind != ChartSeriesKind.StateTimeline) continue;
            var segments = new List<ChartStateTimelineResolvedSegment>(series.Points.Count);
            for (var i = 0; i < series.Points.Count; i++) {
                var key = series.PointLabels[i] ?? string.Empty;
                if (!states.TryGetValue(key, out var state)) {
                    state = new ChartStateCategory(key.Length == 0 ? "?" : key, key, chart.Options.Theme.MutedText);
                    states[key] = state;
                }

                var detail = i < series.StateTimelineDetails.Count ? series.StateTimelineDetails[i] : null;
                var last = segments.Count - 1;
                // Contiguous rollup buckets in the same state draw as one run so bucket seams do not read as state changes.
                if (last >= 0 && ReferenceEquals(segments[last].State, state) && segments[last].End == series.Points[i].X && string.Equals(segments[last].Detail, detail, StringComparison.Ordinal)) {
                    segments[last] = new ChartStateTimelineResolvedSegment(segments[last].PointIndex, segments[last].Start, series.Points[i].Y, state, detail);
                } else {
                    segments.Add(new ChartStateTimelineResolvedSegment(i, series.Points[i].X, series.Points[i].Y, state, detail));
                }

                min = Math.Min(min, series.Points[i].X);
                max = Math.Max(max, series.Points[i].Y);
            }

            lanes.Add(new ChartStateTimelineLane(seriesIndex, series.Name, series.StateTimelineSummary, segments));
        }

        var axis = chart.Options.XAxis;
        if (axis.Minimum.HasValue) min = axis.Minimum.Value;
        if (axis.Maximum.HasValue) max = axis.Maximum.Value;
        if (!(max > min)) {
            if (axis.Maximum.HasValue && !axis.Minimum.HasValue) min = max - Math.Max(1.0 / 24.0, Math.Abs(max) * 1e-12);
            else max = min + Math.Max(1.0 / 24.0, Math.Abs(min) * 1e-12);
        }
        // Timeline segments are UTC instants even when callers leave the default linear axis.
        var ticks = ChartTimeScale.Generate(axis, min, max, inside: true)
            ?? ChartTicks.GenerateInside(new ChartAxis { Scale = ChartScaleKind.Time, TickCount = axis.TickCount }, min, max);
        return new ChartStateTimelineModel(chart, lanes, min, max, ticks, new List<ChartStateCategory>(chart.Options.StateCategories));
    }

    /// <summary>Returns the lane plot area after reserving the label column, summary column, axis, and legend.</summary>
    public ChartRect PlotArea(ChartRect bounds, double laneLabelWidth, double summaryWidth, double summaryHeaderHeight, double legendHeight, double axisLabelReserve = AxisReserve, double axisTitleReserve = AxisTitleReserve) {
        var options = Chart.Options;
        var labelReserve = options.ShowAxes && options.ShowYAxis ? Math.Min(laneLabelWidth + ColumnGap, bounds.Width * 0.34) : 0;
        var summaryReserve = HasSummary ? SummaryColumnWidth(bounds, summaryWidth) + ColumnGap : 0;
        var axisReserve = options.ShowAxes && options.ShowXAxis ? axisLabelReserve + (string.IsNullOrWhiteSpace(ChartTimeScale.DecorateTitle(options.XAxis, Chart.XAxisTitle)) ? 0 : axisTitleReserve) : 0;
        var topReserve = HasSummary && !string.IsNullOrWhiteSpace(SummaryHeader) ? summaryHeaderHeight + 6 : 0;
        var width = Math.Max(1, bounds.Width - labelReserve - summaryReserve);
        var height = Math.Max(1, bounds.Height - axisReserve - legendHeight - topReserve);
        return new ChartRect(bounds.Left + labelReserve, bounds.Top + topReserve, width, height);
    }

    /// <summary>Returns the summary column width, capped so the lanes keep most of the chart width.</summary>
    public static double SummaryColumnWidth(ChartRect bounds, double measuredWidth) => Math.Max(8, Math.Min(measuredWidth, bounds.Width * 0.2 - ColumnGap));

    /// <summary>Insets the plot surface so lane labels and the summary column do not touch its border.</summary>
    public static ChartRect ContentBounds(ChartRect surface) => new(surface.Left + ContentInset, surface.Top, Math.Max(1, surface.Width - ContentInset * 2), surface.Height);

    public double LaneSlot(ChartRect plot) => plot.Height / Math.Max(1, Lanes.Count);

    public double LaneBand(ChartRect plot) => Math.Min(LaneSlot(plot) * 0.9, Math.Max(LaneBandMinimum, Math.Min(LaneBandMaximum, LaneSlot(plot) * 0.72)));

    public double LaneTop(ChartRect plot, int laneIndex) => plot.Top + laneIndex * LaneSlot(plot) + (LaneSlot(plot) - LaneBand(plot)) / 2;

    public double X(double value, ChartRect plot) => plot.Left + ChartMath.Normalize(Math.Max(Min, Math.Min(Max, value)), Min, Max) * plot.Width;

    /// <summary>Returns the pixel span of a segment clipped to the axis, or false when it lies outside the visible range.</summary>
    public bool TrySegmentSpan(ChartStateTimelineResolvedSegment segment, ChartRect plot, out double left, out double width) {
        left = 0;
        width = 0;
        if (segment.End <= Min || segment.Start >= Max) return false;
        left = X(segment.Start, plot);
        width = Math.Max(1, X(segment.End, plot) - left);
        return true;
    }

    public string FormatTick(double value) => ChartAxisValueFormatter.Format(Chart.Options.XAxis, value,
        tick => ChartTicks.IsNumericTimeFallback(Ticks) ? tick.ToString("G17", CultureInfo.InvariantCulture) : ChartTimeScale.Format(Chart.Options.XAxis, tick), Ticks);

    public string SegmentSummary(ChartStateTimelineLane lane, ChartStateTimelineResolvedSegment segment) {
        var text = lane.Name + " · " + segment.State.Label + " · " + FormatInstant(segment.Start) + " – " + FormatInstant(segment.End) + " (" + FormatDuration(segment.End - segment.Start) + ")";
        return string.IsNullOrWhiteSpace(segment.Detail) ? text : text + " · " + segment.Detail;
    }

    public string FormatInstant(double value) {
        var axis = Chart.Options.XAxis;
        var local = ChartTimeScale.ToDisplayTime(axis, value, roundToSeconds: false);
        if (!local.HasValue) return ChartNumericFormatter.FormatCompact(value);
        var format = local.Value.Millisecond != 0 ? "yyyy-MM-dd HH:mm:ss.fff" : local.Value.Second == 0 ? "yyyy-MM-dd HH:mm" : "yyyy-MM-dd HH:mm:ss";
        return local.Value.ToString(format, CultureInfo.InvariantCulture) + " " + ChartTimeScale.ZoneDesignator(axis);
    }

    public static string FormatDuration(double days) {
        var totalSeconds = Math.Max(0, days) * 86400;
        if (totalSeconds > 0 && totalSeconds < 1) return Math.Round(totalSeconds * 1000).ToString(CultureInfo.InvariantCulture) + "ms";
        var seconds = (long)Math.Round(totalSeconds);
        if (seconds < 60) return seconds.ToString(CultureInfo.InvariantCulture) + "s";
        var minutes = seconds / 60;
        if (minutes < 60) return minutes.ToString(CultureInfo.InvariantCulture) + "m";
        var hours = minutes / 60;
        if (hours < 48) return hours.ToString(CultureInfo.InvariantCulture) + "h" + (minutes % 60 == 0 ? string.Empty : " " + (minutes % 60).ToString(CultureInfo.InvariantCulture) + "m");
        return (hours / 24).ToString(CultureInfo.InvariantCulture) + "d" + (hours % 24 == 0 ? string.Empty : " " + (hours % 24).ToString(CultureInfo.InvariantCulture) + "h");
    }

    /// <summary>Wraps and budgets legend rows through the shared legend policy.</summary>
    public IReadOnlyList<ChartStateTimelineLegendItem> LayoutLegend(Func<string, double> measure, double left, double width, double availableHeight) {
        var rows = new List<List<(ChartStateCategory State, double Width)>>();
        var row = new List<(ChartStateCategory State, double Width)>();
        var rowWidth = 0.0;
        foreach (var state in LegendStates) {
            var itemWidth = Math.Min(width, LegendSwatch + 6 + measure(state.Label));
            var needed = row.Count == 0 ? itemWidth : rowWidth + LegendItemGap + itemWidth;
            if (row.Count > 0 && needed > width) {
                rows.Add(row);
                row = new List<(ChartStateCategory State, double Width)>();
                rowWidth = 0;
            }
            rowWidth = row.Count == 0 ? itemWidth : rowWidth + LegendItemGap + itemWidth;
            row.Add((state, itemWidth));
        }
        if (row.Count > 0) rows.Add(row);
        var omitted = 0;
        LegendRowBudget.Apply(rows, Chart, entry => entry.Count, count => {
            omitted = count;
            return new List<(ChartStateCategory State, double Width)>();
        }, availableHeight);
        var items = new List<ChartStateTimelineLegendItem>();
        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++) {
            row = rows[rowIndex];
            if (row.Count == 0) {
                items.Add(new ChartStateTimelineLegendItem(null, left, rowIndex, omitted));
                continue;
            }
            rowWidth = 0;
            foreach (var entry in row) rowWidth += entry.Width;
            rowWidth += Math.Max(0, row.Count - 1) * LegendItemGap;
            var x = left + Math.Max(0, (width - rowWidth) / 2);
            foreach (var entry in row) {
                items.Add(new ChartStateTimelineLegendItem(entry.State, x, rowIndex));
                x += entry.Width + LegendItemGap;
            }
        }
        return items;
    }

    /// <summary>Adapts the shared row policy's fixed padding to the timeline's compact eight-pixel legend padding.</summary>
    public static double LegendBudgetHeight(double plotHeight) => Math.Max(0, plotHeight - LaneBandMaximum + 18 + ChartVisualPrimitives.LegendPlotGap - 8);

    public static double LegendHeight(Chart chart, IReadOnlyList<ChartStateTimelineLegendItem> items) {
        var rows = 0;
        foreach (var item in items) rows = Math.Max(rows, item.Row + 1);
        return rows == 0 ? 0 : rows * LegendRowBudget.RowHeight(chart) + 8;
    }
}

/// <summary>One lane of a state timeline with its resolved segments.</summary>
internal sealed class ChartStateTimelineLane {
    public ChartStateTimelineLane(int seriesIndex, string name, string? summary, IReadOnlyList<ChartStateTimelineResolvedSegment> segments) {
        SeriesIndex = seriesIndex;
        Name = name;
        Summary = summary;
        Segments = segments;
    }

    public int SeriesIndex { get; }

    public string Name { get; }

    public string? Summary { get; }

    public IReadOnlyList<ChartStateTimelineResolvedSegment> Segments { get; }
}

/// <summary>A lane segment with its state definition resolved from the chart's state map.</summary>
internal readonly struct ChartStateTimelineResolvedSegment {
    public ChartStateTimelineResolvedSegment(int pointIndex, double start, double end, ChartStateCategory state, string? detail) {
        PointIndex = pointIndex;
        Start = start;
        End = end;
        State = state;
        Detail = detail;
    }

    public int PointIndex { get; }

    public double Start { get; }

    public double End { get; }

    public ChartStateCategory State { get; }

    public string? Detail { get; }
}

/// <summary>A positioned legend entry; <see cref="Row"/> counts from the top of the legend block.</summary>
internal readonly struct ChartStateTimelineLegendItem {
    public ChartStateTimelineLegendItem(ChartStateCategory? state, double x, int row, int omitted = 0) {
        State = state;
        X = x;
        Row = row;
        Omitted = omitted;
    }

    public ChartStateCategory? State { get; }

    public double X { get; }

    public int Row { get; }

    public int Omitted { get; }
}

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
    public const double ColumnGap = 14;
    public const double ContentInset = 12;

    private ChartStateTimelineModel(Chart chart, List<ChartStateTimelineLane> lanes, double min, double max, IReadOnlyList<double> ticks, ChartStateCategoryLegend legend) {
        Chart = chart;
        Lanes = lanes;
        Min = min;
        Max = max;
        Ticks = ticks;
        Legend = legend;
        foreach (var lane in lanes) HasSummary |= !string.IsNullOrWhiteSpace(lane.Summary);
        HasSummary |= !string.IsNullOrWhiteSpace(chart.Options.LaneSummaryHeader) && lanes.Count > 0;
    }

    public Chart Chart { get; }

    public IReadOnlyList<ChartStateTimelineLane> Lanes { get; }

    public double Min { get; }

    public double Max { get; }

    public IReadOnlyList<double> Ticks { get; }

    public ChartStateCategoryLegend Legend { get; }

    public bool HasSummary { get; }

    public string? SummaryHeader => Chart.Options.LaneSummaryHeader;

    public static ChartStateTimelineModel Build(Chart chart) {
        var legend = new ChartStateCategoryLegend(chart);
        var lanes = new List<ChartStateTimelineLane>();
        var min = double.PositiveInfinity;
        var max = double.NegativeInfinity;
        for (var seriesIndex = 0; seriesIndex < chart.Series.Count; seriesIndex++) {
            var series = chart.Series[seriesIndex];
            if (series.Kind != ChartSeriesKind.StateTimeline) continue;
            var segments = new List<ChartStateTimelineResolvedSegment>(series.Points.Count);
            for (var i = 0; i < series.Points.Count; i++) {
                var state = legend.Resolve(series.PointLabels[i]);

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

            lanes.Add(new ChartStateTimelineLane(seriesIndex, series.Name, series.LaneSummary, segments));
        }

        var axis = chart.Options.XAxis;
        if (axis.Minimum.HasValue) min = axis.Minimum.Value;
        if (axis.Maximum.HasValue) max = axis.Maximum.Value;
        if (!(max > min)) max = min + 1.0 / 24.0;
        var tickCount = Math.Max(2, axis.TickCount);
        var ticks = ChartTimeScale.Generate(axis, min, max, true) ?? ChartTicks.GenerateInside(min, max, tickCount);
        return new ChartStateTimelineModel(chart, lanes, min, max, ticks, legend);
    }

    /// <summary>Returns the lane plot area after reserving the label column, summary column, axis, and legend.</summary>
    public ChartRect PlotArea(ChartRect bounds, double laneLabelWidth, double summaryWidth, double summaryHeaderHeight, double legendHeight) =>
        LanePlotArea(Chart, bounds, HasSummary, SummaryHeader, laneLabelWidth, summaryWidth, summaryHeaderHeight, legendHeight, 0);

    /// <summary>Shared lane-chart layout: label column, summary column, time axis, legend, and an optional top reserve.</summary>
    public static ChartRect LanePlotArea(Chart chart, ChartRect bounds, bool hasSummary, string? summaryHeader, double laneLabelWidth, double summaryWidth, double summaryHeaderHeight, double legendHeight, double topLabelHeight) {
        var options = chart.Options;
        var labelReserve = options.ShowAxes ? Math.Min(laneLabelWidth + ColumnGap, bounds.Width * 0.34) : 0;
        var summaryReserve = hasSummary ? SummaryColumnWidth(bounds, summaryWidth) + ColumnGap : 0;
        var axisReserve = options.ShowAxes ? AxisReserve + (string.IsNullOrWhiteSpace(ChartTimeScale.DecorateTitle(options.XAxis, chart.XAxisTitle)) ? 0 : AxisTitleReserve) : 0;
        var topReserve = Math.Max(hasSummary && !string.IsNullOrWhiteSpace(summaryHeader) ? summaryHeaderHeight + 6 : 0, topLabelHeight);
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

    public double X(double value, ChartRect plot) => plot.Left + (Math.Max(Min, Math.Min(Max, value)) - Min) / (Max - Min) * plot.Width;

    /// <summary>Returns the pixel span of a segment clipped to the axis, or false when it lies outside the visible range.</summary>
    public bool TrySegmentSpan(ChartStateTimelineResolvedSegment segment, ChartRect plot, out double left, out double width) {
        left = 0;
        width = 0;
        if (segment.End <= Min || segment.Start >= Max) return false;
        left = X(segment.Start, plot);
        width = Math.Max(1, X(segment.End, plot) - left);
        return true;
    }

    public string FormatTick(double value) => ChartTimeScale.FormatTick(Chart.Options.XAxis, value);

    public string SegmentSummary(ChartStateTimelineLane lane, ChartStateTimelineResolvedSegment segment) {
        var text = lane.Name + " · " + segment.State.Label + " · " + FormatInstant(segment.Start) + " – " + FormatInstant(segment.End) + " (" + FormatDuration(segment.End - segment.Start) + ")";
        return string.IsNullOrWhiteSpace(segment.Detail) ? text : text + " · " + segment.Detail;
    }

    public string FormatInstant(double value) => ChartTimeScale.FormatInstant(Chart.Options.XAxis, value);

    public static string FormatDuration(double days) {
        var seconds = (long)Math.Round(Math.Max(0, days) * 86400);
        if (seconds < 60) return seconds.ToString(CultureInfo.InvariantCulture) + "s";
        var minutes = seconds / 60;
        if (minutes < 60) return minutes.ToString(CultureInfo.InvariantCulture) + "m";
        var hours = minutes / 60;
        if (hours < 48) return hours.ToString(CultureInfo.InvariantCulture) + "h" + (minutes % 60 == 0 ? string.Empty : " " + (minutes % 60).ToString(CultureInfo.InvariantCulture) + "m");
        return (hours / 24).ToString(CultureInfo.InvariantCulture) + "d" + (hours % 24 == 0 ? string.Empty : " " + (hours % 24).ToString(CultureInfo.InvariantCulture) + "h");
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

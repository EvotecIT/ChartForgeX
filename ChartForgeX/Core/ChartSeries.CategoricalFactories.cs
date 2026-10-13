using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Creates a waterfall from typed changes, balances and calculated checkpoints in authored order.</summary>
    public static ChartSeries CreateWaterfall(ChartSeriesId id, string name, IEnumerable<ChartWaterfallItem> items, ChartColor? color = null) =>
        Waterfall(new ChartSeries(id, name, ChartSeriesKind.Waterfall, Array.Empty<ChartPoint>()) { Color = color }, items);

    internal static ChartSeries CreateWaterfall(string name, IEnumerable<ChartWaterfallItem> items, ChartColor? color) =>
        Waterfall(new ChartSeries(name, ChartSeriesKind.Waterfall, Array.Empty<ChartPoint>()) { Color = color }, items);

    private static ChartSeries Waterfall(ChartSeries series, IEnumerable<ChartWaterfallItem> items) {
        series.SetWaterfallItems(items);
        return series;
    }

    /// <summary>Creates a state timeline lane, retaining ordered states and details with each interval.</summary>
    public static ChartSeries CreateStateTimelineLane(ChartSeriesId id, string name, IEnumerable<ChartStateTimelineSegment> segments,
        string? summary = null, string? group = null) =>
        StateTimeline(new ChartSeries(id, name, ChartSeriesKind.StateTimeline, Array.Empty<ChartPoint>()), segments, summary, group);

    internal static ChartSeries CreateStateTimelineLane(string name, IEnumerable<ChartStateTimelineSegment> segments,
        string? summary, string? group) =>
        StateTimeline(new ChartSeries(name, ChartSeriesKind.StateTimeline, Array.Empty<ChartPoint>()), segments, summary, group);

    private static ChartSeries StateTimeline(ChartSeries series, IEnumerable<ChartStateTimelineSegment> segments, string? summary, string? group) {
        series.ShowInLegend = false; series.LaneSummary = summary;
        series.LaneGroup = string.IsNullOrWhiteSpace(group) ? null : group;
        series.ReplaceStateTimelineSegments(segments);
        return series;
    }

    /// <summary>Creates an independently configurable Gantt lane with an explicit stable ID and complete typed items.</summary>
    public static ChartSeries CreateGanttLane(ChartSeriesId id, string name, IEnumerable<ChartGanttLaneItem> items,
        string? group = null, string? summary = null) =>
        GanttLane(new ChartSeries(id, name, ChartSeriesKind.GanttLane, Array.Empty<ChartPoint>()), items, group, summary);

    internal static ChartSeries CreateGanttLane(string name, IEnumerable<ChartGanttLaneItem> items, string? group, string? summary) =>
        GanttLane(new ChartSeries(name, ChartSeriesKind.GanttLane, Array.Empty<ChartPoint>()), items, group, summary);

    private static ChartSeries GanttLane(ChartSeries series, IEnumerable<ChartGanttLaneItem> items, string? group, string? summary) {
        series.ShowInLegend = false; series.LaneSummary = summary;
        series.LaneGroup = string.IsNullOrWhiteSpace(group) ? null : group;
        series.ReplaceGanttLaneItems(items);
        return series;
    }
}

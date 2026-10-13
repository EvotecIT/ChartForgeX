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
        if (segments == null) throw new ArgumentNullException(nameof(segments));
        var source = segments.ToArray();
        foreach (var segment in source)
            if (segment.State == null) throw new ArgumentException("Segments must be created with a state.", nameof(segments));
        var ordered = source.Select((segment, index) => (Segment: segment, Index: index))
            .OrderBy(item => item.Segment.Start).ThenBy(item => item.Segment.End).ThenBy(item => item.Index);
        series.ShowInLegend = false; series.LaneSummary = summary;
        series.LaneGroup = string.IsNullOrWhiteSpace(group) ? null : group;
        foreach (var item in ordered) {
            series.Points.Add(new ChartPoint(item.Segment.Start, item.Segment.End));
            series.PointLabels.Add(item.Segment.State);
            series.StateTimelineDetails.Add(item.Segment.Detail);
        }
        return series;
    }
}

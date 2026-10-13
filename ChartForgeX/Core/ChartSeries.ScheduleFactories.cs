using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Creates a task with numeric schedule bounds and progress. Attach it before adding chart-owned dependency relationships.</summary>
    public static ChartSeries CreateGanttTask(ChartSeriesId id, string name, double start, double end, double progress = 0,
        ChartColor? color = null) => Gantt(id, name, start, end, progress, false, color, false);

    /// <summary>Creates a task from date bounds. Configure the chart's time axis when attaching this standalone series.</summary>
    public static ChartSeries CreateGanttTask(ChartSeriesId id, string name, DateTime start, DateTime end, double progress = 0,
        ChartColor? color = null) => CreateGanttTask(id, name, start.ToOADate(), end.ToOADate(), progress, color);

    /// <summary>Creates a milestone at a numeric schedule coordinate.</summary>
    public static ChartSeries CreateGanttMilestone(ChartSeriesId id, string name, double when, ChartColor? color = null) =>
        Gantt(id, name, when, when, 1, true, color, false);

    /// <summary>Creates a milestone at a date. Configure the chart's time axis when attaching this standalone series.</summary>
    public static ChartSeries CreateGanttMilestone(ChartSeriesId id, string name, DateTime when, ChartColor? color = null) =>
        CreateGanttMilestone(id, name, when.ToOADate(), color);

    /// <summary>Creates a vertical schedule marker or band with its own stable series identity.</summary>
    public static ChartSeries CreateGanttMarker(ChartSeriesId id, string name, double when, double? rangeEnd = null, ChartColor? color = null) =>
        Gantt(id, name, when, rangeEnd ?? when, 0, false, color, true);

    /// <summary>Creates a vertical date marker or band. Configure the chart's time axis when attaching this standalone series.</summary>
    public static ChartSeries CreateGanttMarker(ChartSeriesId id, string name, DateTime when, DateTime? rangeEnd = null, ChartColor? color = null) =>
        CreateGanttMarker(id, name, when.ToOADate(), (rangeEnd ?? when).ToOADate(), color);

    internal static ChartSeries CreateGantt(string name, double start, double end, double progress,
        bool milestone, ChartColor? color, bool verticalMarker) =>
        Gantt(null, name, start, end, progress, milestone, color, verticalMarker);

    private static ChartSeries Gantt(ChartSeriesId? id, string name, double start, double end, double progress,
        bool milestone, ChartColor? color, bool verticalMarker) {
        var value = new ChartGanttItem(start, end, progress, milestone, verticalMarker);
        var series = id.HasValue ? new ChartSeries(id.Value, name, ChartSeriesKind.Gantt, Array.Empty<ChartPoint>()) : new ChartSeries(name, ChartSeriesKind.Gantt, Array.Empty<ChartPoint>());
        series.ReplaceGanttItems(new[] { value });
        series.Color = color; series.ShowInLegend = !verticalMarker;
        return series;
    }
}

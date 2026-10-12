using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds a Gantt task with a start date, end date, progress value, and optional dependency.
    /// </summary>
    public Chart AddGanttTask(string name, DateTime start, DateTime end, double progress = 0, int dependsOn = -1, ChartColor? color = null) {
        AddGanttRange(name, start.ToOADate(), end.ToOADate(), progress, dependsOn, false, color);
        Options.XAxis.UseTimeScaleByDefault();
        return this;
    }

    /// <summary>
    /// Adds a Gantt task with numeric start and end schedule values.
    /// </summary>
    public Chart AddGanttTask(string name, double start, double end, double progress = 0, int dependsOn = -1, ChartColor? color = null) =>
        AddGanttRange(name, start, end, progress, dependsOn, false, color);

    /// <summary>
    /// Adds a Gantt milestone at the specified date.
    /// </summary>
    public Chart AddGanttMilestone(string name, DateTime when, int dependsOn = -1, ChartColor? color = null) {
        AddGanttRange(name, when.ToOADate(), when.ToOADate(), 1, dependsOn, true, color);
        Options.XAxis.UseTimeScaleByDefault();
        return this;
    }

    /// <summary>
    /// Adds a Gantt milestone at the specified numeric schedule value.
    /// </summary>
    public Chart AddGanttMilestone(string name, double when, int dependsOn = -1, ChartColor? color = null) =>
        AddGanttRange(name, when, when, 1, dependsOn, true, color);

    private Chart AddGanttRange(string name, double start, double end, double progress, int dependsOn, bool milestone, ChartColor? color, bool verticalMarker = false) {
        ChartGuards.Finite(start, nameof(start));
        ChartGuards.Finite(end, nameof(end));
        ChartGuards.UnitInterval(progress, nameof(progress));
        if (end < start) throw new ArgumentOutOfRangeException(nameof(end), end, "Gantt end must be greater than or equal to start.");
        var taskCount = 0;
        foreach (var series in Series) if (series.Kind == ChartSeriesKind.Gantt) taskCount++;
        if (dependsOn < -1 || dependsOn >= taskCount) throw new ArgumentOutOfRangeException(nameof(dependsOn), dependsOn, "Gantt dependency must reference an earlier zero-based Gantt task index.");
        AppendSeries(new ChartSeries(name, ChartSeriesKind.Gantt, new[] {
            new ChartPoint(start, end),
            new ChartPoint(progress, dependsOn),
            new ChartPoint(milestone ? 1 : 0, verticalMarker ? 1 : 0)
        }) { Color = color, ShowInLegend = !verticalMarker });
        return this;
    }
}

using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds a Gantt task with a start date, end date, progress value, and optional dependency.
    /// </summary>
    /// <remarks>The optional predecessor must already be attached to this chart. The relationship retains that handle when rows move.</remarks>
    public Chart AddGanttTask(string name, DateTime start, DateTime end, double progress = 0, ChartSeries? dependsOn = null, ChartColor? color = null) {
        AddGanttRange(name, start.ToOADate(), end.ToOADate(), progress, dependsOn, false, color);
        Options.XAxis.UseTimeScaleByDefault();
        return this;
    }

    /// <summary>
    /// Adds a Gantt task with numeric start and end schedule values.
    /// </summary>
    /// <remarks>The optional predecessor must already be attached to this chart. The relationship retains that handle when rows move.</remarks>
    public Chart AddGanttTask(string name, double start, double end, double progress = 0, ChartSeries? dependsOn = null, ChartColor? color = null) =>
        AddGanttRange(name, start, end, progress, dependsOn, false, color);

    /// <summary>
    /// Adds a Gantt milestone at the specified date.
    /// </summary>
    /// <remarks>The optional predecessor must already be attached to this chart and is registered as a chart-owned relationship.</remarks>
    public Chart AddGanttMilestone(string name, DateTime when, ChartSeries? dependsOn = null, ChartColor? color = null) {
        AddGanttRange(name, when.ToOADate(), when.ToOADate(), 1, dependsOn, true, color);
        Options.XAxis.UseTimeScaleByDefault();
        return this;
    }

    /// <summary>
    /// Adds a Gantt milestone at the specified numeric schedule value.
    /// </summary>
    /// <remarks>The optional predecessor must already be attached to this chart and is registered as a chart-owned relationship.</remarks>
    public Chart AddGanttMilestone(string name, double when, ChartSeries? dependsOn = null, ChartColor? color = null) =>
        AddGanttRange(name, when, when, 1, dependsOn, true, color);

    private Chart AddGanttRange(string name, double start, double end, double progress, ChartSeries? dependsOn, bool milestone, ChartColor? color, bool verticalMarker = false) {
        ChartGuards.Finite(start, nameof(start));
        ChartGuards.Finite(end, nameof(end));
        ChartGuards.UnitInterval(progress, nameof(progress));
        if (end < start) throw new ArgumentOutOfRangeException(nameof(end), end, "Gantt end must be greater than or equal to start.");
        if (dependsOn != null) { ValidateGanttEndpoint(dependsOn, nameof(dependsOn)); RequireGanttDependencyRows(); }
        var added = ChartSeries.CreateGantt(name, start, end, progress, milestone, color, verticalMarker);
        AppendSeries(added);
        if (dependsOn != null) AddGanttDependency(dependsOn, added);
        return this;
    }
}

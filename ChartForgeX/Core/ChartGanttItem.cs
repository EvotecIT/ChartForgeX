using System;

namespace ChartForgeX.Core;

/// <summary>Describes one schedule task, milestone or vertical marker with its numeric schedule coordinates.</summary>
/// <remarks>Date constructors preserve the wall-clock OLE Automation date coordinates used by classic Gantt charts.</remarks>
public readonly struct ChartGanttItem {
    /// <summary>Gets the start coordinate, or the line coordinate for a vertical marker.</summary>
    public double Start { get; }
    /// <summary>Gets the end coordinate included in the schedule window.</summary>
    public double End { get; }
    /// <summary>Gets completion as a fraction between zero and one.</summary>
    public double Progress { get; }
    /// <summary>Gets an earlier zero-based Gantt series index, or -1 for no inline dependency.</summary>
    public int DependsOn { get; }
    /// <summary>Gets whether the item paints a milestone diamond.</summary>
    public bool IsMilestone { get; }
    /// <summary>Gets whether the item paints a vertical reference marker without reserving a task row.</summary>
    public bool IsVerticalMarker { get; }

    /// <summary>Creates a task, milestone or marker. Dependency membership is checked when the series is attached and prepared.</summary>
    public ChartGanttItem(double start, double end, double progress = 0, int dependsOn = -1,
        bool isMilestone = false, bool isVerticalMarker = false) {
        ChartGuards.Finite(start, nameof(start)); ChartGuards.Finite(end, nameof(end)); ChartGuards.UnitInterval(progress, nameof(progress));
        if (end < start) throw new ArgumentOutOfRangeException(nameof(end), end, "Gantt end must be greater than or equal to start.");
        if (dependsOn < -1) throw new ArgumentOutOfRangeException(nameof(dependsOn));
        Start = start; End = end; Progress = progress; DependsOn = dependsOn;
        IsMilestone = isMilestone; IsVerticalMarker = isVerticalMarker;
    }

    /// <summary>Creates an item from wall-clock date bounds.</summary>
    public ChartGanttItem(DateTime start, DateTime end, double progress = 0, int dependsOn = -1,
        bool isMilestone = false, bool isVerticalMarker = false)
        : this(start.ToOADate(), end.ToOADate(), progress, dependsOn, isMilestone, isVerticalMarker) { }
}

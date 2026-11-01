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
    /// <summary>Gets whether the item paints a milestone diamond.</summary>
    public bool IsMilestone { get; }
    /// <summary>Gets whether the item paints a vertical reference marker without reserving a task row.</summary>
    public bool IsVerticalMarker { get; }

    /// <summary>Creates a fixed task, milestone or marker with the established five-argument constructor contract.</summary>
    public ChartGanttItem(double start, double end, double progress, bool isMilestone, bool isVerticalMarker)
        : this(start, end, progress, isMilestone, isVerticalMarker, false) { }

    /// <summary>Creates a fixed item from wall-clock date bounds with the established five-argument constructor contract.</summary>
    public ChartGanttItem(DateTime start, DateTime end, double progress, bool isMilestone, bool isVerticalMarker)
        : this(start, end, progress, isMilestone, isVerticalMarker, false) { }

    /// <summary>Creates a task, milestone or marker. The chart owns dependency relationships separately from these observations.</summary>
    public ChartGanttItem(double start, double end, double progress = 0,
        bool isMilestone = false, bool isVerticalMarker = false, bool isOpenEnded = false) {
        ChartGuards.Finite(start, nameof(start)); ChartGuards.Finite(end, nameof(end)); ChartGuards.UnitInterval(progress, nameof(progress));
        if (isOpenEnded && (isMilestone || isVerticalMarker))
            throw new ArgumentException("An open-ended task cannot be a milestone or vertical marker.", nameof(isOpenEnded));
        if (isOpenEnded && end != start)
            throw new ArgumentException("An open-ended task must store its start as its end. Use OpenEnded to create it.", nameof(end));
        if (end < start) throw new ArgumentOutOfRangeException(nameof(end), end, "Gantt end must be greater than or equal to start.");
        Start = start; End = end; Progress = progress;
        IsMilestone = isMilestone; IsVerticalMarker = isVerticalMarker; IsOpenEnded = isOpenEnded;
    }

    /// <summary>Creates an item from wall-clock date bounds.</summary>
    public ChartGanttItem(DateTime start, DateTime end, double progress = 0,
        bool isMilestone = false, bool isVerticalMarker = false, bool isOpenEnded = false)
        : this(start.ToOADate(), end.ToOADate(), progress, isMilestone, isVerticalMarker, isOpenEnded) { }

    /// <summary>Gets whether this task continues beyond its authored start with no fixed end.</summary>
    public bool IsOpenEnded { get; }

    /// <summary>Creates a task with no fixed end. Its stored end equals start; rendering resolves the visible endpoint from the chart domain.</summary>
    public static ChartGanttItem OpenEnded(double start, double progress = 0) => new(start, start, progress, isOpenEnded: true);

    /// <summary>Creates an open task from a wall-clock date, using the classic Gantt coordinate convention.</summary>
    public static ChartGanttItem OpenEnded(DateTime start, double progress = 0) => OpenEnded(start.ToOADate(), progress);

    /// <summary>Gets optional immutable navigation, accessibility and host metadata.</summary>
    public ChartMarkOptions? MarkOptions { get; }

    /// <summary>Returns a copy with replacement mark metadata, retaining all authored values.</summary>
    /// <param name="options">Immutable metadata, or null to clear it.</param>
    /// <returns>A new observation. The current observation is unchanged.</returns>
    public ChartGanttItem WithMarkOptions(ChartMarkOptions? options) => new(this, options);

    private ChartGanttItem(ChartGanttItem source, ChartMarkOptions? options) {
        this = source;
        MarkOptions = options;
    }
}

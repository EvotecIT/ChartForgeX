using System;

namespace ChartForgeX.Core;

/// <summary>Controls the visible continuation of a schedule item without a known end.</summary>
public enum ChartGanttOpenEndStyle {
    /// <summary>Draws a continuation chevron at the current display endpoint.</summary>
    Arrow,
    /// <summary>Fades the final part of the bar to transparent.</summary>
    Fade
}

public sealed partial class ChartOptions {
    private ChartGanttOpenEndStyle _ganttOpenEndStyle;
    /// <summary>Gets or sets the continuation style for open classic tasks and Gantt-lane items.</summary>
    /// <remarks>Classic open tasks require an explicit X-axis maximum. Lane items retain their existing
    /// current-time endpoint policy. Neither style reads the clock during export.</remarks>
    public ChartGanttOpenEndStyle GanttOpenEndStyle {
        get => _ganttOpenEndStyle;
        set {
            if (!Enum.IsDefined(typeof(ChartGanttOpenEndStyle), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _ganttOpenEndStyle = value;
        }
    }
}

public sealed partial class Chart {
    /// <summary>Chooses a continuation chevron or a transparent fade for open schedule bars.</summary>
    public Chart WithGanttOpenEndStyle(ChartGanttOpenEndStyle style) { Options.GanttOpenEndStyle = style; return this; }
}

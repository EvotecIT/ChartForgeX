namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Sets fixed calendar ticks for a classic Gantt time axis, or null for automatic ticks.
    /// Explicit axis labels take precedence. Numeric schedules retain numeric ticks.</summary>
    public Chart WithGanttTickInterval(ChartTimeTickInterval? interval) { Options.GanttTickInterval = interval; return this; }
}

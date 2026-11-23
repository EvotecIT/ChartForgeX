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
}

public sealed partial class Chart {
}

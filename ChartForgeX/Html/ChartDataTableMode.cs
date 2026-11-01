namespace ChartForgeX.Html;

/// <summary>Controls script-free HTML tables accompanying a chart's inline SVG.</summary>
public enum ChartDataTableMode {
    /// <summary>Emits only the chart, preserving the default fragment output.</summary>
    None,
    /// <summary>Places captioned tables in a native details disclosure with a descriptive summary.</summary>
    Details,
    /// <summary>Keeps tables available to assistive technology, hides them visually on screen, and shows them in print.</summary>
    VisuallyHidden
}

namespace ChartForgeX.Core;

/// <summary>
/// Defines the kind of annotation rendered on a chart.
/// </summary>
public enum ChartAnnotationKind {
    /// <summary>
    /// Renders a horizontal line at a y-axis value.
    /// </summary>
    HorizontalLine,

    /// <summary>
    /// Renders a vertical line at an x-axis value.
    /// </summary>
    VerticalLine,

    /// <summary>
    /// Renders a horizontal band between two y-axis values.
    /// </summary>
    HorizontalBand,

    /// <summary>
    /// Renders a vertical band between two x-axis values.
    /// </summary>
    VerticalBand
}

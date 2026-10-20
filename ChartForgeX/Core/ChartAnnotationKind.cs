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
    VerticalBand,

    /// <summary>Renders a caption anchored to one Cartesian source coordinate.</summary>
    Note,

    /// <summary>Renders a finite line between two Cartesian source coordinates.</summary>
    LineSegment,

    /// <summary>Renders a source-coordinate line and its parallel source-Y offset, with a filled channel between them.</summary>
    ParallelChannel
}

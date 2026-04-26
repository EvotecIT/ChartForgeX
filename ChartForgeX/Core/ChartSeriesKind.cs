namespace ChartForgeX.Core;

/// <summary>
/// Defines the visual style used to render a chart series.
/// </summary>
public enum ChartSeriesKind {
    /// <summary>
    /// Renders connected line segments.
    /// </summary>
    Line,

    /// <summary>
    /// Renders a filled area under connected line segments.
    /// </summary>
    Area,

    /// <summary>
    /// Renders independent point markers.
    /// </summary>
    Scatter,

    /// <summary>
    /// Renders vertical bars.
    /// </summary>
    Bar,

    /// <summary>
    /// Renders horizontal bars.
    /// </summary>
    HorizontalBar,

    /// <summary>
    /// Renders a matrix of colored value cells.
    /// </summary>
    Heatmap,

    /// <summary>
    /// Renders a single-value radial gauge.
    /// </summary>
    Gauge,

    /// <summary>
    /// Renders compact value, target, and qualitative range bars.
    /// </summary>
    Bullet,

    /// <summary>
    /// Renders cumulative positive and negative change bars.
    /// </summary>
    Waterfall,

    /// <summary>
    /// Renders values on a radial category axis.
    /// </summary>
    Radar,

    /// <summary>
    /// Renders descending stage values as centered funnel segments.
    /// </summary>
    Funnel,

    /// <summary>
    /// Renders horizontal date or numeric ranges.
    /// </summary>
    Timeline,

    /// <summary>
    /// Renders a proportional pie chart.
    /// </summary>
    Pie,

    /// <summary>
    /// Renders a proportional donut chart.
    /// </summary>
    Donut
}

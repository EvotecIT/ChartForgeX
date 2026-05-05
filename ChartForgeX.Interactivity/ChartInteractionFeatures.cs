using System;

namespace ChartForgeX.Interactivity;

/// <summary>
/// Describes interaction capabilities that a host adapter may expose for a ChartForgeX chart.
/// </summary>
[Flags]
public enum ChartInteractionFeatures {
    /// <summary>
    /// Disables all interactive affordances.
    /// </summary>
    None = 0,

    /// <summary>
    /// Shows formatted metadata while hovering or focusing chart regions.
    /// </summary>
    Tooltips = 1,

    /// <summary>
    /// Allows chart regions to be selected through pointer or keyboard input.
    /// </summary>
    Selection = 2,

    /// <summary>
    /// Allows legend items to toggle the visibility or emphasis of matching series.
    /// </summary>
    LegendToggles = 4,

    /// <summary>
    /// Allows keyboard navigation or keyboard activation for interactive regions.
    /// </summary>
    KeyboardNavigation = 8,

    /// <summary>
    /// Allows an adapter to zoom into chart data or chart geometry.
    /// </summary>
    Zoom = 16,

    /// <summary>
    /// Allows an adapter to pan a zoomed chart view.
    /// </summary>
    Pan = 32,

    /// <summary>
    /// Allows an adapter to select a range such as a brushed x-axis interval.
    /// </summary>
    Brush = 64,

    /// <summary>
    /// Allows charts with the same group name to synchronize hover, selection, zoom, or brush state.
    /// </summary>
    SynchronizedCharts = 128,

    /// <summary>
    /// Allows an adapter to expose chart, image, or data export commands.
    /// </summary>
    Export = 256,

    /// <summary>
    /// Enables the first report-friendly interaction set for generated dashboards.
    /// </summary>
    ReportReview = Tooltips | Selection | LegendToggles | KeyboardNavigation
}

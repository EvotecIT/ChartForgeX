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
    /// Allows an adapter to switch between named scenarios, flows, or alternate analytical paths.
    /// </summary>
    Scenarios = 512,

    /// <summary>
    /// Allows an adapter to step through ordered route, scenario, or annotation states.
    /// </summary>
    StepPlayback = 1024,

    /// <summary>
    /// Allows an adapter to publish or consume links that restore interactive chart state.
    /// </summary>
    DeepLinks = 2048,

    /// <summary>
    /// Allows an adapter to show nearest-point crosshair exploration.
    /// </summary>
    Crosshair = 4096,

    /// <summary>
    /// Allows an adapter to summarize selected targets for quick visual comparison.
    /// </summary>
    CompareMarkers = 8192,

    /// <summary>
    /// Allows an adapter to keep a short visual breadcrumb of recently focused targets.
    /// </summary>
    FocusTrail = 16384,

    /// <summary>
    /// Allows an adapter to reveal compact labels beside the current hover, focus, or route targets.
    /// </summary>
    RevealLabels = 32768,

    /// <summary>
    /// Allows an adapter or host page to capture and reapply reusable interaction snapshots.
    /// </summary>
    StateBookmarks = 65536,

    /// <summary>
    /// Enables the first report-friendly interaction set for generated dashboards.
    /// </summary>
    ReportReview = Tooltips | Selection | LegendToggles | KeyboardNavigation | Crosshair | CompareMarkers
}

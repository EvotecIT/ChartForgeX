namespace ChartForgeX.Core;

/// <summary>
/// Defines how heatmap cell values are converted into colors.
/// </summary>
public enum ChartHeatmapScale {
    /// <summary>
    /// Blends from the plot background to the series high-intensity color.
    /// </summary>
    Sequential,

    /// <summary>
    /// Uses the theme negative, warning, and positive colors for status-oriented matrices.
    /// </summary>
    Semantic
}

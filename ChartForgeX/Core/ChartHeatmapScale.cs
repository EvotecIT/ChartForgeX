namespace ChartForgeX.Core;

/// <summary>
/// Defines how heatmap cell values are converted into colors.
/// </summary>
public enum ChartHeatmapScale {
    /// <summary>
    /// Blends from the plot background to the series high-intensity color, or interpolates <see cref="ChartForgeX.Themes.ChartTheme.SequentialRamp"/> when set and the series has no colour.
    /// </summary>
    Sequential,

    /// <summary>
    /// Uses the theme negative, warning, and positive colors for status-oriented matrices.
    /// </summary>
    Semantic
}

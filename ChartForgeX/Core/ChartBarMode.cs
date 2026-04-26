namespace ChartForgeX.Core;

/// <summary>
/// Defines how multiple bar series share the same x-axis category.
/// </summary>
public enum ChartBarMode {
    /// <summary>
    /// Renders each bar series side by side within a category.
    /// </summary>
    Grouped,

    /// <summary>
    /// Renders bar series stacked on top of one another within a category.
    /// </summary>
    Stacked
}

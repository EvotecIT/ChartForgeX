namespace ChartForgeX.Core;

/// <summary>
/// Defines how aggressively renderers reduce axis labels when space is limited.
/// </summary>
public enum ChartLabelDensity {
    /// <summary>
    /// Lets the renderer choose a balanced label count for the available space.
    /// </summary>
    Auto,

    /// <summary>
    /// Keeps more labels visible and accepts tighter spacing.
    /// </summary>
    Dense,

    /// <summary>
    /// Shows fewer labels to preserve generous spacing.
    /// </summary>
    Relaxed,

    /// <summary>
    /// Keeps eligible axis labels without collision thinning. Individual labels still fit their viewport and avoid marks;
    /// text may be shortened or omitted when it cannot fit. Coincident full-turn ticks remain a single seam caption.
    /// </summary>
    All
}

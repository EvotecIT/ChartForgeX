namespace ChartForgeX.Core;

/// <summary>
/// Defines reusable texture overlays for filled chart marks.
/// </summary>
public enum ChartFillPattern {
    /// <summary>
    /// Renders the mark with a solid or gradient fill only.
    /// </summary>
    None,

    /// <summary>
    /// Adds forward diagonal hatch strokes over the mark fill.
    /// </summary>
    DiagonalForward,

    /// <summary>
    /// Adds backward diagonal hatch strokes over the mark fill.
    /// </summary>
    DiagonalBackward,

    /// <summary>
    /// Adds both forward and backward diagonal hatch strokes over the mark fill.
    /// </summary>
    Crosshatch
}

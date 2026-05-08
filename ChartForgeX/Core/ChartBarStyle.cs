namespace ChartForgeX.Core;

/// <summary>
/// Defines the visual treatment used for bar and horizontal-bar marks.
/// </summary>
public enum ChartBarStyle {
    /// <summary>
    /// Renders bars as solid rounded rectangles.
    /// </summary>
    Solid,

    /// <summary>
    /// Renders bars as translucent rounded segments with a stronger rounded cap at the value edge.
    /// </summary>
    SegmentedCapsule
}

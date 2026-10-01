namespace ChartForgeX.Core;

/// <summary>
/// Defines the visual treatment used for bar and horizontal-bar marks.
/// </summary>
public enum ChartBarStyle {
    /// <summary>
    /// Renders bars, horizontal bars, and histograms as rounded rectangles with a soft vertical gradient derived from the
    /// bar colour and a light highlight along the top edge (SVG range bars are a translucent fill).
    /// </summary>
    Solid,

    /// <summary>
    /// Renders bars as translucent rounded segments with a stronger rounded cap at the value edge.
    /// </summary>
    SegmentedCapsule,

    /// <summary>
    /// Renders bars, horizontal bars, histograms, range bars, and waterfall steps as rounded rectangles filled with the
    /// bar colour only: no gradient, no highlight, full opacity, so a bar is exactly its series or point colour (for
    /// example a design-token colour).
    /// </summary>
    Flat
}

namespace ChartForgeX.Core;

/// <summary>Chooses how a numeric color key is presented independently of its value-to-color mapping.</summary>
public enum ChartColorLegendStyle {
    /// <summary>Preserves the chart family's default numeric key presentation.</summary>
    Auto,
    /// <summary>Draws the continuous scale's exact stops at their numeric positions.</summary>
    Gradient,
    /// <summary>Draws bounded samples of a continuous scale or the exact intervals of a discrete scale.</summary>
    Swatches
}

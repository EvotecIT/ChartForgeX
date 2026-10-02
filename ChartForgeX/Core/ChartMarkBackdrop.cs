namespace ChartForgeX.Core;

/// <summary>
/// Chooses the surface ChartForgeX treats as the colour behind the marks: the colour of state pattern lines and the
/// surface outlined and quiet heatmap cells are blended over, the base of neutral zero counts, and the
/// background text on translucent marks is checked against. It never changes which surfaces are drawn.
/// </summary>
public enum ChartMarkBackdrop {
    /// <summary>
    /// The theme background (<see cref="Themes.ChartTheme.Background"/>), then the card and the plot background where
    /// they are drawn, each composited over the previous one. The theme background counts even when
    /// <see cref="ChartOptions.TransparentBackground"/> leaves it undrawn: it stands for the surface the host places the chart on. Without any opaque surface the backdrop is white or black, whichever contrasts with the text colour.
    /// </summary>
    Layered,

    /// <summary>The theme background, whether or not it is drawn.</summary>
    Background,

    /// <summary>The theme card colour (<see cref="Themes.ChartTheme.CardBackground"/>), whether or not the card is drawn: for a chart placed on a host's card.</summary>
    Card,

    /// <summary>The theme plot background (<see cref="Themes.ChartTheme.PlotBackground"/>), whether or not it is drawn.</summary>
    Plot
}

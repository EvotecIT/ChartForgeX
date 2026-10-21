namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Caps the thickness of regular vertical or horizontal bars, including grouped and stacked bars. Groups stay centred on their categories.</summary>
    /// <param name="pixels">Maximum thickness in drawing pixels. Null restores the theme default.</param>
    /// <returns>The chart for chaining.</returns>
    public Chart WithMaximumBarWidth(double? pixels) { Options.MaximumBarWidth = pixels; return this; }
}

namespace ChartForgeX.Core;

/// <summary>
/// Named PNG output-density presets. The chart keeps its logical layout while the emitted PNG dimensions are multiplied by the selected value.
/// </summary>
public enum ChartRasterScale {
    /// <summary>
    /// Emits a PNG at the chart's logical width and height.
    /// </summary>
    Standard = 1,

    /// <summary>
    /// Emits a two-times-density PNG for high-DPI screens and web previews.
    /// </summary>
    Retina = 2,

    /// <summary>
    /// Emits a three-times-density PNG for presentation decks and large dashboards.
    /// </summary>
    Presentation = 3,

    /// <summary>
    /// Emits a four-times-density PNG for print, 4K/8K display surfaces, and aggressive downsampling workflows.
    /// </summary>
    Print = 4
}

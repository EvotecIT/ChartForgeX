using ChartForgeX.Interactivity;

namespace ChartForgeX.Interactivity.Html;

/// <summary>Configures HTML crosshair presentation independently of tooltip acquisition and palette.</summary>
public sealed class HtmlChartCrosshairOptions {
    /// <summary>
    /// Gets or sets whether the crosshair includes a target label. The default is true for every palette.
    /// The <see cref="ChartInteractionFeatures.Crosshair"/> feature controls whether the guide is enabled.
    /// </summary>
    public bool ShowLabel { get; set; } = true;
}

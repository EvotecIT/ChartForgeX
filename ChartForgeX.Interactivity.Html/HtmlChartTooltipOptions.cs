using ChartForgeX.Interactivity;

namespace ChartForgeX.Interactivity.Html;

/// <summary>Configures HTML tooltip content and pointer acquisition without enabling interaction features.</summary>
public sealed class HtmlChartTooltipOptions {
    /// <summary>
    /// Gets or sets the readout mode. The default is <see cref="HtmlChartTooltipMode.SharedX"/>.
    /// Shared-x readouts fall back to one target when comparable observations are unavailable.
    /// </summary>
    public HtmlChartTooltipMode Mode { get; set; } = HtmlChartTooltipMode.SharedX;

    /// <summary>
    /// Gets or sets the pointer acquisition range. The default is 120 CSS pixels from the nearest painted
    /// Cartesian observation. Native hits, keyboard focus, and pinned readouts retain their target semantics.
    /// The <see cref="ChartInteractionFeatures.Tooltips"/> feature controls whether tooltips are enabled.
    /// </summary>
    public HtmlChartTooltipRange Range { get; set; } = HtmlChartTooltipRange.WithinDistance(120);
}

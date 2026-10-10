using ChartForgeX.Interactivity;

namespace ChartForgeX.Interactivity.Html;

/// <summary>Configures HTML tooltip content, acquisition, timing and positioning without enabling interaction features.</summary>
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

    /// <summary>
    /// Gets or sets how long a pointer target must remain acquired before its tooltip appears, in milliseconds.
    /// The default is zero (immediate). Values must be non-negative. Movement within the same target updates
    /// the tooltip position without restarting the delay. Keyboard focus and explicit pins are immediate.
    /// </summary>
    public int DelayMilliseconds { get; set; }

    /// <summary>Gets the anchor, ordered placement and CSS pixel offset options.</summary>
    public HtmlChartTooltipPositionOptions Position { get; } = new HtmlChartTooltipPositionOptions();
}

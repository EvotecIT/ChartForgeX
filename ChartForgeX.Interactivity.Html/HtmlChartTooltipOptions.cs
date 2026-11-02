using ChartForgeX.Interactivity;

namespace ChartForgeX.Interactivity.Html;

/// <summary>Configures HTML tooltip content, acquisition, timing and positioning without enabling interaction features.</summary>
public sealed class HtmlChartTooltipOptions {
    /// <summary>Gets or sets whether selecting a mark pins its readout. The default is true.
    /// Disabling pinning retains ordinary pointer and keyboard tooltips.</summary>
    public bool PinOnSelect { get; set; } = true;

    /// <summary>
    /// Gets or sets the readout mode. The default is <see cref="HtmlChartTooltipMode.Auto"/>.
    /// Shared-x readouts fall back to one target when comparable observations are unavailable.
    /// </summary>
    public HtmlChartTooltipMode Mode { get; set; } = HtmlChartTooltipMode.Auto;

    /// <summary>
    /// Gets or sets the pointer acquisition range. The default is <see cref="HtmlChartTooltipRange.Auto"/>:
    /// nearest category for line, area and bar charts, or nearest observation for other Cartesian charts,
    /// within 120 CSS pixels. Native hits, keyboard focus, and pinned readouts retain their target semantics.
    /// The <see cref="ChartInteractionFeatures.Tooltips"/> feature controls whether tooltips are enabled.
    /// </summary>
    public HtmlChartTooltipRange Range { get; set; } = HtmlChartTooltipRange.Auto;

    /// <summary>
    /// Gets or sets how long a pointer target must remain acquired before its tooltip appears, in milliseconds.
    /// The default is zero (immediate). Values must be non-negative. Movement within the same target updates
    /// the tooltip position without restarting the delay. Keyboard focus and explicit pins are immediate.
    /// </summary>
    public int DelayMilliseconds { get; set; }

    /// <summary>Gets or sets an optional plain-text tooltip template for saved HTML reports.
    /// The placeholders <c>{title}</c>, <c>{series}</c>, <c>{category}</c> and <c>{value}</c>
    /// insert the current formatted facts. Markup is displayed as text, never executed.
    /// A browser callback assigned to the chart section's <c>cfxTooltipRenderer</c> property takes precedence.</summary>
    public string? Template { get; set; }

    /// <summary>Gets the anchor, ordered placement and CSS pixel offset options.</summary>
    public HtmlChartTooltipPositionOptions Position { get; } = new HtmlChartTooltipPositionOptions();
}

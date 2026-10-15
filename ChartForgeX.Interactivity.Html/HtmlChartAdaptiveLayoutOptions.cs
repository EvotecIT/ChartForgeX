namespace ChartForgeX.Interactivity.Html;

/// <summary>Configures a second native chart preparation for narrow HTML containers.</summary>
public sealed class HtmlChartAdaptiveLayoutOptions {
    /// <summary>Gets or sets the narrow drawing's logical width. The default is 360 pixels.</summary>
    public int NarrowWidth { get; set; } = 360;

    /// <summary>Gets or sets the narrow drawing's logical height. Null retains the authored chart height.</summary>
    public int? NarrowHeight { get; set; }

    /// <summary>Gets or sets the container width below which the narrow drawing is used. The default is 640 CSS pixels.</summary>
    public int Breakpoint { get; set; } = 640;
}

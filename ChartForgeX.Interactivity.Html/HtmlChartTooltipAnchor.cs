namespace ChartForgeX.Interactivity.Html;

/// <summary>Selects the screen geometry used to position an HTML chart tooltip.</summary>
public enum HtmlChartTooltipAnchor {
    /// <summary>Uses the latest pointer position, or the target centre for keyboard focus. This is the default.</summary>
    Pointer,
    /// <summary>Uses the acquired native target's current screen bounds.</summary>
    Node,
    /// <summary>Uses the displayed chart stage, including its contained readable viewport.</summary>
    Chart
}

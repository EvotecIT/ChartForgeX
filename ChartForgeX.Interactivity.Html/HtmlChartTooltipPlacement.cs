namespace ChartForgeX.Interactivity.Html;

/// <summary>Specifies a tooltip direction relative to its anchor's screen bounds.</summary>
public enum HtmlChartTooltipPlacement {
    /// <summary>Centres the tooltip above the anchor.</summary>
    Top,
    /// <summary>Places the tooltip above and to the right of the anchor.</summary>
    TopRight,
    /// <summary>Centres the tooltip to the right of the anchor.</summary>
    Right,
    /// <summary>Places the tooltip below and to the right of the anchor. This is the default.</summary>
    BottomRight,
    /// <summary>Centres the tooltip below the anchor.</summary>
    Bottom,
    /// <summary>Places the tooltip below and to the left of the anchor.</summary>
    BottomLeft,
    /// <summary>Centres the tooltip to the left of the anchor.</summary>
    Left,
    /// <summary>Places the tooltip above and to the left of the anchor.</summary>
    TopLeft
}

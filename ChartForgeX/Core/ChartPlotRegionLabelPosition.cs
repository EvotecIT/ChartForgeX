namespace ChartForgeX.Core;

/// <summary>Chooses a screen-relative caption anchor inside the visible intersection of a Cartesian plot region.</summary>
public enum ChartPlotRegionLabelPosition {
    /// <summary>Tries the center followed by the four corners while avoiding marks and prior captions.</summary>
    Auto,
    /// <summary>Centers the caption without falling back to another anchor.</summary>
    Center,
    /// <summary>Uses the visible region's upper-left corner.</summary>
    TopLeft,
    /// <summary>Uses the center of the visible region's top edge.</summary>
    Top,
    /// <summary>Uses the visible region's upper-right corner.</summary>
    TopRight,
    /// <summary>Uses the center of the visible region's left edge.</summary>
    Left,
    /// <summary>Uses the center of the visible region's right edge.</summary>
    Right,
    /// <summary>Uses the visible region's lower-left corner.</summary>
    BottomLeft,
    /// <summary>Uses the center of the visible region's bottom edge.</summary>
    Bottom,
    /// <summary>Uses the visible region's lower-right corner.</summary>
    BottomRight
}

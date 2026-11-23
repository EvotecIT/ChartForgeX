using System;

namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
}

/// <summary>Chooses which side of each pictorial symbol retains a fractional unit.</summary>
public enum ChartPictorialFillDirection {
    /// <summary>Fills from the left edge to the right edge.</summary>
    LeftToRight,
    /// <summary>Fills from the right edge to the left edge.</summary>
    RightToLeft,
    /// <summary>Fills from the bottom edge upward.</summary>
    BottomToTop,
    /// <summary>Fills from the top edge downward.</summary>
    TopToBottom
}

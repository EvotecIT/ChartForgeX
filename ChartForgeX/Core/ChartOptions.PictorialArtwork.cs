using System;

namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    private ChartPictorialFillDirection _pictorialFillDirection;
    /// <summary>Gets or sets optional immutable image artwork. Null restores the authored custom contour or built-in symbol.</summary>
    /// <remarks>Artwork takes precedence without clearing any symbol, contour, value scale, color or spacing settings. Its own colors are preserved.</remarks>
    public ChartPictorialArtwork? PictorialArtwork { get; set; }
    /// <summary>Gets or sets the direction of the nominal partial rectangle for built-in, custom contour and image symbols.</summary>
    /// <remarks>LeftToRight is the unchanged default. Image values clip the centered fitted source rectangle; contours use their existing square symbol bounds.</remarks>
    public ChartPictorialFillDirection PictorialFillDirection {
        get => _pictorialFillDirection;
        set { if (!Enum.IsDefined(typeof(ChartPictorialFillDirection), value)) throw new ArgumentOutOfRangeException(nameof(value)); _pictorialFillDirection = value; }
    }
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

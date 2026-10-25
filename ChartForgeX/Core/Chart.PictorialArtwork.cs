namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Sets immutable image artwork for pictorial symbols, or clears it to restore the authored contour or built-in shape.</summary>
    /// <param name="artwork">Captured artwork, or null. Other pictorial settings remain unchanged.</param>
    /// <returns>The current chart.</returns>
    public Chart WithPictorialArtwork(ChartPictorialArtwork? artwork) { Options.PictorialArtwork = artwork; return this; }
    /// <summary>Sets the partial-unit clipping direction shared by built-in, custom contour and image symbols.</summary>
    /// <param name="direction">The filled edge; LeftToRight is the default.</param>
    /// <returns>The current chart.</returns>
    public Chart WithPictorialFillDirection(ChartPictorialFillDirection direction) { Options.PictorialFillDirection = direction; return this; }
}

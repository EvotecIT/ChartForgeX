namespace ChartForgeX.Effects;

/// <summary>Explicit gamma-encoded sRGB compositing for an isolated captured source and a known destination.</summary>
public enum VisualBlendMode {
    /// <summary>Source over the backdrop.</summary>
    Normal = 0,
    /// <summary>Multiplies overlapping unassociated channels, with source-over alpha.</summary>
    Multiply = 1,
    /// <summary>Screens overlapping unassociated channels, with source-over alpha.</summary>
    Screen = 2,
    /// <summary>Adds premultiplied channels and alpha independently, clamping each to one.</summary>
    PlusLighter = 3,
    /// <summary>Multiplies or Screens each channel according to the destination channel.</summary>
    Overlay = 4,
    /// <summary>Selects the darker source or destination channel.</summary>
    Darken = 5,
    /// <summary>Selects the lighter source or destination channel.</summary>
    Lighten = 6,
    /// <summary>Brightens the destination by the inverse source channel, with the standard endpoint rules.</summary>
    ColorDodge = 7,
    /// <summary>Darkens the destination by the source channel, with the standard endpoint rules.</summary>
    ColorBurn = 8,
    /// <summary>Multiplies or Screens each channel according to the source channel.</summary>
    HardLight = 9,
    /// <summary>Applies the standard piecewise soft-light channel law.</summary>
    SoftLight = 10,
    /// <summary>Selects the absolute difference between source and destination channels.</summary>
    Difference = 11,
    /// <summary>Combines channels as source plus destination minus twice their product.</summary>
    Exclusion = 12,
    /// <summary>Uses source hue with destination saturation and luminosity.</summary>
    Hue = 13,
    /// <summary>Uses source saturation with destination hue and luminosity.</summary>
    Saturation = 14,
    /// <summary>Uses source hue and saturation with destination luminosity.</summary>
    Color = 15,
    /// <summary>Uses source luminosity with destination hue and saturation.</summary>
    Luminosity = 16
}

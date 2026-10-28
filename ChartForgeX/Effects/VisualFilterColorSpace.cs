namespace ChartForgeX.Effects;

/// <summary>The working channel space of one explicitly typed native filter.</summary>
/// <remarks>Stored RGBA image bytes remain unassociated sRGB. LinearRgb selects the existing sRGB transfer
/// owner for RGB output only; alpha always represents linear coverage.</remarks>
public enum VisualFilterColorSpace {
    /// <summary>Gamma-encoded sRGB working channels.</summary>
    Srgb = 0,
    /// <summary>Linear-light sRGB working channels, encoded to sRGB at image output.</summary>
    LinearRgb = 1
}

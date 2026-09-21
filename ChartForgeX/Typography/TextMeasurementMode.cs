namespace ChartForgeX.Typography;

/// <summary>Controls whether layout widths depend on fonts installed on the rendering host.</summary>
public enum TextMeasurementMode {
    /// <summary>Uses the portable width estimate. Geometry is independent of installed fonts; actual glyph widths can differ.</summary>
    PortableEstimate,
    /// <summary>Measures the theme family using installed outline fonts. Geometry can differ between hosts.</summary>
    InstalledFonts
}

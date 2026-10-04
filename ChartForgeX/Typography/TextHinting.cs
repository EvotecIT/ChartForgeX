namespace ChartForgeX.Typography;

/// <summary>
/// Controls how raster text is fitted to the pixel grid.
/// </summary>
public enum TextHinting {
    /// <summary>
    /// Text drawn at 12 output pixels or smaller has its baseline, x-height, and cap height moved to
    /// whole pixels, vertically only, so stems and x-heights are crisp while advances, widths, and
    /// wrapping stay exactly those of unhinted text. Larger text is drawn as designed.
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Every glyph is drawn exactly as designed, at any size: use it where outline geometry matters
    /// more than crisp rows, such as animation frames that move text by fractions of a pixel.
    /// </summary>
    None = 1,

    /// <summary>
    /// Adds horizontal fitting and limited darkening of narrow, straight stems to <see cref="Auto"/>
    /// at 12 output pixels and below. Glyph ink can move by less than one output pixel; advances,
    /// measurement, wrapping, and fitting retain the font's layout. Slanted and colour glyphs keep
    /// their existing outline policy. Font instruction programs are not executed.
    /// </summary>
    Full = 2
}

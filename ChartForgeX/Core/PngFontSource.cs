namespace ChartForgeX.Core;

/// <summary>
/// Describes the font source selected for PNG text rendering.
/// </summary>
public enum PngFontSource {
    /// <summary>
    /// The configured PNG font path was loaded.
    /// </summary>
    Requested,

    /// <summary>
    /// ChartForgeX selected an available platform font automatically.
    /// </summary>
    Automatic,

    /// <summary>
    /// ChartForgeX will use the built-in tiny fallback font.
    /// </summary>
    BuiltIn
}

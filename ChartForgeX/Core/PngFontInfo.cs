namespace ChartForgeX.Core;

/// <summary>
/// Describes the font that ChartForgeX will use for PNG text rendering.
/// </summary>
public sealed class PngFontInfo {
    internal PngFontInfo(PngFontSource source, string? themeFontFamily, string? requestedPath, int? requestedCollectionIndex, string? requestedFaceName, string? resolvedPath, int? resolvedCollectionIndex, string? resolvedFaceName) {
        Source = source;
        ThemeFontFamily = themeFontFamily;
        RequestedPath = requestedPath;
        RequestedCollectionIndex = requestedCollectionIndex;
        RequestedFaceName = requestedFaceName;
        ResolvedPath = resolvedPath;
        ResolvedCollectionIndex = resolvedCollectionIndex;
        ResolvedFaceName = resolvedFaceName;
    }

    /// <summary>
    /// Gets the selected PNG font source.
    /// </summary>
    public PngFontSource Source { get; }

    /// <summary>
    /// Gets a value indicating whether PNG text will render from TrueType outlines.
    /// </summary>
    public bool UsesOutlineFont => Source != PngFontSource.BuiltIn;

    /// <summary>
    /// Gets the SVG/HTML font-family stack used to select an automatic PNG font.
    /// </summary>
    public string? ThemeFontFamily { get; }

    /// <summary>
    /// Gets the requested PNG font path, if one was configured.
    /// </summary>
    public string? RequestedPath { get; }

    /// <summary>
    /// Gets the requested TrueType collection index, if one was configured.
    /// </summary>
    public int? RequestedCollectionIndex { get; }

    /// <summary>
    /// Gets the requested font face name, if one was configured.
    /// </summary>
    public string? RequestedFaceName { get; }

    /// <summary>
    /// Gets the resolved font path, or null when the built-in fallback font will be used.
    /// </summary>
    public string? ResolvedPath { get; }

    /// <summary>
    /// Gets the resolved collection index when it is known.
    /// </summary>
    public int? ResolvedCollectionIndex { get; }

    /// <summary>
    /// Gets the resolved face name when it is known.
    /// </summary>
    public string? ResolvedFaceName { get; }
}

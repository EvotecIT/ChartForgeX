namespace ChartForgeX.Core;

/// <summary>
/// Defines dependency-free raster export formats.
/// </summary>
public enum RasterImageFormat {
    /// <summary>
    /// Uncompressed 24-bit BMP.
    /// </summary>
    Bmp,

    /// <summary>
    /// Binary P6 portable pixmap.
    /// </summary>
    Ppm,

    /// <summary>
    /// Baseline uncompressed RGB TIFF.
    /// </summary>
    Tiff,

    /// <summary>
    /// Portable Network Graphics with RGBA alpha.
    /// </summary>
    Png,

    /// <summary>
    /// Baseline JPEG flattened against the configured raster background.
    /// </summary>
    Jpeg
}

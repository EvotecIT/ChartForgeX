using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>
/// Defines common options for raster image exports.
/// </summary>
public class RasterImageOptions {
    private int _jpegQuality = 90;

    /// <summary>
    /// Gets or sets the background used when flattening transparent pixels for formats that do not preserve alpha.
    /// </summary>
    /// <remarks>
    /// The background alpha channel is ignored by opaque encoders.
    /// </remarks>
    public ChartColor Background { get; set; } = ChartColors.White;

    /// <summary>
    /// Gets or sets JPEG quality from 1 to 100.
    /// </summary>
    public int JpegQuality {
        get => _jpegQuality;
        set {
            if (value < 1 || value > 100) throw new System.ArgumentOutOfRangeException(nameof(value), value, "JPEG quality must be between 1 and 100.");
            _jpegQuality = value;
        }
    }
}

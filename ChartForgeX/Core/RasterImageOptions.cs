using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>
/// Defines common options for opaque raster image exports.
/// </summary>
public class RasterImageOptions {
    /// <summary>
    /// Gets or sets the background used when flattening transparent pixels for formats that do not preserve alpha.
    /// </summary>
    /// <remarks>
    /// The background alpha channel is ignored by opaque encoders.
    /// </remarks>
    public ChartColor Background { get; set; } = ChartColors.White;
}

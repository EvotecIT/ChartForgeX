using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>
/// Defines common options for raster image exports.
/// </summary>
public class RasterImageOptions {
    private const double InchesPerMeter = 1D / 0.0254D;
    private int _jpegQuality = 90;
    private int _pngCompressionLevel = 6;
    private double? _dpi;

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

    /// <summary>
    /// Gets or sets PNG deflate compression from 0 to 9, where 0 disables compression, 1-3 favors speed, and 4-9 favors smaller output.
    /// </summary>
    /// <remarks>
    /// The dependency-free PNG writer maps the level to the compression modes available on the current .NET target.
    /// </remarks>
    public int PngCompressionLevel {
        get => _pngCompressionLevel;
        set {
            if (value < 0 || value > 9) throw new System.ArgumentOutOfRangeException(nameof(value), value, "PNG compression level must be between 0 and 9.");
            _pngCompressionLevel = value;
        }
    }

    /// <summary>
    /// Gets or sets optional output resolution metadata in dots per inch.
    /// PNG exports encode this value as a physical pixel density without changing pixel dimensions.
    /// </summary>
    public double? Dpi {
        get => _dpi;
        set {
            if (value.HasValue && !IsRepresentablePngDensity(value.Value)) {
                throw new System.ArgumentOutOfRangeException(nameof(value), value, "DPI must be finite, positive, and representable as PNG pixels-per-meter metadata.");
            }
            _dpi = value;
        }
    }

    private static bool IsRepresentablePngDensity(double dpi) {
        if (double.IsNaN(dpi) || double.IsInfinity(dpi) || dpi <= 0) return false;
        var pixelsPerMeter = System.Math.Round(dpi * InchesPerMeter, System.MidpointRounding.AwayFromZero);
        return pixelsPerMeter >= 1 && pixelsPerMeter <= uint.MaxValue;
    }
}

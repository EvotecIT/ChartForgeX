using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using PowerForge.Fluent;

namespace ChartForgeX.Core;

/// <summary>Fonts, hinting and scale for every raster format. Configure it through <see cref="ChartOptions.Raster"/>.</summary>
[Fluent(Copy = nameof(Clone))]
public sealed class ChartRasterOptions {
    internal ChartRasterOptions Clone() => (ChartRasterOptions)MemberwiseClone();
    private string? _pngFontPath;

    private string? _pngFontFaceName;

    private int? _pngFontCollectionIndex;

    private int _pngSupersamplingScale = 2;

    private Typography.TextHinting _pngTextHinting;

    private int _pngOutputScale = 1;


    /// <summary>
    /// Gets or sets the preferred TrueType font file or TrueType collection used by the PNG renderer.
    /// </summary>
    /// <remarks>
    /// SVG and HTML use <see cref="VisualTypography.Family"/>. PNG rendering uses this .ttf or .ttc file when it can be loaded and falls back to an auto-detected platform font or the built-in tiny font.
    /// </remarks>
    public string? FontPath {
        get => _pngFontPath;
        set {
            if (value != null && string.IsNullOrWhiteSpace(value)) throw new ArgumentException("PNG font path must not be empty.", nameof(value));
            _pngFontPath = value;
        }
    }


    /// <summary>
    /// Gets or sets the optional family, subfamily, full, or PostScript face name used when selecting a PNG font.
    /// </summary>
    public string? FontFaceName {
        get => _pngFontFaceName;
        set {
            if (value != null && string.IsNullOrWhiteSpace(value)) throw new ArgumentException("PNG font face name must not be empty.", nameof(value));
            _pngFontFaceName = value;
        }
    }


    /// <summary>
    /// Gets or sets the optional face index used when <see cref="ChartRasterOptions.FontPath"/> points to a TrueType collection.
    /// </summary>
    public int? FontCollectionIndex {
        get => _pngFontCollectionIndex;
        set {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), value, "PNG font collection index must be non-negative.");
            _pngFontCollectionIndex = value;
        }
    }


    /// <summary>
    /// Gets or sets how the PNG renderer fits text to the pixel grid.
    /// </summary>
    /// <remarks>
    /// <see cref="Typography.TextHinting.Auto"/> (the default) snaps the baseline, x-height, and cap height of labels at 12 output pixels and below to whole pixels without changing their widths. <see cref="Typography.TextHinting.Full"/> also fits narrow straight stems while retaining layout widths. Use <see cref="Typography.TextHinting.None"/> where exact outline geometry matters, such as animation frames.
    /// </remarks>
    public Typography.TextHinting TextHinting {
        get => _pngTextHinting;
        set {
            if (!Enum.IsDefined(typeof(Typography.TextHinting), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown text hinting mode.");
            _pngTextHinting = value;
        }
    }


    /// <summary>
    /// Gets or sets the internal supersampling scale used by the PNG renderer.
    /// </summary>
    /// <remarks>
    /// Higher values improve antialiased edges at the cost of render time and memory. The output dimensions stay equal to <see cref="ChartOptions.Size"/>.
    /// </remarks>
    public int SupersamplingScale {
        get => _pngSupersamplingScale;
        set {
            if (value < 1 || value > 4) throw new ArgumentOutOfRangeException(nameof(value), value, "PNG supersampling scale must be between one and four.");
            _pngSupersamplingScale = value;
        }
    }


    /// <summary>
    /// Gets or sets the output pixel multiplier used by the PNG renderer.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="ChartRasterOptions.SupersamplingScale"/>, this changes the emitted PNG dimensions while preserving the chart's logical layout.
    /// </remarks>
    public int OutputScale {
        get => _pngOutputScale;
        set {
            if (value < 1 || value > 4) throw new ArgumentOutOfRangeException(nameof(value), value, "PNG output scale must be between one and four.");
            _pngOutputScale = value;
        }
    }
}

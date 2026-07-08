using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Composition;

/// <summary>Central logo or icon badge layer.</summary>
public sealed class VisualCanvasHeroBadgeLayer : VisualCanvasLayer {
    private string _symbol;
    private string _imageHref = string.Empty;
    private double _imagePadding = 10;
    private double _imageOpacity = 1;

    /// <summary>Initializes a hero badge layer.</summary>
    /// <param name="x">The badge X coordinate.</param>
    /// <param name="y">The badge Y coordinate.</param>
    /// <param name="width">The badge width.</param>
    /// <param name="height">The badge height.</param>
    /// <param name="symbol">The badge symbol.</param>
    public VisualCanvasHeroBadgeLayer(double x, double y, double width, double height, string symbol) : base(x, y, width, height) {
        _symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
    }

    /// <summary>Gets or sets the badge symbol.</summary>
    public string Symbol { get => _symbol; set => _symbol = value ?? throw new ArgumentNullException(nameof(value)); }
    /// <summary>Gets or sets the badge accent color.</summary>
    public ChartColor Accent { get => AccentOverride ?? ChartColor.FromHex("#22A7FF"); set => AccentOverride = value; }
    /// <summary>Gets or sets an explicit badge accent color. When empty, renderers use the current theme secondary accent.</summary>
    public ChartColor? AccentOverride { get; set; }
    /// <summary>Gets or sets an SVG-compatible image reference for the badge logo.</summary>
    public string ImageHref { get => _imageHref; set => _imageHref = value ?? throw new ArgumentNullException(nameof(value)); }
    /// <summary>Gets or sets optional source RGBA pixels for PNG output.</summary>
    public byte[]? ImageRgba { get; set; }
    /// <summary>Gets or sets the source bitmap width for PNG output.</summary>
    public int ImageSourceWidth { get; set; }
    /// <summary>Gets or sets the source bitmap height for PNG output.</summary>
    public int ImageSourceHeight { get; set; }
    /// <summary>Gets or sets how the badge image is placed inside the badge.</summary>
    public VisualCanvasImageFit ImageFit { get; set; } = VisualCanvasImageFit.Contain;
    /// <summary>Gets or sets padding between the badge shell and image content.</summary>
    public double ImagePadding {
        get => _imagePadding;
        set {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Hero badge image padding must be finite and greater than or equal to zero.");
            _imagePadding = value;
        }
    }
    /// <summary>Gets or sets badge image opacity from zero to one.</summary>
    public double ImageOpacity {
        get => _imageOpacity;
        set {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value), value, "Hero badge image opacity must be between zero and one.");
            _imageOpacity = value;
        }
    }
    /// <summary>Gets whether the badge has image content for at least one renderer.</summary>
    public bool HasImage => ImageHref.Length > 0 || ImageRgba != null;
}

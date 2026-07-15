using System;

namespace ChartForgeX.Composition;

public sealed partial class VisualCanvas {
    private int? _designWidth;
    private int? _designHeight;
    private VisualCanvasImageFit _responsiveFit = VisualCanvasImageFit.Contain;

    /// <summary>Gets the coordinate-space width used to lay out layers.</summary>
    public int DesignWidth => _designWidth ?? Width;

    /// <summary>Gets the coordinate-space height used to lay out layers.</summary>
    public int DesignHeight => _designHeight ?? Height;

    /// <summary>Gets whether output dimensions differ from the layer design coordinate space.</summary>
    public bool IsResponsive => _designWidth.HasValue && (_designWidth.Value != Width || _designHeight!.Value != Height);

    /// <summary>Gets or sets how the design coordinate space fits the requested output size.</summary>
    public VisualCanvasImageFit ResponsiveFit {
        get => _responsiveFit;
        set {
            ValidateResponsiveFit(value, nameof(value));
            _responsiveFit = value;
        }
    }

    /// <summary>Sets output dimensions without changing layer coordinates.</summary>
    public VisualCanvas WithOutputSize(int width, int height) {
        Width = width;
        Height = height;
        return this;
    }

    /// <summary>Preserves a design coordinate space while adapting SVG and PNG output to other dimensions.</summary>
    public VisualCanvas WithResponsiveLayout(int designWidth, int designHeight, VisualCanvasImageFit fit = VisualCanvasImageFit.Contain) {
        if (designWidth <= 0) throw new ArgumentOutOfRangeException(nameof(designWidth), designWidth, "Design width must be positive.");
        if (designHeight <= 0) throw new ArgumentOutOfRangeException(nameof(designHeight), designHeight, "Design height must be positive.");
        ValidateResponsiveFit(fit, nameof(fit));
        _designWidth = designWidth;
        _designHeight = designHeight;
        _responsiveFit = fit;
        return this;
    }

    /// <summary>Uses output dimensions as the layer coordinate space again.</summary>
    public VisualCanvas WithFixedLayout() {
        _designWidth = null;
        _designHeight = null;
        _responsiveFit = VisualCanvasImageFit.Contain;
        return this;
    }

    internal static void ValidateResponsiveFit(VisualCanvasImageFit fit, string parameterName) {
        if (fit != VisualCanvasImageFit.Contain && fit != VisualCanvasImageFit.Cover && fit != VisualCanvasImageFit.Stretch) {
            throw new ArgumentOutOfRangeException(parameterName, fit, "Responsive canvas fit must be Contain, Cover, or Stretch.");
        }
    }
}

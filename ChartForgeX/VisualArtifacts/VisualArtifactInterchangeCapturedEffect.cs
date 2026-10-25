using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualArtifacts;

/// <summary>One finite ordered captured presentation step, with exactly one active typed effect payload.</summary>
/// <remarks>Root steps precede at most one explicit terminal capture. After capture, only the existing
/// captured-layer mask operation is supported. This is not a scene command list or arbitrary filter graph.</remarks>
public sealed class VisualArtifactInterchangeCapturedEffect {
    /// <summary>Gets or sets a terminal Gaussian capture. Blur is not an implicit root presentation wrapper.</summary>
    public VisualLayerBlur? Blur { get; set; }
    /// <summary>Gets or sets the unchanged-source alpha shadow policy.</summary>
    public VisualDropShadow? DropShadow { get; set; }
    /// <summary>Gets or sets the original-sized alpha-preserving inner shadow policy.</summary>
    public VisualInnerShadow? InnerShadow { get; set; }
    /// <summary>Gets or sets the outer alpha-band policy.</summary>
    public VisualOutline? Outline { get; set; }
    /// <summary>Gets or sets the explicit gamma-sRGB luminance-ramp bloom policy.</summary>
    public VisualBloom? Bloom { get; set; }
    /// <summary>Gets or sets detached original mask samples and native coverage policy.</summary>
    public VisualArtifactInterchangeMask? Mask { get; set; }
    /// <summary>Gets or sets the single terminal native capture's actual output-frame facts; null retains the root viewport.</summary>
    public VisualArtifactInterchangeCaptureFrame? CaptureFrame { get; set; }
}

/// <summary>Distinguishes an explicit baked output frame from its independent source viewport and typed facts.</summary>
/// <remarks>The envelope Width/Height remain source dimensions. Generic prepared-artifact dimension checks stay strict.
/// Replay must reproduce Bounds exactly; an edited source with a different actual footprint requires a fresh capture.</remarks>
public sealed class VisualArtifactInterchangeCaptureFrame {
    /// <summary>Gets or sets actual outward device-grid output bounds in original source logical coordinates.</summary>
    public ChartRect Bounds { get; set; }
    /// <summary>Gets or sets immutable native density, supersampling, pixel cap and hinting selections.</summary>
    public VisualRenderOptions RasterOptions { get; set; } = new();
}

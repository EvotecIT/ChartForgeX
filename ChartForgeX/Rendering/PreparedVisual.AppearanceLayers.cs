using System;
using System.Threading;
using ChartForgeX.Effects;

namespace ChartForgeX.Rendering;

public sealed partial class PreparedVisual {
    /// <summary>Captures a square-kernel outer alpha band, additively merged with this unchanged native child.</summary>
    /// <param name="effect">Immutable literal tint and logical radius, on the selected device grid.</param>
    /// <param name="options">Existing device density, supersampling, pixel cap and text policy.</param>
    /// <param name="cancellationToken">Cancels before capture, between rows and before the detached snapshot is published.</param>
    /// <returns>A baked layer containing source and outward band. Radius zero or transparent tint preserves source bytes.</returns>
    /// <remarks>This is an alpha-silhouette effect, not a geometric path stroke. Bounds include the propagated support;
    /// source facts and the original prepared scene remain unchanged. Pixel/work limits are checked before capture.</remarks>
    public CapturedVisualLayer CaptureLayer(VisualOutline effect, VisualRenderOptions? options = null,
        CancellationToken cancellationToken = default) {
        if (effect == null) throw new ArgumentNullException(nameof(effect));
        return VisualSceneLayerCapture.Capture(this, effect, options ?? new VisualRenderOptions(), cancellationToken);
    }

    /// <summary>Captures the explicit sRGB luminance bright pass, Gaussian approximation and Screen merge as one layer.</summary>
    /// <param name="effect">Required immutable threshold and strength with logical-unit sigma targets.</param>
    /// <param name="options">Existing density, supersampling, pixel cap and text policy.</param>
    /// <param name="cancellationToken">Cancels before allocations, between rows and before publication.</param>
    /// <returns>A baked source-plus-bloom snapshot. No bright samples retain original source pixels and bounds.</returns>
    /// <remarks>The bright pass remains premultiplied16 through the shared Gaussian boxes and Screen merge before final
    /// quantization. This is gamma-encoded sRGB, not HDR. It does not add retained vector or portable effect replay.</remarks>
    public CapturedVisualLayer CaptureLayer(VisualBloom effect, VisualRenderOptions? options = null,
        CancellationToken cancellationToken = default) {
        if (effect == null) throw new ArgumentNullException(nameof(effect));
        return VisualSceneLayerCapture.Capture(this, effect, options ?? new VisualRenderOptions(), cancellationToken);
    }
}

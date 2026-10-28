using System;
using System.Threading;
using ChartForgeX.Effects;

namespace ChartForgeX.Rendering;

public sealed partial class PreparedVisual {
    /// <summary>Captures this complete viewport and applies one detached fixed-grid mask without changing source facts or geometry.</summary>
    /// <param name="mask">Immutable mask in the original child's coordinates, with the same density as the capture.</param>
    /// <param name="options">Existing native density, coverage, budget and text policy.</param>
    /// <param name="cancellationToken">Cancels before capture, within mask rows and before snapshot publication.</param>
    /// <returns>A static baked layer retaining its original source and viewport.</returns>
    /// <remarks>Preflight counts source, mask, declared native/capture buffers, output and private snapshot before native paint.
    /// RGB remains byte-exact wherever output alpha is positive; only alpha can dither. Outside the mask coverage is zero.
    /// Native SVG uses the existing baked image; this does not emit a live SVG mask or change ordinary scene exports.
    /// Existing synchronous native painting retains its cancellation lifetime and font/geometry allocation owners.</remarks>
    public CapturedVisualLayer CaptureLayer(VisualMask mask, VisualRenderOptions? options = null, CancellationToken cancellationToken = default) {
        if (mask == null) throw new ArgumentNullException(nameof(mask));
        return VisualSceneLayerCapture.Capture(this, mask, options ?? new VisualRenderOptions(), cancellationToken);
    }
}

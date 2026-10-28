using System;
using System.Threading;
using ChartForgeX.Effects;

namespace ChartForgeX.Rendering;

public sealed partial class PreparedVisual {
    /// <summary>Captures this complete prepared viewport as one immutable blurred composition layer.</summary>
    /// <param name="effect">Explicit immutable logical-unit blur. Existing ordinary exports are never changed.</param>
    /// <param name="options">Existing native output density, coverage, source pixel budget and hinting policy.</param>
    /// <param name="cancellationToken">Cancellation observed before and after native capture, within effect rows and before publishing the snapshot.</param>
    /// <returns>A bounded static layer whose image, SVG and composition consumer use the same private pixels.</returns>
    /// <remarks>All marks, captured text and decorations are painted together by the existing native scene renderer once.
    /// The complete child viewport is the source boundary; no arbitrary group/crop, backdrop, mask or blend graph is inferred.
    /// Blur support may extend outside that viewport, but never grows or mutates this original prepared scene.
    /// The returned Visual is a self-contained baked-image lowering; native SVG filters/profile parity remain unsupported.
    /// Native capture itself retains its current synchronous lifetime; cancellation does not claim to interrupt every existing paint operation.
    /// Pixel-array accounting excludes encoder/metadata allocations and existing font/geometry-owner intermediates; it is not an RSS or latency guarantee.</remarks>
    public CapturedVisualLayer CaptureLayer(VisualLayerBlur effect, VisualRenderOptions? options = null, CancellationToken cancellationToken = default) {
        if (effect == null) throw new ArgumentNullException(nameof(effect));
        return VisualSceneLayerCapture.Capture(this, effect, options ?? new VisualRenderOptions(), cancellationToken);
    }
    /// <summary>Captures this complete prepared viewport with one spread/blurred alpha shadow beneath its original source.</summary>
    /// <param name="effect">Explicit immutable logical-unit shadow; source and shadow support are retained separately.</param>
    /// <param name="options">Existing output density, coverage, source budget and hinting policy.</param>
    /// <param name="cancellationToken">Cancellation checked around native capture, within effect rows and before publication.</param>
    /// <returns>A detached static layer covering the union of the source and propagated shadow support.</returns>
    /// <remarks>The original prepared source is never mutated. SourceAlpha is spread, blurred, offset and tinted;
    /// its unchanged SourceGraphic is composited above that shadow once. A transparent tint skips effect conversion.
    /// The existing baked-image SVG route is explicit; native filter profiles and backdrop/layer graphs are separate capabilities.
    /// Capture bounds, declared pixel arrays and sample work reject before rendering when existing safety ceilings are exceeded.
    /// Existing synchronous native painting retains its current cancellation lifetime and font/geometry guards.</remarks>
    public CapturedVisualLayer CaptureLayer(VisualDropShadow effect, VisualRenderOptions? options = null, CancellationToken cancellationToken = default) {
        if (effect == null) throw new ArgumentNullException(nameof(effect));
        return VisualSceneLayerCapture.Capture(this, effect, options ?? new VisualRenderOptions(), cancellationToken);
    }

}

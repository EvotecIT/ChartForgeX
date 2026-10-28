using System;
using System.Threading;
using ChartForgeX.Effects;

namespace ChartForgeX.Rendering;

public sealed partial class PreparedVisual {
    /// <summary>Captures an original-sized alpha-preserving inner shadow through the existing finite effect plan.</summary>
    /// <param name="effect">Immutable inset request; transparent tint or zero opacity preserves captured source bytes.</param>
    /// <param name="options">Existing native density, coverage, pixel budget and text policy.</param>
    /// <param name="cancellationToken">Observed before allocation, during existing kernel/output rows and before publication.</param>
    /// <returns>A detached captured layer with original viewport bounds and independent source facts.</returns>
    public CapturedVisualLayer CaptureLayer(VisualInnerShadow effect, VisualRenderOptions? options = null,
        CancellationToken cancellationToken = default) {
        if (effect == null) throw new ArgumentNullException(nameof(effect));
        return VisualSceneLayerCapture.Capture(this, effect, options ?? new VisualRenderOptions(), cancellationToken);
    }

    /// <summary>Retains a finite inner shadow on this complete root with its original viewport, facts and source alpha.</summary>
    /// <param name="effect">Immutable inset request. Zero geometry alone is not identity for semitransparent source.</param>
    /// <returns>This source for zero tint/opacity, otherwise an independent retained presentation.</returns>
    public PreparedVisual WithInnerShadow(VisualInnerShadow effect) {
        if (effect == null) throw new ArgumentNullException(nameof(effect));
        return WithRootEffect(VisualCapturedEffect.From(effect));
    }
}

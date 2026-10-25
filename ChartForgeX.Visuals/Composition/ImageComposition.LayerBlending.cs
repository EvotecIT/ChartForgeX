using System;
using System.Threading;
using ChartForgeX.Effects;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.Composition;

public sealed partial class ImageComposition {
    /// <summary>Composites a fixed-grid layer over the current destination using one explicitly selected sRGB operator.</summary>
    /// <param name="layer">Immutable native capture; Source remains its original prepared child.</param>
    /// <param name="x">Original child's destination left edge in logical composition units.</param>
    /// <param name="y">Original child's destination top edge in logical composition units.</param>
    /// <param name="blendMode">Required explicit gamma-sRGB blend. Standard modes use source-over alpha; PlusLighter adds and clamps premultiplied color and alpha.</param>
    /// <param name="orderedDither">Existing final RGBA8 Bayer quantization, anchored to destination device coordinates.</param>
    /// <param name="cancellationToken">Cancels before the final atomic replacement; rejected or canceled requests leave this composition unchanged.</param>
    /// <returns>The current fixed-frame composition.</returns>
    /// <remarks>This opt-in path requires matching density and device-grid placement; use the existing three-argument overload
    /// for ordinary affine sampling and its incumbent byte arithmetic. Explicit Normal uses premultiplied16 source-over.
    /// Captured opacity is applied once. There is no implicit page backdrop or nonisolated group blend.
    /// Source/destination arrays, complete output and sample work are preflighted before allocation. Do not mutate concurrently.</remarks>
    public ImageComposition DrawLayer(CapturedVisualLayer layer, double x, double y, VisualBlendMode blendMode,
        bool orderedDither = true, CancellationToken cancellationToken = default) {
        if (layer == null) throw new ArgumentNullException(nameof(layer));
        RasterEffectPlan.ValidateBlendMode(blendMode);
        var density = _canvas.EffectPixelsPerUnit;
        if (layer.PixelsPerUnit != density) throw new ArgumentException("Explicit layer blending requires matching device density.", nameof(layer));
        var left = RasterEffectGrid.Coordinate(x + layer.Bounds.X, density, nameof(x));
        var top = RasterEffectGrid.Coordinate(y + layer.Bounds.Y, density, nameof(y));
        cancellationToken.ThrowIfCancellationRequested();
        var backdrop = _canvas.BorrowDeviceSurfaceForEffects();
        var plan = RasterEffectPlan.Composite(new RasterEffectRegion(0, 0, backdrop.Width, backdrop.Height), layer.Image, left, top, blendMode);
        var batch = RasterEffectPreflight.Prepare(new[] { plan }, new[] { backdrop },
            additionalWorkSamples: checked((long)backdrop.Width * backdrop.Height * 4));
        var result = RasterEffects.Execute(batch, 0, orderedDither, cancellationToken);
        CommitEffect(result.Image.Pixels, cancellationToken);
        return this;
    }
}

using System;
using System.Threading;
using ChartForgeX.Effects;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.Composition;

public sealed partial class ImageComposition {
    /// <summary>Composites one explicit captured source over this destination frame with a selected alpha operator.</summary>
    /// <param name="source">Immutable fixed-density native capture.</param>
    /// <param name="x">Original child's left edge in composition logical coordinates.</param>
    /// <param name="y">Original child's top edge in composition logical coordinates.</param>
    /// <param name="operation">Required explicit Porter-Duff operation; blending remains a separate overload.</param>
    /// <param name="orderedDither">Uses the existing final RGBA8 Bayer quantizer; Copy preserves exact raw source bytes.</param>
    /// <param name="cancellationToken">Cancels before final atomic replacement, leaving the composition unchanged.</param>
    /// <returns>This fixed-frame composition.</returns>
    /// <remarks>Source outside its placed extent is transparent and participates throughout the destination frame.
    /// Copy clears outside the source extent; Clear clears the whole frame. This path requires matching density/grid.
    /// Source/destination arrays and all result work are preflighted before allocation. No implicit page backdrop exists.</remarks>
    public ImageComposition DrawLayer(CapturedVisualLayer source, double x, double y, VisualCompositeOperator operation,
        bool orderedDither = true, CancellationToken cancellationToken = default) {
        if (source == null) throw new ArgumentNullException(nameof(source));
        RasterEffectPlan.ValidateCompositeOperator(operation);
        var density = _canvas.EffectPixelsPerUnit;
        if (source.PixelsPerUnit != density)
            throw new ArgumentException("Explicit layer compositing requires matching device density.", nameof(source));
        var left = RasterEffectGrid.Coordinate(x + source.Bounds.X, density, nameof(x));
        var top = RasterEffectGrid.Coordinate(y + source.Bounds.Y, density, nameof(y));
        cancellationToken.ThrowIfCancellationRequested();
        var backdrop = _canvas.BorrowDeviceSurfaceForEffects();
        var plan = RasterEffectPlan.Composite(new RasterEffectRegion(0, 0, backdrop.Width, backdrop.Height), source.Image, left, top, operation);
        var batch = RasterEffectPreflight.Prepare(new[] { plan }, new[] { backdrop },
            additionalWorkSamples: checked((long)backdrop.Width * backdrop.Height * 4));
        var result = RasterEffects.Execute(batch, 0, orderedDither, cancellationToken);
        CommitEffect(result.Image.Pixels, cancellationToken);
        return this;
    }
}

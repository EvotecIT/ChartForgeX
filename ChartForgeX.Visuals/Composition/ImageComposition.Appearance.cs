using System;
using System.Threading;
using ChartForgeX.Effects;
using ChartForgeX.Raster;

namespace ChartForgeX.Composition;

public sealed partial class ImageComposition {
    /// <summary>Adds only the square-kernel alpha band outside the current source, clipped to this fixed frame.</summary>
    /// <param name="effect">Logical radius and tint. Zero radius or zero tint alpha preserves raw source pixels.</param>
    /// <param name="cancellationToken">Cancellation leaves the destination unchanged until one final commit.</param>
    /// <returns>The current composition.</returns>
    public ImageComposition Outline(VisualOutline effect, CancellationToken cancellationToken = default) {
        if (effect == null) throw new ArgumentNullException(nameof(effect));
        var radius = effect.DeviceRadius(_canvas.EffectPixelsPerUnit);
        cancellationToken.ThrowIfCancellationRequested();
        if (radius == 0 || effect.Color.A == 0) return this;
        var source = _canvas.BorrowDeviceSurfaceForEffects();
        var plan = RasterEffectPlan.Outline(new RasterEffectRegion(0, 0, source.Width, source.Height), effect.Color, radius);
        var batch = EffectBatch(source, plan, 20, finalBuffer: true);
        var result = RasterEffects.Execute(batch, 0, effect.OrderedDither, cancellationToken);
        CommitEffect(CropEffect(source, result, cancellationToken), cancellationToken);
        return this;
    }

    /// <summary>Blurs an explicit luminance bright pass and Screens it over the current fixed-frame pixels.</summary>
    /// <param name="effect">Required threshold and strength; sigma is resolved at this composition's device density once.</param>
    /// <param name="cancellationToken">Cancellation leaves the destination unchanged until one final commit.</param>
    /// <returns>The current composition.</returns>
    /// <remarks>No bright samples, threshold one or strength zero preserve raw pixels, including hidden RGB.</remarks>
    public ImageComposition Bloom(VisualBloom effect, CancellationToken cancellationToken = default) {
        if (effect == null) throw new ArgumentNullException(nameof(effect));
        var x = effect.HorizontalQ8(_canvas.EffectPixelsPerUnit);
        var y = effect.VerticalQ8(_canvas.EffectPixelsPerUnit);
        cancellationToken.ThrowIfCancellationRequested();
        if (effect.Threshold16 == 65535 || effect.Strength16 == 0) return this;
        var source = _canvas.BorrowDeviceSurfaceForEffects();
        var plan = RasterEffectPlan.Bloom(new RasterEffectRegion(0, 0, source.Width, source.Height),
            x, y, effect.Threshold16, effect.Strength16);
        var batch = EffectBatch(source, plan, 20, finalBuffer: true);
        var result = RasterEffects.Execute(batch, 0, effect.OrderedDither, cancellationToken);
        if (!ReferenceEquals(result.Image.Pixels, source.Pixels))
            CommitEffect(CropEffect(source, result, cancellationToken), cancellationToken);
        return this;
    }

    /// <summary>Replaces the explicit destination by its blur in proportion to detached mask coverage.</summary>
    /// <param name="coverage">Same-density, device-aligned coverage. Its dither option affects final mixed alpha only.</param>
    /// <param name="blur">Logical Gaussian targets. Its dither option affects the filtered RGBA result before mixing.</param>
    /// <param name="cancellationToken">Cancellation leaves the original destination unchanged before commit.</param>
    /// <returns>The current composition.</returns>
    /// <remarks>Replacement is (1-C)B+C*F in premultiplied channels and alpha, not source-over.
    /// No implicit host/page backdrop is sampled. Zero sigma or zero coverage preserves raw destination pixels.</remarks>
    public ImageComposition BlurRegion(VisualMask coverage, VisualLayerBlur blur,
        CancellationToken cancellationToken = default) {
        if (coverage == null) throw new ArgumentNullException(nameof(coverage));
        if (blur == null) throw new ArgumentNullException(nameof(blur));
        var density = _canvas.EffectPixelsPerUnit;
        if (coverage.PixelsPerUnit != density)
            throw new ArgumentException("Backdrop coverage and destination must have matching density.", nameof(coverage));
        var left = RasterEffectGrid.Coordinate(coverage.Bounds.X, density, nameof(coverage));
        var top = RasterEffectGrid.Coordinate(coverage.Bounds.Y, density, nameof(coverage));
        var x = blur.HorizontalQ8(density);
        var y = blur.VerticalQ8(density);
        cancellationToken.ThrowIfCancellationRequested();
        if (x == 0 && y == 0) return this;
        var source = _canvas.BorrowDeviceSurfaceForEffects();
        var plan = RasterEffectPlan.BlurRegion(new RasterEffectRegion(0, 0, source.Width, source.Height),
            coverage.Image, left, top, coverage.Mode, x, y, blur.OrderedDither);
        var batch = EffectBatch(source, plan, 4, finalBuffer: false);
        var result = RasterEffects.Execute(batch, 0, coverage.OrderedDither, cancellationToken);
        if (!ReferenceEquals(result.Image.Pixels, source.Pixels)) CommitEffect(result.Image.Pixels, cancellationToken);
        return this;
    }

    private static byte[] CropEffect(RgbaImage source, RasterEffectResult filtered, CancellationToken cancellationToken) =>
        RasterEffects.Crop(filtered, new RasterEffectRegion(0, 0, source.Width, source.Height), cancellationToken).Pixels;
}

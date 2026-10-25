using System;
using System.Threading;
using ChartForgeX.Effects;
using ChartForgeX.Raster;

namespace ChartForgeX.Rendering;

public sealed partial class CapturedVisualLayer {
    /// <summary>Masks these already captured pixels, including effect overflow, without repainting the original prepared source.</summary>
    /// <param name="mask">Detached mask in original child coordinates, matching this layer's device density and grid.</param>
    /// <param name="cancellationToken">Cancels before allocations, within rows and before publishing a new immutable layer.</param>
    /// <returns>A new static capture with unchanged Source, SourceBounds, Bounds, density and semantic identities.</returns>
    /// <remarks>Uncovered pixels become transparent. Positive output alpha preserves unassociated RGB exactly.
    /// The operation preflights both borrowed snapshots, output and the new private snapshot; existing layers stay unchanged.</remarks>
    public CapturedVisualLayer WithMask(VisualMask mask, CancellationToken cancellationToken = default) {
        var preflight = PrepareMask(mask, cancellationToken);
        var result = RasterEffects.Execute(preflight, 0, mask.OrderedDither, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var masks = new System.Collections.Generic.List<VisualMask>(PostCaptureMasks) { mask };
        var layer = new CapturedVisualLayer(Source, Plan, result.Image, Bounds, masks);
        cancellationToken.ThrowIfCancellationRequested();
        return layer;
    }
    /// <summary>One actual mask workflow plan. Tightened internal limits avoid allocating ceiling-sized validation fixtures.</summary>
    internal RasterEffectPreflight PrepareMask(VisualMask mask, CancellationToken cancellationToken = default,
        long maximumLiveBytes = RasterEffectPreflight.MaximumLiveBytes) {
        if (mask == null) throw new ArgumentNullException(nameof(mask));
        if (mask.PixelsPerUnit != PixelsPerUnit) throw new ArgumentException("Mask and layer must use the same device density.", nameof(mask));
        var x = RasterEffectGrid.Coordinate(mask.Bounds.X - Bounds.X, PixelsPerUnit, nameof(mask));
        var y = RasterEffectGrid.Coordinate(mask.Bounds.Y - Bounds.Y, PixelsPerUnit, nameof(mask));
        cancellationToken.ThrowIfCancellationRequested();
        var source = Image;
        var plan = RasterEffectPlan.Mask(new RasterEffectRegion(0, 0, source.Width, source.Height), mask.Image, x, y, mask.Mode,
            RasterEffectGrid.Coordinate(Bounds.X, PixelsPerUnit, nameof(Bounds)), RasterEffectGrid.Coordinate(Bounds.Y, PixelsPerUnit, nameof(Bounds)));
        var snapshot = checked((long)source.Width * source.Height * 4);
        var borrowed = new System.Collections.Generic.HashSet<byte[]>();
        VisualSceneRasterRenderer.CollectBorrowedNativeImages(Source.Scene, borrowed);
        if (Plan.Operation.Mask is { } firstMask) borrowed.Add(firstMask.Image.Pixels);
        foreach (var previous in PostCaptureMasks) borrowed.Add(previous.Image.Pixels);
        // The batch already charges these two current operands. Count every other retained raw input once.
        borrowed.Remove(source.Pixels);
        borrowed.Remove(mask.Image.Pixels);
        long retainedBytes = 0;
        foreach (var pixels in borrowed) retainedBytes = checked(retainedBytes + pixels.LongLength);
        return RasterEffectPreflight.Prepare(new[] { plan }, new[] { source },
            maximumLiveBytes: maximumLiveBytes, additionalPixelBytes: checked(snapshot + retainedBytes), additionalWorkSamples: checked(snapshot * 3));
    }

}

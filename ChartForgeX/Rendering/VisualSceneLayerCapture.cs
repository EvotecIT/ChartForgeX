using System;
using System.Collections.Generic;
using System.Threading;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.Rendering;

/// <summary>Maps one complete prepared native child to the existing bounded raster effect owner; no new painter or scene command.</summary>
internal static partial class VisualSceneLayerCapture {
    internal static CapturedVisualLayer Capture(PreparedVisual source, VisualLayerBlur effect, VisualRenderOptions options, CancellationToken cancellation) {
        cancellation.ThrowIfCancellationRequested();
        var plan = VisualSceneLayerCapturePlan.Create(source.Scene, effect, options);
        return Capture(source, plan, effect.OrderedDither, cancellation);
    }
    internal static CapturedVisualLayer Capture(PreparedVisual source, VisualDropShadow effect, VisualRenderOptions options, CancellationToken cancellation) {
        cancellation.ThrowIfCancellationRequested();
        var plan = VisualSceneLayerCapturePlan.Create(source.Scene, effect, options);
        return Capture(source, plan, effect.OrderedDither, cancellation);
    }
    internal static CapturedVisualLayer Capture(PreparedVisual source, VisualMask mask, VisualRenderOptions options, CancellationToken cancellation) {
        cancellation.ThrowIfCancellationRequested();
        var plan = VisualSceneLayerCapturePlan.Create(source.Scene, mask, options);
        return Capture(source, plan, mask.OrderedDither, cancellation);
    }
    private static CapturedVisualLayer Capture(PreparedVisual source, VisualSceneLayerCapturePlan plan, bool orderedDither, CancellationToken cancellation) {
        var options = plan.Options;
        // Keep the source's native painter, captured faces, rigid transforms and clips on their existing path.
        var image = VisualSceneRasterRenderer.Render(source.Scene, options.Scale, options.Supersampling, options.PixelBudget, options.TextHinting, cancellation);
        cancellation.ThrowIfCancellationRequested();
        var effectPlan = plan.Effect;
        var bounds = plan.LogicalBounds;
        if (effectPlan != null) {
            var preflight = RasterEffectPreflight.Prepare(new[] { effectPlan }, new[] { image },
                additionalPixelBytes: plan.AdditionalPixelBytes, additionalWorkSamples: plan.AdditionalWorkSamples);
            var result = RasterEffects.Execute(preflight, 0, orderedDither, cancellation);
            image = result.Image;
            bounds = new ChartRect((double)result.Region.X / options.Scale, (double)result.Region.Y / options.Scale,
                (double)result.Region.Width / options.Scale, (double)result.Region.Height / options.Scale);
        }
        cancellation.ThrowIfCancellationRequested();
        var captured = new CapturedVisualLayer(source, plan, image, bounds);
        cancellation.ThrowIfCancellationRequested();
        return captured;
    }
}

/// <summary>One immutable source/effect/snapshot pixel-array plan, checked before capture starts.</summary>
internal sealed class VisualSceneLayerCapturePlan {
    private VisualSceneLayerCapturePlan(VisualScene scene, VisualRenderOptions options, VisualLayerBlur? blur, VisualDropShadow? shadow, VisualMask? mask = null, VisualOutline? outline = null, VisualBloom? bloom = null, VisualInnerShadow? innerShadow = null) {
        Options = options;
        var allocation = VisualSceneRasterRenderer.CalculateAllocation(scene.Size, options.Scale, options.Supersampling, options.PixelBudget);
        SourceWidth = allocation.PixelWidth / options.Supersampling; SourceHeight = allocation.PixelHeight / options.Supersampling;
        var source = new RasterEffectRegion(0, 0, SourceWidth, SourceHeight);
        Operation = innerShadow != null ? VisualCapturedEffect.From(innerShadow)
            : mask != null ? VisualCapturedEffect.From(mask)
            : blur != null ? VisualCapturedEffect.From(blur)
            : outline != null ? VisualCapturedEffect.From(outline)
            : bloom != null ? VisualCapturedEffect.From(bloom)
            : VisualCapturedEffect.From(shadow ?? throw new ArgumentNullException(nameof(shadow)));
        Effect = Operation.Plan(source, scene.Size, options);
        var output = Effect?.ResultRegion ?? source;
        LogicalBounds = new ChartRect((double)output.X / options.Scale, (double)output.Y / options.Scale,
            (double)output.Width / options.Scale, (double)output.Height / options.Scale);
        SourcePixelBytes = checked((long)SourceWidth * SourceHeight * 4);
        var snapshotBytes = checked((long)output.Width * output.Height * 4);
        // Conservatively count the capture canvas even when it aliases its final output at density/coverage1.
        // Count every borrowed native scene-image allocation once, including images outside visible clips.
        var borrowedBytes = VisualSceneRasterRenderer.BorrowedNativeImageBytes(scene, Effect?.Operand?.Pixels);
        var opacity = VisualSceneRasterRenderer.CaptureEffectAccounting(scene, allocation, options);
        // A retained whole-view mask returns its owned result directly; it does not create
        // an additional enclosing canvas or return array before this explicit capture.
        var sourceCaptureBytes = opacity.ReturnsCapturedRoot
            ? checked(opacity.PixelBytes - SourcePixelBytes) : checked(allocation.ByteCount + opacity.PixelBytes);
        AdditionalPixelBytes = checked(sourceCaptureBytes + snapshotBytes + borrowedBytes);
        AdditionalWorkSamples = checked(opacity.WorkSamples + (long)output.Width * output.Height * 8 +
            (opacity.ReturnsCapturedRoot ? 0 : (long)allocation.PixelWidth * allocation.PixelHeight * 4 +
                (long)SourceWidth * SourceHeight * 8));
        RasterEffectPreflight.ValidateCapture(Effect, SourcePixelBytes, AdditionalPixelBytes, AdditionalWorkSamples,
            out var work, out var bytes);
        WorkSamples = work; ConservativePixelBytes = bytes;
    }
    internal static VisualSceneLayerCapturePlan Create(VisualScene scene, VisualLayerBlur effect, VisualRenderOptions options) => new(scene, options, effect, null);
    internal static VisualSceneLayerCapturePlan Create(VisualScene scene, VisualDropShadow effect, VisualRenderOptions options) => new(scene, options, null, effect);
    internal static VisualSceneLayerCapturePlan Create(VisualScene scene, VisualMask mask, VisualRenderOptions options) => new(scene, options, null, null, mask);
    internal static VisualSceneLayerCapturePlan Create(VisualScene scene, VisualOutline effect, VisualRenderOptions options) =>
        new(scene, options, null, null, outline: effect);
    internal static VisualSceneLayerCapturePlan Create(VisualScene scene, VisualBloom effect, VisualRenderOptions options) =>
        new(scene, options, null, null, bloom: effect);
    internal static VisualSceneLayerCapturePlan Create(VisualScene scene, VisualInnerShadow effect, VisualRenderOptions options) =>
        new(scene, options, null, null, innerShadow: effect);
    internal VisualCapturedEffect Operation { get; }
    internal VisualRenderOptions Options { get; }
    internal RasterEffectPlan? Effect { get; }
    internal int SourceWidth { get; }
    internal int SourceHeight { get; }
    internal ChartRect LogicalBounds { get; }
    internal long SourcePixelBytes { get; }
    internal long AdditionalPixelBytes { get; }
    internal long AdditionalWorkSamples { get; }
    internal long WorkSamples { get; }
    internal long ConservativePixelBytes { get; }
}

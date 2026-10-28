using System;
using System.Collections.Generic;
using System.Threading;
using ChartForgeX.Raster;

namespace ChartForgeX.Rendering;

/// <summary>One local completed-object capture through the canonical effect and native allocation owners.</summary>
internal sealed class VisualRetainedEffectPlan {
    internal VisualRetainedEffectPlan(VisualScene scene, int first, int last,
        VisualSceneRetainedEffect scope, VisualRenderOptions options) {
        Scope = scope; Options = options;
        var allocation = VisualSceneRasterRenderer.CalculateAllocation(scope.SourceSize,
            options.Scale, options.Supersampling, options.PixelBudget);
        var width = allocation.PixelWidth / options.Supersampling;
        var height = allocation.PixelHeight / options.Supersampling;
        Frame = new RasterEffectRegion(0, 0, width, height);
        Effect = scope.Effect.Plan(Frame, scope.SourceSize, options);
        var nodes = new List<VisualSceneNode>(last - first);
        for (var index = first; index < last; index++) nodes.Add(scene.Nodes[index]);
        Source = new VisualScene(scope.SourceSize, nodes, Array.Empty<VisualDiagnostic>(), Array.Empty<VisualSemanticRegion>());
        var nested = VisualSceneRasterRenderer.CaptureEffectAccounting(Source, allocation, options);
        PeakDepth = checked(nested.PeakDepth + 1);
        SourceBytes = checked((long)width * height * 4);
        CapturedSourceBytes = checked(nested.PixelBytes + (nested.ReturnsCapturedRoot ? 0 : allocation.ByteCount + SourceBytes));
        var output = Effect?.ResultRegion ?? Frame;
        var cropBytes = output.X != Frame.X || output.Y != Frame.Y || output.Width != Frame.Width || output.Height != Frame.Height
            ? SourceBytes : 0;
        AdditionalBytes = checked(CapturedSourceBytes - SourceBytes + cropBytes);
        AdditionalWork = checked(nested.WorkSamples + (nested.ReturnsCapturedRoot
            ? 0 : (long)allocation.PixelWidth * allocation.PixelHeight * 4 + SourceBytes * 2) + cropBytes * 2);
        SourceBorrowedBytes = VisualSceneRasterRenderer.BorrowedNativeImageBytes(Source, Effect?.Operand?.Pixels);
        RasterEffectPreflight.ValidateCapture(Effect, SourceBytes, checked(AdditionalBytes + SourceBorrowedBytes),
            AdditionalWork, out var work, out _);
        PixelBytes = checked(CapturedSourceBytes + (Effect?.AllocatedBytes ?? 0) + cropBytes);
        WorkSamples = work;
    }
    internal VisualSceneRetainedEffect Scope { get; }
    internal VisualRenderOptions Options { get; }
    internal VisualScene Source { get; }
    internal RasterEffectRegion Frame { get; }
    internal RasterEffectPlan? Effect { get; }
    internal long SourceBytes { get; }
    internal long AdditionalBytes { get; }
    internal long CapturedSourceBytes { get; }
    internal long AdditionalWork { get; }
    internal long SourceBorrowedBytes { get; }
    internal long PixelBytes { get; }
    internal long WorkSamples { get; }
    internal int PeakDepth { get; }

    internal RgbaImage Capture(CancellationToken cancellation) {
        cancellation.ThrowIfCancellationRequested();
        var image = VisualSceneRasterRenderer.Render(Source, Options.Scale, Options.Supersampling,
            Options.PixelBudget, Options.TextHinting, cancellation);
        if (Effect == null) return image;
        var preflight = RasterEffectPreflight.Prepare(new[] { Effect }, new[] { image },
            additionalPixelBytes: checked(AdditionalBytes + SourceBorrowedBytes), additionalWorkSamples: AdditionalWork);
        var result = RasterEffects.Execute(preflight, 0, Scope.Effect.OrderedDither, cancellation);
        cancellation.ThrowIfCancellationRequested();
        return RasterEffects.Crop(result, Frame, cancellation);
    }
}

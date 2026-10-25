using ChartForgeX.Raster;

namespace ChartForgeX.Rendering;

/// <summary>Native SVG resolves the same finite operation/support without allocating a raster child or kernel buffers.</summary>
internal sealed class VisualRetainedNativeEffectPlan {
    internal VisualRetainedNativeEffectPlan(VisualSceneRetainedEffect scope) {
        Scope = scope;
        var options = new VisualRenderOptions(1, 1);
        var allocation = VisualSceneRasterRenderer.CalculateAllocation(scope.SourceSize, options.Scale,
            options.Supersampling, options.PixelBudget);
        Effect = scope.Effect.Plan(new RasterEffectRegion(0, 0, allocation.PixelWidth, allocation.PixelHeight), scope.SourceSize, options);
    }
    internal VisualSceneRetainedEffect Scope { get; }
    internal RasterEffectPlan? Effect { get; }
}

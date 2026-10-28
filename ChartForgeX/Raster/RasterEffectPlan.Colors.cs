using System;

namespace ChartForgeX.Raster;

internal sealed partial class RasterEffectPlan {
    internal RasterColorTransform? ColorTransform { get; }
    private RasterEffectPlan(RasterEffectRegion source, RasterColorTransform transform) {
        if (transform == null) throw new ArgumentNullException(nameof(transform));
        _ = new RasterEffectRegion(source.X, source.Y, source.Width, source.Height);
        SourceRegion = OutputRegion = ResultRegion = MorphologyRegion = source;
        PixelOperation = RasterPixelOperation.ColorTransform; ColorTransform = transform;
        HorizontalRadii = Array.AsReadOnly(Array.Empty<int>()); VerticalRadii = Array.AsReadOnly(Array.Empty<int>());
        // One result array, source reads, straight-channel decode/math/encode and final writes. No timing guarantee.
        WorkSamples = checked(Pixels * 160L * transform.StageCount);
    }
    internal static RasterEffectPlan TransformColor(RasterEffectRegion source, RasterColorTransform transform) => new(source, transform);
}

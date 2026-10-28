using System.Threading;
using ChartForgeX.Effects;
using ChartForgeX.Raster;

namespace ChartForgeX.Rendering;

/// <summary>One export-local alpha-mask input, shared when the immutable native mask is reused.</summary>
internal sealed class VisualMaskSvgCoverage {
    private RgbaImage? _materialized;

    internal VisualMaskSvgCoverage(VisualMask mask) {
        Mask = mask;
        if (mask.Mode == VisualMaskMode.Luminance) {
            var plan = RasterEffectPlan.Mask(new RasterEffectRegion(0, 0, mask.Image.Width, mask.Image.Height),
                mask.Image, 0, 0, mask.Mode);
            PixelBytes = plan.AllocatedBytes;
            WorkSamples = plan.WorkSamples;
        }
    }

    internal VisualMask Mask { get; }
    internal long PixelBytes { get; }
    internal long WorkSamples { get; }

    internal RgbaImage Image(CancellationToken cancellation) {
        cancellation.ThrowIfCancellationRequested();
        if (Mask.Mode == VisualMaskMode.Alpha) return Mask.Image;
        if (_materialized is RgbaImage cached) return cached;
        var image = RasterMaskCoverage.MaterializeAlpha(Mask.Image, Mask.Mode,
            RasterEffectGrid.Coordinate(Mask.Bounds.X, Mask.PixelsPerUnit, nameof(Mask)),
            RasterEffectGrid.Coordinate(Mask.Bounds.Y, Mask.PixelsPerUnit, nameof(Mask)),
            Mask.OrderedDither, cancellation);
        _materialized = image;
        return image;
    }
}

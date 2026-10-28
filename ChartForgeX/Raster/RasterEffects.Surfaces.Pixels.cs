using System;
using System.Threading;

namespace ChartForgeX.Raster;

internal static partial class RasterEffects {
    private static RasterEffectSurface ExecuteSurfacePixels(RasterEffectSurface? input, RasterEffectPlan plan, CancellationToken cancellation) {
        var region = plan.SourceRegion;
        cancellation.ThrowIfCancellationRequested(); var pixels = new ushort[checked(region.Width * region.Height * 4)];
        var source = input.GetValueOrDefault(); var operand = plan.SurfaceOperand.GetValueOrDefault();
        var transform = plan.SurfaceColorTransform;
        var linearRgb = transform?.OutputLinearRgb ?? plan.SurfaceLinearRgb;
        for (var y = 0; y < region.Height; y++) {
            cancellation.ThrowIfCancellationRequested();
            for (var x = 0; x < region.Width; x++) {
                var at = (y * region.Width + x) * 4;
                var original = input.HasValue ? RasterEffectPixels.Read16(source.Pixels, at) : default;
                var other = plan.SurfaceOperand.HasValue ? RasterEffectPixels.Read16(operand.Pixels, at) : default;
                RasterEffectPixel16 pixel;
                switch (plan.SurfaceOperation) {
                    case RasterSurfaceOperation.Convert:
                        pixel = source.LinearRgb == linearRgb ? original : RasterEffectPixels.Convert16(original, linearRgb); break;
                    case RasterSurfaceOperation.Alpha: pixel = new RasterEffectPixel16(0, 0, 0, original.A); break;
                    case RasterSurfaceOperation.Offset:
                        var sx = (long)x - plan.OffsetX; var sy = (long)y - plan.OffsetY;
                        pixel = sx < 0 || sy < 0 || sx >= region.Width || sy >= region.Height ? default
                            : RasterEffectPixels.Read16(source.Pixels, checked(((int)sy * region.Width + (int)sx) * 4)); break;
                    case RasterSurfaceOperation.Flood:
                        pixel = plan.SurfaceFloodColor; break;
                    case RasterSurfaceOperation.Color:
                        var coverage = original.A / 65535d;
                        double Straight(int channel) => original.A == 0 ? 0 : channel / (double)original.A;
                        transform!.Apply(Straight(original.R), Straight(original.G), Straight(original.B), coverage,
                            out var red, out var green, out var blue, out var resultAlpha, cancellation);
                        pixel = RasterEffectPixels.Straight16(red, green, blue, resultAlpha); break;
                    case RasterSurfaceOperation.Blend: pixel = RasterEffectPixels.Blend16(other, original, plan.BlendMode); break;
                    case RasterSurfaceOperation.Composite: pixel = RasterEffectPixels.Composite16(other, original, plan.CompositeOperator); break;
                    case RasterSurfaceOperation.Arithmetic:
                        pixel = RasterEffectPixels.Arithmetic16(other, original, plan.SurfaceK1, plan.SurfaceK2, plan.SurfaceK3, plan.SurfaceK4); break;
                    default: throw new InvalidOperationException("The admitted surface operation has no pixel representation.");
                }
                RasterEffectPixels.Store16(pixels, at, pixel);
            }
        }
        cancellation.ThrowIfCancellationRequested();
        var alphaOnly = plan.SurfaceOperation == RasterSurfaceOperation.Alpha
            || plan.SurfaceOperation == RasterSurfaceOperation.Flood && plan.SurfaceFloodColor.R == 0 && plan.SurfaceFloodColor.G == 0 && plan.SurfaceFloodColor.B == 0
            || plan.SurfaceOperation == RasterSurfaceOperation.Convert && source.AlphaOnly
            || plan.SurfaceOperation == RasterSurfaceOperation.Offset && source.AlphaOnly
            || (plan.SurfaceOperation == RasterSurfaceOperation.Blend || plan.SurfaceOperation == RasterSurfaceOperation.Composite
                || plan.SurfaceOperation == RasterSurfaceOperation.Arithmetic && plan.SurfaceK4 == 0) && source.AlphaOnly && operand.AlphaOnly;
        return new RasterEffectSurface(region, pixels, linearRgb, alphaOnly);
    }
}

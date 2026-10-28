using System;
using System.Threading;

namespace ChartForgeX.Raster;

internal static partial class RasterEffects {
    /// <summary>Captures the actual admitted byte surface into the common premultiplied16 kernel seam once.</summary>
    internal static RasterEffectSurface CaptureSurface(RasterEffectPreflight preflight, CancellationToken cancellation) {
        cancellation.ThrowIfCancellationRequested();
        var plan = preflight.Take(0, out var source);
        if (plan.SurfaceOperation != RasterSurfaceOperation.Capture) throw new InvalidOperationException("A surface capture permit is required.");
        var region = plan.SourceRegion; var pixels = new ushort[checked(region.Width * region.Height * 4)];
        for (var y = 0; y < region.Height; y++) {
            cancellation.ThrowIfCancellationRequested();
            for (var x = 0; x < region.Width; x++) {
                var sx = (long)region.X + x; var sy = (long)region.Y + y;
                if (sx < 0 || sy < 0 || sx >= source.Width || sy >= source.Height) continue;
                var input = checked(((int)sy * source.Width + (int)sx) * 4); var alpha = source.Pixels[input + 3] * 257;
                RasterEffectPixels.Store16(pixels, (y * region.Width + x) * 4, new RasterEffectPixel16(
                    RasterEffectPixels.Premultiply(source.Pixels[input], alpha), RasterEffectPixels.Premultiply(source.Pixels[input + 1], alpha),
                    RasterEffectPixels.Premultiply(source.Pixels[input + 2], alpha), alpha));
            }
        }
        cancellation.ThrowIfCancellationRequested();
        return new RasterEffectSurface(region, pixels, false);
    }

    /// <summary>Executes one represented operation through the same preflight and kernels; graph ordering belongs to the existing SVG owner.</summary>
    internal static RasterEffectSurface ExecuteSurface(RasterEffectPreflight preflight, CancellationToken cancellation) {
        cancellation.ThrowIfCancellationRequested();
        var plan = preflight.TakeSurface(out var source);
        if (plan.SurfaceOperation == RasterSurfaceOperation.Blur) return BlurSurface(source!.Value, plan, cancellation);
        if (plan.SurfaceOperation == RasterSurfaceOperation.Morphology) return MorphologySurface(source!.Value, plan, cancellation);
        return ExecuteSurfacePixels(source, plan, cancellation);
    }

    private static RasterEffectSurface BlurSurface(RasterEffectSurface source, RasterEffectPlan plan, CancellationToken cancellation) {
        var expanded = plan.OutputRegion; var frame = plan.SourceRegion;
        cancellation.ThrowIfCancellationRequested();
        var first = new ushort[checked(expanded.Width * expanded.Height * 4)];
        cancellation.ThrowIfCancellationRequested(); var second = new ushort[first.Length];
        for (var y = 0; y < frame.Height; y++) {
            cancellation.ThrowIfCancellationRequested();
            Buffer.BlockCopy(source.Pixels, checked(y * frame.Width * 8), first,
                checked(((frame.Y + y - expanded.Y) * expanded.Width + frame.X - expanded.X) * 8), checked(frame.Width * 8));
        }
        Convolve16(ref first, ref second, plan, 4, cancellation);
        cancellation.ThrowIfCancellationRequested(); var pixels = new ushort[checked(frame.Width * frame.Height * 4)];
        for (var y = 0; y < frame.Height; y++) {
            cancellation.ThrowIfCancellationRequested();
            Buffer.BlockCopy(first, checked(((frame.Y + y - expanded.Y) * expanded.Width + frame.X - expanded.X) * 8),
                pixels, checked(y * frame.Width * 8), checked(frame.Width * 8));
        }
        cancellation.ThrowIfCancellationRequested();
        return new RasterEffectSurface(frame, pixels, source.LinearRgb, source.AlphaOnly);
    }

    private static RasterEffectSurface MorphologySurface(RasterEffectSurface source, RasterEffectPlan plan, CancellationToken cancellation) {
        var frame = source.Region; var channels = new ushort[4][];
        for (var channel = 0; channel < channels.Length; channel++) {
            cancellation.ThrowIfCancellationRequested();
            channels[channel] = RasterAlphaExtrema.Filter(source.Pixels, frame.Width, frame.Height, 4, channel,
                plan.SurfaceRadiusX, plan.SurfaceRadiusY, plan.SurfaceMaximize, cancellation);
        }
        cancellation.ThrowIfCancellationRequested(); var pixels = new ushort[checked(frame.Width * frame.Height * 4)];
        for (var y = 0; y < frame.Height; y++) {
            cancellation.ThrowIfCancellationRequested();
            for (var x = 0; x < frame.Width; x++) {
                var at = y * frame.Width + x;
                RasterEffectPixels.Store16(pixels, at * 4, new RasterEffectPixel16(channels[0][at], channels[1][at], channels[2][at], channels[3][at]));
            }
        }
        cancellation.ThrowIfCancellationRequested();
        return new RasterEffectSurface(frame, pixels, source.LinearRgb, source.AlphaOnly);
    }
}

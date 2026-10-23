using System;
using System.Threading;

namespace ChartForgeX.Raster;

/// <summary>Opt-in scalar kernels over detached premultiplied RGBA16 or alpha16 buffers; legacy canvas arithmetic is untouched.</summary>
internal static class RasterEffects {
    /// <summary>Runs one already-budgeted operation once. Source pixels must remain frozen until its capture completes.</summary>
    internal static RasterEffectResult Execute(RasterEffectPreflight preflight, int index, bool orderedDither, CancellationToken cancellation) {
        if (preflight == null) throw new ArgumentNullException(nameof(preflight));
        cancellation.ThrowIfCancellationRequested();
        var plan = preflight.Take(index, out var source);
        int width = plan.OutputRegion.Width, height = plan.OutputRegion.Height, channels = plan.Channels;
        var first = new ushort[checked(width * height * channels)];
        cancellation.ThrowIfCancellationRequested();
        var second = new ushort[first.Length];
        Capture(source, plan, first, cancellation);
        foreach (var radius in plan.HorizontalRadii) if (radius > 0) {
            BoxPass(first, second, width, height, channels, radius, true, cancellation); Swap(ref first, ref second);
        }
        foreach (var radius in plan.VerticalRadii) if (radius > 0) {
            BoxPass(first, second, width, height, channels, radius, false, cancellation); Swap(ref first, ref second);
        }
        cancellation.ThrowIfCancellationRequested();
        var result = plan.ResultRegion;
        var rgba = new byte[checked(result.Width * result.Height * 4)];
        for (int y = 0; y < result.Height; y++) {
            cancellation.ThrowIfCancellationRequested();
            for (int x = 0; x < result.Width; x++) {
                var output = (y * result.Width + x) * 4;
                int r, g, b, a;
                if (plan.Shadow.HasValue) {
                    var color = plan.Shadow.Value;
                    long fx = (long)result.X + x - plan.OutputRegion.X, fy = (long)result.Y + y - plan.OutputRegion.Y;
                    var coverage = fx >= 0 && fx < width && fy >= 0 && fy < height ? first[(int)fy * width + (int)fx] : 0;
                    a = RasterEffectPixels.Multiply(coverage, color.A * 257);
                    r = RasterEffectPixels.Premultiply(color.R, a); g = RasterEffectPixels.Premultiply(color.G, a); b = RasterEffectPixels.Premultiply(color.B, a);
                    if (plan.CompositeSource) {
                        var sx = (long)result.X + x; var sy = (long)result.Y + y;
                        if (sx >= 0 && sx < source.Width && sy >= 0 && sy < source.Height) {
                            var at = checked(((int)sy * source.Width + (int)sx) * 4);
                            if (a == 0 && source.Pixels[at + 3] > 0) { Buffer.BlockCopy(source.Pixels, at, rgba, output, 4); continue; }
                            RasterEffectPixels.SourceOver(ref r, ref g, ref b, ref a, source.Pixels, at);
                        }
                    }
                } else {
                    var input = (y * width + x) * 4; r = first[input]; g = first[input + 1]; b = first[input + 2]; a = first[input + 3];
                }
                RasterEffectPixels.Write(rgba, output, r, g, b, a, unchecked(result.X + x), unchecked(result.Y + y), orderedDither);
            }
        }
        return new RasterEffectResult(new RgbaImage(result.Width, result.Height, rgba), result);
    }

    /// <summary>Copies only the explicitly selected atomic source region; outside it and the source image is transparent black.</summary>
    private static void Capture(RgbaImage source, RasterEffectPlan plan, ushort[] target, CancellationToken cancellation) {
        var region = plan.OutputRegion; var selected = plan.SourceRegion;
        for (int y = 0; y < region.Height; y++) {
            cancellation.ThrowIfCancellationRequested();
            long sy = (long)region.Y + y - plan.OffsetY;
            if (sy < selected.Y || sy >= (long)selected.Y + selected.Height || sy < 0 || sy >= source.Height) continue;
            for (int x = 0; x < region.Width; x++) {
                long sx = (long)region.X + x - plan.OffsetX;
                if (sx < selected.X || sx >= (long)selected.X + selected.Width || sx < 0 || sx >= source.Width) continue;
                int input = checked(((int)sy * source.Width + (int)sx) * 4), output = (y * region.Width + x) * plan.Channels;
                var alpha = source.Pixels[input + 3] * 257;
                if (plan.Channels == 1) { target[output] = (ushort)alpha; continue; }
                target[output] = (ushort)RasterEffectPixels.Premultiply(source.Pixels[input], alpha);
                target[output + 1] = (ushort)RasterEffectPixels.Premultiply(source.Pixels[input + 1], alpha);
                target[output + 2] = (ushort)RasterEffectPixels.Premultiply(source.Pixels[input + 2], alpha); target[output + 3] = (ushort)alpha;
            }
        }
    }

    /// <summary>Fixed-denominator running sums implement transparent edges, with round-half-up after each one-dimensional pass.</summary>
    private static void BoxPass(ushort[] source, ushort[] target, int width, int height, int channels, int radius,
        bool horizontal, CancellationToken cancellation) {
        int lines = horizontal ? height : width, length = horizontal ? width : height, divisor = radius * 2 + 1;
        for (int line = 0; line < lines; line++) {
            cancellation.ThrowIfCancellationRequested();
            for (int channel = 0; channel < channels; channel++) {
                long sum = 0;
                for (int sample = 0; sample < length && sample <= radius; sample++) sum += source[At(line, sample, channel)];
                for (int sample = 0; sample < length; sample++) {
                    target[At(line, sample, channel)] = (ushort)((sum + divisor / 2) / divisor);
                    int remove = sample - radius, add = sample + radius + 1;
                    if (remove >= 0) sum -= source[At(line, remove, channel)];
                    if (add < length) sum += source[At(line, add, channel)];
                }
                int At(int row, int column, int component) => ((horizontal ? row * width + column : column * width + row) * channels) + component;
            }
        }
    }

    private static void Swap(ref ushort[] first, ref ushort[] second) { var previous = first; first = second; second = previous; }
}

internal readonly struct RasterEffectResult {
    internal RasterEffectResult(RgbaImage image, RasterEffectRegion region) { Image = image; Region = region; }
    internal RgbaImage Image { get; }
    internal RasterEffectRegion Region { get; }
}

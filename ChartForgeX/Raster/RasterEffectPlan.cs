using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

/// <summary>One bounded captured-raster operation; public composition surfaces remain thin.</summary>
internal sealed partial class RasterEffectPlan {
    internal const int MaximumSigmaQ8 = 128 * 256;
    internal const int MaximumDeviceDimension = 8192;
    internal const long MaximumPixels = 16_777_216;
    internal RasterEffectRegion SourceRegion { get; }
    internal RasterEffectRegion OutputRegion { get; }
    internal int OffsetX { get; }
    internal int OffsetY { get; }
    internal ChartColor? Shadow { get; }
    internal bool CompositeSource { get; }
    internal double ShadowOpacity { get; } = 1;
    internal RasterEffectRegion? ShadowVisibleRegion { get; }
    internal RasterEffectRegion ResultRegion { get; }
    internal int SpreadRadius { get; }
    internal RasterEffectRegion MorphologyRegion { get; }
    internal long MorphologyAllocatedBytes => SpreadRadius == 0 ? 0 : checked((long)MorphologyRegion.Width * MorphologyRegion.Height * 3
        + (long)Math.Max(MorphologyRegion.Width, MorphologyRegion.Height) * sizeof(int));
    // Two linear deque passes plus capture and zeroed intermediates. Each sample is inserted/removed at most once per pass.
    internal long MorphologyWorkSamples => SpreadRadius == 0 ? 0 : checked((long)MorphologyRegion.Width * MorphologyRegion.Height * 32
        + (long)SourceRegion.Width * SourceRegion.Height * 2 + (long)Math.Max(MorphologyRegion.Width, MorphologyRegion.Height));
    internal int Channels => Shadow.HasValue ? 1 : 4;
    internal IReadOnlyList<int> HorizontalRadii { get; }
    internal IReadOnlyList<int> VerticalRadii { get; }
    internal long Pixels => (long)OutputRegion.Width * OutputRegion.Height;
    // Includes both 16-bit ping-pong arrays and the final RGBA8 result. No pooling/GC timing is assumed.
    internal long AllocatedBytes => BackdropBlur != null
        ? checked(BackdropBlur.AllocatedBytes + Pixels * 4)
        : PixelOperation != RasterPixelOperation.None ? checked(Pixels * 4)
        : checked(Pixels * Channels * 4L + (long)ResultRegion.Width * ResultRegion.Height * 4 + MorphologyAllocatedBytes);
    internal long WorkSamples { get; }

    private RasterEffectPlan(RasterEffectRegion source, int sigmaXQ8, int sigmaYQ8, int offsetX, int offsetY, ChartColor? shadow, bool compositeSource = false, bool unionSource = false, int spreadRadius = 0, double shadowOpacity = 1, RasterEffectRegion? shadowClip = null, bool outline = false, int bloomThreshold = -1, int bloomStrength = 65535, bool innerShadow = false) {
        if (source.Width <= 0 || source.Height <= 0) throw new ArgumentOutOfRangeException(nameof(source));
        if (spreadRadius == int.MinValue || spreadRadius != 0 && !shadow.HasValue) throw new ArgumentOutOfRangeException(nameof(spreadRadius));
        if (double.IsNaN(shadowOpacity) || double.IsInfinity(shadowOpacity) || shadowOpacity < 0 || shadowOpacity > 1) {
            throw new ArgumentOutOfRangeException(nameof(shadowOpacity));
        }
        ShadowOpacity = shadowOpacity;
        IsOutline = outline; BloomThreshold16 = bloomThreshold; BloomStrength16 = bloomStrength;
        IsInnerShadow = innerShadow;
        SourceRegion = source; OffsetX = offsetX; OffsetY = offsetY; Shadow = shadow; CompositeSource = compositeSource;
        SpreadRadius = spreadRadius;
        var expansion = Math.Max(0, spreadRadius);
        MorphologyRegion = expansion == 0 ? source : new RasterEffectRegion(checked(source.X - expansion), checked(source.Y - expansion),
            checked(source.Width + expansion * 2), checked(source.Height + expansion * 2));
        HorizontalRadii = Array.AsReadOnly(BoxRadii(sigmaXQ8)); VerticalRadii = Array.AsReadOnly(BoxRadii(sigmaYQ8));
        int supportX = 0, supportY = 0, passes = 0;
        foreach (var radius in HorizontalRadii) { supportX += radius; if (radius > 0) passes++; }
        foreach (var radius in VerticalRadii) { supportY += radius; if (radius > 0) passes++; }
        OutputRegion = new RasterEffectRegion(checked(MorphologyRegion.X + offsetX - supportX), checked(MorphologyRegion.Y + offsetY - supportY),
            checked(MorphologyRegion.Width + supportX * 2), checked(MorphologyRegion.Height + supportY * 2));
        ShadowVisibleRegion = shadowClip.HasValue ? Intersect(OutputRegion, shadowClip.Value) : OutputRegion;
        var visible = ShadowVisibleRegion;
        if (unionSource) {
            var left = Math.Min(source.X, visible?.X??source.X); var top = Math.Min(source.Y, visible?.Y??source.Y);
            var right = Math.Max(checked(source.X + source.Width),visible.HasValue?checked(visible.Value.X+visible.Value.Width):checked(source.X+source.Width));
            var bottom = Math.Max(checked(source.Y + source.Height),visible.HasValue?checked(visible.Value.Y+visible.Value.Height):checked(source.Y+source.Height));
            ResultRegion = new RasterEffectRegion(left, top, checked(right - left), checked(bottom - top));
        } else ResultRegion = innerShadow ? SourceRegion : CompositeSource ? SourceRegion : visible??OutputRegion;
        // Conservative sample/write visits: capture, both zeroed arrays, final reads/writes,
        // and each pass including running-sum initialization. This is not a CPU-cycle estimate.
        WorkSamples = checked(Pixels * (Channels * (5L + passes * 5L) + 4)
            + (CompositeSource ? (long)ResultRegion.Width * ResultRegion.Height * 8 : 0) + MorphologyWorkSamples
            + (IsBloom ? (long)SourceRegion.Width * SourceRegion.Height * 48 + Pixels * 64 : 0)
            + (IsInnerShadow ? (long)SourceRegion.Width * SourceRegion.Height * 64 : 0));
        _ = RasterAllocationGuard.Calculate(OutputRegion.Width, OutputRegion.Height, 1, 1);
    }

    /// <summary>One shared finite sigma/density check and round-half-up conversion for explicit effect producers.</summary>
    internal static int SigmaQ8(double sigma, string parameter, int density = 1) {
        if (density <= 0) throw new ArgumentOutOfRangeException(nameof(density));
        var device = sigma * density;
        if (double.IsNaN(device) || double.IsInfinity(device) || sigma < 0 || device > 128)
            throw new ArgumentOutOfRangeException(parameter);
        return (int)Math.Floor(device * 256 + .5);
    }

    internal static RasterEffectPlan Blur(RasterEffectRegion source, int sigmaXQ8, int sigmaYQ8) => new(source, sigmaXQ8, sigmaYQ8, 0, 0, null);
    internal static RasterEffectPlan AlphaShadow(RasterEffectRegion source, int sigmaXQ8, int sigmaYQ8, int offsetX, int offsetY, ChartColor color) =>
        new(source, sigmaXQ8, sigmaYQ8, offsetX, offsetY, color);

    internal static RasterEffectPlan ShadowUnderSource(RasterEffectRegion source, int sigmaXQ8, int sigmaYQ8, int offsetX, int offsetY, ChartColor color) =>
        new(source, sigmaXQ8, sigmaYQ8, offsetX, offsetY, color, compositeSource: true);

    /// <summary>Complete-child alpha shadow with morphology and union support; legacy fixed-frame shadows remain unchanged.</summary>
    internal static RasterEffectPlan LayerShadow(RasterEffectRegion source, int sigmaXQ8, int sigmaYQ8, int offsetX, int offsetY,
        ChartColor color, int spreadRadius) => new(source, sigmaXQ8, sigmaYQ8, offsetX, offsetY, color,
            compositeSource: true, unionSource: true, spreadRadius: spreadRadius);

    /// <summary>Complete child source plus its independently clipped/tinted shadow.</summary>
    internal static RasterEffectPlan LayerShadow(RasterEffectRegion source, int sigmaXQ8, int sigmaYQ8,
        int offsetX, int offsetY, ChartColor color, int spreadRadius, double opacity, RasterEffectRegion? clip) =>
        new(source, sigmaXQ8, sigmaYQ8, offsetX, offsetY, color, compositeSource: true,
            unionSource: true, spreadRadius: spreadRadius, shadowOpacity: opacity, shadowClip: clip);

    /// <summary>Shadow-only support behind retained native children; no source pixels are copied into its output.</summary>
    internal static RasterEffectPlan SurfaceShadow(RasterEffectRegion source, int sigmaXQ8, int sigmaYQ8,
        int offsetX, int offsetY, ChartColor color, int spreadRadius, double opacity, RasterEffectRegion? clip) =>
        new(source, sigmaXQ8, sigmaYQ8, offsetX, offsetY, color, spreadRadius: spreadRadius,
            shadowOpacity: opacity, shadowClip: clip);

    private static RasterEffectRegion? Intersect(RasterEffectRegion first, RasterEffectRegion second) {
        var x = Math.Max(first.X, second.X);
        var y = Math.Max(first.Y, second.Y);
        var right = Math.Min(checked(first.X + first.Width), checked(second.X + second.Width));
        var bottom = Math.Min(checked(first.Y + first.Height), checked(second.Y + second.Height));
        return right <= x || bottom <= y ? null : new RasterEffectRegion(x, y, right - x, bottom - y);
    }

    /// <summary>Variance-matched odd box widths for a three-box Gaussian approximation; only setup uses integer square root.</summary>
    private static int[] BoxRadii(int sigmaQ8) {
        if (sigmaQ8 < 0 || sigmaQ8 > MaximumSigmaQ8) throw new ArgumentOutOfRangeException(nameof(sigmaQ8));
        var square = (long)sigmaQ8 * sigmaQ8;
        var lower = (int)(IntegerSquareRoot(4 * square + 65536) / 256);
        if (lower % 2 == 0) lower--; lower = Math.Max(1, lower);
        var denominator = (4L * lower + 4) * 65536;
        var numerator = (3L * lower * lower + 12L * lower + 9) * 65536 - 12 * square;
        var lowerCount = (int)Math.Max(0, Math.Min(3, (numerator + denominator / 2) / denominator));
        var result = new int[3];
        for (int index = 0; index < result.Length; index++) result[index] = ((index < lowerCount ? lower : lower + 2) - 1) / 2;
        return result;
    }

    private static long IntegerSquareRoot(long value) {
        long result = 0, bit = 1L << 62;
        while (bit > value) bit >>= 2;
        while (bit != 0) {
            if (value >= result + bit) { value -= result + bit; result = (result >> 1) + bit; }
            else result >>= 1;
            bit >>= 2;
        }
        return result;
    }
}

/// <summary>Explicit device-pixel capture bounds. Logical transforms/stroke coverage must be resolved by the producer before this seam.</summary>
internal readonly struct RasterEffectRegion {
    internal RasterEffectRegion(int x, int y, int width, int height) {
        if (width <= 0 || height <= 0 || width > RasterEffectPlan.MaximumDeviceDimension || height > RasterEffectPlan.MaximumDeviceDimension
            || (long)width * height > RasterEffectPlan.MaximumPixels) throw new ArgumentOutOfRangeException(nameof(width));
        _ = checked(x + width); _ = checked(y + height);
        X = x; Y = y; Width = width; Height = height;
    }
    internal int X { get; }
    internal int Y { get; }
    internal int Width { get; }
    internal int Height { get; }
}

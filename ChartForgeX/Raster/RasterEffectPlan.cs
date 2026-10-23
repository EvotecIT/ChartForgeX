using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

/// <summary>One bounded captured-raster operation; public composition surfaces remain thin.</summary>
internal sealed class RasterEffectPlan {
    internal const int MaximumSigmaQ8 = 128 * 256;
    internal const int MaximumDeviceDimension = 8192;
    internal const long MaximumPixels = 16_777_216;
    internal RasterEffectRegion SourceRegion { get; }
    internal RasterEffectRegion OutputRegion { get; }
    internal int OffsetX { get; }
    internal int OffsetY { get; }
    internal ChartColor? Shadow { get; }
    internal bool CompositeSource { get; }
    internal RasterEffectRegion ResultRegion => CompositeSource ? SourceRegion : OutputRegion;
    internal int Channels => Shadow.HasValue ? 1 : 4;
    internal IReadOnlyList<int> HorizontalRadii { get; }
    internal IReadOnlyList<int> VerticalRadii { get; }
    internal long Pixels => (long)OutputRegion.Width * OutputRegion.Height;
    // Includes both 16-bit ping-pong arrays and the final RGBA8 result. No pooling/GC timing is assumed.
    internal long AllocatedBytes => checked(Pixels * Channels * 4L + (long)ResultRegion.Width * ResultRegion.Height * 4);
    internal long WorkSamples { get; }

    private RasterEffectPlan(RasterEffectRegion source, int sigmaXQ8, int sigmaYQ8, int offsetX, int offsetY, ChartColor? shadow, bool compositeSource = false) {
        if (source.Width <= 0 || source.Height <= 0) throw new ArgumentOutOfRangeException(nameof(source));
        SourceRegion = source; OffsetX = offsetX; OffsetY = offsetY; Shadow = shadow; CompositeSource = compositeSource;
        HorizontalRadii = Array.AsReadOnly(BoxRadii(sigmaXQ8)); VerticalRadii = Array.AsReadOnly(BoxRadii(sigmaYQ8));
        int supportX = 0, supportY = 0, passes = 0;
        foreach (var radius in HorizontalRadii) { supportX += radius; if (radius > 0) passes++; }
        foreach (var radius in VerticalRadii) { supportY += radius; if (radius > 0) passes++; }
        OutputRegion = new RasterEffectRegion(checked(source.X + offsetX - supportX), checked(source.Y + offsetY - supportY),
            checked(source.Width + supportX * 2), checked(source.Height + supportY * 2));
        // Conservative sample/write visits: capture, both zeroed arrays, final reads/writes,
        // and each pass including running-sum initialization. This is not a CPU-cycle estimate.
        WorkSamples = checked(Pixels * (Channels * (5L + passes * 5L) + 4)
            + (CompositeSource ? (long)ResultRegion.Width * ResultRegion.Height * 8 : 0));
        _ = RasterAllocationGuard.Calculate(OutputRegion.Width, OutputRegion.Height, 1, 1);
    }

    internal static RasterEffectPlan Blur(RasterEffectRegion source, int sigmaXQ8, int sigmaYQ8) => new(source, sigmaXQ8, sigmaYQ8, 0, 0, null);
    internal static RasterEffectPlan AlphaShadow(RasterEffectRegion source, int sigmaXQ8, int sigmaYQ8, int offsetX, int offsetY, ChartColor color) =>
        new(source, sigmaXQ8, sigmaYQ8, offsetX, offsetY, color);

    internal static RasterEffectPlan ShadowUnderSource(RasterEffectRegion source, int sigmaXQ8, int sigmaYQ8, int offsetX, int offsetY, ChartColor color) =>
        new(source, sigmaXQ8, sigmaYQ8, offsetX, offsetY, color, compositeSource: true);

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

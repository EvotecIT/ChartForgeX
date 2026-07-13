using System;

namespace ChartForgeX.Raster;

internal static class RasterAllocationGuard {
    // A single canvas is often accompanied by encoder and downsampling buffers.
    // Keep the per-canvas ceiling well below the CLR's array limit so failures are
    // deterministic and actionable instead of becoming overflow or OOM failures.
    internal const long MaximumCanvasBytes = 512L * 1024 * 1024;

    internal static RasterAllocation Calculate(int width, int height, int supersamplingScale, int outputScale) {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), width, "Raster width must be positive.");
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), height, "Raster height must be positive.");
        if (supersamplingScale <= 0) throw new ArgumentOutOfRangeException(nameof(supersamplingScale), supersamplingScale, "Raster supersampling scale must be positive.");
        if (outputScale <= 0) throw new ArgumentOutOfRangeException(nameof(outputScale), outputScale, "Raster output scale must be positive.");

        long combinedScale;
        long pixelWidth;
        long pixelHeight;
        long byteCount;
        try {
            combinedScale = checked((long)supersamplingScale * outputScale);
            pixelWidth = checked((long)width * combinedScale);
            pixelHeight = checked((long)height * combinedScale);
            byteCount = checked(pixelWidth * pixelHeight * 4L);
        }
        catch (OverflowException) {
            throw new ArgumentOutOfRangeException(nameof(width), width, "Raster dimensions and scales exceed the supported allocation range.");
        }

        if (pixelWidth > int.MaxValue || pixelHeight > int.MaxValue || byteCount > MaximumCanvasBytes) {
            throw new ArgumentOutOfRangeException(
                nameof(width),
                width,
                "Raster dimensions and scales require " + byteCount + " bytes; a single canvas is limited to " + MaximumCanvasBytes + " bytes.");
        }

        return new RasterAllocation((int)combinedScale, (int)pixelWidth, (int)pixelHeight, (int)byteCount);
    }
}

internal readonly struct RasterAllocation {
    internal RasterAllocation(int combinedScale, int pixelWidth, int pixelHeight, int byteCount) {
        CombinedScale = combinedScale;
        PixelWidth = pixelWidth;
        PixelHeight = pixelHeight;
        ByteCount = byteCount;
    }

    internal int CombinedScale { get; }
    internal int PixelWidth { get; }
    internal int PixelHeight { get; }
    internal int ByteCount { get; }
}

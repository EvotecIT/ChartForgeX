using System;

namespace ChartForgeX.Raster;

internal static class AnimatedRasterMemoryBudget {
    internal const long MaximumRetainedBytes = 256L * 1024 * 1024;

    internal static long EncoderRetainedBytes(
        long width,
        long height,
        int frameCount,
        AnimatedRasterFormat format) {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (frameCount <= 0) throw new ArgumentOutOfRangeException(nameof(frameCount));
        var pixelCount = checked(width * height);
        switch (format) {
            case AnimatedRasterFormat.Gif:
                return checked(pixelCount * frameCount);
            case AnimatedRasterFormat.Apng:
                return checked(pixelCount * 4);
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported animated raster format.");
        }
    }
}

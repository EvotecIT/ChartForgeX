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
                return checked(
                    pixelCount * frameCount +
                    pixelCount * 2 +
                    GifCompressedFrameUpperBound(pixelCount) * 2 +
                    1024L * 1024);
            case AnimatedRasterFormat.Apng:
                return checked(
                    ApngWorkingBytes(width, height) +
                    ApngEncodedUpperBound(width, height, frameCount) * 3);
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported animated raster format.");
        }
    }

    /// <summary>Estimates concurrent APNG frame, filter, compression, and chunk buffers excluding encoded output.</summary>
    internal static long ApngWorkingBytes(long width, long height) {
        var pixelBytes = checked(width * height * 4);
        var rawFrameBytes = checked(pixelBytes + height);
        var compressedFrameBytes = DeflateBound(rawFrameBytes);
        return checked(
            pixelBytes +
            rawFrameBytes +
            compressedFrameBytes * 3 +
            1024L * 1024);
    }

    /// <summary>Calculates the encoded-output ceiling when chunks and the exact returned array coexist.</summary>
    internal static long MaximumStreamedApngBytes(long retainedWithoutOutput) {
        return MaximumStreamedEncodedBytes(retainedWithoutOutput);
    }

    /// <summary>Calculates the bounded GIF output ceiling when chunks and the returned array coexist.</summary>
    internal static long MaximumStreamedGifBytes(long retainedWithoutOutput) {
        return MaximumStreamedEncodedBytes(retainedWithoutOutput);
    }

    private static long MaximumStreamedEncodedBytes(long retainedWithoutOutput) {
        var available = checked(MaximumRetainedBytes - retainedWithoutOutput - BoundedChunkStream.ChunkSize);
        if (available <= 0) return 0;
        return Math.Min(int.MaxValue, available / 2);
    }

    private static long ApngEncodedUpperBound(long width, long height, int frameCount) {
        var rawFrameBytes = checked(width * height * 4 + height);
        var compressedFrameBytes = DeflateBound(rawFrameBytes);
        return checked(61 + frameCount * checked(54 + compressedFrameBytes));
    }

    private static long GifCompressedFrameUpperBound(long pixelCount) =>
        checked((checked((pixelCount * 2 + 1) * 9) + 7) / 8);

    private static long DeflateBound(long sourceBytes) =>
        checked(
            sourceBytes +
            (sourceBytes >> 12) +
            (sourceBytes >> 14) +
            (sourceBytes >> 25) +
            64);
}

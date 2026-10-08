using System;

namespace ChartForgeX.Raster;

internal static class AnimatedRasterMemoryBudget {
    internal const long MaximumRetainedBytes = 256L * 1024 * 1024;
    // Conservative 64-bit CLR sizes, including array headers and alignment.
    private const long ArrayOverheadBytes = 32;
    private const long CollectionOverheadBytes = 128;
    private const int RgbaImageBytes = 16;
    private const int GifIndexedFrameBytes = 24;

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
                // The histogram and palette sample list coexist during quantization;
                // row error buffers also scale with width, independently of pixel count.
                return checked(
                    ArrayBytes(pixelCount) * frameCount +
                    CollectionBytes(frameCount, GifIndexedFrameBytes) +
                    ArrayBytes(pixelCount) * 2 +
                    ArrayBytes(GifCompressedFrameUpperBound(pixelCount)) * 3 +
                    width * 48 +
                    8L * 1024 * 1024);
            case AnimatedRasterFormat.Apng:
                return checked(
                    ApngWorkingBytes(width, height) +
                    ArrayBytes(ApngEncodedUpperBound(width, height, frameCount)) * 3);
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported animated raster format.");
        }
    }

    /// <summary>Includes complete RGBA arrays, image descriptors, and an exactly sized frame collection.</summary>
    internal static long RgbaFramesRetainedBytes(long width, long height, int frameCount) {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (frameCount <= 0) throw new ArgumentOutOfRangeException(nameof(frameCount));
        return checked(ArrayBytes(checked(width * height * 4)) * frameCount + CollectionBytes(frameCount, RgbaImageBytes));
    }

    /// <summary>Includes copied borrowed-image descriptors and per-frame timing arrays, excluding input pixels.</summary>
    internal static long TimedFrameDescriptorBytes(int frameCount, RasterAnimationFormat format) {
        if (frameCount <= 0) throw new ArgumentOutOfRangeException(nameof(frameCount));
        if (format != RasterAnimationFormat.Gif && format != RasterAnimationFormat.Apng) throw new ArgumentOutOfRangeException(nameof(format));
        return checked(CollectionBytes(frameCount, RgbaImageBytes) + ArrayBytes(checked((long)frameCount * (format == RasterAnimationFormat.Gif ? 4 : 8))));
    }

    /// <summary>Includes the supersampled canvas retained while a renderer creates an RGBA frame.</summary>
    internal static long RenderWorkingBytes(long width, long height, int supersampling) {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (supersampling <= 0) throw new ArgumentOutOfRangeException(nameof(supersampling));
        return ArrayBytes(checked(width * height * 4 * supersampling * supersampling));
    }

    /// <summary>Estimates concurrent APNG frame, filter, compression, and chunk buffers excluding encoded output.</summary>
    internal static long ApngWorkingBytes(long width, long height) {
        var pixelBytes = checked(width * height * 4);
        var rawFrameBytes = checked(pixelBytes + height);
        var compressedFrameBytes = DeflateBound(rawFrameBytes);
        return checked(
            ArrayBytes(pixelBytes) +
            ArrayBytes(rawFrameBytes) +
            ArrayBytes(compressedFrameBytes) * 3 +
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
        var available = checked(MaximumRetainedBytes - retainedWithoutOutput - BoundedChunkStream.ChunkSize - 256);
        if (available <= 0) return 0;
        var provisionalOutput = Math.Min(int.MaxValue, available / 2);
        var chunkCount = checked((provisionalOutput + BoundedChunkStream.ChunkSize - 1) / BoundedChunkStream.ChunkSize);
        // Each retained chunk has an array header; List<byte[]> capacity may approach twice its count.
        return Math.Max(0, Math.Min(int.MaxValue, checked(available - chunkCount * (ArrayOverheadBytes + 16)) / 2));
    }

    private static long ArrayBytes(long payloadBytes) => checked(payloadBytes + ArrayOverheadBytes);

    private static long CollectionBytes(int count, int elementBytes) =>
        checked(ArrayBytes(checked((long)count * elementBytes)) + CollectionOverheadBytes);

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

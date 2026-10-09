using System;
using System.IO;
using System.Threading;

namespace ChartForgeX.Raster;

public static partial class RasterAnimationEncoder {
    /// <summary>Encodes a repeatable producer with memory bounded independently of frame count.</summary>
    /// <remarks>Frames, declared producer working memory and encoded output share the encoder's 256 MiB budget.</remarks>
    public static byte[] Encode(RasterAnimationSource source, RasterAnimationFormat format,
        RasterAnimationOptions? options = null, CancellationToken cancellationToken = default) {
        options = SnapshotOptions(options);
        var working = ValidateSource(source, format, options, cancellationToken);
        var maximum = AnimatedRasterMemoryBudget.MaximumStreamedApngBytes(working);
        if (maximum <= 0) throw new InvalidOperationException("Animation leaves no bounded memory for encoded output.");
        using var stream = new BoundedChunkStream(maximum);
        WriteSource(stream, source, format, options, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return stream.ToArray();
    }

    /// <summary>Writes a repeatable producer to a caller-owned stream, leaving it open.</summary>
    /// <remarks>Metadata is validated before writing. A producer failure, cancellation or I/O failure can leave partial output. GIF reads each frame twice to keep one stable palette.</remarks>
    public static void WriteTo(Stream stream, RasterAnimationSource source, RasterAnimationFormat format,
        RasterAnimationOptions? options = null, CancellationToken cancellationToken = default) {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (!stream.CanWrite) throw new ArgumentException("Animation output requires a writable stream.", nameof(stream));
        options = SnapshotOptions(options);
        ValidateSource(source, format, options, cancellationToken);
        WriteSource(stream, source, format, options, cancellationToken);
    }

    private static RasterAnimationOptions SnapshotOptions(RasterAnimationOptions? options) => new() {
        PlayCount = options?.PlayCount ?? 0, PngCompressionLevel = options?.PngCompressionLevel ?? 6
    };

    private static long ValidateSource(RasterAnimationSource source, RasterAnimationFormat format,
        RasterAnimationOptions? options, CancellationToken cancellationToken) {
        if (source == null) throw new ArgumentNullException(nameof(source));
        cancellationToken.ThrowIfCancellationRequested();
        if (format != RasterAnimationFormat.Gif && format != RasterAnimationFormat.Apng) throw new ArgumentOutOfRangeException(nameof(format));
        if (options != null && (options.PlayCount < 0 || format == RasterAnimationFormat.Gif && options.PlayCount > 65536)) throw new ArgumentOutOfRangeException(nameof(options));
        if (format == RasterAnimationFormat.Apng) PngWriter.ToCompressionLevel(options?.PngCompressionLevel ?? 6);
        var working = checked(source.AdditionalWorkingBytes + AnimatedRasterMemoryBudget.RgbaFramesRetainedBytes(source.Width, source.Height, 2) +
            (format == RasterAnimationFormat.Gif
                ? AnimatedRasterMemoryBudget.EncoderRetainedBytes(source.Width, source.Height, 1, AnimatedRasterFormat.Gif)
                : AnimatedRasterMemoryBudget.ApngWorkingBytes(source.Width, source.Height)));
        if (working > AnimatedRasterMemoryBudget.MaximumRetainedBytes) throw new InvalidOperationException("Animation producer and encoder buffers exceed 256 MiB. Reduce the canvas size.");
        return working;
    }

    private static void WriteSource(Stream stream, RasterAnimationSource source, RasterAnimationFormat format,
        RasterAnimationOptions? options, CancellationToken cancellationToken) {
        if (format == RasterAnimationFormat.Gif) {
            GifWriter.WriteSource(stream, source, options?.PlayCount ?? 0, cancellationToken);
        } else {
            var first = source.GetFrame(0, cancellationToken);
            var current = default(RasterAnimationFrame);
            ApngWriter.WriteCore(stream, source.Width, source.Height, source.FrameCount, options?.PlayCount ?? 0,
                index => { current = index == 0 ? first : source.GetFrame(index, cancellationToken); first = default; return current.Image; },
                _ => RasterFrameDelay.Apng(current.Duration), options?.PngCompressionLevel ?? 6, cancellationToken);
        }
        cancellationToken.ThrowIfCancellationRequested();
    }
}

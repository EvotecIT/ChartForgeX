using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace ChartForgeX.Raster;

/// <summary>Encodes complete RGBA canvases as GIF or animated PNG using the managed raster engines.</summary>
/// <remarks>
/// Frames must share their dimensions. Both containers preserve zero frame delays. GIF rounds positive
/// durations to the nearest 10 milliseconds, with a 10 millisecond minimum and 655.35 second maximum,
/// and treats alpha below 128 as transparent.
/// APNG preserves representable rational durations and otherwise rounds to a supported fraction,
/// with a 65,535 second maximum. Encoder working buffers and returned output share a 256 MiB ceiling;
/// caller-owned input pixel buffers are excluded. The frame list and pixel buffers must remain unchanged while encoding.
/// </remarks>
public static partial class RasterAnimationEncoder {
    /// <summary>Encodes frames into a new byte array.</summary>
    /// <param name="frames">One or more complete RGBA frames with matching dimensions.</param>
    /// <param name="format">The GIF or APNG container.</param>
    /// <param name="options">Playback and encoding options; omitted options repeat indefinitely with PNG compression level 6.</param>
    /// <param name="cancellationToken">Cancels validation and encoding between units of work.</param>
    /// <returns>The encoded animation.</returns>
    public static byte[] Encode(
        IReadOnlyList<RasterAnimationFrame> frames,
        RasterAnimationFormat format,
        RasterAnimationOptions? options = null,
        CancellationToken cancellationToken = default) {
        var animation = Prepare(frames, format, options, cancellationToken, out var workingBytes, out var pngCompressionLevel);
        var maximumOutputBytes = AnimatedRasterMemoryBudget.MaximumStreamedApngBytes(workingBytes);
        if (maximumOutputBytes <= 0) {
            throw new InvalidOperationException("The animation leaves no bounded memory for encoded output. Reduce the canvas size or frame count.");
        }
        using var stream = new BoundedChunkStream(maximumOutputBytes);
        WritePrepared(stream, animation, format, pngCompressionLevel, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return stream.ToArray();
    }

    /// <summary>Writes an animation to a caller-owned writable stream and leaves that stream open.</summary>
    /// <param name="stream">The destination stream. Validation completes before any bytes are written.</param>
    /// <param name="frames">One or more complete RGBA frames with matching dimensions.</param>
    /// <param name="format">The GIF or APNG container.</param>
    /// <param name="options">Playback and encoding options; omitted options repeat indefinitely with PNG compression level 6.</param>
    /// <param name="cancellationToken">Cancels validation and encoding between units of work.</param>
    /// <remarks>An I/O failure or cancellation during encoding can leave a partial animation in the stream.</remarks>
    public static void WriteTo(
        Stream stream,
        IReadOnlyList<RasterAnimationFrame> frames,
        RasterAnimationFormat format,
        RasterAnimationOptions? options = null,
        CancellationToken cancellationToken = default) {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (!stream.CanWrite) throw new ArgumentException("Animation output requires a writable stream.", nameof(stream));
        var animation = Prepare(frames, format, options, cancellationToken, out _, out var pngCompressionLevel);
        WritePrepared(stream, animation, format, pngCompressionLevel, cancellationToken);
    }

    private static AnimatedRasterFrames Prepare(
        IReadOnlyList<RasterAnimationFrame> frames,
        RasterAnimationFormat format,
        RasterAnimationOptions? options,
        CancellationToken cancellationToken,
        out long workingBytes,
        out int pngCompressionLevel) {
        cancellationToken.ThrowIfCancellationRequested();
        var plays = options?.PlayCount ?? 0;
        pngCompressionLevel = options?.PngCompressionLevel ?? 6;
        if (frames == null) throw new ArgumentNullException(nameof(frames));
        if (frames.Count == 0) throw new ArgumentException("Animation export requires at least one frame.", nameof(frames));
        if (format != RasterAnimationFormat.Gif && format != RasterAnimationFormat.Apng) {
            throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported animation format.");
        }
        if (plays < 0 || (format == RasterAnimationFormat.Gif && plays > 65536)) {
            throw new ArgumentOutOfRangeException(nameof(options), plays, "Play count must fit the selected animation container.");
        }
        if (format == RasterAnimationFormat.Apng) PngWriter.ToCompressionLevel(pngCompressionLevel);
        RasterAnimationFrame.ValidateImage(frames[0].Image);
        var width = frames[0].Image.Width;
        var height = frames[0].Image.Height;
        if (format == RasterAnimationFormat.Gif && (width > 65535 || height > 65535)) {
            throw new ArgumentException("GIF canvas dimensions must fit unsigned 16-bit fields.", nameof(frames));
        }
        try {
            workingBytes = checked(AnimatedRasterMemoryBudget.TimedFrameDescriptorBytes(frames.Count, format) + (format == RasterAnimationFormat.Gif
                ? AnimatedRasterMemoryBudget.EncoderRetainedBytes(width, height, frames.Count, AnimatedRasterFormat.Gif)
                : AnimatedRasterMemoryBudget.ApngWorkingBytes(width, height)));
        } catch (OverflowException) {
            throw new ArgumentException("Animation dimensions and frame count exceed the supported memory range.", nameof(frames));
        }
        if (workingBytes > AnimatedRasterMemoryBudget.MaximumRetainedBytes) {
            throw new InvalidOperationException("Animation encoder buffers exceed 256 MiB. Reduce the canvas size or frame count.");
        }
        return AnimatedRasterFrames.Create(frames, plays, format, cancellationToken);
    }

    private static void WritePrepared(Stream stream, AnimatedRasterFrames animation, RasterAnimationFormat format, int pngCompressionLevel, CancellationToken cancellationToken) {
        if (format == RasterAnimationFormat.Gif) {
            GifWriter.WriteRgba(stream, animation, cancellationToken);
        } else {
            ApngWriter.WriteRgba(stream, animation, pngCompressionLevel, cancellationToken);
        }
        cancellationToken.ThrowIfCancellationRequested();
    }
}

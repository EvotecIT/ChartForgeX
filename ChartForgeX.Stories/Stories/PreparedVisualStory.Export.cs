using System;
using System.IO;
using System.Threading;
using ChartForgeX.Raster;

namespace ChartForgeX.Stories;

public sealed partial class PreparedVisualStory {
    /// <summary>Encodes GIF with a global palette and bounded two-pass frame production.</summary>
    public byte[] ToGif(VisualStoryFrameOptions? options = null, CancellationToken cancellationToken = default) =>
        RasterAnimationEncoder.Encode(GifSource(options), RasterAnimationFormat.Gif,
            new RasterAnimationOptions { PlayCount = Playback.PlayCount }, cancellationToken);

    /// <summary>Encodes APNG while producing and releasing one frame at a time.</summary>
    public byte[] ToApng(VisualStoryFrameOptions? options = null, CancellationToken cancellationToken = default) =>
        RasterAnimationEncoder.Encode(FrameSource(options), RasterAnimationFormat.Apng,
            new RasterAnimationOptions { PlayCount = Playback.PlayCount }, cancellationToken);

    /// <summary>Writes an animation directly to a caller-owned stream without retaining the encoded file.</summary>
    public void WriteAnimation(Stream stream, RasterAnimationFormat format, VisualStoryFrameOptions? options = null,
        CancellationToken cancellationToken = default) {
        if (format != RasterAnimationFormat.Gif && format != RasterAnimationFormat.Apng) throw new ArgumentOutOfRangeException(nameof(format));
        RasterAnimationEncoder.WriteTo(stream, format == RasterAnimationFormat.Gif ? GifSource(options) : FrameSource(options),
            format, new RasterAnimationOptions { PlayCount = Playback.PlayCount }, cancellationToken);
    }

    private RasterAnimationSource GifSource(VisualStoryFrameOptions? options) {
        var sampling = options ?? new VisualStoryFrameOptions();
        var source = FrameSource(sampling);
        // Quantize boundaries rather than independent delays: a six-fps story does not drift by two centiseconds per second.
        var total = Math.Max(1, (long)Math.Round(Duration.TotalSeconds * 100, MidpointRounding.AwayFromZero));
        var count = source.FrameCount;
        while (count > 1 && GifBoundary(count - 1, sampling.FramesPerSecond) >= total) count--;
        return new RasterAnimationSource(source.Width, source.Height, count, (index, cancellation) => {
            cancellation.ThrowIfCancellationRequested();
            var frame = index == count - 1
                ? new RasterAnimationFrame(RenderAt(ContentDuration, sampling.OutputScale), TimeSpan.Zero)
                : source.GetFrame(index, cancellation);
            cancellation.ThrowIfCancellationRequested();
            var start = GifBoundary(index, sampling.FramesPerSecond);
            var end = index == count - 1 ? total : GifBoundary(index + 1, sampling.FramesPerSecond);
            return new RasterAnimationFrame(frame.Image, TimeSpan.FromMilliseconds((end - start) * 10));
        }, source.AdditionalWorkingBytes);
    }

    private static long GifBoundary(int index, int rate) => (long)Math.Round(index * 100d / rate, MidpointRounding.AwayFromZero);
}

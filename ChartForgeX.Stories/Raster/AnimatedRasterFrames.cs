using System;
using System.Collections.Generic;
using System.Threading;

namespace ChartForgeX.Raster;

internal sealed class AnimatedRasterFrames {
    private AnimatedRasterFrames(
        IReadOnlyList<RgbaImage> frames,
        int width,
        int height,
        int delayCentiseconds,
        int finalDelayCentiseconds,
        bool loop) : this(frames, width, height, delayCentiseconds, finalDelayCentiseconds, loop ? 0 : 1, null, null) { }

    private AnimatedRasterFrames(
        IReadOnlyList<RgbaImage> frames,
        int width,
        int height,
        int delayCentiseconds,
        int finalDelayCentiseconds,
        int playCount,
        int[]? gifDelays,
        RasterFrameDelay[]? apngDelays) {
        Frames = frames;
        Width = width;
        Height = height;
        DelayCentiseconds = delayCentiseconds;
        FinalDelayCentiseconds = finalDelayCentiseconds;
        PlayCount = playCount;
        _gifDelays = gifDelays;
        _apngDelays = apngDelays;
    }

    public IReadOnlyList<RgbaImage> Frames { get; }
    public int Width { get; }
    public int Height { get; }
    public int DelayCentiseconds { get; }
    public int FinalDelayCentiseconds { get; }
    public int PlayCount { get; }
    private readonly int[]? _gifDelays;
    private readonly RasterFrameDelay[]? _apngDelays;

    public static AnimatedRasterFrames Create(IReadOnlyList<RgbaImage> frames, int delayCentiseconds, bool loop, string formatName) {
        return Create(frames, delayCentiseconds, delayCentiseconds, loop, formatName);
    }

    public static AnimatedRasterFrames Create(
        IReadOnlyList<RgbaImage> frames,
        int delayCentiseconds,
        int finalDelayCentiseconds,
        bool loop,
        string formatName) {
        if (frames == null) throw new ArgumentNullException(nameof(frames));
        if (frames.Count == 0) throw new ArgumentException("Animated " + formatName + " export requires at least one frame.", nameof(frames));
        var width = frames[0].Width;
        var height = frames[0].Height;
        foreach (var frame in frames) {
            if (frame.Width != width || frame.Height != height) throw new ArgumentException("Animated " + formatName + " frames must have matching dimensions.", nameof(frames));
        }

        return new AnimatedRasterFrames(
            frames,
            width,
            height,
            ClampDelay(delayCentiseconds),
            ClampDelay(finalDelayCentiseconds),
            loop);
    }

    public int DelayForFrame(int index) =>
        _gifDelays != null ? _gifDelays[index] : index == Frames.Count - 1 ? FinalDelayCentiseconds : DelayCentiseconds;

    internal RasterFrameDelay ApngDelayForFrame(int index) =>
        _apngDelays != null ? _apngDelays[index] : new RasterFrameDelay(DelayForFrame(index), 100);

    internal static AnimatedRasterFrames Create(
        IReadOnlyList<RasterAnimationFrame> frames,
        int playCount,
        RasterAnimationFormat format,
        CancellationToken cancellationToken) {
        var images = new RgbaImage[frames.Count];
        var gifDelays = format == RasterAnimationFormat.Gif ? new int[frames.Count] : null;
        var apngDelays = format == RasterAnimationFormat.Apng ? new RasterFrameDelay[frames.Count] : null;
        var width = frames[0].Image.Width;
        var height = frames[0].Image.Height;
        for (var index = 0; index < frames.Count; index++) {
            cancellationToken.ThrowIfCancellationRequested();
            var frame = frames[index];
            RasterAnimationFrame.ValidateImage(frame.Image);
            if (frame.Image.Width != width || frame.Image.Height != height) {
                throw new ArgumentException("Animation frames must have matching canvas dimensions.", nameof(frames));
            }
            images[index] = frame.Image;
            if (gifDelays != null) gifDelays[index] = RasterFrameDelay.GifCentiseconds(frame.Duration);
            if (apngDelays != null) apngDelays[index] = RasterFrameDelay.Apng(frame.Duration);
        }
        return new AnimatedRasterFrames(images, width, height, 1, 1, playCount, gifDelays, apngDelays);
    }

    private static int ClampDelay(int delayCentiseconds) =>
        Math.Max(1, Math.Min(65535, delayCentiseconds));
}

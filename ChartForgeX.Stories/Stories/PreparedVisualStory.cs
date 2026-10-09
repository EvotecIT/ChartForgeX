using System;
using System.Collections.Generic;
using System.Threading;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.Stories;

/// <summary>A detached story observation with one clock for seeking, playback and export.</summary>
/// <remarks>Rendering clamps to one play. Repetition is an export or player policy; the endpoint remains the completed scene.</remarks>
public sealed partial class PreparedVisualStory {
    private readonly VisualStory _story;
    private readonly IReadOnlyList<VisualStoryChapter> _chapters;
    private readonly long _assetBytes;
    private readonly string _transcript;

    internal PreparedVisualStory(VisualStory story, VisualStoryPlaybackOptions playback) {
        if (story == null) throw new ArgumentNullException(nameof(story));
        story.Validate();
        Playback = playback;
        _story = Capture(story, out _assetBytes);
        _transcript = new VisualStoryTranscriptRenderer().Render(_story);
        var chapters = new List<VisualStoryChapter>();
        var start = TimeSpan.Zero;
        foreach (var scene in _story.Scenes) {
            var duration = TimeSpan.FromTicks((long)Math.Round(scene.DurationSeconds * TimeSpan.TicksPerSecond, MidpointRounding.AwayFromZero));
            chapters.Add(new VisualStoryChapter(scene.Id, scene.Title, start, duration));
            start += duration;
        }
        _chapters = chapters.AsReadOnly(); ContentDuration = start; Duration = start + playback.EndHold;
    }

    /// <summary>Gets the title captured during preparation.</summary>
    public string Title => _story.Title;
    /// <summary>Gets the logical width.</summary>
    public int Width => _story.Width;
    /// <summary>Gets the logical height.</summary>
    public int Height => _story.Height;
    /// <summary>Gets the authored duration before the completed-state hold.</summary>
    public TimeSpan ContentDuration { get; }
    /// <summary>Gets one complete play, including the completed-state hold.</summary>
    public TimeSpan Duration { get; }
    /// <summary>Gets immutable playback timing.</summary>
    public VisualStoryPlaybackOptions Playback { get; }
    /// <summary>Gets detached scene boundaries for seeking.</summary>
    public IReadOnlyList<VisualStoryChapter> Chapters => _chapters;

    /// <summary>Renders an independently owned RGBA frame at a timestamp within one play.</summary>
    public RgbaImage RenderAt(TimeSpan timestamp, int outputScale = 1) {
        return PrepareFrame(timestamp, outputScale).ToRgba(new VisualRenderOptions(outputScale, supersampling: 1));
    }

    /// <summary>Prepares a timestamp as native geometry shared by SVG and PNG backends.</summary>
    public PreparedVisual PrepareFrame(TimeSpan timestamp) => PrepareFrame(timestamp, 1);

    /// <summary>Exports a static vector frame at a timestamp; omission selects the completed poster.</summary>
    public string ToSvg(TimeSpan? timestamp = null) => PrepareFrame(timestamp ?? ContentDuration).ToSvg();

    private PreparedVisual PrepareFrame(TimeSpan timestamp, int outputScale) {
        if (timestamp < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timestamp));
        if (outputScale < 1 || outputScale > 4) throw new ArgumentOutOfRangeException(nameof(outputScale));
        EnsureRenderBudget(outputScale);
        var elapsed = Math.Min(timestamp.TotalSeconds, ContentDuration.TotalSeconds);
        var index = VisualStoryTimeline.FindScene(_story, elapsed, out var timing);
        var completed = timestamp >= ContentDuration;
        var current = NativeVisualStoryRenderer.Prepare(_story, index, completed ? null : elapsed - timing.Start, outputScale, _transcript);
        var transition = Math.Min(Playback.Transition.TotalSeconds, _story.Scenes[index].DurationSeconds);
        var remaining = timing.End - elapsed;
        if (index + 1 < _story.Scenes.Count && transition > 0 && remaining < transition) {
            var next = NativeVisualStoryRenderer.Prepare(_story, index + 1, 0, outputScale, _transcript);
            var renderOptions = new VisualRenderOptions(outputScale, supersampling: 1);
            var pixels = VisualStoryAnimatedRasterRenderer.CrossFade(current.ToRgba(renderOptions), next.ToRgba(renderOptions), 1 - remaining / transition);
            var builder = new VisualSceneBuilder(current.Size, ChartForgeX.Typography.FontSpec.FromFamily(_story.Theme.FontFamily));
            builder.Image(pixels, new ChartForgeX.Primitives.ChartRect(0, 0, Width, Height), role: "story-transition");
            return new PreparedVisual(builder.Build(), current.Accessibility);
        }
        return current;
    }

    /// <summary>Renders a PNG at a timestamp; omission selects the completed poster.</summary>
    public byte[] ToPng(TimeSpan? timestamp = null, int outputScale = 1) => PngWriter.WriteRgba(RenderAt(timestamp ?? ContentDuration, outputScale));

    /// <summary>Gets the captured accessible transcript, including complete source and terminal output.</summary>
    public string ToTranscript() => _transcript;

    /// <summary>Produces owning frames lazily, using rational cadence and exact tick durations.</summary>
    /// <remarks>The final sample always shows the completed state. Consumers can release each frame before requesting the next.</remarks>
    public IEnumerable<RasterAnimationFrame> Frames(VisualStoryFrameOptions? options = null, CancellationToken cancellationToken = default) {
        var source = FrameSource(options);
        for (var i = 0; i < source.FrameCount; i++) yield return source.GetFrame(i, cancellationToken);
    }

    /// <summary>Produces owning frames with the selected container's timing and Stories readability policy.</summary>
    public IEnumerable<RasterAnimationFrame> Frames(RasterAnimationFormat format, VisualStoryFrameOptions? options = null, CancellationToken cancellationToken = default) {
        var source = FrameSource(format, options);
        for (var i = 0; i < source.FrameCount; i++) yield return source.GetFrame(i, cancellationToken);
    }

    /// <summary>Creates a repeatable producer with container-specific timing, including GIF visibility and viewer delay limits.</summary>
    /// <remarks>Use this overload when passing prepared story frames to a generic GIF or APNG encoder.</remarks>
    public RasterAnimationSource FrameSource(RasterAnimationFormat format, VisualStoryFrameOptions? options = null) => format switch {
        RasterAnimationFormat.Gif => GifSource(options),
        RasterAnimationFormat.Apng => FrameSource(options),
        _ => throw new ArgumentOutOfRangeException(nameof(format))
    };

    /// <summary>Creates a repeatable frame source on the exact tick clock, suitable for timestamp observations and optional video encoders.</summary>
    /// <remarks>Use the format overload for GIF-specific cadence, quantization and readability validation.</remarks>
    public RasterAnimationSource FrameSource(VisualStoryFrameOptions? options = null) {
        var sampling = options ?? new VisualStoryFrameOptions();
        var count = FrameCount(sampling);
        return new RasterAnimationSource(checked(Width * sampling.OutputScale), checked(Height * sampling.OutputScale), count,
            (index, cancellation) => {
                cancellation.ThrowIfCancellationRequested();
                var start = SampleTicks(index, sampling.FramesPerSecond);
                var end = index + 1 == count ? Duration.Ticks : SampleTicks(index + 1, sampling.FramesPerSecond);
                var timestamp = index + 1 == count ? ContentDuration : TimeSpan.FromTicks(start);
                return new RasterAnimationFrame(RenderAt(timestamp, sampling.OutputScale), TimeSpan.FromTicks(end - start));
            }, ProducerWorkingBytes(sampling.OutputScale));
    }

    internal int FrameCount(VisualStoryFrameOptions options) {
        var count = checked((int)Math.Ceiling(Duration.TotalSeconds * options.FramesPerSecond));
        if (count > options.MaximumFrames) throw new InvalidOperationException("Story sampling requires " + count + " frames; reduce duration or frame rate, or increase the explicit frame budget.");
        EnsureSceneCoverage(Math.Max(1, count), options.FramesPerSecond);
        return Math.Max(1, count);
    }

    private static long SampleTicks(int index, int rate) => (long)index * TimeSpan.TicksPerSecond / rate;

    private long ProducerWorkingBytes(int outputScale) => checked(_assetBytes * 2 +
        AnimatedRasterMemoryBudget.RgbaFramesRetainedBytes((long)Width * outputScale, (long)Height * outputScale, 4) +
        PngVisualStoryRenderer.MaximumFittedTerminalWorkingBytes(_story, outputScale, Playback.Transition > TimeSpan.Zero));

    private void EnsureRenderBudget(int outputScale) {
        if (ProducerWorkingBytes(outputScale) + AnimatedRasterMemoryBudget.RgbaFramesRetainedBytes((long)Width * outputScale, (long)Height * outputScale, 1) > AnimatedRasterMemoryBudget.MaximumRetainedBytes)
            throw new InvalidOperationException("Story assets and render buffers exceed the 256 MiB payload budget. Reduce media, canvas size or scale.");
    }

    private void EnsureSceneCoverage(int count, int rate) {
        EnsureSceneCoverage(count, index => TimeSpan.FromTicks(SampleTicks(index, rate)));
    }

    private void EnsureSceneCoverage(int count, Func<int, TimeSpan> sampleStart, Func<int, TimeSpan>? renderTime = null, TimeSpan? displayEnd = null, TimeSpan[]? chapterSamples = null) {
        var failure = SceneCoverageFailure(count, sampleStart, renderTime, displayEnd, chapterSamples);
        if (failure != null) throw new InvalidOperationException(failure);
    }

    private string? SceneCoverageFailure(int count, Func<int, TimeSpan> sampleStart, Func<int, TimeSpan>? renderTime = null, TimeSpan? displayEnd = null, TimeSpan[]? chapterSamples = null) {
        var visibility = new ChapterVisibility[Chapters.Count];
        var endOfPlay = displayEnd ?? Duration;
        var cadence = count > 1 ? (sampleStart(1) - sampleStart(0)).TotalSeconds : endOfPlay.TotalSeconds;
        for (var i = 0; i < count; i++) {
            var start = sampleStart(i);
            var end = i + 1 == count ? endOfPlay : sampleStart(i + 1);
            var seconds = (end - start).TotalSeconds;
            var sample = i + 1 == count ? ContentDuration : renderTime?.Invoke(i) ?? start;
            var index = VisualStoryTimeline.FindScene(_story, Math.Min(sample.TotalSeconds, ContentDuration.TotalSeconds), out var timing);
            if (start < Chapters[index].Start) return "Frame cadence would reveal a scene before its boundary. Increase the frame rate or completed-state hold.";
            var transition = Math.Min(Playback.Transition.TotalSeconds, _story.Scenes[index].DurationSeconds);
            var opacity = transition > 0 && index + 1 < Chapters.Count ? Math.Max(0, Math.Min(1, (timing.End - sample.TotalSeconds) / transition)) : 1;
            visibility[index].Credit(opacity, seconds, start);
            if (index + 1 < Chapters.Count) {
                visibility[index + 1].Credit(1 - opacity, seconds, start);
            }
        }
        for (var i = 0; i < Chapters.Count; i++) {
            var required = Math.Max(.01, Math.Min(cadence, Chapters[i].Duration.TotalSeconds));
            if (visibility[i].MaximumOpacity < .5 || visibility[i].WeightedSeconds + 1e-9 < required)
                return "Frame cadence skips a readable scene: " + Chapters[i].Id + ". Increase the frame rate, scene duration or completed-state hold.";
            if (chapterSamples != null) chapterSamples[i] = visibility[i].FirstReadableSample;
        }
        return null;
    }

    private struct ChapterVisibility {
        internal double MaximumOpacity;
        internal double WeightedSeconds;
        internal TimeSpan FirstReadableSample;

        internal void Credit(double opacity, double seconds, TimeSpan sample) {
            if (MaximumOpacity < .5 && opacity >= .5) FirstReadableSample = sample;
            MaximumOpacity = Math.Max(MaximumOpacity, opacity);
            WeightedSeconds += seconds * opacity;
        }
    }

    private VisualStoryFrameOptions DefaultSvgSampling() {
        var normal = new VisualStoryFrameOptions();
        var count = Math.Max(1, checked((int)Math.Ceiling(Duration.TotalSeconds * normal.FramesPerSecond)));
        if (count > normal.MaximumFrames || SceneCoverageFailure(count, index => TimeSpan.FromTicks(SampleTicks(index, normal.FramesPerSecond))) == null) return normal;
        foreach (var rate in new[] { 12, 24, 30, 60 }) {
            var candidate = new VisualStoryFrameOptions(rate);
            count = Math.Max(1, checked((int)Math.Ceiling(Duration.TotalSeconds * rate)));
            if (count > candidate.MaximumFrames || SceneCoverageFailure(count, index => TimeSpan.FromTicks(SampleTicks(index, rate))) == null) return candidate;
        }
        return new VisualStoryFrameOptions(60);
    }

    private static VisualStory Capture(VisualStory story, out long assetBytes) {
        var copy = VisualStory.Create(story.Title).WithSize(story.Width, story.Height).WithTheme(story.Theme.Clone());
        copy.WithPanelReflow(story.ReflowPanels);
        if (story.Description.Length > 0) copy.WithDescription(story.Description);
        var surfaces = new Dictionary<VisualStorySurface, VisualStorySurface>();
        assetBytes = 0;
        foreach (var scene in story.Scenes) {
            var target = copy.Scene(scene.Id, scene.Title, scene.DurationSeconds, scene.Layout);
            foreach (var panel in scene.Panels) {
                if (!surfaces.TryGetValue(panel.Surface, out var surface)) {
                    switch (panel.Surface) {
                        case VisualStorySourceSurface source:
                            assetBytes = checked(assetBytes + (source.Timeline?.RetainedCharacters ?? source.Source.Text.Length) * 2L);
                            surface = source.Capture(); break;
                        case VisualStoryTerminalSurface terminal:
                            assetBytes = checked(assetBytes + terminal.AccessibleText.Length * 2L);
                            surface = terminal.Capture(); break;
                        case VisualStoryTextSurface text:
                            assetBytes = checked(assetBytes + text.Text.Length * 2L);
                            surface = new VisualStoryTextSurface(text.Text, text.Emphasized); break;
                        case VisualStoryMediaSurface media:
                            var bytes = checked(media.Raster.Width * (long)media.Raster.Height * 4);
                            assetBytes = checked(assetBytes + bytes + media.Svg.Length * 2L);
                            if (assetBytes > 256L * 1024 * 1024) throw new InvalidOperationException("Prepared story media exceeds the 256 MiB asset budget.");
                            var pixels = new byte[checked((int)bytes)];
                            Buffer.BlockCopy(media.Raster.Pixels, 0, pixels, 0, pixels.Length);
                            surface = new VisualStoryMediaSurface(new RgbaImage(media.Raster.Width, media.Raster.Height, pixels), media.AccessibleText, media.Svg);
                            break;
                        default: throw new InvalidOperationException("Unknown story surface.");
                    }
                    if (assetBytes > 128L * 1024 * 1024) throw new InvalidOperationException("Prepared story assets exceed 128 MiB. Split the story or reduce media.");
                    surfaces.Add(panel.Surface, surface);
                }
                target.Panel(panel.Id, surface, panel.Title, panel.Weight);
            }
        }
        foreach (var outcome in story.Outcomes) copy.Outcome(outcome.Id, outcome.Label, outcome.PanelId);
        return copy;
    }
}

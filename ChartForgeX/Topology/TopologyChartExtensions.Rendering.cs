using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ChartForgeX.Raster;

namespace ChartForgeX.Topology;

public static partial class TopologyChartExtensions {
    /// <summary>
    /// Renders the topology chart to SVG.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="options">Optional render options.</param>
    /// <returns>Complete SVG markup.</returns>
    public static string ToSvg(this TopologyChart chart, TopologyRenderOptions? options = null) => new TopologySvgRenderer().Render(chart, options);

    /// <summary>
    /// Renders the topology chart to an HTML fragment.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="options">Optional render options.</param>
    /// <returns>An embeddable HTML fragment.</returns>
    public static string ToHtmlFragment(this TopologyChart chart, TopologyRenderOptions? options = null) => new TopologyHtmlRenderer().RenderFragment(chart, options);

    /// <summary>
    /// Renders the topology chart to a complete HTML page.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="options">Optional render options.</param>
    /// <returns>A complete HTML page.</returns>
    public static string ToHtmlPage(this TopologyChart chart, TopologyRenderOptions? options = null) => new TopologyHtmlRenderer().RenderPage(chart, options);

    /// <summary>
    /// Renders the topology chart to PNG bytes.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="options">Optional render options.</param>
    /// <returns>A PNG image.</returns>
    public static byte[] ToPng(this TopologyChart chart, TopologyRenderOptions? options = null) => new TopologyPngRenderer().Render(chart, options);

    /// <summary>
    /// Renders the topology chart to animated GIF bytes by sampling topology motion frames.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="options">Optional render options. When motion is unset, the first scenario route is animated.</param>
    /// <returns>An animated GIF image.</returns>
    public static byte[] ToGif(this TopologyChart chart, TopologyRenderOptions? options = null) {
        return ToAnimatedRaster(chart, options, AnimatedRasterFormat.Gif);
    }

    /// <summary>
    /// Renders the topology chart to animated PNG bytes by sampling topology motion frames.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="options">Optional render options. When motion is unset, the first scenario route is animated.</param>
    /// <returns>An animated PNG image.</returns>
    public static byte[] ToApng(this TopologyChart chart, TopologyRenderOptions? options = null) {
        return ToAnimatedRaster(chart, options, AnimatedRasterFormat.Apng);
    }

    private static byte[] ToAnimatedRaster(TopologyChart chart, TopologyRenderOptions? options, AnimatedRasterFormat format) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        return AnimatedRasterEncoder.Encode(format, BuildMotionFrames(chart, options, format.GetDisplayName()));
    }

    private static void WriteAnimatedRasterCore(TopologyChart chart, Stream stream, TopologyRenderOptions? options, AnimatedRasterFormat format) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        AnimatedRasterEncoder.Write(stream, format, BuildMotionFrames(chart, options, format.GetDisplayName()));
    }

    private static void SaveAnimatedRaster(TopologyChart chart, string path, TopologyRenderOptions? options, AnimatedRasterFormat format) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        var frames = BuildMotionFrames(chart, options, format.GetDisplayName());
        using var stream = File.Create(path);
        AnimatedRasterEncoder.Write(stream, format, frames);
    }

    private static AnimatedRasterFrames BuildMotionFrames(TopologyChart chart, TopologyRenderOptions? options, string formatName) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        options ??= new TopologyRenderOptions();
        var originalMotion = options.Motion;
        var motion = (originalMotion ?? TopologyMotionOptions.RoutePulse()).Clone();
        motion.Validate();
        try {
            options.Motion = motion;
            if (options.Preset != TopologyViewPreset.Default) options.ApplyPreset(options.Preset);
            var validator = new TopologyChartValidator();
            var sourceValidation = validator.ValidateScenarioReferences(chart);
            if (!sourceValidation.IsValid) throw new TopologyValidationException(sourceValidation);

            var prepared = TopologyLayoutEngine.Prepare(chart, options.View, options);
            var validation = validator.Validate(prepared, validateScenarioReferences: false, options);
            if (!validation.IsValid) throw new TopologyValidationException(validation);

            var plan = TopologyMotionPlanner.Build(prepared, options);
            if (plan == null) throw new InvalidOperationException("Topology animated " + formatName + " export requires a motion route. Add scenario edge steps or use TopologyMotionOptions.RoutePulseForEdges(...).");
            var delay = Math.Max(1, (int)Math.Round(100.0 / motion.FramesPerSecond));
            var frameCount = RasterFrameCount(motion, delay);
            var frames = new List<RgbaImage>(frameCount);
            var renderer = new TopologyPngRenderer();
            var requestedWidth = (int)Math.Ceiling(chart.Viewport.Width);
            var requestedHeight = (int)Math.Ceiling(chart.Viewport.Height);
            for (var frame = 0; frame < frameCount; frame++) {
                motion.Progress = RasterFrameProgress(motion, frame, frameCount);
                frames.Add(renderer.RenderPreparedImage(prepared, options, requestedWidth, requestedHeight, plan));
            }

            return AnimatedRasterFrames.Create(frames, delay, motion.Loop, "topology motion");
        } finally {
            options.Motion = originalMotion;
        }
    }

    private static int RasterFrameCount(TopologyMotionOptions motion, int delayCentiseconds) {
        var rawFrameCount = Math.Ceiling(motion.DurationSeconds * 100.0 / delayCentiseconds);
        if (rawFrameCount > motion.MaximumRasterFrames) throw new ArgumentOutOfRangeException(nameof(TopologyMotionOptions.MaximumRasterFrames), rawFrameCount, "Topology animated raster export would exceed the configured motion frame limit.");
        return Math.Max(1, (int)rawFrameCount);
    }

    internal static double RasterFrameProgress(TopologyMotionOptions motion, int frame, int frameCount) {
        if (motion == null) throw new ArgumentNullException(nameof(motion));
        if (frameCount <= 0) throw new ArgumentOutOfRangeException(nameof(frameCount), frameCount, "Topology animated raster export requires at least one frame.");
        if (frame < 0 || frame >= frameCount) throw new ArgumentOutOfRangeException(nameof(frame), frame, "Topology animated raster frame index is outside the sampled frame range.");
        if (frameCount == 1) return motion.Loop ? 0 : 1;
        return motion.Loop ? frame / (double)frameCount : frame / (double)(frameCount - 1);
    }

    /// <summary>
    /// Saves the topology chart as SVG.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="path">The output path.</param>
    /// <param name="options">Optional render options.</param>
    public static void SaveSvg(this TopologyChart chart, string path, TopologyRenderOptions? options = null) => File.WriteAllText(path, chart.ToSvg(options), Encoding.UTF8);

    /// <summary>
    /// Saves the topology chart as a complete HTML page.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="path">The output path.</param>
    /// <param name="options">Optional render options.</param>
    public static void SaveHtml(this TopologyChart chart, string path, TopologyRenderOptions? options = null) => File.WriteAllText(path, chart.ToHtmlPage(options), Encoding.UTF8);

    /// <summary>
    /// Saves the topology chart as PNG.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="path">The output path.</param>
    /// <param name="options">Optional render options.</param>
    public static void SavePng(this TopologyChart chart, string path, TopologyRenderOptions? options = null) => File.WriteAllBytes(path, chart.ToPng(options));

    /// <summary>
    /// Saves the topology chart as an animated GIF by sampling topology motion frames.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="path">The output path.</param>
    /// <param name="options">Optional render options. When motion is unset, the first scenario route is animated.</param>
    public static void SaveGif(this TopologyChart chart, string path, TopologyRenderOptions? options = null) {
        SaveAnimatedRaster(chart, path, options, AnimatedRasterFormat.Gif);
    }

    /// <summary>
    /// Saves the topology chart as an animated PNG by sampling topology motion frames.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="path">The output path.</param>
    /// <param name="options">Optional render options. When motion is unset, the first scenario route is animated.</param>
    public static void SaveApng(this TopologyChart chart, string path, TopologyRenderOptions? options = null) {
        SaveAnimatedRaster(chart, path, options, AnimatedRasterFormat.Apng);
    }

    /// <summary>
    /// Writes the topology chart as an animated GIF stream by sampling topology motion frames.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="stream">The destination stream.</param>
    /// <param name="options">Optional render options. When motion is unset, the first scenario route is animated.</param>
    public static void WriteGif(this TopologyChart chart, Stream stream, TopologyRenderOptions? options = null) {
        WriteAnimatedRasterCore(chart, stream, options, AnimatedRasterFormat.Gif);
    }

    /// <summary>
    /// Writes the topology chart as an animated PNG stream by sampling topology motion frames.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="stream">The destination stream.</param>
    /// <param name="options">Optional render options. When motion is unset, the first scenario route is animated.</param>
    public static void WriteApng(this TopologyChart chart, Stream stream, TopologyRenderOptions? options = null) {
        WriteAnimatedRasterCore(chart, stream, options, AnimatedRasterFormat.Apng);
    }
}

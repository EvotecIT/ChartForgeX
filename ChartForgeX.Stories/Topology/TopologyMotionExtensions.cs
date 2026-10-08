using System;
using System.IO;
using ChartForgeX.Raster;

namespace ChartForgeX.Topology;

/// <summary>Adds animation exports to core topology models without storing motion in static render options.</summary>
public static class TopologyMotionExtensions {
    /// <summary>Prepares one detached native topology and adds explicit motion policy.</summary>
    public static TopologyMotionPresentation WithMotion(this TopologyChart chart, TopologyMotionOptions motion,
        TopologyRenderOptions? options = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        return new TopologyMotionPresentation(chart.Prepare(options), motion);
    }
    /// <summary>Prepares motion with a preferred route scenario independently of static highlighting.</summary>
    /// <remarks>The preference uses the normal active-scenario fallback policy. Explicit motion ScenarioId and EdgeIds
    /// remain authoritative. Hosts can clear static highlighting for interactive controls while retaining route selection.</remarks>
    public static TopologyMotionPresentation WithMotion(this TopologyChart chart, TopologyMotionOptions motion,
        TopologyRenderOptions? options, string? preferredScenarioId) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        return new TopologyMotionPresentation(chart.Prepare(options), motion, preferredScenarioId);
    }
    /// <summary>Adds motion to an already prepared topology without repeating layout.</summary>
    public static TopologyMotionPresentation WithMotion(this PreparedTopology prepared, TopologyMotionOptions motion) => new(prepared, motion);
    /// <summary>Samples topology route motion into animated GIF bytes.</summary>
    public static byte[] ToGif(this TopologyChart chart, TopologyRenderOptions? options = null, TopologyMotionOptions? motion = null) =>
        chart.WithMotion(motion ?? TopologyMotionOptions.RoutePulse(), options).ToGif();
    /// <summary>Samples topology route motion into animated PNG bytes.</summary>
    public static byte[] ToApng(this TopologyChart chart, TopologyRenderOptions? options = null, TopologyMotionOptions? motion = null) =>
        chart.WithMotion(motion ?? TopologyMotionOptions.RoutePulse(), options).ToApng();
    /// <summary>Saves sampled topology motion as GIF.</summary>
    public static void SaveGif(this TopologyChart chart, string path, TopologyRenderOptions? options = null, TopologyMotionOptions? motion = null) {
        if (path == null) throw new ArgumentNullException(nameof(path));
        var frames = chart.WithMotion(motion ?? TopologyMotionOptions.RoutePulse(), options).Frames();
        using var stream = File.Create(path);
        AnimatedRasterEncoder.Write(stream, AnimatedRasterFormat.Gif, frames);
    }
    /// <summary>Saves sampled topology motion as animated PNG.</summary>
    public static void SaveApng(this TopologyChart chart, string path, TopologyRenderOptions? options = null, TopologyMotionOptions? motion = null) {
        if (path == null) throw new ArgumentNullException(nameof(path));
        var frames = chart.WithMotion(motion ?? TopologyMotionOptions.RoutePulse(), options).Frames();
        using var stream = File.Create(path);
        AnimatedRasterEncoder.Write(stream, AnimatedRasterFormat.Apng, frames);
    }
    /// <summary>Writes sampled topology motion to a GIF stream.</summary>
    public static void WriteGif(this TopologyChart chart, Stream stream, TopologyRenderOptions? options = null, TopologyMotionOptions? motion = null) {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        AnimatedRasterEncoder.Write(stream, AnimatedRasterFormat.Gif,
            chart.WithMotion(motion ?? TopologyMotionOptions.RoutePulse(), options).Frames());
    }
    /// <summary>Writes sampled topology motion to an animated PNG stream.</summary>
    public static void WriteApng(this TopologyChart chart, Stream stream, TopologyRenderOptions? options = null, TopologyMotionOptions? motion = null) {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        AnimatedRasterEncoder.Write(stream, AnimatedRasterFormat.Apng,
            chart.WithMotion(motion ?? TopologyMotionOptions.RoutePulse(), options).Frames());
    }
    internal static double RasterFrameProgress(TopologyMotionOptions motion, int frame, int frameCount) {
        if (motion == null) throw new ArgumentNullException(nameof(motion));
        if (frameCount <= 0) throw new ArgumentOutOfRangeException(nameof(frameCount));
        if (frame < 0 || frame >= frameCount) throw new ArgumentOutOfRangeException(nameof(frame));
        if (frameCount == 1) return motion.Loop ? 0 : 1;
        return motion.Loop ? frame / (double)frameCount : frame / (double)(frameCount - 1);
    }
}

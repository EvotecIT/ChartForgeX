using System;
using System.Collections.Generic;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Topology;

/// <summary>A detached topology presentation that adds script-free motion to resolved static geometry.</summary>
public sealed class TopologyMotionPresentation : IStaticVisualSource {
    private readonly PreparedTopology _basis;
    private readonly TopologyMotionOptions _motion;
    private readonly ResolvedTopologyMotionVisual _visual;
    private readonly TopologyMotionSvgAdapter _animation;

    internal TopologyMotionPresentation(PreparedTopology basis, TopologyMotionOptions motion, string? preferredScenarioId = null) {
        _basis = basis ?? throw new ArgumentNullException(nameof(basis));
        _motion = (motion ?? throw new ArgumentNullException(nameof(motion))).Clone();
        _motion.Validate();
        var geometry = basis.Geometry;
        var planningOptions = geometry.Options;
        if (preferredScenarioId != null) {
            planningOptions = geometry.Options.CloneForRendering();
            planningOptions.ActiveScenarioId = preferredScenarioId;
        }
        var plan = TopologyMotionPlanner.Build(geometry.Chart, planningOptions, _motion, geometry.Routes)
            ?? throw new InvalidOperationException("Topology motion requires a scenario route or explicitly selected edges.");
        _visual = new ResolvedTopologyMotionVisual(plan, _motion, geometry);
        _animation = new TopologyMotionSvgAdapter(_visual);
    }

    /// <summary>Gets the resolved logical width shared by all exports.</summary>
    public double Width => _basis.Width;
    /// <summary>Gets the resolved logical height shared by all exports.</summary>
    public double Height => _basis.Height;
    /// <summary>Gets the unchanged static prepared scene beneath the motion.</summary>
    public PreparedVisual StaticVisual => _basis.Visual;

    /// <summary>Exports script-free animated SVG over the native resolved route plan.</summary>
    public string ToSvg() {
        var geometry = _basis.Geometry;
        var accessibility = StaticVisual.Accessibility;
        var prefix = VisualSvgOptions.NamespaceFromExternalId(geometry.Options.IdScope)
            ?? VisualSceneSvgRenderer.Identity(StaticVisual.Scene, accessibility.Name, accessibility.Description,
                accessibility.Language, accessibility.IsDecorative,
                new VisualSvgOptions(colorVariables: geometry.Options.SvgColorVariables,
                    linkTarget: geometry.Options.OpenLinksInNewTab ? VisualSvgLinkTarget.NewContext : VisualSvgLinkTarget.SameContext,
                    responsive: geometry.Options.UseResponsiveSvg)) + "-" + _animation.PolicyIdentity;
        return _animation.Compose(StaticVisual.ToSvg(accessibility, prefix));
    }

    /// <summary>Exports animated SVG with a caller-owned embedding namespace.</summary>
    public string ToSvg(string idScope) => _animation.Compose(StaticVisual.ToSvg(
        new VisualSvgOptions(VisualSvgOptions.NamespaceFromExternalId(idScope), _basis.Geometry.Options.SvgColorVariables,
            _basis.Geometry.Options.OpenLinksInNewTab ? VisualSvgLinkTarget.NewContext : VisualSvgLinkTarget.SameContext,
            responsive: _basis.Geometry.Options.UseResponsiveSvg)));

    /// <summary>Samples the same resolved route into a detached static scene without mutating the base scene.</summary>
    /// <param name="progress">Progress from zero to one. One retains the completed route for non-looping motion,
    /// and returns to the route start at the seam of looping motion.</param>
    public PreparedVisual Sample(double progress) {
        if (progress < 0 || progress > 1 || double.IsNaN(progress) || double.IsInfinity(progress))
            throw new ArgumentOutOfRangeException(nameof(progress), progress, "Topology motion progress must be between 0.0 and 1.0.");
        var geometry = _basis.Geometry;
        return StaticVisual.WithScene(TopologyMotionSceneAdapter.Sample(StaticVisual.Scene, geometry, _visual, progress),
            new VisualSvgOptions(VisualSvgOptions.NamespaceFromExternalId(geometry.Options.IdScope),
                geometry.Options.SvgColorVariables, geometry.Options.OpenLinksInNewTab
                    ? VisualSvgLinkTarget.NewContext : VisualSvgLinkTarget.SameContext, responsive: geometry.Options.UseResponsiveSvg));
    }

    /// <summary>Exports the configured progress sample as RGBA pixels.</summary>
    public RgbaImage ToRgbaImage() => Sample(_motion.Progress).ToRgba(_basis.RasterOptions);
    /// <summary>Exports the configured progress sample as PNG.</summary>
    public byte[] ToPng() => PngWriter.WriteRgba(ToRgbaImage());
    /// <summary>Exports a sampled animated GIF.</summary>
    public byte[] ToGif() => Encode(AnimatedRasterFormat.Gif);
    /// <summary>Exports a sampled animated PNG.</summary>
    public byte[] ToApng() => Encode(AnimatedRasterFormat.Apng);
    /// <summary>Exports the animated SVG inside an embeddable HTML wrapper.</summary>
    public string ToHtmlFragment() => "<div class=\"chartforgex-topology-motion\">" + ToSvg() + "</div>";
    /// <summary>Exports a complete HTML document containing the animated SVG.</summary>
    public string ToHtmlPage() => VisualArtifactRendering.WrapSvgPage(_basis.Title ?? "Topology motion", ToSvg(), _basis.Language);

    /// <summary>Exports animated HTML after a caller-owned transformation of the trusted SVG presentation.</summary>
    /// <remarks>Use this to compose optional SVG decorators without coupling the Stories package to them.</remarks>
    public string ToHtmlPage(Func<string, string> svgDecorator) {
        if (svgDecorator == null) throw new ArgumentNullException(nameof(svgDecorator));
        var svg = svgDecorator(ToSvg());
        if (string.IsNullOrWhiteSpace(svg)) throw new InvalidOperationException("The SVG decorator returned no presentation.");
        return VisualArtifactRendering.WrapSvgPage(_basis.Title ?? "Topology motion", svg, _basis.Language);
    }

    string IStaticVisualSource.RenderSvg(string idScope) => Sample(_motion.Progress).ToSvg(
        new VisualSvgOptions(VisualSvgOptions.NamespaceFromExternalId(idScope), _basis.Geometry.Options.SvgColorVariables,
            _basis.Geometry.Options.OpenLinksInNewTab ? VisualSvgLinkTarget.NewContext : VisualSvgLinkTarget.SameContext,
            responsive: _basis.Geometry.Options.UseResponsiveSvg));
    RgbaImage IStaticVisualSource.RenderRgba() => ToRgbaImage();

    private byte[] Encode(AnimatedRasterFormat format) {
        var frames = Frames(format, out var maximumEncodedBytes);
        return AnimatedRasterEncoder.EncodeBounded(format, frames, maximumEncodedBytes);
    }

    internal AnimatedRasterFrames Frames(AnimatedRasterFormat format, out long maximumEncodedBytes) {
        var delay = Math.Max(1, (int)Math.Round(100.0 / _motion.FramesPerSecond));
        var rawCount = Math.Ceiling(_motion.DurationSeconds * 100.0 / delay);
        if (rawCount > _motion.MaximumRasterFrames)
            throw new ArgumentOutOfRangeException(nameof(TopologyMotionOptions.MaximumRasterFrames), rawCount,
                "Topology animated raster export would exceed the configured motion frame limit.");
        var count = Math.Max(1, (int)rawCount);
        var raster = _basis.RasterOptions;
        var allocation = VisualSceneRasterRenderer.CalculateAllocation(StaticVisual.Size,
            raster.Scale, raster.Supersampling, raster.PixelBudget);
        var width = allocation.PixelWidth / raster.Supersampling;
        var height = allocation.PixelHeight / raster.Supersampling;
        var encoderBytes = format == AnimatedRasterFormat.Gif
            ? AnimatedRasterMemoryBudget.EncoderRetainedBytes(width, height, count, format)
            : AnimatedRasterMemoryBudget.ApngWorkingBytes(width, height);
        // Sampling and encoding occur consecutively while all completed frames remain retained.
        var retained = checked(AnimatedRasterMemoryBudget.RgbaFramesRetainedBytes(width, height, count) +
            Math.Max(AnimatedRasterMemoryBudget.RenderWorkingBytes(width, height, raster.Supersampling), encoderBytes));
        maximumEncodedBytes = AnimatedRasterMemoryBudget.MaximumStreamedApngBytes(retained);
        if (maximumEncodedBytes <= 0) {
            throw new InvalidOperationException("Animated topology would exceed 256 MiB of sampled frames, render buffers, encoder buffers, and encoded output. Lower the canvas size, supersampling, or frame count.");
        }
        var frames = new List<RgbaImage>(count);
        for (var index = 0; index < count; index++)
            frames.Add(Sample(TopologyMotionExtensions.RasterFrameProgress(_motion, index, count)).ToRgba(_basis.RasterOptions));
        return AnimatedRasterFrames.Create(frames, delay, _motion.Loop, "topology motion");
    }
}

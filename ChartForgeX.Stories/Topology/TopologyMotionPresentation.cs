using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Topology;

/// <summary>A detached topology presentation that adds script-free motion to resolved static geometry.</summary>
public sealed class TopologyMotionPresentation : IStaticVisualSource {
    private readonly PreparedTopology _basis;
    private readonly TopologyMotionOptions _motion;
    private readonly TopologyMotionPlan _plan;
    private readonly TopologyMotionSvgAdapter _animation;

    internal TopologyMotionPresentation(PreparedTopology basis, TopologyMotionOptions motion) {
        _basis = basis ?? throw new ArgumentNullException(nameof(basis));
        _motion = (motion ?? throw new ArgumentNullException(nameof(motion))).Clone();
        _motion.Validate();
        var geometry = basis.Geometry;
        _plan = TopologyMotionPlanner.Build(geometry.Chart, geometry.Options, _motion, geometry.Routes)
            ?? throw new InvalidOperationException("Topology motion requires a scenario route or explicitly selected edges.");
        string ColorFor(string? authored, TopologyHealthStatus status) {
            var selected = _motion.MarkerColor ?? _plan.Color ?? authored;
            return geometry.PaintColor(geometry.ResolveColor(selected, status),
                string.IsNullOrWhiteSpace(selected) ? SvgColorRole.Status : SvgColorRole.Any);
        }
        var nodes = _plan.NodeIds.Select(id => geometry.Chart.Nodes.FirstOrDefault(node => node.Id == id))
            .OfType<TopologyNode>().Where(node => geometry.NodeCenters.ContainsKey(node.Id))
            .Select(node => (node.Id, geometry.NodeCenters[node.Id], ColorFor(node.Color, node.Status))).ToArray();
        _animation = new TopologyMotionSvgAdapter(_plan, _motion,
            geometry.PaintColor(geometry.Background, SvgColorRole.Surface), edge => ColorFor(edge.Color, edge.Status),
            nodes, geometry.Scale);
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
    public PreparedVisual Sample(double progress) {
        var motion = _motion.Clone().AtProgress(progress);
        var geometry = _basis.Geometry;
        var sample = TopologyMotionPlanner.Sample(_plan, motion, geometry.Theme);
        var color = geometry.ResolveColor(sample.Color, sample.Status);
        var radius = motion.MarkerRadius * geometry.Scale;
        var builder = new VisualSceneBuilder(StaticVisual.Size, geometry.Context.Font);
        builder.Append(StaticVisual.Scene, 0, 0, "topology-frame");
        using (builder.PushClip(geometry.Clip)) {
            builder.Ellipse(sample.Point.X, sample.Point.Y, radius + 7 * geometry.Scale, radius + 7 * geometry.Scale,
                color.WithOpacity(42d / 255), role: "topology-motion-halo");
            builder.Ellipse(sample.Point.X, sample.Point.Y, radius + 3 * geometry.Scale, radius + 3 * geometry.Scale,
                geometry.Background.WithOpacity(230d / 255), role: "topology-motion-surface");
            builder.Ellipse(sample.Point.X, sample.Point.Y, radius, radius, color, role: "topology-motion-marker",
                paint: new VisualScenePaintBinding(SvgPaint.Of(color,
                    string.IsNullOrWhiteSpace(motion.MarkerColor) ? SvgColorRole.Status : SvgColorRole.Any), null));
            builder.Ellipse(sample.Point.X, sample.Point.Y, radius + 3 * geometry.Scale, radius + 3 * geometry.Scale,
                null, color.WithOpacity(180d / 255), 1.5 * geometry.Scale, "topology-motion-outline");
        }
        return StaticVisual.WithScene(builder.Build(),
            new VisualSvgOptions(VisualSvgOptions.NamespaceFromExternalId(geometry.Options.IdScope),
                geometry.Options.SvgColorVariables, geometry.Options.OpenLinksInNewTab
                    ? VisualSvgLinkTarget.NewContext : VisualSvgLinkTarget.SameContext, responsive: geometry.Options.UseResponsiveSvg));
    }

    /// <summary>Exports the configured progress sample as RGBA pixels.</summary>
    public RgbaImage ToRgbaImage() => Sample(_motion.Progress).ToRgba(_basis.RasterOptions);
    /// <summary>Exports the configured progress sample as PNG.</summary>
    public byte[] ToPng() => PngWriter.WriteRgba(ToRgbaImage());
    /// <summary>Exports a sampled animated GIF.</summary>
    public byte[] ToGif() => AnimatedRasterEncoder.Encode(AnimatedRasterFormat.Gif, Frames());
    /// <summary>Exports a sampled animated PNG.</summary>
    public byte[] ToApng() => AnimatedRasterEncoder.Encode(AnimatedRasterFormat.Apng, Frames());
    /// <summary>Exports the animated SVG inside an embeddable HTML wrapper.</summary>
    public string ToHtmlFragment() => "<div class=\"chartforgex-topology-motion\">" + ToSvg() + "</div>";
    /// <summary>Exports a complete HTML document containing the animated SVG.</summary>
    public string ToHtmlPage() => VisualArtifactRendering.WrapSvgPage(_basis.Title ?? "Topology motion", ToSvg(), _basis.Language);

    string IStaticVisualSource.RenderSvg(string idScope) => Sample(_motion.Progress).ToSvg(
        new VisualSvgOptions(VisualSvgOptions.NamespaceFromExternalId(idScope), _basis.Geometry.Options.SvgColorVariables,
            _basis.Geometry.Options.OpenLinksInNewTab ? VisualSvgLinkTarget.NewContext : VisualSvgLinkTarget.SameContext,
            responsive: _basis.Geometry.Options.UseResponsiveSvg));
    RgbaImage IStaticVisualSource.RenderRgba() => ToRgbaImage();

    internal AnimatedRasterFrames Frames() {
        var delay = Math.Max(1, (int)Math.Round(100.0 / _motion.FramesPerSecond));
        var rawCount = Math.Ceiling(_motion.DurationSeconds * 100.0 / delay);
        if (rawCount > _motion.MaximumRasterFrames)
            throw new ArgumentOutOfRangeException(nameof(TopologyMotionOptions.MaximumRasterFrames), rawCount,
                "Topology animated raster export would exceed the configured motion frame limit.");
        var count = Math.Max(1, (int)rawCount);
        var frames = new List<RgbaImage>(count);
        for (var index = 0; index < count; index++)
            frames.Add(Sample(TopologyMotionExtensions.RasterFrameProgress(_motion, index, count)).ToRgba(_basis.RasterOptions));
        return AnimatedRasterFrames.Create(frames, delay, _motion.Loop, "topology motion");
    }
}

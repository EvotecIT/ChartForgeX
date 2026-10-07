using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    internal PreparedVisual MotionFrame(PreparedVisual basis, TopologyMotionOptions motion, TopologyMotionPlan? plan = null) {
        var options = _options.CloneForRendering(); options.Motion = motion.Clone(); options.Motion.Validate();
        plan ??= MotionPlan(options);
        if (plan == null) throw new InvalidOperationException("Topology motion requires a scenario route or explicitly selected edges.");
        var sample = TopologyMotionPlanner.Sample(plan, options, Theme());
        var color = ChartColor.Parse(sample.Color); var radius = motion.MarkerRadius * _scale;
        var builder = new VisualSceneBuilder(basis.Size, _context.Font); builder.Append(basis.Scene, 0, 0, "topology-frame");
        using (builder.PushClip(_plot)) {
            builder.Ellipse(sample.Point.X, sample.Point.Y, radius + 7 * _scale, radius + 7 * _scale, color.WithOpacity(42d / 255), role: "topology-motion-halo");
            builder.Ellipse(sample.Point.X, sample.Point.Y, radius + 3 * _scale, radius + 3 * _scale, _colors.Background.WithOpacity(230d / 255), role: "topology-motion-surface");
            builder.Ellipse(sample.Point.X, sample.Point.Y, radius, radius, color, role: "topology-motion-marker",
                paint: Paint(color, string.IsNullOrWhiteSpace(motion.MarkerColor) ? SvgColorRole.Status : SvgColorRole.Any));
            builder.Ellipse(sample.Point.X, sample.Point.Y, radius + 3 * _scale, radius + 3 * _scale, null, color.WithOpacity(180d / 255), 1.5 * _scale, "topology-motion-outline");
        }
        return basis.WithScene(builder.Build(),
            new VisualSvgOptions(VisualSvgOptions.NamespaceFromExternalId(_options.IdScope), _options.SvgColorVariables,
                _options.OpenLinksInNewTab ? VisualSvgLinkTarget.NewContext : VisualSvgLinkTarget.SameContext));
    }

    internal TopologyMotionPlan? MotionPlan(TopologyRenderOptions options) {
        var routes = _routes.ToDictionary(entry => entry.Key, entry => (IReadOnlyList<ChartPoint>)entry.Value.Select(Point).ToArray(), StringComparer.Ordinal);
        return TopologyMotionPlanner.Build(_chart, options, routes);
    }
}

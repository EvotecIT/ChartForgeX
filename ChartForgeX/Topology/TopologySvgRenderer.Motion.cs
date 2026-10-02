using ChartForgeX.Themes;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

public sealed partial class TopologySvgRenderer {
    private static void AddMotionRouteLayer(SvgElement root, TopologyChart chart, string prefix, TopologyTheme theme, TopologyRenderOptions options, TopologyMotionPlan? plan) {
        if (plan == null) return;
        var nodes = chart.Nodes.ToDictionary(node => node.Id, System.StringComparer.Ordinal);
        var layer = new SvgElement("g")
            .Class(prefix + "__motion")
            .Attribute("data-cfx-role", "topology-motion")
            .Attribute("data-cfx-motion-source", plan.SourceId)
            .Attribute("data-cfx-motion-kind", "route-pulse");
        var duration = MotionDuration(options.Motion!);
        var tourPathId = TopologySvgIds.Element(chart, options, "motion-tour", plan.SourceId);
        layer.Element("path", path => path
            .Attribute("id", tourPathId)
            .Class(prefix + "__motion-tour-path")
            .Attribute("data-cfx-role", "topology-motion-tour-path")
            .Attribute("d", MotionTourPath(chart, nodes, options, plan))
            .Attribute("fill", "none")
            .Attribute("stroke", "none")
            .Attribute("opacity", "0"));
        for (var index = 0; index < plan.Entries.Count; index++) {
            var entry = plan.Entries[index];
            var edge = entry.Edge;
            var points = EdgePoints(chart, edge, nodes);
            var color = MotionColor(entry, plan, options, theme);
            var pathId = TopologySvgIds.Element(chart, options, "motion-route", edge.Id + "-" + index.ToString(CultureInfo.InvariantCulture));
            layer.Element("path", path => {
                path
                    .Attribute("id", pathId)
                    .Class(prefix + "__motion-route")
                    .Attribute("data-cfx-role", "topology-motion-route")
                    .Attribute("data-edge-id", edge.Id)
                    .Attribute("d", EdgePath(chart, edge, nodes, points, options))
                    .Attribute("fill", "none")
                    .Paint("stroke", color)
                    .Attribute("stroke-width", System.Math.Max(2.5, options.Motion!.MarkerRadius * 0.7))
                    .Attribute("stroke-linecap", "round")
                    .Attribute("stroke-linejoin", "round")
                    .Attribute("stroke-dasharray", "10 16")
                    .Attribute("opacity", "0.76");
                path.Element("animate", animate => animate
                    .Attribute("attributeName", "stroke-dashoffset")
                    .Attribute("from", "26")
                    .Attribute("to", "0")
                    .Attribute("dur", duration)
                    .Attribute("repeatCount", options.Motion!.Loop ? "indefinite" : "1")
                    .Attribute("fill", MotionAnimationFill(options.Motion)));
            });
        }

        root.AddElement(layer);
    }

    private static void AddMotionMarkerLayer(SvgElement root, TopologyChart chart, string prefix, TopologyTheme theme, TopologyRenderOptions options, TopologyMotionPlan? plan) {
        if (plan == null) return;
        var layer = new SvgElement("g")
            .Class(prefix + "__motion-overlay")
            .Attribute("data-cfx-role", "topology-motion-overlay")
            .Attribute("data-cfx-motion-source", plan.SourceId)
            .Attribute("data-cfx-motion-kind", "route-pulse");
        var duration = MotionDuration(options.Motion!);
        var tourPathId = TopologySvgIds.Element(chart, options, "motion-tour", plan.SourceId);
        layer.Element("circle", circle => {
            circle
                .Class(prefix + "__motion-marker")
                .Attribute("data-cfx-role", "topology-motion-marker")
                .Attribute("data-cfx-motion-source", plan.SourceId)
                .Attribute("r", options.Motion!.MarkerRadius)
                .Paint("fill", MotionMarkerColor(plan, options, theme))
                .Attribute("stroke", theme.Background)
                .Attribute("stroke-width", "2")
                .Attribute("opacity", "0.95");
            circle.Element("animateMotion", animate => {
                animate
                    .Attribute("dur", duration)
                    .Attribute("repeatCount", options.Motion!.Loop ? "indefinite" : "1")
                    .Attribute("fill", MotionAnimationFill(options.Motion))
                    .Attribute("rotate", "auto");
                animate.Element("mpath", mpath => mpath
                    .Attribute("xmlns:xlink", "http://www.w3.org/1999/xlink")
                    .Attribute("href", "#" + tourPathId)
                    .Attribute("xlink:href", "#" + tourPathId));
            });
        });

        foreach (var nodeId in plan.NodeIds.Distinct(System.StringComparer.Ordinal)) {
            var node = chart.Nodes.FirstOrDefault(candidate => string.Equals(candidate.Id, nodeId, System.StringComparison.Ordinal));
            if (node == null || EffectiveNodeDisplayMode(node, options) == TopologyNodeDisplayMode.Hidden) continue;
            var color = MotionNodeColor(node, plan, options, theme);
            layer.Element("circle", circle => {
                circle
                    .Class(prefix + "__motion-node")
                    .Attribute("data-cfx-role", "topology-motion-node")
                    .Attribute("data-node-id", node.Id)
                    .Attribute("cx", CenterX(node))
                    .Attribute("cy", CenterY(node))
                    .Attribute("r", options.Motion!.MarkerRadius + 2)
                    .Attribute("fill", "none")
                    .Paint("stroke", color)
                    .Attribute("stroke-width", "2")
                    .Attribute("opacity", "0.22");
                circle.Element("animate", animate => animate
                    .Attribute("attributeName", "r")
                    .Attribute("values", F(options.Motion!.MarkerRadius + 1) + ";" + F(options.Motion.MarkerRadius + 8) + ";" + F(options.Motion.MarkerRadius + 1))
                    .Attribute("dur", duration)
                    .Attribute("repeatCount", options.Motion.Loop ? "indefinite" : "1")
                    .Attribute("fill", MotionAnimationFill(options.Motion)));
                circle.Element("animate", animate => animate
                    .Attribute("attributeName", "opacity")
                    .Attribute("values", "0.18;0.62;0.18")
                    .Attribute("dur", duration)
                    .Attribute("repeatCount", options.Motion.Loop ? "indefinite" : "1")
                    .Attribute("fill", MotionAnimationFill(options.Motion)));
            });
        }

        root.AddElement(layer);
    }

    private static SvgPaint MotionColor(TopologyMotionEntry entry, TopologyMotionPlan plan, TopologyRenderOptions options, TopologyTheme theme) {
        if (!string.IsNullOrWhiteSpace(options.Motion?.MarkerColor)) return SvgPaint.Plain(options.Motion!.MarkerColor!.Trim());
        if (!string.IsNullOrWhiteSpace(plan.Color)) return SvgPaint.Plain(plan.Color!);
        if (!string.IsNullOrWhiteSpace(entry.Edge.Color)) return SvgPaint.Plain(entry.Edge.Color!.Trim());
        return StatusColorPaint(theme.StatusColor(entry.Edge.Status));
    }

    private static SvgPaint MotionMarkerColor(TopologyMotionPlan plan, TopologyRenderOptions options, TopologyTheme theme) {
        if (!string.IsNullOrWhiteSpace(options.Motion?.MarkerColor)) return SvgPaint.Plain(options.Motion!.MarkerColor!.Trim());
        if (!string.IsNullOrWhiteSpace(plan.Color)) return SvgPaint.Plain(plan.Color!);
        var first = plan.Entries.Count == 0 ? null : plan.Entries[0].Edge;
        if (!string.IsNullOrWhiteSpace(first?.Color)) return SvgPaint.Plain(first!.Color!.Trim());
        return first == null ? SvgPaint.Plain(theme.Accent) : StatusColorPaint(theme.StatusColor(first.Status));
    }

    private static string MotionTourPath(TopologyChart chart, IReadOnlyDictionary<string, TopologyNode> nodes, TopologyRenderOptions options, TopologyMotionPlan plan) {
        var builder = new StringBuilder(plan.Entries.Count * 64);
        var hasPoint = false;
        foreach (var entry in plan.Entries) {
            var points = EdgePoints(chart, entry.Edge, nodes);
            if (points.Count == 0) continue;
            if (builder.Length > 0) builder.Append(' ');
            builder.Append(EdgePath(chart, entry.Edge, nodes, points, options));
            hasPoint = true;
        }

        return hasPoint ? builder.ToString() : "M 0 0";
    }

    private static SvgPaint MotionNodeColor(TopologyNode node, TopologyMotionPlan plan, TopologyRenderOptions options, TopologyTheme theme) {
        if (!string.IsNullOrWhiteSpace(options.Motion?.MarkerColor)) return SvgPaint.Plain(options.Motion!.MarkerColor!.Trim());
        if (!string.IsNullOrWhiteSpace(plan.Color)) return SvgPaint.Plain(plan.Color!);
        if (!string.IsNullOrWhiteSpace(node.Color)) return SvgPaint.Plain(node.Color!.Trim());
        return StatusColorPaint(theme.StatusColor(node.Status));
    }

    private static string MotionDuration(TopologyMotionOptions motion) =>
        motion.DurationSeconds.ToString("0.###", CultureInfo.InvariantCulture) + "s";

    private static string MotionAnimationFill(TopologyMotionOptions motion) =>
        motion.Loop ? "remove" : "freeze";
}

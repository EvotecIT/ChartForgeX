using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
        var tourPathId = SafeElementId(chart.Id, "motion-tour", plan.SourceId);
        layer.Element("path", path => path
            .Attribute("id", tourPathId)
            .Class(prefix + "__motion-tour-path")
            .Attribute("data-cfx-role", "topology-motion-tour-path")
            .Attribute("d", MotionTourPath(plan))
            .Attribute("fill", "none")
            .Attribute("stroke", "none")
            .Attribute("opacity", "0"));
        for (var index = 0; index < plan.Entries.Count; index++) {
            var entry = plan.Entries[index];
            var edge = entry.Edge;
            var points = EdgePoints(chart, edge, nodes);
            var color = MotionColor(entry, plan, options, theme);
            var pathId = SafeElementId(chart.Id, "motion-route", edge.Id + "-" + index.ToString(CultureInfo.InvariantCulture));
            layer.Element("path", path => {
                path
                    .Attribute("id", pathId)
                    .Class(prefix + "__motion-route")
                    .Attribute("data-cfx-role", "topology-motion-route")
                    .Attribute("data-edge-id", edge.Id)
                    .Attribute("d", EdgePath(chart, edge, nodes, points, options))
                    .Attribute("fill", "none")
                    .Attribute("stroke", color)
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
                    .Attribute("repeatCount", options.Motion!.Loop ? "indefinite" : "1"));
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
        var tourPathId = SafeElementId(chart.Id, "motion-tour", plan.SourceId);
        layer.Element("circle", circle => {
            circle
                .Class(prefix + "__motion-marker")
                .Attribute("data-cfx-role", "topology-motion-marker")
                .Attribute("data-cfx-motion-source", plan.SourceId)
                .Attribute("r", options.Motion!.MarkerRadius)
                .Attribute("fill", MotionMarkerColor(plan, options, theme))
                .Attribute("stroke", theme.Background)
                .Attribute("stroke-width", "2")
                .Attribute("opacity", "0.95");
            circle.Element("animateMotion", animate => {
                animate
                    .Attribute("dur", duration)
                    .Attribute("repeatCount", options.Motion!.Loop ? "indefinite" : "1")
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
                    .Attribute("stroke", color)
                    .Attribute("stroke-width", "2")
                    .Attribute("opacity", "0.22");
                circle.Element("animate", animate => animate
                    .Attribute("attributeName", "r")
                    .Attribute("values", F(options.Motion!.MarkerRadius + 1) + ";" + F(options.Motion.MarkerRadius + 8) + ";" + F(options.Motion.MarkerRadius + 1))
                    .Attribute("dur", duration)
                    .Attribute("repeatCount", options.Motion.Loop ? "indefinite" : "1"));
                circle.Element("animate", animate => animate
                    .Attribute("attributeName", "opacity")
                    .Attribute("values", "0.18;0.62;0.18")
                    .Attribute("dur", duration)
                    .Attribute("repeatCount", options.Motion.Loop ? "indefinite" : "1"));
            });
        }

        root.AddElement(layer);
    }

    private static string MotionColor(TopologyMotionEntry entry, TopologyMotionPlan plan, TopologyRenderOptions options, TopologyTheme theme) {
        if (!string.IsNullOrWhiteSpace(options.Motion?.MarkerColor)) return options.Motion!.MarkerColor!.Trim();
        if (!string.IsNullOrWhiteSpace(plan.Color)) return plan.Color!;
        if (!string.IsNullOrWhiteSpace(entry.Edge.Color)) return entry.Edge.Color!.Trim();
        return theme.StatusColor(entry.Edge.Status);
    }

    private static string MotionMarkerColor(TopologyMotionPlan plan, TopologyRenderOptions options, TopologyTheme theme) {
        if (!string.IsNullOrWhiteSpace(options.Motion?.MarkerColor)) return options.Motion!.MarkerColor!.Trim();
        if (!string.IsNullOrWhiteSpace(plan.Color)) return plan.Color!;
        var first = plan.Entries.Count == 0 ? null : plan.Entries[0].Edge;
        if (!string.IsNullOrWhiteSpace(first?.Color)) return first!.Color!.Trim();
        return first == null ? theme.Accent : theme.StatusColor(first.Status);
    }

    private static string MotionTourPath(TopologyMotionPlan plan) {
        var builder = new SvgPathDataBuilder(plan.Entries.Count * 24);
        var hasPoint = false;
        foreach (var entry in plan.Entries) {
            if (entry.Points.Count == 0) continue;
            builder.MoveTo(entry.Points[0]);
            hasPoint = true;
            for (var i = 1; i < entry.Points.Count; i++) builder.LineTo(entry.Points[i]);
        }

        return hasPoint ? builder.Build() : "M 0 0";
    }

    private static string MotionNodeColor(TopologyNode node, TopologyMotionPlan plan, TopologyRenderOptions options, TopologyTheme theme) {
        if (!string.IsNullOrWhiteSpace(options.Motion?.MarkerColor)) return options.Motion!.MarkerColor!.Trim();
        if (!string.IsNullOrWhiteSpace(plan.Color)) return plan.Color!;
        if (!string.IsNullOrWhiteSpace(node.Color)) return node.Color!.Trim();
        return theme.StatusColor(node.Status);
    }

    private static string MotionDuration(TopologyMotionOptions motion) =>
        motion.DurationSeconds.ToString("0.###", CultureInfo.InvariantCulture) + "s";
}

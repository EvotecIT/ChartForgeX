using System;
using ChartForgeX.Svg;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

public sealed partial class TopologySvgRenderer {
    private static SvgElement BuildDiagramNodeBody(TopologyNode node, TopologyTheme theme, string color, TopologyRenderOptions options, bool selected) {
        var body = new SvgElement("g").Attribute("data-cfx-role", "topology-node-body").Attribute("data-node-id", node.Id);
        body.Element("path", path => path.Attribute("data-cfx-role", "diagram-node-surface").Attribute("data-node-shape", node.Shape!.Value.ToString())
            .Attribute("d", TopologyNodeShapeGeometry.Path(node)).Paint("fill", NodePaint(node, theme, color, options, NodeAccentRole(node, options)))
            .Paint("stroke", NodeAccentPaint(node, color, options)).Attribute("stroke-width", selected ? 2.8 : 1.5));
        if (!options.IncludeNodeLabels) return body;
        var subtitleOffset = string.IsNullOrWhiteSpace(node.Subtitle) ? 0 : 20;
        var lines = DiagramNodeLabelLines(node, options);
        for (var i = 0; i < lines.Count; i++) {
            var line = lines[i];
            body.Element("text", text => text.Attribute("x", CenterX(node)).Attribute("y", DiagramNodeLabelCenterY(node) - (lines.Count - 1) * 7 + i * 14 + 4)
                .Attribute("text-anchor", "middle").Attribute("fill", theme.Foreground).Attribute("font-size", 12).Attribute("font-weight", 700).Text(line));
        }
        if (subtitleOffset > 0) body.Element("text", text => text.Attribute("x", CenterX(node)).Attribute("y", node.Y + 44).Attribute("text-anchor", "middle")
            .Attribute("fill", theme.MutedForeground).Attribute("font-size", 10).Text(TrimToEstimatedWidth(node.Subtitle!, node.Width - 24, 10, false, options.TextMeasurement)));
        for (var i = 0; i < node.Details.Count; i++) {
            var detail = node.Details[i];
            if (i == 0) body.Element("line", line => line.Attribute("x1", node.X + 12).Attribute("x2", node.X + node.Width - 12).Attribute("y1", node.Y + 34 + subtitleOffset).Attribute("y2", node.Y + 34 + subtitleOffset).Attribute("stroke", theme.Border));
            var textValue = detail.Text ?? (detail.Label + " " + detail.Value);
            body.Element("text", text => text.Attribute("data-cfx-role", "topology-node-detail-label").Attribute("x", node.X + 12).Attribute("y", node.Y + 52 + subtitleOffset + i * 18)
                .Attribute("fill", theme.Foreground).Attribute("font-size", 10).Text(TrimToEstimatedWidth(textValue, node.Width - 24, 10, false, options.TextMeasurement)));
        }
        return body;
    }
}

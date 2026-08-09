using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

public sealed partial class TopologySvgRenderer {
    private static void AddEndpointLabels(SvgElement root, TopologyChart chart, string prefix, TopologyTheme theme, TopologyRenderOptions options, TopologyHighlightState highlight) {
        if (!options.IncludeEndpointLabels) return;
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var layer = new SvgElement("g").Class(prefix + "__endpoint-labels").Attribute("data-cfx-role", "topology-endpoint-labels");
        foreach (var edge in chart.Edges) {
            if (string.IsNullOrWhiteSpace(edge.SourceLabel) && string.IsNullOrWhiteSpace(edge.TargetLabel)) continue;
            var points = EdgePoints(chart, edge, nodes);
            if (points.Count < 2) continue;
            var opacity = highlight.IsEdgeHighlighted(edge) ? EdgeOpacity(edge, options) : EdgeOpacity(edge, options) * highlight.DimmedOpacity;
            if (!string.IsNullOrWhiteSpace(edge.SourceLabel)) AddEndpointLabel(layer, edge, edge.SourceLabel!, EdgeEndpointLabelPoint(points[0], points[1]), "source", prefix, theme, opacity);
            if (!string.IsNullOrWhiteSpace(edge.TargetLabel)) AddEndpointLabel(layer, edge, edge.TargetLabel!, EdgeEndpointLabelPoint(points[points.Count - 1], points[points.Count - 2]), "target", prefix, theme, opacity);
        }
        root.AddElement(layer);
    }

    private static void AddEndpointLabel(SvgElement layer, TopologyEdge edge, string text, ChartPoint point, string endpoint, string prefix, TopologyTheme theme, double opacity) {
        layer.Element("text", label => label
            .Class(prefix + "__endpoint-label")
            .Attribute("data-cfx-role", "topology-edge-endpoint-label")
            .Attribute("data-edge-id", edge.Id)
            .Attribute("data-endpoint", endpoint)
            .Attribute("x", point.X)
            .Attribute("y", point.Y)
            .Attribute("fill", theme.MutedForeground)
            .Attribute("font-size", 9.5)
            .Attribute("font-weight", 600)
            .Attribute("text-anchor", "middle")
            .Attribute("dominant-baseline", "middle")
            .Attribute("paint-order", "stroke")
            .Attribute("stroke", theme.Background)
            .Attribute("stroke-width", 3)
            .Attribute("stroke-linejoin", "round")
            .Attribute("opacity", opacity)
            .Text(text.Trim()));
    }

}

using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

public sealed partial class TopologySvgRenderer {
    private static void AddPremiumEdgePath(SvgElement edgeGroup, TopologyChart chart, TopologyEdge edge, IReadOnlyDictionary<string, TopologyNode> nodes, IReadOnlyList<ChartPoint> points, string prefix, TopologyRenderOptions options, string svgId, bool selected, string color, string dash) {
        var pathData = EdgePath(chart, edge, nodes, points, options);
        var style = EdgeVisualStyle(edge, selected, options);
        if (!ChartColor.TryParse(color, out var edgeColor)) {
            AddCssColorPremiumEdgePathLayers(edgeGroup, edge, prefix, options, svgId, selected, color, dash, pathData, style);
            return;
        }

        foreach (var layer in ChartLineVisualLayers.Build(edgeColor, EdgeStrokeWidth(edge, selected, options), style)) {
            if (!layer.IsVisible) continue;
            AddPremiumEdgePathLayer(edgeGroup, edge, prefix, options, svgId, selected, color, dash, pathData, layer);
        }
    }

    private static void AddCssColorPremiumEdgePathLayers(SvgElement edgeGroup, TopologyEdge edge, string prefix, TopologyRenderOptions options, string svgId, bool selected, string color, string dash, string pathData, ChartLineVisualStyle style) {
        var strokeWidth = EdgeStrokeWidth(edge, selected, options);
        if (style.AmbientHaloOpacity > 0 && style.AmbientHaloStrokeExtra > 0) AddCssColorPremiumEdgePathLayer(edgeGroup, edge, prefix, options, svgId, color, dash, pathData, "-ambient-halo", color, strokeWidth + style.AmbientHaloStrokeExtra, style.AmbientHaloOpacity, false);
        if (style.HaloOpacity > 0 && style.HaloStrokeExtra > 0) AddCssColorPremiumEdgePathLayer(edgeGroup, edge, prefix, options, svgId, color, dash, pathData, "-halo", color, strokeWidth + style.HaloStrokeExtra, style.HaloOpacity, false);
        AddCssColorPremiumEdgePathLayer(edgeGroup, edge, prefix, options, svgId, color, dash, pathData, string.Empty, color, strokeWidth, 1, true);
        if (style.HighlightOpacity > 0) AddCssColorPremiumEdgePathLayer(edgeGroup, edge, prefix, options, svgId, color, dash, pathData, "-highlight", ChartColor.White.ToCss(), System.Math.Max(1.0, strokeWidth * style.HighlightStrokeRatio), style.HighlightOpacity, false);
    }

    private static void AddCssColorPremiumEdgePathLayer(SvgElement edgeGroup, TopologyEdge edge, string prefix, TopologyRenderOptions options, string svgId, string markerColor, string dash, string pathData, string roleSuffix, string stroke, double strokeWidth, double layerOpacity, bool foreground) {
        edgeGroup.Element("path", path => {
            path
                .Class(prefix + "__edge " + ChartVisualPrimitives.SvgPremiumStrokeClass + (foreground ? string.Empty : " " + prefix + "__edge--premium-layer"))
                .Attribute("data-cfx-role", "topology-edge-path" + roleSuffix)
                .Attribute("d", pathData)
                .Attribute("fill", "none")
                .Attribute("stroke", stroke)
                .Attribute("stroke-width", strokeWidth)
                .Attribute("stroke-linecap", "round")
                .Attribute("stroke-linejoin", "round")
                .Attribute("stroke-dasharray", dash);
            var opacity = EdgeOpacity(edge, options) * layerOpacity;
            if (opacity < 1) path.Attribute("opacity", opacity);
            if (!foreground) return;
            if (options.IncludeDirectionMarkers && edge.Direction is VisualLinkDirection.Backward or VisualLinkDirection.Bidirectional) path.Attribute("marker-start", "url(#" + ArrowMarkerId(svgId, markerColor) + ")");
            if (options.IncludeDirectionMarkers && edge.Direction is VisualLinkDirection.Forward or VisualLinkDirection.Bidirectional) path.Attribute("marker-end", "url(#" + ArrowMarkerId(svgId, markerColor) + ")");
        });
    }

    private static void AddPremiumEdgePathLayer(SvgElement edgeGroup, TopologyEdge edge, string prefix, TopologyRenderOptions options, string svgId, bool selected, string color, string dash, string pathData, ChartLineVisualLayer? layer) {
        var foreground = !layer.HasValue || layer.Value.IsForeground;
        var stroke = !layer.HasValue ? color : (foreground ? color : layer.Value.Color.ToCss());
        var strokeWidth = layer.HasValue ? layer.Value.StrokeWidth : EdgeStrokeWidth(edge, selected, options);
        var roleSuffix = layer.HasValue ? layer.Value.RoleSuffix : string.Empty;
        var layerOpacity = layer.HasValue ? layer.Value.Opacity : 1;
        edgeGroup.Element("path", path => {
            path
                .Class(prefix + "__edge " + ChartVisualPrimitives.SvgPremiumStrokeClass + (foreground ? string.Empty : " " + prefix + "__edge--premium-layer"))
                .Attribute("data-cfx-role", "topology-edge-path" + roleSuffix)
                .Attribute("d", pathData)
                .Attribute("fill", "none")
                .Attribute("stroke", stroke)
                .Attribute("stroke-width", strokeWidth)
                .Attribute("stroke-linecap", "round")
                .Attribute("stroke-linejoin", "round")
                .Attribute("stroke-dasharray", dash);
            var opacity = EdgeOpacity(edge, options) * layerOpacity;
            if (opacity < 1) path.Attribute("opacity", opacity);
            if (!foreground) return;
            if (options.IncludeDirectionMarkers && edge.Direction is VisualLinkDirection.Backward or VisualLinkDirection.Bidirectional) path.Attribute("marker-start", "url(#" + ArrowMarkerId(svgId, color) + ")");
            if (options.IncludeDirectionMarkers && edge.Direction is VisualLinkDirection.Forward or VisualLinkDirection.Bidirectional) path.Attribute("marker-end", "url(#" + ArrowMarkerId(svgId, color) + ")");
        });
    }

    private static double EdgeLabelHaloStrokeWidth(double fontSize, bool emphasized) => ChartTextHalo.SvgStrokeWidth(fontSize, emphasized);
}

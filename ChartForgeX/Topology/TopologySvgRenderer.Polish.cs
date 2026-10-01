using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;
using ChartForgeX.Themes;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

public sealed partial class TopologySvgRenderer {
    private static void AddPremiumEdgePath(SvgElement edgeGroup, TopologyChart chart, TopologyEdge edge, IReadOnlyDictionary<string, TopologyNode> nodes, IReadOnlyList<ChartPoint> points, string prefix, TopologyRenderOptions options, string svgId, bool selected, string color, string dash, string markerKey) {
        var pathData = EdgePath(chart, edge, nodes, points, options);
        var style = EdgeVisualStyle(edge, selected, options);
        if (!ChartColor.TryParse(color, out var edgeColor)) {
            AddCssColorPremiumEdgePathLayers(edgeGroup, edge, prefix, options, svgId, selected, color, dash, markerKey, pathData, style);
            return;
        }

        foreach (var layer in ChartLineVisualLayers.Build(edgeColor, EdgeStrokeWidth(edge, selected, options), style)) {
            if (!layer.IsVisible) continue;
            AddPremiumEdgePathLayer(edgeGroup, edge, prefix, options, svgId, selected, color, dash, markerKey, pathData, layer);
        }
    }

    private static void AddCssColorPremiumEdgePathLayers(SvgElement edgeGroup, TopologyEdge edge, string prefix, TopologyRenderOptions options, string svgId, bool selected, string color, string dash, string markerKey, string pathData, ChartLineVisualStyle style) {
        var strokeWidth = EdgeStrokeWidth(edge, selected, options);
        if (style.AmbientHaloOpacity > 0 && style.AmbientHaloStrokeExtra > 0) AddCssColorPremiumEdgePathLayer(edgeGroup, edge, prefix, options, svgId, markerKey, dash, pathData, "-ambient-halo", SvgPaint.Plain(color), strokeWidth + style.AmbientHaloStrokeExtra, style.AmbientHaloOpacity, false);
        if (style.HaloOpacity > 0 && style.HaloStrokeExtra > 0) AddCssColorPremiumEdgePathLayer(edgeGroup, edge, prefix, options, svgId, markerKey, dash, pathData, "-halo", SvgPaint.Plain(color), strokeWidth + style.HaloStrokeExtra, style.HaloOpacity, false);
        AddCssColorPremiumEdgePathLayer(edgeGroup, edge, prefix, options, svgId, markerKey, dash, pathData, string.Empty, SvgPaint.Plain(color), strokeWidth, 1, true);
        if (style.HighlightOpacity > 0) AddCssColorPremiumEdgePathLayer(edgeGroup, edge, prefix, options, svgId, markerKey, dash, pathData, ChartLineVisualLayer.HighlightSuffix, SvgPaint.Literal(ChartColor.White), System.Math.Max(1.0, strokeWidth * style.HighlightStrokeRatio), style.HighlightOpacity, false);
    }

    private static void AddCssColorPremiumEdgePathLayer(SvgElement edgeGroup, TopologyEdge edge, string prefix, TopologyRenderOptions options, string svgId, string markerKey, string dash, string pathData, string roleSuffix, SvgPaint stroke, double strokeWidth, double layerOpacity, bool foreground) {
        edgeGroup.Element("path", path => {
            path
                .Class(prefix + "__edge " + ChartVisualPrimitives.SvgPremiumStrokeClass + (foreground ? string.Empty : " " + prefix + "__edge--premium-layer"))
                .Attribute("data-cfx-role", "topology-edge-path" + roleSuffix)
                .Attribute("d", pathData)
                .Attribute("fill", "none")
                .Paint("stroke", stroke)
                .Attribute("stroke-width", strokeWidth)
                .Attribute("stroke-linecap", "round")
                .Attribute("stroke-linejoin", "round")
                .Attribute("stroke-dasharray", dash);
            var opacity = EdgeOpacity(edge, options) * layerOpacity;
            if (opacity < 1) path.Attribute("opacity", opacity);
            if (!foreground) return;
            AddEndpointMarkerAttributes(path, edge, options, svgId, markerKey);
        });
    }

    private static void AddPremiumEdgePathLayer(SvgElement edgeGroup, TopologyEdge edge, string prefix, TopologyRenderOptions options, string svgId, bool selected, string color, string dash, string markerKey, string pathData, ChartLineVisualLayer? layer) {
        var foreground = !layer.HasValue || layer.Value.IsForeground;
        // The sheen layer is a derived white that never takes a token property; the line and its halos keep their colour.
        var stroke = !layer.HasValue || foreground ? SvgPaint.Plain(color) : layer.Value.IsHighlight ? SvgPaint.Literal(layer.Value.Color) : SvgPaint.Plain(layer.Value.Color);
        var strokeWidth = layer.HasValue ? layer.Value.StrokeWidth : EdgeStrokeWidth(edge, selected, options);
        var roleSuffix = layer.HasValue ? layer.Value.RoleSuffix : string.Empty;
        var layerOpacity = layer.HasValue ? layer.Value.Opacity : 1;
        edgeGroup.Element("path", path => {
            path
                .Class(prefix + "__edge " + ChartVisualPrimitives.SvgPremiumStrokeClass + (foreground ? string.Empty : " " + prefix + "__edge--premium-layer"))
                .Attribute("data-cfx-role", "topology-edge-path" + roleSuffix)
                .Attribute("d", pathData)
                .Attribute("fill", "none")
                .Paint("stroke", stroke)
                .Attribute("stroke-width", strokeWidth)
                .Attribute("stroke-linecap", "round")
                .Attribute("stroke-linejoin", "round")
                .Attribute("stroke-dasharray", dash);
            var opacity = EdgeOpacity(edge, options) * layerOpacity;
            if (opacity < 1) path.Attribute("opacity", opacity);
            if (!foreground) return;
            AddEndpointMarkerAttributes(path, edge, options, svgId, markerKey);
        });
    }

    private static void AddEndpointMarkerAttributes(SvgElement path, TopologyEdge edge, TopologyRenderOptions options, string svgId, string markerKey) {
        var source = RenderedSourceMarker(edge, options.IncludeDirectionMarkers);
        var target = RenderedTargetMarker(edge, options.IncludeDirectionMarkers);
        if (source != TopologyMarkerKind.None) path.Attribute("marker-start", "url(#" + MarkerId(svgId, markerKey, source) + ")");
        if (target != TopologyMarkerKind.None) path.Attribute("marker-end", "url(#" + MarkerId(svgId, markerKey, target) + ")");
    }

    private static string MarkerId(string svgId, string markerKey, TopologyMarkerKind kind) => kind == TopologyMarkerKind.Arrow ? ArrowMarkerId(svgId, markerKey) : EndpointMarkerId(svgId, markerKey, kind);

    private static double EdgeLabelHaloStrokeWidth(double fontSize, bool emphasized) => ChartTextHalo.SvgStrokeWidth(fontSize, emphasized);
}

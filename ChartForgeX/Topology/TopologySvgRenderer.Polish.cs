using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

public sealed partial class TopologySvgRenderer {
    private static void AddPremiumEdgePath(SvgElement edgeGroup, TopologyChart chart, TopologyEdge edge, IReadOnlyDictionary<string, TopologyNode> nodes, IReadOnlyList<ChartPoint> points, string prefix, TopologyRenderOptions options, string svgId, bool selected, string color, string dash) {
        var pathData = EdgePath(chart, edge, nodes, points, options);
        var edgeColor = ChartColor.TryParse(color, out var parsedColor) ? parsedColor : ChartColor.FromRgb(37, 99, 235);
        var style = ChartRouteVisualStyles.TopologyEdge(IsMonitoringDashboardStyle(options), edge.IsMuted, selected);
        foreach (var layer in ChartLineVisualLayers.Build(edgeColor, EdgeStrokeWidth(edge, selected, options), style)) {
            if (!layer.IsVisible) continue;
            edgeGroup.Element("path", path => {
                path
                    .Class(prefix + "__edge " + ChartVisualPrimitives.SvgPremiumStrokeClass + (layer.IsForeground ? string.Empty : " " + prefix + "__edge--premium-layer"))
                    .Attribute("data-cfx-role", "topology-edge-path" + layer.RoleSuffix)
                    .Attribute("d", pathData)
                    .Attribute("fill", "none")
                    .Attribute("stroke", layer.Color.ToCss())
                    .Attribute("stroke-width", layer.StrokeWidth)
                    .Attribute("stroke-linecap", "round")
                    .Attribute("stroke-linejoin", "round")
                    .Attribute("stroke-dasharray", dash);
                var opacity = EdgeOpacity(edge, options) * layer.Opacity;
                if (opacity < 1) path.Attribute("opacity", opacity);
                if (!layer.IsForeground) return;
                if (options.IncludeDirectionMarkers && edge.Direction is TopologyDirection.Backward or TopologyDirection.Bidirectional) path.Attribute("marker-start", "url(#" + ArrowMarkerId(svgId, color) + ")");
                if (options.IncludeDirectionMarkers && edge.Direction is TopologyDirection.Forward or TopologyDirection.Bidirectional) path.Attribute("marker-end", "url(#" + ArrowMarkerId(svgId, color) + ")");
            });
        }
    }

    private static double EdgeLabelHaloStrokeWidth(double fontSize, bool emphasized) => ChartTextHalo.SvgStrokeWidth(fontSize, emphasized);
}

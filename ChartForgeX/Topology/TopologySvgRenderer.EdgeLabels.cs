using ChartForgeX.Rendering;
using ChartForgeX.Svg;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

public sealed partial class TopologySvgRenderer {
    private static void AddEdgeLabelLeader(SvgElement group, TopologyEdgeLabelLayout layout, string color, TopologyTheme theme, TopologyRenderOptions options) {
        if (!ShouldDrawEdgeLabelLeader(layout, options)) return;
        var end = EdgeLabelLeaderEnd(layout);
        var path = "M " + F(layout.AnchorX) + " " + F(layout.AnchorY) + " L " + F(end.X) + " " + F(end.Y);
        var style = ChartRouteVisualStyles.TopologyEdgeLabelLeader(IsMonitoringDashboardStyle(options));
        group.Element("path", halo => halo
            .Attribute("data-cfx-role", "topology-edge-label-leader-halo")
            .Attribute("d", path)
            .Attribute("fill", "none")
            .Attribute("stroke", theme.Background)
            .Attribute("stroke-opacity", style.HaloOpacity)
            .Attribute("stroke-width", style.HaloStrokeWidth)
            .Attribute("stroke-linecap", "round"));
        group.Element("path", leader => leader
            .Attribute("data-cfx-role", "topology-edge-label-leader")
            .Attribute("d", path)
            .Attribute("fill", "none")
            .Attribute("stroke", color)
            .Attribute("stroke-opacity", style.StrokeOpacity)
            .Attribute("stroke-width", style.StrokeWidth)
            .Attribute("stroke-dasharray", F(style.Dash) + " " + F(style.Gap))
            .Attribute("stroke-linecap", "round"));
    }

    private static void AddEdgeLabelBackplate(SvgElement group, TopologyEdgeLabelLayout layout, double cx, double cy, TopologyTheme theme, TopologyRenderOptions options) {
        if (!options.IncludeEdgeLabelBackplates) return;
        group.Element("rect", rect => rect
            .Attribute("data-cfx-role", "topology-edge-label-backplate")
            .Attribute("x", EdgeLabelBackplateX(layout, cx))
            .Attribute("y", EdgeLabelBackplateY(layout, cy))
            .Attribute("width", layout.Width)
            .Attribute("height", layout.Height)
            .Attribute("rx", EdgeLabelBackplateRadius(options))
            .Attribute("fill", EdgeLabelBackplateFill(theme, options))
            .Attribute("fill-opacity", EdgeLabelBackplateFillOpacity(options))
            .Attribute("stroke", theme.Border)
            .Attribute("stroke-opacity", EdgeLabelBackplateStrokeOpacity(options)));
    }

    private static void AddEdgeLabelClearance(SvgElement group, TopologyChart chart, TopologyEdgeLabelLayout layout, double cx, double cy, TopologyTheme theme, TopologyRenderOptions options) {
        if (!ShouldDrawEdgeLabelClearance(layout, options)) return;
        var surfaceGroup = EdgeLabelClearanceGroup(chart, layout);
        group.Element("rect", rect => rect
            .Attribute("data-cfx-role", "topology-edge-label-clearance")
            .Attribute("data-clearance-surface", surfaceGroup == null ? "background" : "group")
            .Attribute("data-clearance-group-id", surfaceGroup?.Id)
            .Attribute("x", EdgeLabelClearanceX(layout, cx))
            .Attribute("y", EdgeLabelClearanceY(layout, cy))
            .Attribute("width", EdgeLabelClearanceWidth(layout))
            .Attribute("height", EdgeLabelClearanceHeight(layout))
            .Attribute("rx", EdgeLabelClearanceRadius)
            .Attribute("fill", EdgeLabelClearanceFill(surfaceGroup, theme, options))
            .Attribute("fill-opacity", EdgeLabelClearanceOpacity(surfaceGroup)));
    }
}

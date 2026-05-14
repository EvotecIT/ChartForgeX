using ChartForgeX.Svg;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

public sealed partial class TopologySvgRenderer {
    private static void AddEdgeLabelLeader(SvgElement group, TopologyEdgeLabelLayout layout, string color, TopologyTheme theme, TopologyRenderOptions options) {
        if (!ShouldDrawEdgeLabelLeader(layout, options)) return;
        var end = EdgeLabelLeaderEnd(layout);
        var path = "M " + F(layout.AnchorX) + " " + F(layout.AnchorY) + " L " + F(end.X) + " " + F(end.Y);
        group.Element("path", halo => halo
            .Attribute("data-cfx-role", "topology-edge-label-leader-halo")
            .Attribute("d", path)
            .Attribute("fill", "none")
            .Attribute("stroke", theme.Background)
            .Attribute("stroke-opacity", IsMonitoringDashboardStyle(options) ? 0.9 : 0.76)
            .Attribute("stroke-width", IsMonitoringDashboardStyle(options) ? 4 : 3)
            .Attribute("stroke-linecap", "round"));
        group.Element("path", leader => leader
            .Attribute("data-cfx-role", "topology-edge-label-leader")
            .Attribute("d", path)
            .Attribute("fill", "none")
            .Attribute("stroke", color)
            .Attribute("stroke-opacity", IsMonitoringDashboardStyle(options) ? 0.48 : 0.42)
            .Attribute("stroke-width", IsMonitoringDashboardStyle(options) ? 1.35 : 1.1)
            .Attribute("stroke-dasharray", "3 4")
            .Attribute("stroke-linecap", "round"));
    }

    private static void AddEdgeLabelClearance(SvgElement group, TopologyChart chart, TopologyEdgeLabelLayout layout, double cx, double cy, TopologyTheme theme, TopologyRenderOptions options) {
        if (!ShouldDrawEdgeLabelClearance(layout, options)) return;
        var surfaceGroup = EdgeLabelClearanceGroup(chart, layout);
        group.Element("rect", rect => rect
            .Attribute("data-cfx-role", "topology-edge-label-clearance")
            .Attribute("data-clearance-surface", surfaceGroup == null ? "background" : "group")
            .Attribute("data-clearance-group-id", surfaceGroup?.Id)
            .Attribute("x", cx - layout.Width / 2 + 3)
            .Attribute("y", cy - layout.Height / 2 + 8)
            .Attribute("width", layout.Width - 6)
            .Attribute("height", System.Math.Max(18, layout.Height - 16))
            .Attribute("rx", 5)
            .Attribute("fill", EdgeLabelClearanceFill(surfaceGroup, theme, options))
            .Attribute("fill-opacity", surfaceGroup == null ? 0.66 : 0.88));
    }
}

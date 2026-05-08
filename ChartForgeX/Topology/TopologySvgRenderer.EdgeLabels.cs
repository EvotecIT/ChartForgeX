using ChartForgeX.Svg;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

public sealed partial class TopologySvgRenderer {
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

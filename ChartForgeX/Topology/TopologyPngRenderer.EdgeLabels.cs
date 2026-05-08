using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

public sealed partial class TopologyPngRenderer {
    private static void DrawEdgeLabelClearance(RgbaCanvas canvas, TopologyChart chart, TopologyEdgeLabelLayout layout, double cx, double cy, TopologyTheme theme, TopologyRenderOptions options) {
        if (!ShouldDrawEdgeLabelClearance(layout, options)) return;
        var surfaceGroup = EdgeLabelClearanceGroup(chart, layout);
        var alpha = surfaceGroup == null ? (byte)168 : (byte)224;
        canvas.FillRoundedRect(cx - layout.Width / 2 + 3, cy - layout.Height / 2 + 8, layout.Width - 6, System.Math.Max(18, layout.Height - 16), 5, WithAlpha(Color(EdgeLabelClearanceFill(surfaceGroup, theme, options)), alpha));
    }
}

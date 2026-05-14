using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

public sealed partial class TopologyPngRenderer {
    private static void DrawEdgeLabelLeader(RgbaCanvas canvas, TopologyEdgeLabelLayout layout, ChartColor color, ChartColor haloColor, TopologyRenderOptions options) {
        if (!ShouldDrawEdgeLabelLeader(layout, options)) return;
        var end = EdgeLabelLeaderEnd(layout);
        canvas.DrawLine(layout.AnchorX, layout.AnchorY, end.X, end.Y, haloColor, IsMonitoringDashboardStyle(options) ? 4 : 3);
        canvas.DrawDashedLine(layout.AnchorX, layout.AnchorY, end.X, end.Y, WithAlpha(color, IsMonitoringDashboardStyle(options) ? (byte)122 : (byte)108), IsMonitoringDashboardStyle(options) ? 1.35 : 1.1, 3, 4);
    }

    private static void DrawEdgeLabelClearance(RgbaCanvas canvas, TopologyChart chart, TopologyEdgeLabelLayout layout, double cx, double cy, TopologyTheme theme, TopologyRenderOptions options, TopologyHighlightState highlight, bool isHighlighted) {
        if (!ShouldDrawEdgeLabelClearance(layout, options)) return;
        var surfaceGroup = EdgeLabelClearanceGroup(chart, layout);
        var alpha = HighlightAlpha(surfaceGroup == null ? (byte)168 : (byte)224, isHighlighted, highlight);
        canvas.FillRoundedRect(cx - layout.Width / 2 + 3, cy - layout.Height / 2 + 8, layout.Width - 6, System.Math.Max(18, layout.Height - 16), 5, WithAlpha(Color(EdgeLabelClearanceFill(surfaceGroup, theme, options)), alpha));
    }
}

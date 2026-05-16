using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

public sealed partial class TopologyPngRenderer {
    private static void DrawEdgeLabelLeader(RgbaCanvas canvas, TopologyEdgeLabelLayout layout, ChartColor color, ChartColor haloColor, TopologyRenderOptions options) {
        if (!ShouldDrawEdgeLabelLeader(layout, options)) return;
        var end = EdgeLabelLeaderEnd(layout);
        var style = ChartRouteVisualStyles.TopologyEdgeLabelLeader(IsMonitoringDashboardStyle(options));
        canvas.DrawLine(layout.AnchorX, layout.AnchorY, end.X, end.Y, WithOpacity(haloColor, style.HaloOpacity), style.HaloStrokeWidth);
        canvas.DrawDashedLine(layout.AnchorX, layout.AnchorY, end.X, end.Y, WithOpacity(color, style.StrokeOpacity), style.StrokeWidth, style.Dash, style.Gap);
    }

    private static void DrawEdgeLabelBackplate(RgbaCanvas canvas, TopologyEdgeLabelLayout layout, double cx, double cy, TopologyTheme theme, TopologyRenderOptions options) {
        if (!options.IncludeEdgeLabelBackplates) return;
        var radius = EdgeLabelBackplateRadius(options);
        canvas.FillRoundedRect(EdgeLabelBackplateX(layout, cx), EdgeLabelBackplateY(layout, cy), layout.Width, layout.Height, radius, Color(EdgeLabelBackplateFill(theme, options)));
        canvas.StrokeRoundedRect(EdgeLabelBackplateX(layout, cx), EdgeLabelBackplateY(layout, cy), layout.Width, layout.Height, radius, WithAlpha(Color(theme.Border), EdgeLabelBackplateStrokeAlpha(options)), EdgeLabelBackplateStrokeWidth);
    }

    private static void DrawEdgeLabelClearance(RgbaCanvas canvas, TopologyChart chart, TopologyEdgeLabelLayout layout, double cx, double cy, TopologyTheme theme, TopologyRenderOptions options, TopologyHighlightState highlight, bool isHighlighted) {
        if (!ShouldDrawEdgeLabelClearance(layout, options)) return;
        var surfaceGroup = EdgeLabelClearanceGroup(chart, layout);
        var alpha = HighlightAlpha(EdgeLabelClearanceAlpha(surfaceGroup), isHighlighted, highlight);
        canvas.FillRoundedRect(EdgeLabelClearanceX(layout, cx), EdgeLabelClearanceY(layout, cy), EdgeLabelClearanceWidth(layout), EdgeLabelClearanceHeight(layout), EdgeLabelClearanceRadius, WithAlpha(Color(EdgeLabelClearanceFill(surfaceGroup, theme, options)), alpha));
    }

    private static ChartColor WithOpacity(ChartColor color, double opacity) =>
        ChartColor.FromRgba(color.R, color.G, color.B, (byte)System.Math.Round(color.A * opacity));
}

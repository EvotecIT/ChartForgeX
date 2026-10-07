using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private static VisualScenePaintBinding Paint(ChartColor? fill = null, SvgColorRole fillRole = SvgColorRole.Any,
        ChartColor? stroke = null, SvgColorRole strokeRole = SvgColorRole.Any) => new(
        fill.HasValue ? SvgPaint.Of(fill.Value, fillRole) : null, stroke.HasValue ? SvgPaint.Of(stroke.Value, strokeRole) : null);

    private static SvgColorRole AccentRole(string? authored) => string.IsNullOrWhiteSpace(authored) ? SvgColorRole.Status : SvgColorRole.Any;

    private SvgPaint NodeFillPaint(TopologyNode node, ChartColor fill, ChartColor accent, SvgColorRole role) {
        if (!string.IsNullOrWhiteSpace(node.BackgroundColor)) return SvgPaint.Plain(fill);
        if (EffectiveNodeSurfaceStyle(_options) == TopologyNodeSurfaceStyle.Card) return SvgPaint.Of(fill, SvgColorRole.Surface);
        return SvgPaint.Mix(fill, _colors.Background, SvgColorRole.Surface, accent, role, IsMonitoringDashboardStyle(_options) ? .065 : .10);
    }
}

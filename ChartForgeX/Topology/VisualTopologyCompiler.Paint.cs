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

    private double HighlightFactor(bool active) => active || !_highlight.IsActive ? 1 : _highlight.DimmedOpacity;

    private static SvgPaint SourcePaint(string? source, ChartColor result, SvgColorRole role, double opacity = 1, ChartColor? fallback = null) =>
        SvgPaint.TryCssVariable(source, fallback ?? result, out _, out var paint) ? (opacity == 1 ? paint : paint.WithOpacity(result, opacity)) : SvgPaint.Of(result, role);

    private SvgPaint NodeAccentPaint(TopologyNode node, ChartColor accent, bool active, double opacity = 1) =>
        SourcePaint(node.Color ?? ResolveNodeIcon(node, _options)?.Color, accent, AccentRole(node.Color ?? ResolveNodeIcon(node, _options)?.Color), HighlightFactor(active) * opacity, Status(node.Status));

    private SvgPaint GroupAccentPaint(TopologyGroup group, ChartColor accent, bool active, double opacity = 1) =>
        SourcePaint(group.Color ?? ResolveGroupIcon(group, _options)?.Color, accent, AccentRole(group.Color ?? ResolveGroupIcon(group, _options)?.Color), HighlightFactor(active) * opacity, Status(group.Status));

    private SvgPaint NodeFillPaint(TopologyNode node, ChartColor fill, ChartColor accent, SvgColorRole role, bool active) {
        var result = Highlight(fill, active);
        if (!string.IsNullOrWhiteSpace(node.BackgroundColor)) return SourcePaint(node.BackgroundColor, result, SvgColorRole.Any, HighlightFactor(active), _colors.Surface);
        if (EffectiveNodeSurfaceStyle(_options) == TopologyNodeSurfaceStyle.Card) return SvgPaint.Of(result, SvgColorRole.Surface);
        var source = NodeAccentPaint(node, accent, true);
        var tint = source.HasCssVariable
            ? SvgPaint.Mix(fill, SvgPaint.Of(_colors.Background, SvgColorRole.Surface), source, IsMonitoringDashboardStyle(_options) ? .065 : .10)
            : SvgPaint.Mix(fill, _colors.Background, SvgColorRole.Surface, accent, role, IsMonitoringDashboardStyle(_options) ? .065 : .10);
        return HighlightFactor(active) == 1 ? tint : tint.WithOpacity(result, HighlightFactor(active));
    }
}

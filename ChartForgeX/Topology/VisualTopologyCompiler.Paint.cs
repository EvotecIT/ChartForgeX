using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private SvgPaint BackgroundPaint(ChartColor result, double opacity = 1) => SourcePaint(_svgTheme?.Background, result, SvgColorRole.Surface, opacity, _colors.Background);
    private SvgPaint CardPaint(ChartColor result, double opacity = 1) => SourcePaint(_svgTheme?.Card, result, SvgColorRole.Surface, opacity, _colors.Surface);
    private SvgPaint BorderPaint(ChartColor result, double opacity = 1, SvgColorRole role = SvgColorRole.Surface) => SourcePaint(_svgTheme?.Border, result, role, opacity, _colors.Border);
    private SvgPaint ForegroundPaint(ChartColor result, double opacity = 1) => SourcePaint(_svgTheme?.Foreground, result, SvgColorRole.Text, opacity, _colors.Foreground);
    private SvgPaint MutedPaint(ChartColor result, double opacity = 1) => SourcePaint(_svgTheme?.MutedForeground, result, SvgColorRole.Text, opacity, _colors.MutedForeground);
    private SvgPaint StatusPaint(TopologyHealthStatus status, ChartColor result, double opacity = 1) => SourcePaint(_svgTheme?.StatusColor(status), result, SvgColorRole.Status, opacity, Status(status));

    private VisualFramePaints? FramePaints() => _svgTheme == null ? null : new VisualFramePaints {
        Background = BackgroundPaint(_colors.Background), Surface = CardPaint(_colors.Surface),
        ElevatedSurface = SourcePaint(_svgTheme.Surface, _colors.ElevatedSurface, SvgColorRole.Surface, fallback: _colors.ElevatedSurface),
        Border = BorderPaint(_colors.Border), Foreground = ForegroundPaint(_colors.Foreground), MutedForeground = MutedPaint(_colors.MutedForeground)
    };

    private static SvgColorRole AccentRole(string? authored) => string.IsNullOrWhiteSpace(authored) ? SvgColorRole.Status : SvgColorRole.Any;

    private double HighlightFactor(bool active) => active || !_highlight.IsActive ? 1 : _highlight.DimmedOpacity;

    private static SvgPaint SourcePaint(string? source, ChartColor result, SvgColorRole role, double opacity = 1, ChartColor? fallback = null) =>
        SvgPaint.TryCssVariable(source, fallback ?? result, out _, out var paint) ? (opacity == 1 ? paint : paint.WithOpacity(result, opacity)) : SvgPaint.Of(result, role);

    private SvgPaint NodeAccentPaint(TopologyNode node, ChartColor accent, bool active, double opacity = 1) =>
        SourcePaint(node.Color ?? ResolveNodeIcon(node, _options)?.Color ?? _svgTheme?.StatusColor(node.Status), accent, AccentRole(node.Color ?? ResolveNodeIcon(node, _options)?.Color), HighlightFactor(active) * opacity, Status(node.Status));

    private SvgPaint GroupAccentPaint(TopologyGroup group, ChartColor accent, bool active, double opacity = 1) =>
        SourcePaint(group.Color ?? ResolveGroupIcon(group, _options)?.Color ?? _svgTheme?.StatusColor(group.Status), accent, AccentRole(group.Color ?? ResolveGroupIcon(group, _options)?.Color), HighlightFactor(active) * opacity, Status(group.Status));

    private SvgPaint NodeFillPaint(TopologyNode node, ChartColor fill, ChartColor accent, SvgColorRole role, bool active) {
        var result = Highlight(fill, active);
        if (!string.IsNullOrWhiteSpace(node.BackgroundColor)) return SourcePaint(node.BackgroundColor, result, SvgColorRole.Any, HighlightFactor(active), _colors.Surface);
        if (EffectiveNodeSurfaceStyle(_options) == TopologyNodeSurfaceStyle.Card) return CardPaint(result, HighlightFactor(active));
        var source = NodeAccentPaint(node, accent, true);
        var background = BackgroundPaint(_colors.Background);
        var tint = source.HasCssVariable || background.HasCssVariable
            ? SvgPaint.Mix(fill, background, source, IsMonitoringDashboardStyle(_options) ? .065 : .10)
            : SvgPaint.Mix(fill, _colors.Background, SvgColorRole.Surface, accent, role, IsMonitoringDashboardStyle(_options) ? .065 : .10);
        return HighlightFactor(active) == 1 ? tint : tint.WithOpacity(result, HighlightFactor(active));
    }
}

using ChartForgeX.Core;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class ChartRelationshipPaint {
    /// <summary>Resolves flow paint and semantic state together; explicit colors retain series provenance even with semantic state.</summary>
    internal static ChartRelationshipFlowPaint Flow(ChartSeries series, string id, int source, VisualThemeColors colors,
        double ribbonOpacity, ChartColor? ribbonFill = null, double? patternOpacity = null) {
        series.FlowStyles.TryGetValue(id, out var style);
        var state = series.FlowStates.TryGetValue(id, out var flowState) ? flowState : State(series, source);
        var pointColor = source >= 0 && source < series.PointColors.Count ? series.PointColors[source] : null;
        var explicitColor = style.Fill ?? pointColor ?? ribbonFill ?? series.Color;
        var color = explicitColor ?? ChartSeriesColours.State(state, colors, colors.Palette[source % colors.Palette.Count]);
        var role = explicitColor.HasValue || state == ChartSeriesState.None ? SvgColorRole.Series : SvgColorRole.Status;
        return new ChartRelationshipFlowPaint(style, state, color, SvgPaint.Of(color, role), ribbonOpacity,
            style.FillPattern ?? Pattern(series, source), patternOpacity);
    }
}

/// <summary>Detached colors and paint provenance shared by both weighted-flow compilers.</summary>
internal readonly struct ChartRelationshipFlowPaint {
    internal ChartRelationshipFlowPaint(ChartFlowStyle style, ChartSeriesState state, ChartColor color, SvgPaint paint,
        double ribbonOpacity, ChartFillPattern pattern, double? patternOpacity) {
        State = state; Color = color; Paint = paint; Pattern = pattern;
        var opacity = style.FillOpacity ?? ribbonOpacity;
        Fill = ChartColorMath.WithOpacity(color, opacity);
        FillPaint = paint.WithOpacity(Fill, opacity);
        var overlayOpacity = patternOpacity ?? opacity;
        PatternColor = ChartColorMath.WithOpacity(color, overlayOpacity);
        PatternPaint = paint.WithOpacity(PatternColor, overlayOpacity);
        // Retain the scene's existing unused default width when no outline is enabled.
        StrokeWidth = style.StrokeWidth ?? 1;
        Stroke = null; StrokePaint = null;
        if (StrokeWidth > 0 && (style.Stroke.HasValue || style.StrokeWidth.HasValue)) {
            var strokeColor = style.Stroke ?? color;
            var strokeOpacity = style.StrokeOpacity ?? 1;
            Stroke = ChartColorMath.WithOpacity(strokeColor, strokeOpacity);
            StrokePaint = (style.Stroke.HasValue ? SvgPaint.Of(strokeColor, SvgColorRole.Series) : paint).WithOpacity(Stroke.Value, strokeOpacity);
        }
    }

    internal ChartSeriesState State { get; }
    internal ChartColor Color { get; }
    internal SvgPaint Paint { get; }
    internal ChartColor Fill { get; }
    internal SvgPaint FillPaint { get; }
    internal ChartFillPattern Pattern { get; }
    internal ChartColor PatternColor { get; }
    internal SvgPaint PatternPaint { get; }
    internal ChartColor? Stroke { get; }
    internal double StrokeWidth { get; }
    internal SvgPaint? StrokePaint { get; }
    internal VisualScenePaintBinding Binding => new(fill: FillPaint, stroke: StrokePaint);
}

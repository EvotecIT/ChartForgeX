using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

/// <summary>Resolves authored node states and ordinal overrides once for weighted flow and hierarchy scenes.</summary>
internal static class ChartRelationshipPaint {
    internal static ChartSeriesState State(ChartSeries series, int node) => node >= 0 && node < series.Nodes.Count
        && series.NodeStates.TryGetValue(series.Nodes[node].Id, out var state) ? state : series.StateRole;

    internal static bool HasExplicitColor(ChartSeries series, int node, ChartColor? fillOverride = null) => fillOverride.HasValue || series.Color.HasValue
        || node >= 0 && node < series.PointColors.Count && series.PointColors[node].HasValue;

    internal static ChartColor Color(ChartSeries series, int node, VisualThemeColors colors, int? paletteIndex = null, bool neutralDefault = false, ChartColor? fillOverride = null) {
        if (node >= 0 && node < series.PointColors.Count && series.PointColors[node].HasValue) return series.PointColors[node]!.Value;
        if (fillOverride.HasValue) return fillOverride.Value;
        if (series.Color.HasValue) return series.Color.Value;
        var fallback = neutralDefault ? colors.Status.Neutral.Fill : colors.Palette[(paletteIndex ?? node) % colors.Palette.Count];
        return ChartSeriesColours.State(State(series, node), colors, fallback);
    }

    internal static SvgColorRole Role(ChartSeries series, int node, bool neutralDefault = false, ChartColor? fillOverride = null) =>
        HasExplicitColor(series, node, fillOverride) || State(series, node) == ChartSeriesState.None && !neutralDefault ? SvgColorRole.Series : SvgColorRole.Status;

    internal static SvgPaint Paint(ChartSeries series, ChartColor color, int node, bool neutralDefault = false, ChartColor? fillOverride = null) => SvgPaint.Of(color, Role(series, node, neutralDefault, fillOverride));

    internal static ChartFillPattern Pattern(ChartSeries series, int node) => node >= 0 && node < series.PointFillPatterns.Count
        && series.PointFillPatterns[node].HasValue ? series.PointFillPatterns[node]!.Value : series.FillPattern;

    internal static TextStyle LabelStyle(Chart chart, VisualRenderContext context, int node, ChartColor color) {
        var series = chart.Series[0];
        var style = series.DataLabelStyle.Resolve(chart.Options.DataLabelStyle.Resolve(new TextStyle {
            Font = context.Font, FontSize = context.Theme.Typography.DataLabelSize, Color = color
        }));
        return node >= 0 && node < series.PointDataLabelStyles.Count && series.PointDataLabelStyles[node] != null
            ? series.PointDataLabelStyles[node]!.Resolve(style) : style;
    }
}

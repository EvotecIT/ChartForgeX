using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

/// <summary>Captures paint provenance at the same decision point that resolves chart colours.</summary>
internal static class VisualChartPaint {
    internal static SvgColorRole SeriesRole(ChartSeries series, int point = -1, bool explicitOverride = false, SvgColorRole fallbackRole = SvgColorRole.Series) =>
        explicitOverride || series.Color.HasValue || point >= 0 && point < series.PointColors.Count && series.PointColors[point].HasValue
            ? SvgColorRole.Series : series.StateRole == ChartSeriesState.None ? fallbackRole : SvgColorRole.Status;

    /// <summary>Writes an automatic palette colour by its slot, so equal RGB values in two slots never share a property.</summary>
    internal static SvgPaint Series(ChartSeries series, ChartColor color, int point = -1, bool explicitOverride = false) {
        var role = SeriesRole(series, point, explicitOverride);
        if (explicitOverride || role != SvgColorRole.Series || series.Color.HasValue) return SvgPaint.Of(color, role);
        var ordinal = ChartSeriesColours.SlotOrdinal(series, point);
        return ordinal < 0 ? SvgPaint.Of(color, role) : SvgPaint.SeriesSlot(color, ordinal);
    }

    internal static VisualScenePaintBinding Fill(ChartColor color, SvgColorRole role) => new(fill: SvgPaint.Of(color, role));
    internal static VisualScenePaintBinding Stroke(ChartColor color, SvgColorRole role) => new(stroke: SvgPaint.Of(color, role));
    internal static VisualScenePaintBinding Fill(SvgPaint paint) => new(fill: paint);
    internal static VisualScenePaintBinding Stroke(SvgPaint paint) => new(stroke: paint);
    internal static SvgPaint Text(TextStyle style) => SvgPaint.Of(style.Color, SvgColorRole.Text);

    internal static SvgPaint LineLayer(ChartSeries series, ChartColor source, ChartLineVisualLayer layer) => layer.IsHighlight
        ? SvgPaint.Literal(layer.ColorWithOpacity()) : Series(series, source).WithOpacity(layer.ColorWithOpacity(), layer.Opacity);

    internal static SvgPaint BarGradient(ChartColor source, SvgColorRole role, bool top, double opacity, SvgPaint? sourcePaint = null) {
        var mix = top ? ChartMarkSurface.BarGradientTop(source) : ChartMarkSurface.BarGradientBottom(source);
        var actual = ChartColorMath.WithOpacity(mix.WithAlpha(source.A), opacity);
        // The RGB interpolation is deliberately straight; CSS mixes translucent operands premultiplied.
        // Retain the exact raster colour for explicit translucent blends instead of changing their appearance.
        if (source.A != 255) return SvgPaint.Literal(actual);
        // A palette-slot source keeps its slot through the blend.
        if (sourcePaint is { IsSeriesSlot: true } slot)
            return SvgPaint.Mix(mix, SvgPaint.Literal(top ? ChartColor.White : ChartColor.Black), slot,
                top ? ChartVisualPrimitives.BarGradientTopBlend : ChartVisualPrimitives.BarGradientBottomBlend).WithOpacity(actual, opacity);
        return SvgPaint.Mix(mix, top ? ChartColor.White : ChartColor.Black, null, source, role,
            top ? ChartVisualPrimitives.BarGradientTopBlend : ChartVisualPrimitives.BarGradientBottomBlend).WithOpacity(actual, opacity);
    }

    internal static bool ExplicitDataLabelColor(Chart chart, int point = -1, bool ticks = false, int seriesIndex = 0) {
        if (ticks) return chart.Options.TickLabelStyle.Color.HasValue;
        var series = chart.Series[seriesIndex];
        return chart.Options.DataLabelStyle.Color.HasValue || series.DataLabelStyle.Color.HasValue
            || point >= 0 && point < series.PointDataLabelStyles.Count && series.PointDataLabelStyles[point]?.Color != null;
    }
}

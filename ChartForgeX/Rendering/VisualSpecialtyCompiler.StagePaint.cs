using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualSpecialtyCompiler {
    /// <summary>Resolves the existing point color over one optional shared stage outline without changing the filled path.</summary>
    private static (ChartColor? Color, double Width, double[]? Dash, SvgPaint? Paint) StageOutline(
        ChartSeries series, int index, ChartColor fill, ChartStrokeStyle? policy, double fallbackWidth = 1) {
        var point = index < series.PointStrokeColors.Count ? series.PointStrokeColors[index] : null;
        if (policy == null && !point.HasValue) return (null, 0, null, null);
        var source = point ?? policy?.Color ?? fill;
        var opacity = policy?.Opacity ?? 1;
        var color = ChartColorMath.WithOpacity(source, opacity);
        var width = policy?.Width ?? (series.HasExplicitStrokeWidth ? series.StrokeWidth : fallbackWidth);
        return (color, width, policy?.ResolveDash(Array.Empty<double>()),
            VisualChartPaint.Series(series, source, index, explicitOverride: point.HasValue || policy?.Color != null).WithOpacity(color, opacity));
    }

    private static void StagePartition(Chart chart, VisualSceneBuilder builder, ChartPath path, int index, ChartColor color,
        string role, ChartStrokeStyle? outline, double phase) {
        var series = chart.Series[0];
        var stroke = StageOutline(series, index, color, outline);
        builder.Path(path, color, stroke.Color, stroke.Width, role: role, close: true, dash: stroke.Dash,
            cap: VisualStrokeCap.Butt, paint: new VisualScenePaintBinding(
                fill: VisualChartPaint.Series(series, color, index), stroke: stroke.Paint), dashOffset: phase);
        Pattern(chart, builder, index, path, color);
    }

    private static ChartFunnelConnectorStyle? FunnelConnectionStyle(Chart chart, int fromIndex) {
        var series = chart.Series[0];
        return (fromIndex < series.FunnelConnectorStyles.Count ? series.FunnelConnectorStyles[fromIndex] : null)
            ?? chart.Options.Funnel.ConnectorStyle;
    }

    private static (ChartColor Color, SvgPaint Paint) FunnelConnectionFill(Chart chart, int fromIndex,
        ChartColor inherited, ChartFunnelForm form, ChartFunnelConnectorStyle? style) {
        var source = style?.Fill ?? inherited;
        var opacity = style?.FillOpacity ?? (form == ChartFunnelForm.Cone ? .3 : .18);
        var fill = ChartColorMath.WithOpacity(source, opacity);
        return (fill, VisualChartPaint.Series(chart.Series[0], source, fromIndex,
            explicitOverride: style?.Fill.HasValue == true).WithOpacity(fill, opacity));
    }

    private static void StyledFunnelConnection(Chart chart, VisualSceneBuilder builder, ChartFunnelConnectionLayout connection,
        ChartColor inherited, ChartFunnelForm form, ChartFunnelConnectorStyle style) {
        if (!style.Visible) return;
        var series = chart.Series[0]; var source = style.Fill ?? inherited;
        var fill = FunnelConnectionFill(chart, connection.FromIndex, inherited, form, style);
        var outline = style.OutlineStroke; var strokeSource = outline?.Color ?? source;
        var stroke = outline == null ? (ChartColor?)null : ChartColorMath.WithOpacity(strokeSource, outline.Opacity);
        builder.Path(connection.Path, fill.Color, stroke, outline?.Width ?? 1,
            role: form == ChartFunnelForm.Cone ? "funnel-connection-area" : "funnel-dropoff", close: true,
            dash: outline?.ResolveDash(Array.Empty<double>()), cap: VisualStrokeCap.Butt, dashOffset: style.StrokeDashOffset,
            paint: new VisualScenePaintBinding(fill: fill.Paint, stroke: outline == null ? null
                : VisualChartPaint.Series(series, strokeSource, connection.FromIndex, explicitOverride: outline.Color.HasValue)
                    .WithOpacity(stroke!.Value, outline.Opacity)));
        if (style.FillPattern.HasValue) builder.Pattern(connection.Path, style.FillPattern.Value,
            ChartColorMath.AccessibleTextOnBackground(source).WithOpacity(.45), role: "fill-pattern");
        else if (form == ChartFunnelForm.Cone) Pattern(chart, builder, connection.FromIndex, connection.Path, source);
    }
}

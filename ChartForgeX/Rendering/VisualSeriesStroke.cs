using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

/// <summary>Resolves authored series strokes before layout and captures identical SVG/native raster paint.</summary>
internal static class VisualSeriesStroke {
    internal static ChartStrokeStyle? Style(ChartSeries series) {
        var specific = series.Kind == ChartSeriesKind.Radar ? series.Radar.OutlineStroke
            : series.Kind == ChartSeriesKind.Polar ? series.Polar.OutlineStroke : null;
        var general = series.StrokeStyle;
        if (specific == null || general == null) return specific ?? general;
        return new ChartStrokeStyle(specific.Color ?? general.Color, specific.Width ?? general.Width,
            specific.Opacity, specific.Dash ?? general.Dash);
    }

    internal static double Width(ChartSeries series, double fallback) =>
        Style(series)?.Width ?? (series.HasExplicitStrokeWidth ? series.StrokeWidth : fallback);

    private static (ChartColor Color, SvgPaint Paint) Resolve(ChartStrokeStyle? style, ChartColor color, SvgPaint paint) {
        if (style == null) return (color, paint);
        var source = style?.Color ?? color;
        var opacity = style?.Opacity ?? 1;
        var actual = ChartColorMath.WithOpacity(source, opacity);
        return (actual, (style?.Color != null ? SvgPaint.Literal(source) : paint).WithOpacity(actual, opacity));
    }

    internal static void DrawPath(Chart chart, VisualSceneBuilder builder, ChartSeries series, ChartPath path,
        ChartColor color, SvgPaint paint, double fallbackWidth, string role, double[]? dash = null, bool close = false, bool layered = true) =>
        DrawPath(chart, builder, Style(series), path, color, paint, fallbackWidth, role, dash, close, layered);

    // Strokes a connected series path, restyling each stroke segment from its source X onward. sourceX projects a source X
    // onto the physical category axis: X for vertical charts, Y for horizontal ones.
    internal static void DrawSegmentedPath(Chart chart, VisualSceneBuilder builder, ChartSeries series, ChartPath path,
        ChartColor color, SvgPaint paint, double fallbackWidth, string role, Func<double, double> sourceX, bool horizontal) {
        var styles = SegmentStyles(series);
        if (styles.Length == 1) { DrawPath(chart, builder, styles[0].Style, path, color, paint, fallbackWidth, role); return; }
        var boundaries = new double[styles.Length - 1];
        for (var index = 1; index < styles.Length; index++) boundaries[index - 1] = sourceX(styles[index].FromX);
        var origin = styles[1].FromX;
        var descending = sourceX(origin + 1) < sourceX(origin);
        // Each piece is an independent path: its dash starts at the join and standalone zoom reprojects both ends identically.
        var pieces = (horizontal ? path.Transpose() : path).PartitionX(boundaries, descending);
        for (var index = 0; index < pieces.Length; index++)
            if (pieces[index].Commands.Count > 0)
                DrawPath(chart, builder, styles[index].Style, horizontal ? pieces[index].Transpose() : pieces[index], color, paint, fallbackWidth, role);
    }

    /// <summary>Returns the series style followed by each segment's merged style in source order.</summary>
    internal static (double FromX, ChartStrokeStyle? Style)[] SegmentStyles(ChartSeries series) {
        var general = Style(series);
        var result = new (double, ChartStrokeStyle?)[series.StrokeSegments.Count + 1];
        result[0] = (double.NegativeInfinity, general);
        var ordered = series.StrokeSegments.Select((segment, order) => (segment, order))
            .OrderBy(item => item.segment.FromX).ThenBy(item => item.order).ToArray();
        for (var index = 0; index < ordered.Length; index++) {
            var specific = ordered[index].segment.Style;
            result[index + 1] = (ordered[index].segment.FromX, new ChartStrokeStyle(specific.Color ?? general?.Color, specific.Width ?? general?.Width,
                specific.Opacity * (general?.Opacity ?? 1), specific.Dash ?? general?.Dash));
        }
        return result;
    }

    private static void DrawPath(Chart chart, VisualSceneBuilder builder, ChartStrokeStyle? style, ChartPath path,
        ChartColor color, SvgPaint paint, double fallbackWidth, string role, double[]? dash = null, bool close = false, bool layered = true) {
        var width = style?.Width ?? fallbackWidth;
        if (width <= 0 || style?.Opacity == 0) return;
        var actual = Resolve(style, color, paint);
        if (!layered) {
            builder.Path(path, stroke: actual.Color, strokeWidth: width, role: role, close: close,
                dash: style?.ResolveDash(dash ?? Array.Empty<double>()) ?? dash, paint: VisualChartPaint.Stroke(actual.Paint));
            return;
        }
        foreach (var layer in ChartLineVisualLayers.Build(actual.Color, width, chart.Options.ResolvePreparedLineVisualStyle()))
            if (layer.IsVisible) builder.Path(path, stroke: layer.ColorWithOpacity(), strokeWidth: layer.StrokeWidth,
                role: role + layer.RoleSuffix, close: close, dash: style?.ResolveDash(dash ?? Array.Empty<double>()) ?? dash,
                paint: VisualChartPaint.Stroke(layer.IsHighlight ? SvgPaint.Literal(layer.ColorWithOpacity())
                    : actual.Paint.WithOpacity(layer.ColorWithOpacity(), layer.Opacity)));
    }

    internal static void DrawLine(VisualSceneBuilder builder, ChartSeries series, double x1, double y1, double x2, double y2,
        ChartColor color, double width, string role, VisualScenePaintBinding? paint = null, double[]? dash = null,
        double maximumWidth = double.PositiveInfinity) {
        var style = Style(series);
        var actual = Resolve(style, color, paint?.Stroke ?? SvgPaint.Literal(color));
        builder.Line(x1, y1, x2, y2, actual.Color, Math.Min(style?.Width ?? width, maximumWidth), role: role,
            dash: style?.ResolveDash(dash ?? Array.Empty<double>()) ?? dash, paint: VisualChartPaint.Stroke(actual.Paint));
    }

    internal static void DrawBox(VisualSceneBuilder builder, ChartSeries series, ChartRect bounds, double radius,
        ChartColor fill, ChartColor color, double width, string role, VisualScenePaintBinding paint) {
        var style = Style(series);
        var actual = Resolve(style, color, paint.Stroke ?? SvgPaint.Literal(color));
        var binding = new VisualScenePaintBinding(paint.Fill, actual.Paint);
        if (style?.Dash is { Count: > 0 })
            builder.Path(ChartPathBuilder.RoundedRectangle(bounds, radius, radius, radius, radius), fill, actual.Color,
                Width(series, width), role: role, close: true, dash: style.ResolveDash(Array.Empty<double>()), paint: binding);
        else builder.Rect(bounds, fill, actual.Color, Width(series, width), radius, role: role, paint: binding);
    }

}

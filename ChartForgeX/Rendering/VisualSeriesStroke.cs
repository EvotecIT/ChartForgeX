using System;
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
        ChartColor color, SvgPaint paint, double fallbackWidth, string role, double[]? dash = null, bool close = false, bool layered = true) {
        var style = Style(series);
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

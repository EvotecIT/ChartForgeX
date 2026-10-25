using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

/// <summary>Shared native paint for standalone and overlaid polar lines and radar forms.</summary>
internal static class VisualPolarSeriesScene {
    /// <summary>Describes authored observations with captured display text; derived gaps retain their missing-observation policy.</summary>
    internal static void DescribePoint(Dictionary<string, string> metadata, ChartSeries series, int source, string category, string formatted) {
        if (source < 0) return;
        var description = series.Name + ", " + category + ": " + formatted;
        metadata["data-cfx-category-label"] = category;
        if (!metadata.ContainsKey("data-cfx-x")) metadata["data-cfx-x"] = series.RenderPoints[source].X.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        metadata["aria-label"] = description;
        metadata["data-cfx-label"] = description;
    }

    internal static void Draw(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartSeries series,
        ChartPath path, ChartColor color, bool radar, bool closed = true) {
        if (closed && (radar ? series.Radar.Form : series.Polar.Form) == ChartLineAreaForm.Area) {
            var opacity = (radar ? series.Radar.FillOpacity : series.Polar.FillOpacity) ?? context.Theme.AreaOpacity;
            var fill = ChartColorMath.WithOpacity(color, opacity);
            builder.Path(path, fill, role: radar ? "radar-area" : "polar-area", close: true,
                paint: VisualChartPaint.Fill(VisualChartPaint.Series(series, color).WithOpacity(fill, opacity)));
            if (series.FillPattern != ChartFillPattern.None && opacity > 0) {
                var hatchOpacity = Math.Min(1, opacity / Math.Max(.000001, context.Theme.AreaOpacity));
                builder.Pattern(path, series.FillPattern, ChartColorMath.WithOpacity(ChartColorMath.AccessibleTextOnBackground(color).WithAlpha(80), hatchOpacity), role: radar ? "radar-pattern" : "polar-pattern");
            }
        }
        var stroke = radar ? series.Radar.OutlineStroke : series.Polar.OutlineStroke;
        var source = stroke?.Color ?? color;
        var outlineOpacity = stroke?.Opacity ?? 1;
        var actual = ChartColorMath.WithOpacity(source, outlineOpacity);
        var width = VisualReferenceStroke.PaintedWidth(stroke, color, series.HasExplicitStrokeWidth ? series.StrokeWidth : context.Theme.SeriesStrokeWidth);
        if (width <= 0) return;
        foreach (var layer in ChartLineVisualLayers.Build(actual, width, chart.Options.LineVisualStyle))
            if (layer.IsVisible) builder.Path(path, stroke: layer.ColorWithOpacity(), strokeWidth: layer.StrokeWidth,
                role: (radar ? "radar-outline" : "polar-line") + layer.RoleSuffix, close: closed,
                dash: stroke?.ResolveDash(Array.Empty<double>()),
                paint: VisualChartPaint.Stroke(layer.IsHighlight ? SvgPaint.Literal(layer.ColorWithOpacity())
                    : (stroke?.Color != null ? SvgPaint.Literal(source) : VisualChartPaint.Series(series, source))
                        .WithOpacity(layer.ColorWithOpacity(), outlineOpacity * layer.Opacity)));
    }

    /// <summary>Measures only painted stroke layers so standalone and mixed radar fit the same outline overrides.</summary>
    internal static double StrokeExtent(Chart chart, VisualRenderContext context, ChartSeries series, ChartColor color) {
        var stroke = series.Kind == ChartSeriesKind.Radar ? series.Radar.OutlineStroke : series.Polar.OutlineStroke;
        var width = VisualReferenceStroke.PaintedWidth(stroke, color, series.HasExplicitStrokeWidth ? series.StrokeWidth : context.Theme.SeriesStrokeWidth);
        if (width <= 0) return 0;
        var actual = ChartColorMath.WithOpacity(stroke?.Color ?? color, stroke?.Opacity ?? 1);
        return ChartLineVisualLayers.Build(actual, width, chart.Options.LineVisualStyle).Where(layer => layer.IsVisible)
            .Select(layer => layer.StrokeWidth).DefaultIfEmpty(0).Max();
    }
}

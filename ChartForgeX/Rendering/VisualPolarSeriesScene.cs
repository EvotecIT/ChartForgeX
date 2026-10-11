using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>Shared native paint for standalone and overlaid polar lines and radar forms.</summary>
internal static class VisualPolarSeriesScene {
    /// <summary>Describes authored observations with captured display text; derived gaps retain their missing-observation policy.</summary>
    internal static void DescribePoint(Dictionary<string, string> metadata, ChartSeries series, int source, string category, string formatted) {
        if (source < 0) return;
        var description = series.Name + ", " + category + ": " + formatted;
        metadata["aria-label"] = description;
        metadata["data-cfx-label"] = description;
    }

    internal static void Draw(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartSeries series,
        ChartPath path, ChartColor color, bool radar) {
        if (radar && series.Radar.Form == ChartLineAreaForm.Area) {
            var opacity = series.Radar.FillOpacity ?? context.Theme.AreaOpacity;
            var fill = ChartColorMath.WithOpacity(color, opacity);
            builder.Path(path, fill, role: "radar-area", close: true,
                paint: VisualChartPaint.Fill(VisualChartPaint.Series(series, color).WithOpacity(fill, opacity)));
            if (series.FillPattern != ChartFillPattern.None && opacity > 0) {
                var hatchOpacity = Math.Min(1, opacity / Math.Max(.000001, context.Theme.AreaOpacity));
                builder.Pattern(path, series.FillPattern, ChartColorMath.WithOpacity(ChartColorMath.AccessibleTextOnBackground(color).WithAlpha(80), hatchOpacity), role: "radar-pattern");
            }
        }
        var width = series.HasExplicitStrokeWidth ? series.StrokeWidth : context.Theme.SeriesStrokeWidth;
        foreach (var layer in ChartLineVisualLayers.Build(color, width, chart.Options.LineVisualStyle))
            if (layer.IsVisible) builder.Path(path, stroke: layer.ColorWithOpacity(), strokeWidth: layer.StrokeWidth,
                role: (radar ? "radar-outline" : "polar-line") + layer.RoleSuffix, close: radar,
                paint: VisualChartPaint.Stroke(VisualChartPaint.LineLayer(series, color, layer)));
    }
}

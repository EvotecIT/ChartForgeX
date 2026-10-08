using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualMapCompiler {
    private static MapLayout ScaleLayout(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, double min, double max) {
        if (!chart.Options.ShowMapScaleLegend || !context.Frame.ShowLegend) return new MapLayout(plot, new ChartRect(0, 0, 0, 0), Array.Empty<string>(), false);
        var values = new[] { ChartHeatmapSurface.MapScaleValue(chart, min, max, 1), ChartHeatmapSurface.MapScaleMidpoint(chart, min, max), ChartHeatmapSurface.MapScaleValue(chart, min, max, 0) };
        var texts = new[] { ChartHeatmapSurface.MapHighLabel(chart) + " · " + ChartNumericFormatter.FormatValue(chart.Options, values[0]),
            (ChartHeatmapSurface.MapMidpointLabel(chart) ?? "") + " " + ChartNumericFormatter.FormatValue(chart.Options, values[1]),
            ChartHeatmapSurface.MapLowLabel(chart) + " · " + ChartNumericFormatter.FormatValue(chart.Options, values[2]) };
        var style = TickStyle(chart, context); var gap = context.Theme.Spacing;
        var height = texts.Concat(new[] { "Mg", chart.Series[0].Name, chart.Options.Labels.NoData })
            .Max(text => builder.MeasureText(text, style).Height);
        if (chart.Options.MapScaleLegendPosition == ChartMapScaleLegendPosition.Right) {
            var measured = texts.Concat(new[] { chart.Series[0].Name, chart.Options.Labels.NoData }).Max(text => builder.MeasureText(text, style).Width);
            var width = Math.Min(plot.Width * .4, measured + height + gap * 3);
            return new MapLayout(new ChartRect(plot.Left, plot.Top, Math.Max(0, plot.Width - width - gap), plot.Height),
                new ChartRect(plot.Right - width, plot.Top, width, plot.Height), texts, true);
        }
        var reserve = Math.Min(plot.Height * .35, height * (ChartHeatmapSurface.MapMidpointLabel(chart) != null ? 4 : 3) + gap * 2);
        return new MapLayout(new ChartRect(plot.Left, plot.Top, plot.Width, Math.Max(0, plot.Height - reserve - gap)),
            new ChartRect(plot.Left, plot.Bottom - reserve, plot.Width, reserve), texts, false);
    }

    private static void DrawScale(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, MapLayout layout, VisualThemeColors colors, double min, double max) {
        if (!chart.Options.ShowMapScaleLegend || !context.Frame.ShowLegend) return;
        var area = layout.Scale; var gap = context.Theme.Spacing; var style = TickStyle(chart, context);
        var row = layout.Texts.Concat(new[] { "Mg", chart.Series[0].Name, chart.Options.Labels.NoData })
            .Max(text => builder.MeasureText(text, style).Height);
        var missing = (chart.Options.RegionMapDefinition?.Regions.Count ?? chart.Options.TileMapDefinition?.Regions.Count ?? 0) > chart.Series[0].Points.Count;
        var steps = ChartHeatmapSurface.MapScaleSteps(chart, min, max);
        using (builder.PushGroup("map-scale", "map-scale", new Dictionary<string, string> {
            ["data-cfx-min-value"] = N(ChartHeatmapSurface.MapScaleValue(chart, min, max, 0)),
            ["data-cfx-max-value"] = N(ChartHeatmapSurface.MapScaleValue(chart, min, max, 1)),
            ["data-cfx-midpoint-value"] = N(ChartHeatmapSurface.MapScaleMidpoint(chart, min, max))
        })) using (builder.PushClip(area)) {
            if (layout.Right) {
                VisualRadialPrimitives.Text(builder, chart.Series[0].Name, new ChartRect(area.Left, area.Top, area.Width, Math.Min(row, area.Height)), style, "map-scale-title", "map-scale-title");
                var top = Math.Min(area.Bottom, area.Top + row + gap); var height = Math.Max(0, area.Height - row * (missing ? 2 : 1) - gap * 2);
                var swatchWidth = Math.Min(row, area.Width * .2);
                for (var index = 0; index < 32; index++) {
                    var value = ChartHeatmapSurface.MapScaleValue(chart, min, max, 1 - index / 31d);
                    var blend = ChartHeatmapSurface.MapBlend(chart, colors, null, High(chart.Series[0], colors), value, min, max, VisualChartPaint.SeriesRole(chart.Series[0]));
                    builder.Rect(new ChartRect(area.Left, top + height * index / 32, swatchWidth, height / 32),
                        blend.Color, role: "map-scale-step", paint: VisualChartPaint.Fill(blend.Paint));
                }
                var textWidth = Math.Max(0, area.Width - swatchWidth - gap);
                var midpoint = ChartHeatmapSurface.MapRatio(chart, ChartHeatmapSurface.MapScaleMidpoint(chart, min, max), min, max);
                var labelHeight = Math.Min(row, height / 3);
                var centers = new[] { top + labelHeight / 2, VisualRadialPrimitives.Clamp(top + height * (1 - midpoint), top + labelHeight * 1.5, top + height - labelHeight * 1.5), top + height - labelHeight / 2 };
                for (var index = 0; index < layout.Texts.Length; index++) VisualRadialPrimitives.Text(builder, layout.Texts[index],
                    Intersect(new ChartRect(area.Left + swatchWidth + gap, centers[index] - labelHeight / 2, textWidth, labelHeight), area), style, "map-scale-label", "map-scale-label-" + index);
                if (missing) MissingScale(chart, builder, colors, Intersect(new ChartRect(area.Left, area.Bottom - row, area.Width, row), area), style, row);
            } else {
                var swatchTop = Math.Min(area.Bottom, area.Top + row); var swatchHeight = Math.Min(row * .7, Math.Max(0, area.Bottom - swatchTop));
                var swatchWidth = area.Width / steps.Length;
                for (var index = 0; index < steps.Length; index++) using (builder.PushGroup(null, "map-scale-step-source", new Dictionary<string, string> { ["data-cfx-value"] = N(steps[index]) })) {
                    var blend = ChartHeatmapSurface.MapBlend(chart, colors, null, High(chart.Series[0], colors), steps[index], min, max, VisualChartPaint.SeriesRole(chart.Series[0]));
                    builder.Rect(new ChartRect(area.Left + index * swatchWidth, swatchTop, swatchWidth, swatchHeight),
                        blend.Color, role: "map-scale-step", paint: VisualChartPaint.Fill(blend.Paint));
                }
                VisualRadialPrimitives.Text(builder, layout.Texts[2], new ChartRect(area.Left, area.Top, area.Width / 2, Math.Min(row, area.Height)), style, "map-scale-label", "map-scale-low");
                VisualRadialPrimitives.Text(builder, layout.Texts[0], new ChartRect(area.Left + area.Width / 2, area.Top, area.Width / 2, Math.Min(row, area.Height)), style, "map-scale-label", "map-scale-high");
                var next = swatchTop + swatchHeight;
                if (ChartHeatmapSurface.MapMidpointLabel(chart) != null) {
                    var center = area.Left + (ChartHeatmapSurface.MapScaleMidpointStep(chart, min, max, steps.Length) + .5) * swatchWidth;
                    var width = Math.Min(area.Width, Math.Max(swatchWidth, builder.MeasureText(layout.Texts[1], style).Width));
                    VisualRadialPrimitives.Text(builder, layout.Texts[1], Intersect(new ChartRect(center - width / 2, next, width, row), area), style, "map-scale-midpoint-label", "map-scale-midpoint");
                    next += row;
                }
                if (missing) MissingScale(chart, builder, colors, Intersect(new ChartRect(area.Left, next, area.Width, row), area), style, row);
            }
        }
    }

    private static void MissingScale(Chart chart, VisualSceneBuilder builder, VisualThemeColors colors, ChartRect area, TextStyle style, double size) {
        var swatch = Math.Min(size * .7, Math.Min(area.Width, area.Height));
        var blend = ChartHeatmapSurface.MapNoDataBlend(chart, colors);
        builder.Rect(new ChartRect(area.Left, area.Top + (area.Height - swatch) / 2, swatch, swatch), blend.Color, role: "map-scale-no-data", paint: VisualChartPaint.Fill(blend.Paint));
        VisualRadialPrimitives.Text(builder, chart.Options.Labels.NoData, new ChartRect(area.Left + swatch, area.Top, Math.Max(0, area.Width - swatch), area.Height), style, "map-scale-no-data-label", "map-scale-no-data-label");
    }

    private sealed class MapLayout {
        internal MapLayout(ChartRect map, ChartRect scale, string[] texts, bool right) { Map = map; Scale = scale; Texts = texts; Right = right; }
        internal ChartRect Map { get; } internal ChartRect Scale { get; } internal string[] Texts { get; } internal bool Right { get; }
    }
}

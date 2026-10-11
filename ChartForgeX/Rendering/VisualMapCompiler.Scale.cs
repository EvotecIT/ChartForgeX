using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualMapCompiler {
    private static MapLayout ScaleLayout(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, double min, double max, ChartColorPresentation presentation) {
        if (!presentation.ShowLegend || !context.Frame.ShowLegend) return new MapLayout(plot, new ChartRect(0, 0, 0, 0), Array.Empty<string>(), false);
        if (chart.Options.MapColorScale?.Mode == ChartColorScaleMode.Discrete) return DiscreteScaleLayout(chart, context, builder, plot, presentation);
        var values = new[] { ChartHeatmapSurface.MapScaleValue(chart, min, max, 1), ChartHeatmapSurface.MapScaleMidpoint(chart, min, max), ChartHeatmapSurface.MapScaleValue(chart, min, max, 0) };
        var captions = presentation.ScaleValues(values);
        var texts = new[] { (chart.Options.MapColorScale?.HighLabel ?? presentation.Labels.More) + " · " + captions[0],
            (ChartHeatmapSurface.MapMidpointLabel(chart) ?? "") + " " + captions[1],
            (chart.Options.MapColorScale?.LowLabel ?? presentation.Labels.Less) + " · " + captions[2] };
        var style = TickStyle(chart, context); var gap = context.Theme.Spacing;
        var title = presentation.HasName ? presentation.Name : chart.Series[0].Name;
        var height = texts.Concat(new[] { "Mg", title, presentation.NoData })
            .Max(text => builder.MeasureText(text, style).Height);
        if (chart.Options.MapScaleLegendPosition == ChartMapScaleLegendPosition.Right) {
            var measured = texts.Concat(new[] { title, presentation.NoData }).Max(text => builder.MeasureText(text, style).Width);
            var width = Math.Min(plot.Width * .4, measured + height + gap * 3);
            return new MapLayout(new ChartRect(plot.Left, plot.Top, Math.Max(0, plot.Width - width - gap), plot.Height),
                new ChartRect(plot.Right - width, plot.Top, width, plot.Height), texts, true);
        }
        var reserve = Math.Min(plot.Height * .35, height * (ChartHeatmapSurface.MapMidpointLabel(chart) != null ? 4 : 3) + gap * 2 + VisualColorScaleLegendScene.HeadingHeight(builder, style, gap / 2, presentation.HasName ? presentation.Name : null));
        return new MapLayout(new ChartRect(plot.Left, plot.Top, plot.Width, Math.Max(0, plot.Height - reserve - gap)),
            new ChartRect(plot.Left, plot.Bottom - reserve, plot.Width, reserve), texts, false);
    }

    private static void DrawScale(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, MapLayout layout, VisualThemeColors colors, double min, double max, ChartColorPresentation presentation) {
        if (!presentation.ShowLegend || !context.Frame.ShowLegend) return;
        if (chart.Options.MapColorScale?.Mode == ChartColorScaleMode.Discrete) {
            DrawDiscreteScale(chart, context, builder, layout, colors, presentation);
            return;
        }
        var area = layout.Scale; var gap = context.Theme.Spacing; var style = TickStyle(chart, context);
        var title = presentation.HasName ? presentation.Name : chart.Series[0].Name;
        var row = layout.Texts.Concat(new[] { "Mg", title, presentation.NoData })
            .Max(text => builder.MeasureText(text, style).Height);
        var missing = HasMissingMapValues(chart);
        var steps = ChartHeatmapSurface.MapScaleSteps(chart, min, max);
        var scale = chart.Options.MapColorScale; var positioned = scale?.HasPositionedStops == true;
        var gradient = presentation.LegendStyle == ChartColorLegendStyle.Gradient;
        var metadata = new Dictionary<string, string> {
            ["data-cfx-scale-mode"] = chart.Options.MapColorScale?.Mode == ChartColorScaleMode.Diverging ? "diverging" : "sequential",
            ["data-cfx-min-value"] = N(ChartHeatmapSurface.MapScaleValue(chart, min, max, 0)),
            ["data-cfx-max-value"] = N(ChartHeatmapSurface.MapScaleValue(chart, min, max, 1)),
            ["data-cfx-midpoint-value"] = N(ChartHeatmapSurface.MapScaleMidpoint(chart, min, max))
        };
        if (presentation.LegendStyle != ChartColorLegendStyle.Auto) metadata["data-cfx-legend-style"] = presentation.LegendStyle.ToString().ToLowerInvariant();
        using (builder.PushGroup("map-scale", "map-scale", metadata)) using (builder.PushClip(area)) {
            if (scale != null) VisualColorScaleLegendScene.PositionedStops(builder, scale, area, "map-scale",
                presentation.ScaleValues, presentation.HasOverrides ? presentation.Name : null);
            if (layout.Right) {
                VisualRadialPrimitives.Text(builder, title, new ChartRect(area.Left, area.Top, area.Width, Math.Min(row, area.Height)), style, "map-scale-title", "map-scale-title");
                var top = Math.Min(area.Bottom, area.Top + row + gap); var height = Math.Max(0, area.Height - row * (missing ? 2 : 1) - gap * 2);
                var swatchWidth = Math.Min(row, area.Width * .2);
                var sampled = positioned || presentation.LegendStyle == ChartColorLegendStyle.Swatches;
                var count = sampled ? steps.Length : 32;
                if (gradient) VisualColorScaleLegendScene.Gradient(builder, scale!, new ChartRect(area.Left, top, swatchWidth, height), min, max, "map-scale-gradient", vertical: true);
                else {
                    for (var index = 0; index < count; index++) {
                        var value = sampled ? steps[steps.Length - 1 - index] : ChartHeatmapSurface.MapScaleValue(chart, min, max, 1 - index / 31d);
                        var blend = ChartHeatmapSurface.MapBlend(chart, colors, null, High(chart.Series[0], colors), value, min, max, VisualChartPaint.SeriesRole(chart.Series[0]));
                        using (sampled ? builder.PushGroup(null, "map-scale-step-source", new Dictionary<string, string> { ["data-cfx-value"] = N(value) }) : null)
                        builder.Rect(new ChartRect(area.Left, top + height * index / count, swatchWidth, height / count),
                            blend.Color, role: "map-scale-step", paint: VisualChartPaint.Fill(blend.Paint));
                    }
                }
                var textWidth = Math.Max(0, area.Width - swatchWidth - gap);
                var midpoint = ChartHeatmapSurface.MapRatio(chart, ChartHeatmapSurface.MapScaleMidpoint(chart, min, max), min, max);
                var labelHeight = Math.Min(row, height / 3);
                var midpointY = !gradient && sampled ? top + (count - .5 - ChartColorScaleLegend.MidpointStep(scale!, min, max, count)) * height / count
                    : top + height * (1 - midpoint);
                var centers = new[] { top + labelHeight / 2, VisualRadialPrimitives.Clamp(midpointY, top + labelHeight * 1.5, top + height - labelHeight * 1.5), top + height - labelHeight / 2 };
                for (var index = 0; index < layout.Texts.Length; index++) VisualRadialPrimitives.Text(builder, layout.Texts[index],
                    Intersect(new ChartRect(area.Left + swatchWidth + gap, centers[index] - labelHeight / 2, textWidth, labelHeight), area), style, "map-scale-label", "map-scale-label-" + index);
                if (missing) MissingScale(chart, builder, colors, Intersect(new ChartRect(area.Left, area.Bottom - row, area.Width, row), area), style, row, presentation.NoData);
            } else {
                if (presentation.HasName) {
                    VisualColorScaleLegendScene.Heading(builder, area, style, "map-scale", presentation.Name);
                    var heading = Math.Min(area.Height, VisualColorScaleLegendScene.HeadingHeight(builder, style, gap / 2, presentation.Name));
                    area = new ChartRect(area.Left, area.Top + heading, area.Width, Math.Max(0, area.Height - heading));
                }
                var swatchTop = Math.Min(area.Bottom, area.Top + row); var swatchHeight = Math.Min(row * .7, Math.Max(0, area.Bottom - swatchTop));
                var swatchWidth = area.Width / steps.Length;
                if (gradient) VisualColorScaleLegendScene.Gradient(builder, scale!, new ChartRect(area.Left, swatchTop, area.Width, swatchHeight), min, max, "map-scale-gradient");
                else {
                    for (var index = 0; index < steps.Length; index++) using (builder.PushGroup(null, "map-scale-step-source", new Dictionary<string, string> { ["data-cfx-value"] = N(steps[index]) })) {
                        var blend = ChartHeatmapSurface.MapBlend(chart, colors, null, High(chart.Series[0], colors), steps[index], min, max, VisualChartPaint.SeriesRole(chart.Series[0]));
                        builder.Rect(new ChartRect(area.Left + index * swatchWidth, swatchTop, swatchWidth, swatchHeight),
                            blend.Color, role: "map-scale-step", paint: VisualChartPaint.Fill(blend.Paint));
                    }
                }
                VisualRadialPrimitives.Text(builder, layout.Texts[2], new ChartRect(area.Left, area.Top, area.Width / 2, Math.Min(row, area.Height)), style, "map-scale-label", "map-scale-low");
                VisualRadialPrimitives.Text(builder, layout.Texts[0], new ChartRect(area.Left + area.Width / 2, area.Top, area.Width / 2, Math.Min(row, area.Height)), style, "map-scale-label", "map-scale-high");
                var next = swatchTop + swatchHeight;
                if (ChartHeatmapSurface.MapMidpointLabel(chart) != null) {
                    var center = gradient ? area.Left + area.Width * ChartColorScaleLegend.MidpointRatio(scale!, min, max)
                        : area.Left + (ChartHeatmapSurface.MapScaleMidpointStep(chart, min, max, steps.Length) + .5) * swatchWidth;
                    // Fit symmetrically around the stop so clipping at either key edge cannot move the text anchor.
                    var width = Math.Min(2 * Math.Min(center - area.Left, area.Right - center),
                        Math.Max(swatchWidth, builder.MeasureText(layout.Texts[1], style).Width));
                    VisualRadialPrimitives.Text(builder, layout.Texts[1], Intersect(new ChartRect(center - width / 2, next, width, row), area), style, "map-scale-midpoint-label", "map-scale-midpoint");
                    next += row;
                }
                if (missing) MissingScale(chart, builder, colors, Intersect(new ChartRect(area.Left, next, area.Width, row), area), style, row, presentation.NoData);
            }
        }
    }

    private static void MissingScale(Chart chart, VisualSceneBuilder builder, VisualThemeColors colors, ChartRect area, TextStyle style, double size, string noData) {
        var swatch = Math.Min(size * .7, Math.Min(area.Width, area.Height));
        var blend = ChartHeatmapSurface.MapNoDataBlend(chart, colors);
        builder.Rect(new ChartRect(area.Left, area.Top + (area.Height - swatch) / 2, swatch, swatch), blend.Color, role: "map-scale-no-data", paint: VisualChartPaint.Fill(blend.Paint));
        VisualRadialPrimitives.Text(builder, noData, new ChartRect(area.Left + swatch, area.Top, Math.Max(0, area.Width - swatch), area.Height), style, "map-scale-no-data-label", "map-scale-no-data-label");
    }

    private sealed class MapLayout {
        internal MapLayout(ChartRect map, ChartRect scale, string[] texts, bool right) { Map = map; Scale = scale; Texts = texts; Right = right; }
        internal ChartRect Map { get; } internal ChartRect Scale { get; } internal string[] Texts { get; } internal bool Right { get; }
    }
}

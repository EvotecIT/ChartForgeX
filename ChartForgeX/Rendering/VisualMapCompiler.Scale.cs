using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualMapCompiler {
    private static MapLayout ScaleLayout(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, double min, double max,
        ChartColorPresentation presentation, TileMapLegendInset? legendInset = null) {
        if (!presentation.ShowLegend || !context.Frame.ShowLegend) return new MapLayout(plot, new ChartRect(0, 0, 0, 0), Array.Empty<string>(), false);
        if (chart.Options.MapColorScale?.Mode == ChartColorScaleMode.Discrete) return DiscreteScaleLayout(chart, context, builder, plot, presentation);
        if (chart.Options.MapColorScale?.Captions.Count > 0) return CaptionedScaleLayout(chart, context, builder, plot, min, max, presentation);
        if (chart.Options.MapColorScale is ChartColorScale scale && !MapHasDomain(chart, scale, min, max))
            return MissingScaleLayout(chart, context, builder, plot, presentation);
        var texts = ResolveScaleCaptions(chart, min, max, presentation);
        if (legendInset != null) return new MapLayout(legendInset.Map, legendInset.ColorKey, texts, false, inset: true, missingDataKey: legendInset.MissingDataKey);
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
        var missingWidth = height * .7 + 4 + builder.MeasureText(presentation.NoData, style).Width;
        // A compact missing-data item uses its own trailing column, outside the quantitative ramp.
        var inlineMissing = !presentation.HasName && ChartHeatmapSurface.MapMidpointLabel(chart) == null && HasMissingMapValues(chart)
            && 2 * Math.Max(builder.MeasureText(texts[0], style).Width, builder.MeasureText(texts[2], style).Width)
                + missingWidth + gap <= plot.Width;
        // An inline key needs one measured caption row and a half-row color ramp. Keep its map separation
        // proportional to that compact key, so a readable key does not consume another full row of tile codes.
        var reserve = Math.Min(plot.Height * .35, inlineMissing ? height * 2
            : height * (ChartHeatmapSurface.MapMidpointLabel(chart) != null ? 4 : 3) + gap * 2
                + VisualColorScaleLegendScene.HeadingHeight(builder, style, gap / 2, presentation.HasName ? presentation.Name : null));
        return new MapLayout(new ChartRect(plot.Left, plot.Top, plot.Width, Math.Max(0, plot.Height - reserve - (inlineMissing ? gap / 4 : gap))),
            new ChartRect(plot.Left, plot.Bottom - reserve, plot.Width, reserve), texts, false, inlineMissing);
    }

    /// <summary>Resolves the same complete three-value key before either inset measurement or static painting.</summary>
    private static string[] ResolveScaleCaptions(Chart chart, double min, double max, ChartColorPresentation presentation) {
        var values = new[] { ChartHeatmapSurface.MapScaleValue(chart, min, max, 1), ChartHeatmapSurface.MapScaleMidpoint(chart, min, max), ChartHeatmapSurface.MapScaleValue(chart, min, max, 0) };
        var captions = presentation.ScaleValues(values);
        return new[] { (chart.Options.MapColorScale?.HighLabel ?? presentation.Labels.More) + " · " + captions[0],
            (ChartHeatmapSurface.MapMidpointLabel(chart) ?? "") + " " + captions[1],
            (chart.Options.MapColorScale?.LowLabel ?? presentation.Labels.Less) + " · " + captions[2] };
    }

    private static void DrawScale(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, MapLayout layout, VisualThemeColors colors, double min, double max, ChartColorPresentation presentation) {
        if (!presentation.ShowLegend || !context.Frame.ShowLegend) return;
        if (chart.Options.MapColorScale?.Captions.Count > 0) {
            DrawCaptionedScale(chart, context, builder, layout, colors, min, max, presentation);
            return;
        }
        if (chart.Options.MapColorScale?.Mode == ChartColorScaleMode.Discrete) {
            DrawDiscreteScale(chart, context, builder, layout, colors, presentation);
            return;
        }
        if (chart.Options.MapColorScale is ChartColorScale configured && !MapHasDomain(chart, configured, min, max)) {
            DrawMissingScale(chart, context, builder, layout, colors, configured, presentation);
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
        var clip = layout.MissingDataKey.HasValue ? new ChartRect(area.Left, layout.MissingDataKey.Value.Top, area.Width, area.Bottom - layout.MissingDataKey.Value.Top) : area;
        using (builder.PushGroup("map-scale", "map-scale", metadata)) using (builder.PushClip(clip)) {
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
                ChartRect? inlineMissing = layout.MissingDataKey;
                if (missing && inlineMissing.HasValue) MissingScale(chart, builder, colors, inlineMissing.Value, style, row, presentation.NoData);
                if (layout.InlineMissing) {
                    var width = row * .7 + 4 + builder.MeasureText(presentation.NoData, style).Width;
                    // Its swatch and caption sit after the ramp, with their center on the color strip.
                    // Low/high captions retain their own ramp-relative anchors in the remaining column.
                    inlineMissing = new ChartRect(area.Right - width, area.Top + row * .85, width, Math.Min(row, Math.Max(0, area.Height - row * .85)));
                    MissingScale(chart, builder, colors, inlineMissing.Value, style, row, presentation.NoData);
                    area = new ChartRect(area.Left, area.Top, Math.Max(0, area.Width - width - gap), area.Height);
                }
                if (presentation.HasName) {
                    var heading = Math.Min(area.Height, VisualColorScaleLegendScene.HeadingHeight(builder, style, gap / 2, presentation.Name));
                    var keyArea = new ChartRect(area.Left, area.Top + heading, area.Width, Math.Max(0, area.Height - heading));
                    if (missing) inlineMissing = MissingScaleHeadingBounds(builder, area, keyArea, style, row, gap,
                        presentation.Name, presentation.NoData, ChartHeatmapSurface.MapMidpointLabel(chart) != null);
                    var titleArea = inlineMissing.HasValue
                        ? new ChartRect(area.Left, area.Top, Math.Max(0, inlineMissing.Value.Left - gap - area.Left), area.Height) : area;
                    VisualColorScaleLegendScene.Heading(builder, titleArea, style, "map-scale", presentation.Name);
                    if (inlineMissing.HasValue) MissingScale(chart, builder, colors, inlineMissing.Value, style, row, presentation.NoData);
                    area = keyArea;
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
                VisualRadialPrimitives.Text(builder, layout.Texts[2], new ChartRect(area.Left, area.Top, area.Width / 2, Math.Min(row, area.Height)), style, "map-scale-label", "map-scale-low",
                    alignment: TextAlignment.Left);
                VisualRadialPrimitives.Text(builder, layout.Texts[0], new ChartRect(area.Left + area.Width / 2, area.Top, area.Width / 2, Math.Min(row, area.Height)), style, "map-scale-label", "map-scale-high",
                    alignment: TextAlignment.Right);
                var next = swatchTop + swatchHeight;
                if (ChartHeatmapSurface.MapMidpointLabel(chart) != null) {
                    var center = gradient ? area.Left + area.Width * ChartColorScaleLegend.MidpointRatio(scale!, min, max)
                        : area.Left + (ChartHeatmapSurface.MapScaleMidpointStep(chart, min, max, steps.Length) + .5) * swatchWidth;
                    if (gradient) {
                        var lane = VisualColorScaleLegendScene.MidpointCaptionLane(builder, layout.Texts[1], style,
                            Intersect(new ChartRect(area.Left, next, area.Width, row), area), center);
                        var captionStyle = VisualColorScaleLegendScene.FitCaptionStyle(builder, layout.Texts[1], style, lane.Bounds.Width);
                        VisualRadialPrimitives.Text(builder, layout.Texts[1], lane.Bounds, captionStyle,
                            "map-scale-midpoint-label", "map-scale-midpoint", alignment: lane.Alignment);
                    } else {
                        var width = Math.Min(2 * Math.Min(center - area.Left, area.Right - center),
                            Math.Max(swatchWidth, builder.MeasureText(layout.Texts[1], style).Width));
                        VisualRadialPrimitives.Text(builder, layout.Texts[1], Intersect(new ChartRect(center - width / 2, next, width, row), area), style,
                            "map-scale-midpoint-label", "map-scale-midpoint");
                    }
                    next += row;
                }
                if (missing && !inlineMissing.HasValue) MissingScale(chart, builder, colors, Intersect(new ChartRect(area.Left, next, area.Width, row), area), style, row, presentation.NoData);
            }
        }
    }

    private static ChartRect? MissingScaleHeadingBounds(VisualSceneBuilder builder, ChartRect headingArea, ChartRect keyArea,
        TextStyle style, double row, double gap, string heading, string noData, bool midpoint) {
        var stripHeight = Math.Min(row * .7, Math.Max(0, keyArea.Height - row));
        var missingTop = Math.Min(keyArea.Height, row) + stripHeight + (midpoint ? row : 0);
        var stackedHeight = Math.Min(row, Math.Max(0, keyArea.Height - missingTop));
        var caption = builder.MeasureText(noData, style); var title = builder.MeasureText(heading, style);
        var height = Math.Min(builder.MeasureText("Mg", style).Height, headingArea.Height);
        var width = row * .7 + caption.Width + gap / 2;
        // Reuse the heading only when both complete captions fit; the numeric key keeps its original bounds.
        if (stackedHeight + .000001 >= caption.Height || Math.Max(caption.Height, title.Height) > height + .000001
            || title.Width + gap + width > headingArea.Width) return null;
        return new ChartRect(headingArea.Right - width, headingArea.Top, width, height);
    }

    private static void MissingScale(Chart chart, VisualSceneBuilder builder, VisualThemeColors colors, ChartRect area, TextStyle style, double size, string noData) {
        var swatch = Math.Min(size * .7, Math.Min(area.Width, area.Height));
        var gap = Math.Min(4, Math.Max(0, area.Width - swatch));
        var captionWidth = Math.Min(builder.MeasureText(noData, style).Width, Math.Max(0, area.Width - swatch - gap));
        var left = area.Left + (area.Width - swatch - gap - captionWidth) / 2;
        var blend = ChartHeatmapSurface.MapNoDataBlend(chart, colors);
        builder.Rect(new ChartRect(left, area.Top + (area.Height - swatch) / 2, swatch, swatch), blend.Color, role: "map-scale-no-data", paint: VisualChartPaint.Fill(blend.Paint));
        VisualRadialPrimitives.Text(builder, noData, new ChartRect(left + swatch + gap, area.Top, captionWidth, area.Height),
            style, "map-scale-no-data-label", "map-scale-no-data-label", alignment: TextAlignment.Left);
    }

    private sealed class MapLayout {
        internal MapLayout(ChartRect map, ChartRect scale, string[] texts, bool right, bool inlineMissing = false, bool inset = false, ChartRect? missingDataKey = null) {
            Map = map; Scale = scale; Texts = texts; Right = right; InlineMissing = inlineMissing; Inset = inset; MissingDataKey = missingDataKey;
        }
        internal ChartRect Map { get; } internal ChartRect Scale { get; } internal string[] Texts { get; } internal bool Right { get; }
        internal bool InlineMissing { get; }
        internal bool Inset { get; }
        internal ChartRect? MissingDataKey { get; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualMapCompiler {
    private static MapLayout DiscreteScaleLayout(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot) {
        var texts = BandCaptions(chart);
        var style = TickStyle(chart, context); var gap = context.Theme.Spacing;
        var height = texts.Concat(new[] { "Mg", chart.Series[0].Name, chart.Options.Labels.NoData })
            .Max(text => builder.MeasureText(text, style).Height);
        if (chart.Options.MapScaleLegendPosition == ChartMapScaleLegendPosition.Right) {
            var measured = texts.Concat(new[] { chart.Series[0].Name, chart.Options.Labels.NoData })
                .Max(text => builder.MeasureText(text, style).Width);
            var width = Math.Min(plot.Width * .5, measured + height * 2 + gap * 3);
            return new MapLayout(new ChartRect(plot.Left, plot.Top, Math.Max(0, plot.Width - width - gap), plot.Height),
                new ChartRect(plot.Right - width, plot.Top, width, plot.Height), texts, true);
        }
        var reserve = Math.Min(plot.Height * .4, height * (HasMissingMapValues(chart) ? 4 : 3) + gap * 2);
        return new MapLayout(new ChartRect(plot.Left, plot.Top, plot.Width, Math.Max(0, plot.Height - reserve - gap)),
            new ChartRect(plot.Left, plot.Bottom - reserve, plot.Width, reserve), texts, false);
    }

    private static void DrawDiscreteScale(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, MapLayout layout, VisualThemeColors colors) {
        var scale = chart.Options.MapColorScale!;
        var area = layout.Scale; var gap = context.Theme.Spacing; var style = TickStyle(chart, context);
        var row = layout.Texts.Concat(new[] { "Mg", chart.Series[0].Name, chart.Options.Labels.NoData })
            .Max(text => builder.MeasureText(text, style).Height);
        var missing = HasMissingMapValues(chart);
        using (builder.PushGroup("map-scale", "map-scale", new Dictionary<string, string> {
            ["data-cfx-scale-mode"] = "discrete", ["data-cfx-band-count"] = N(scale.Bands.Count)
        })) using (builder.PushClip(area)) {
            VisualRadialPrimitives.Text(builder, chart.Series[0].Name, new ChartRect(area.Left, area.Top, area.Width, Math.Min(row, area.Height)),
                style, "map-scale-title", "map-scale-title");
            var top = Math.Min(area.Bottom, area.Top + row + gap / 2);
            var available = Math.Max(0, area.Bottom - top - (missing ? row + gap / 2 : 0));
            var swatchWidth = layout.Right ? Math.Min(row, area.Width * .2) : area.Width / scale.Bands.Count;
            var height = layout.Right ? available / scale.Bands.Count : Math.Min(row * .7, available);
            for (var index = 0; index < scale.Bands.Count; index++) {
                var band = scale.Bands[index];
                var bounds = new ChartRect(area.Left + (layout.Right ? 0 : index * swatchWidth),
                    top + (layout.Right ? index * height : 0), swatchWidth, height);
                var metadata = new Dictionary<string, string> {
                    ["data-cfx-band"] = N(index), ["data-cfx-label"] = layout.Texts[index], ["aria-label"] = layout.Texts[index],
                    ["data-cfx-lower-bound"] = index == 0 ? "" : N(scale.Bands[index - 1].UpperBound!.Value),
                    ["data-cfx-upper-bound"] = band.UpperBound.HasValue ? N(band.UpperBound.Value) : "",
                    ["data-cfx-lower-inclusive"] = "true", ["data-cfx-upper-exclusive"] = "true"
                };
                if (band.Label != null) metadata["data-cfx-band-label"] = band.Label;
                using (builder.PushGroup(null, "map-scale-step-source", metadata)) {
                    var paint = ChartColorBlend.Solid(band.Color, SvgColorRole.Ramp);
                    builder.Rect(bounds, paint.Color, role: "map-scale-step", paint: VisualChartPaint.Fill(paint.Paint));
                }
                var textBounds = layout.Right
                    ? new ChartRect(bounds.Right + gap, bounds.Top, Math.Max(0, area.Right - bounds.Right - gap), bounds.Height)
                    : new ChartRect(bounds.Left, bounds.Bottom, bounds.Width, Math.Min(row, Math.Max(0, area.Bottom - bounds.Bottom)));
                VisualRadialPrimitives.Text(builder, layout.Texts[index], Intersect(textBounds, area), style, "map-scale-band-label", "map-scale-band-label-" + index,
                    alignment: layout.Right ? TextAlignment.Left : TextAlignment.Center);
            }
            if (missing) MissingScale(chart, builder, colors, Intersect(new ChartRect(area.Left, area.Bottom - row, area.Width, row), area), style, row);
        }
    }

    private static string[] BandCaptions(Chart chart) {
        var bands = chart.Options.MapColorScale!.Bands;
        var bounds = ChartNumericFormatter.FormatScaleValues(chart.Options, bands.Take(bands.Count - 1).Select(band => band.UpperBound!.Value).ToArray());
        var captions = new string[bands.Count];
        for (var index = 0; index < bands.Count; index++) {
            var interval = bands.Count == 1 ? "All values" : index == 0 ? "< " + bounds[0]
                : index == bands.Count - 1 ? "≥ " + bounds[index - 1] : bounds[index - 1] + " ≤ value < " + bounds[index];
            captions[index] = bands[index].Label == null ? interval : bands[index].Label + " · " + interval;
        }
        return captions;
    }

    private static bool HasMissingMapValues(Chart chart) =>
        (chart.Options.RegionMapDefinition?.Regions.Count ?? chart.Options.TileMapDefinition?.Regions.Count ?? 0) > chart.Series[0].Points.Count;
}

using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualMatrixCompiler {
    private static TextStyle ColorScaleStyle(Chart chart, VisualRenderContext context) =>
        context.Frame.LegendStyle ?? VisualStateSceneTools.TickStyle(chart, context);

    // Preserve the calendar's established missing-day fallback while honoring an authored scale color.
    private static ChartColorBlend CalendarMissingBlend(Chart chart, VisualThemeColors colors) =>
        chart.Options.HeatmapColorScale?.NoDataColor != null
            ? ChartColorScaleSurface.NoData(chart.Options.HeatmapColorScale, colors) : ChartHeatmapSurface.CalendarEmptyBlend(colors);

    private static int ColorScaleColumns(Chart chart, VisualSceneBuilder builder, ChartColorScale scale, TextStyle style, double width, double gap) {
        if (scale.Mode != ChartColorScaleMode.Discrete) return 1;
        var widest = ChartColorScaleLegend.BandCaptions(chart.Options, scale).Max(text => builder.MeasureText(text, style).Width);
        return Math.Max(1, Math.Min(scale.Bands.Count, (int)Math.Floor(width / Math.Max(1, widest + gap))));
    }

    /// <summary>Reserves measured caption rows for the same explicit numeric key in every matrix family.</summary>
    private static double ColorScaleHeight(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, double width, double maximum, bool missing = false) {
        var scale = chart.Options.HeatmapColorScale!; var style = ColorScaleStyle(chart, context);
        var row = builder.MeasureText("Mg", style).Height; var gap = context.Theme.Spacing / 2;
        var columns = ColorScaleColumns(chart, builder, scale, style, width, gap);
        var rows = scale.Mode == ChartColorScaleMode.Discrete ? (int)Math.Ceiling(scale.Bands.Count / (double)columns) : 1;
        var extra = (scale.MidpointLabel != null ? row + gap : 0) + (missing ? row + gap : 0);
        return Math.Max(0, Math.Min(maximum, rows * (12 + row + gap) + gap + extra));
    }

    /// <summary>Draws canonical scale stops or exact bands, keeping raw values and complete captions in semantic regions.</summary>
    private static void ColorScale(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect bounds,
        double min, double max, string prefix, bool missing = false, ChartColorBlend? missingBlend = null) {
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        var scale = chart.Options.HeatmapColorScale!; var style = ColorScaleStyle(chart, context);
        var row = builder.MeasureText("Mg", style).Height; var gap = context.Theme.Spacing / 2;
        var columns = ColorScaleColumns(chart, builder, scale, style, bounds.Width, gap);
        var captions = scale.Mode == ChartColorScaleMode.Discrete ? ChartColorScaleLegend.BandCaptions(chart.Options, scale) : null;
        var extra = (scale.MidpointLabel != null ? row + gap : 0) + (missing ? row + gap : 0);
        var available = Math.Max(0, bounds.Height - gap - extra);
        var rows = captions != null ? (int)Math.Ceiling(captions.Length / (double)columns) : 1;
        if (rows * (12 + row + gap) > available + .000001) {
            // Fit more bands in each row before sacrificing caption height. Complete intervals remain in regions.
            if (captions != null) {
                var capacity = Math.Max(1, (int)Math.Floor(available / (12 + row + gap)));
                columns = Math.Max(columns, (int)Math.Ceiling(captions.Length / (double)capacity));
                rows = (int)Math.Ceiling(captions.Length / (double)columns);
            }
            if (rows * (12 + row + gap) > available + .000001)
                builder.AddDiagnostic(new VisualDiagnostic("matrix.scale-overflow", "The numeric color key does not fit the viewport; complete scale values and captions remain in descriptive regions."));
        }
        var pitchY = available / rows; var swatchHeight = Math.Min(12, Math.Max(0, pitchY - row - gap));
        var y = bounds.Top + gap;
        var metadata = new Dictionary<string, string> { ["data-cfx-scale-mode"] = scale.Mode.ToString().ToLowerInvariant() };
        if (captions == null) {
            metadata["data-cfx-min-value"] = VisualStateSceneTools.Number(scale.EffectiveMinimum(min));
            metadata["data-cfx-max-value"] = VisualStateSceneTools.Number(scale.EffectiveMaximum(max));
            metadata["data-cfx-midpoint-value"] = VisualStateSceneTools.Number(scale.EffectiveMidpoint(min, max));
        }
        builder.AddRegion(new VisualSemanticRegion(prefix, prefix, bounds, captions != null ? string.Join("; ", captions)
            : string.Join(" – ", ChartNumericFormatter.FormatScaleValues(chart.Options, new[] { scale.EffectiveMinimum(min), scale.EffectiveMaximum(max) }))));
        using (builder.PushGroup(prefix, prefix, metadata)) using (builder.PushClip(bounds)) {
            if (captions != null) {
                var width = bounds.Width / columns;
                for (var index = 0; index < captions.Length; index++) {
                    var band = scale.Bands[index]; var left = bounds.Left + index % columns * width; var top = y + index / columns * pitchY;
                    var source = new Dictionary<string, string> {
                        ["data-cfx-band"] = index.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        ["data-cfx-label"] = captions[index],
                        ["data-cfx-lower-bound"] = index == 0 ? "" : VisualStateSceneTools.Number(scale.Bands[index - 1].UpperBound!.Value),
                        ["data-cfx-upper-bound"] = band.UpperBound.HasValue ? VisualStateSceneTools.Number(band.UpperBound.Value) : "",
                        ["data-cfx-lower-inclusive"] = "true", ["data-cfx-upper-exclusive"] = "true"
                    };
                    if (band.Label != null) source["data-cfx-band-label"] = band.Label;
                    Tile(index, new ChartRect(left, top, Math.Max(0, width - gap / 2), swatchHeight),
                        ChartColorBlend.Solid(band.Color, SvgColorRole.Ramp), captions[index], source);
                    Caption(captions[index], new ChartRect(left, top + swatchHeight + gap / 2, Math.Max(0, width - gap / 2), row), "band-" + index, TextAlignment.Center);
                }
            } else {
                var steps = ChartColorScaleLegend.Steps(scale, min, max); var width = bounds.Width / steps.Length;
                var labels = ChartNumericFormatter.FormatScaleValues(chart.Options, steps);
                for (var index = 0; index < steps.Length; index++)
                    Tile(index, new ChartRect(bounds.Left + index * width, y, width, swatchHeight), scale.BlendFor(steps[index], min, max), labels[index],
                        new Dictionary<string, string> { ["data-cfx-value"] = VisualStateSceneTools.Number(steps[index]) });
                var endpoints = ChartNumericFormatter.FormatScaleValues(chart.Options, new[] { scale.EffectiveMinimum(min), scale.EffectiveMaximum(max) });
                Caption((scale.LowLabel ?? chart.Options.Labels.Less) + " · " + endpoints[0],
                    new ChartRect(bounds.Left, y + swatchHeight + gap / 2, bounds.Width / 2, row), "low", TextAlignment.Left);
                Caption((scale.HighLabel ?? chart.Options.Labels.More) + " · " + endpoints[1],
                    new ChartRect(bounds.Left + bounds.Width / 2, y + swatchHeight + gap / 2, bounds.Width / 2, row), "high", TextAlignment.Right);
            }
            y += rows * pitchY;
            if (scale.MidpointLabel != null) {
                Caption(scale.MidpointLabel + " · " + ChartNumericFormatter.FormatScaleValues(chart.Options, new[] { scale.EffectiveMidpoint(min, max) })[0],
                    new ChartRect(bounds.Left, y, bounds.Width, row), "midpoint", TextAlignment.Center);
                y += row + gap;
            }
            if (missing) {
                var blend = missingBlend ?? ChartColorScaleSurface.NoData(scale, context.Theme.Resolve(context.ThemeMode));
                var box = new ChartRect(bounds.Left, y + row * .15, row * .7, row * .7);
                using (VisualStateSceneTools.Mark(builder, prefix + "-empty", prefix + "-empty", box, chart.Options.Labels.NoData,
                    new Dictionary<string, string> { ["data-cfx-empty"] = "true" }))
                    builder.Rect(box, blend.Color, radius: 1, role: prefix + "-missing-swatch", paint: VisualChartPaint.Fill(blend.Paint));
                Caption(chart.Options.Labels.NoData, new ChartRect(bounds.Left + row, y, Math.Max(0, bounds.Width - row), row), "missing", TextAlignment.Left);
            }
        }

        void Tile(int index, ChartRect box, ChartColorBlend blend, string label, Dictionary<string, string> source) {
            using (VisualStateSceneTools.Mark(builder, prefix + "-" + index, prefix + "-step", box, label, source))
                builder.Rect(box, blend.Color, radius: 1, role: prefix + "-swatch", paint: VisualChartPaint.Fill(blend.Paint));
        }
        void Caption(string text, ChartRect box, string id, TextAlignment alignment) =>
            VisualStateSceneTools.Text(builder, text, box, style, prefix + "-label", prefix + "-label-" + id, alignment);
    }
}

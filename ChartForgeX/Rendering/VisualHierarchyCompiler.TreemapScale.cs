using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualHierarchyCompiler {
    private static bool HasTreemapScale(Chart chart, VisualRenderContext context, ChartTreemapSurface surface) =>
        surface.Scale != null && chart.Options.Treemap.ShowColorScaleLegend && context.Frame.ShowLegend && chart.Series[0].ShowInLegend;

    private static TextStyle TreemapScaleStyle(VisualRenderContext context) => context.Frame.LegendStyle ?? new TextStyle {
        Font = context.Font, FontSize = context.Theme.Typography.LegendSize, Color = context.Theme.Resolve(context.ThemeMode).Foreground
    };

    private static (ChartRect Content, ChartRect Scale) TreemapScaleLayout(Chart chart, VisualRenderContext context, VisualSceneBuilder builder,
        ChartRect plot, ChartTreemapSurface surface) {
        if (!HasTreemapScale(chart, context, surface)) return (plot, new ChartRect(plot.X, plot.Bottom, 0, 0));
        var style = TreemapScaleStyle(context); var row = builder.MeasureText("Mg", style).Height;
        var scale = surface.Scale!;
        var numeric = scale.Mode == ChartColorScaleMode.Discrete || surface.HasDomain;
        var lines = numeric ? 2 + (scale.MidpointLabel != null ? 1 : 0) + (surface.HasMissing ? 1 : 0) : 2;
        var height = Math.Min(plot.Height * .35, row * lines + (numeric ? row * .8 : 0) + context.Theme.Spacing);
        var gap = Math.Min(context.Theme.Spacing, plot.Height * .05);
        return (new ChartRect(plot.X, plot.Y, plot.Width, Math.Max(0, plot.Height - height - gap)), new ChartRect(plot.X, plot.Bottom - height, plot.Width, height));
    }

    private static void DrawTreemapScale(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect area,
        ChartTreemapSurface surface, VisualThemeColors colors) {
        if (!HasTreemapScale(chart, context, surface) || area.Width <= 0 || area.Height <= 0) return;
        var scale = surface.Scale!; var style = TreemapScaleStyle(context); var row = builder.MeasureText("Mg", style).Height;
        var title = chart.Options.Treemap.ColorLegendTitle ?? chart.Options.Labels.Color;
        var metadata = new Dictionary<string, string> { ["data-cfx-scale-mode"] = scale.Mode.ToString().ToLowerInvariant() };
        if (surface.HasDomain && scale.Mode != ChartColorScaleMode.Discrete) {
            metadata["data-cfx-min-value"] = N(scale.EffectiveMinimum(surface.Minimum));
            metadata["data-cfx-max-value"] = N(scale.EffectiveMaximum(surface.Maximum));
            metadata["data-cfx-midpoint-value"] = N(scale.EffectiveMidpoint(surface.Minimum, surface.Maximum));
        }
        using (builder.PushGroup("treemap-color-scale", "treemap-color-scale", metadata)) using (builder.PushClip(area)) {
            VisualRadialPrimitives.Text(builder, title, new ChartRect(area.X, area.Y, area.Width, Math.Min(row, area.Height)), style,
                "treemap-color-scale-title", "treemap-color-scale-title", alignment: TextAlignment.Left);
            var swatchY = Math.Min(area.Bottom, area.Y + row + 3);
            var numeric = scale.Mode == ChartColorScaleMode.Discrete || surface.HasDomain;
            var swatchHeight = numeric ? Math.Min(row * .7, Math.Max(0, area.Bottom - swatchY - row)) : 0;
            var labelY = swatchY + swatchHeight + 3;
            if (scale.Mode == ChartColorScaleMode.Discrete) {
                var captions = ChartColorScaleLegend.BandCaptions(chart.Options, scale);
                for (var i = 0; i < scale.Bands.Count; i++) {
                    var band = scale.Bands[i]; var width = area.Width / scale.Bands.Count;
                    var source = new Dictionary<string, string> {
                        ["data-cfx-band"] = N(i), ["data-cfx-label"] = captions[i],
                        ["data-cfx-lower-bound"] = i == 0 ? "" : N(scale.Bands[i - 1].UpperBound!.Value),
                        ["data-cfx-upper-bound"] = band.UpperBound.HasValue ? N(band.UpperBound.Value) : "",
                        ["data-cfx-lower-inclusive"] = "true", ["data-cfx-upper-exclusive"] = "true"
                    };
                    if (band.Label != null) source["data-cfx-band-label"] = band.Label;
                    using (builder.PushGroup(null, "treemap-color-scale-step-source", source)) {
                        var blend = ChartColorBlend.Solid(band.Color, SvgColorRole.Ramp);
                        builder.Rect(new ChartRect(area.X + i * width, swatchY, width, swatchHeight), blend.Color,
                            role: "treemap-color-scale-step", paint: VisualChartPaint.Fill(blend.Paint));
                    }
                    ScaleCaption(captions[i], area.X + i * width, labelY, width, TextAlignment.Center, "band-" + i);
                }
            } else if (surface.HasDomain) {
                var steps = ChartColorScaleLegend.Steps(scale, surface.Minimum, surface.Maximum); var width = area.Width / steps.Length;
                for (var i = 0; i < steps.Length; i++) using (builder.PushGroup(null, "treemap-color-scale-step-source", new Dictionary<string, string> { ["data-cfx-value"] = N(steps[i]) })) {
                    var blend = scale.BlendFor(steps[i], surface.Minimum, surface.Maximum);
                    builder.Rect(new ChartRect(area.X + i * width, swatchY, width, swatchHeight), blend.Color,
                        role: "treemap-color-scale-step", paint: VisualChartPaint.Fill(blend.Paint));
                }
                var values = ChartNumericFormatter.FormatScaleValues(chart.Options, new[] { scale.EffectiveMinimum(surface.Minimum), scale.EffectiveMaximum(surface.Maximum) });
                ScaleCaption((scale.LowLabel ?? chart.Options.Labels.Less) + " · " + values[0], area.X, labelY, area.Width / 2, TextAlignment.Left, "low");
                ScaleCaption((scale.HighLabel ?? chart.Options.Labels.More) + " · " + values[1], area.X + area.Width / 2, labelY, area.Width / 2, TextAlignment.Right, "high");
                if (scale.MidpointLabel != null) {
                    var text = scale.MidpointLabel + " · " + ChartNumericFormatter.FormatScaleValues(chart.Options, new[] { scale.EffectiveMidpoint(surface.Minimum, surface.Maximum) })[0];
                    ScaleCaption(text, area.X, labelY + row, area.Width, TextAlignment.Center, "midpoint");
                }
            }
            if (surface.HasMissing) {
                var missingY = numeric ? area.Bottom - row : swatchY;
                var height = Math.Min(row, Math.Max(0, area.Bottom - missingY));
                var blend = ChartColorScaleSurface.NoData(scale, colors);
                builder.Rect(new ChartRect(area.X, missingY + height * .15, height * .7, height * .7), blend.Color,
                    role: "treemap-color-scale-missing", paint: VisualChartPaint.Fill(blend.Paint));
                ScaleCaption(chart.Options.Labels.NoData, area.X + height, missingY, Math.Max(0, area.Width - height), TextAlignment.Left, "missing");
            }
        }

        void ScaleCaption(string full, double x, double y, double width, TextAlignment alignment, string id) {
            VisualRadialPrimitives.Text(builder, full, new ChartRect(x, y, width, Math.Min(row, Math.Max(0, area.Bottom - y))), style,
                "treemap-color-scale-label", "treemap-color-scale-label-" + id, alignment: alignment);
        }
    }
}

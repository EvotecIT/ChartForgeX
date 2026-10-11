using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    /// <summary>Composes per-series bottom keys within the frame's combined categorical and numeric legend budget.</summary>
    private static ChartRect ColorScaleViewport(Chart chart, VisualRenderContext context, VisualSceneBuilder builder,
        ChartRect viewport, ChartColorPresentationSet surfaces, double frameLegendHeight) {
        if (!context.Frame.ShowLegend) return viewport;
        var keys = Enumerable.Range(0, chart.Series.Count).Where(index => chart.Series[index].ShowInLegend
            && surfaces[index]?.Scale != null && surfaces[index]!.Count > 0).ToArray();
        if (keys.Length == 0) return viewport;
        var style = context.Frame.LegendStyle ?? VisualStateSceneTools.TickStyle(chart, context);
        var row = builder.MeasureText("Mg", style).Height; var gap = context.Theme.Spacing / 2;
        var keyStyle = style; var compact = false;
        var missingWidths = new double[chart.Series.Count];
        // Headers and categorical legends have already consumed space. Preserve the same body fraction for axes and marks.
        var budget = Math.Max(0, Math.Min(viewport.Height * context.Frame.LegendMaximumHeightFraction,
            context.Layout.Size.Height * context.Frame.LegendMaximumHeightFraction - frameLegendHeight));
        double[] Measure() => keys.Select(index => {
            // A missing swatch can share its source heading when both complete captions fit at their measured sizes.
            var missingWidth = builder.MeasureText("Mg", keyStyle).Height + builder.MeasureText(surfaces[index]!.Presentation.NoData, keyStyle).Width + gap;
            var title = surfaces[index]!.SeriesName + " · " + surfaces[index]!.Presentation.Name;
            missingWidths[index] = compact && surfaces[index]!.Missing && builder.MeasureText(title, style).Width + gap + missingWidth <= viewport.Width ? missingWidth : 0;
            return row + VisualColorScaleLegendScene.Height(builder, surfaces[index]!.Scale!, surfaces[index]!.Presentation.Labels,
                keyStyle, gap, viewport.Width, double.MaxValue, surfaces[index]!.Missing, surfaces[index]!.Presentation.ScaleValues,
                surfaces[index]!.HasDomain, compact, surfaces[index]!.Minimum, surfaces[index]!.Maximum, missingWidths[index] > 0);
        }).ToArray();
        var heights = Measure(); var requested = heights.Sum() + gap * keys.Length;
        if (requested > budget) {
            compact = true; gap /= 2;
            keyStyle = style.Clone(); keyStyle.FontSize = style.EffectiveFontSize; keyStyle.Baseline = TextBaseline.Normal;
            var minimum = Math.Min(8, style.EffectiveFontSize);
            heights = Measure(); requested = heights.Sum() + gap * keys.Length;
            while (requested > budget && keyStyle.FontSize > minimum) {
                keyStyle.FontSize = Math.Max(minimum, keyStyle.FontSize - .25);
                heights = Measure(); requested = heights.Sum() + gap * keys.Length;
            }
        }
        var height = Math.Min(requested, budget);
        if (height + .000001 < requested) builder.AddDiagnostic(new VisualDiagnostic("cartesian.color-scale-overflow",
            "Some numeric color keys do not fit the combined legend budget; complete captions and domains remain in descriptive regions."));
        var y = viewport.Bottom - height;
        for (var ordinal = 0; ordinal < keys.Length; ordinal++) {
            var key = keys[ordinal]; var surface = surfaces[key]!; var scale = surface.Scale!;
            var allocation = requested > 0 ? height * (heights[ordinal] + gap) / requested : 0;
            var titleHeight = Math.Min(row, Math.Max(0, allocation - gap));
            var bounds = new ChartRect(viewport.Left, y, Math.Max(0, viewport.Width - missingWidths[key] - (missingWidths[key] > 0 ? gap : 0)), titleHeight);
            var prefix = "color-scale-" + key;
            var title = surface.SeriesName + " · " + surface.Presentation.Name;
            using (builder.PushGroup(prefix + "-source", "color-scale-source", new Dictionary<string, string> {
                ["data-cfx-series"] = Number(key), ["data-cfx-series-key"] = chart.Series[key].InteractionIdentityKey,
                ["data-cfx-label"] = title, ["data-cfx-label-color"] = surface.Presentation.Name, ["data-cfx-label-no-data"] = surface.Presentation.NoData
            })) {
                VisualStateSceneTools.Text(builder, title, bounds, style, "color-scale-title", prefix + "-title");
                VisualColorScaleLegendScene.Draw(builder, scale, surface.Presentation.Labels, keyStyle, gap,
                    new ChartRect(viewport.Left, y + titleHeight, viewport.Width, Math.Max(0, allocation - titleHeight - gap)),
                    surface.Minimum, surface.Maximum, prefix, surface.Missing, ChartColorScaleSurface.NoData(scale, context.Theme.Resolve(context.ThemeMode)),
                    surface.Presentation.ScaleValues, "cartesian.color-scale-overflow", surface.HasDomain, compact,
                    missingWidths[key] > 0 ? new ChartRect(viewport.Right - missingWidths[key], y, missingWidths[key], titleHeight) : null,
                    valueName: surface.Presentation.HasOverrides ? surface.Presentation.Name : null);
            }
            y += allocation;
        }
        return new ChartRect(viewport.Left, viewport.Top, viewport.Width, Math.Max(0, viewport.Height - height));
    }
}

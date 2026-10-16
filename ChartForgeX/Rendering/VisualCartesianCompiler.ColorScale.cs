using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    /// <summary>Composes per-series bottom keys within the frame's combined categorical and numeric legend budget.</summary>
    private static ChartRect NumericScaleViewport(Chart chart, VisualRenderContext context, VisualSceneBuilder builder,
        ChartRect viewport, ChartColorPresentationSet surfaces, double frameLegendHeight, ChartBubbleSizeScale? bubbleScale,
        out VisualBubbleSizeKeyScene? sizeKey) {
        sizeKey = null;
        if (!context.Frame.ShowLegend) return viewport;
        var keys = Enumerable.Range(0, chart.Series.Count).Where(index => surfaces[index]?.Presentation.ShowLegend == true
            && surfaces[index]?.Scale != null && surfaces[index]!.Count > 0).ToArray();
        var showSize = bubbleScale?.HasValues == true && bubbleScale.Presentation.ShowLegend;
        if (keys.Length == 0 && !showSize) return viewport;
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
                surfaces[index]!.HasDomain, compact, surfaces[index]!.Minimum, surfaces[index]!.Maximum, missingWidths[index] > 0, legendStyle: surfaces[index]!.Presentation.LegendStyle);
        }).ToArray();
        if (showSize) sizeKey = VisualBubbleSizeKeyScene.Measure(builder, bubbleScale!, style, gap, viewport, compact: false);
        var heights = Measure(); var requested = heights.Sum() + gap * keys.Length + (sizeKey?.Height ?? 0);
        if (requested > budget) {
            compact = true; gap /= 2;
            keyStyle = style.Clone(); keyStyle.FontSize = style.EffectiveFontSize; keyStyle.Baseline = TextBaseline.Normal;
            var minimum = Math.Min(VisualTextFitting.MinimumOrdinaryFontSize, style.EffectiveFontSize);
            if (showSize) sizeKey = VisualBubbleSizeKeyScene.Measure(builder, bubbleScale!, style, gap, viewport, compact: true);
            heights = Measure(); requested = heights.Sum() + gap * keys.Length + (sizeKey?.Height ?? 0);
            if (showSize && keys.Length == 1 && TrySharedNumericKeyRow(chart, context, builder, viewport, surfaces[keys[0]]!,
                keys[0], bubbleScale!, style, gap, budget, out var sharedViewport, out var sharedSizeKey)) {
                sizeKey = sharedSizeKey;
                return sharedViewport;
            }
            while (requested > budget && keyStyle.FontSize > minimum) {
                keyStyle.FontSize = Math.Max(minimum, keyStyle.FontSize - .25);
                heights = Measure(); requested = heights.Sum() + gap * keys.Length + (sizeKey?.Height ?? 0);
            }
        }
        if (sizeKey != null && !sizeKey.Fits && !compact) sizeKey = VisualBubbleSizeKeyScene.Measure(builder, bubbleScale!, style, gap, viewport, compact: true);
        if (sizeKey != null && (!sizeKey.Fits || heights.Sum() + gap * keys.Length + sizeKey.Height > budget + .000001)) {
            sizeKey.Omit();
            builder.AddDiagnostic(new VisualDiagnostic("cartesian.size-scale-overflow",
                "The numeric size key does not fit the combined legend budget; complete source-domain captions remain in descriptive regions."));
        }
        var sizeHeight = sizeKey?.Height ?? 0;
        requested = heights.Sum() + gap * keys.Length;
        var height = Math.Min(requested, Math.Max(0, budget - sizeHeight));
        if (height + .000001 < requested) builder.AddDiagnostic(new VisualDiagnostic("cartesian.color-scale-overflow",
            "Some numeric color keys do not fit the combined legend budget; complete captions and domains remain in descriptive regions."));
        var y = viewport.Bottom - sizeHeight - height;
        if (sizeKey != null) sizeKey.Bounds = new ChartRect(viewport.Left, viewport.Bottom - sizeHeight, viewport.Width, sizeHeight);
        for (var ordinal = 0; ordinal < keys.Length; ordinal++) {
            var key = keys[ordinal]; var surface = surfaces[key]!;
            var allocation = requested > 0 ? height * (heights[ordinal] + gap) / requested : 0;
            var titleHeight = Math.Min(row, Math.Max(0, allocation - gap));
            var title = surface.SeriesName + " · " + surface.Presentation.Name;
            DrawNumericColorKey(chart, context, builder, surface, key, new ChartRect(viewport.Left, y, viewport.Width, allocation),
                title, style, keyStyle, gap, titleHeight, compact, missingWidths[key]);
            y += allocation;
        }
        return new ChartRect(viewport.Left, viewport.Top, viewport.Width, Math.Max(0, viewport.Height - height - sizeHeight));
    }

    /// <summary>Places independent numeric dimensions alongside each other before omitting a normal compact key.</summary>
    private static bool TrySharedNumericKeyRow(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect viewport,
        ChartSeriesColorSurface surface, int index, ChartBubbleSizeScale bubbleScale, TextStyle style, double gap, double budget,
        out ChartRect content, out VisualBubbleSizeKeyScene sizeKey) {
        content = viewport;
        sizeKey = VisualBubbleSizeKeyScene.Measure(builder, bubbleScale, style, gap, viewport, compact: true);
        var sizeWidth = Math.Ceiling(sizeKey.RequiredWidth); var colorWidth = viewport.Width - sizeWidth - gap * 2;
        var titleWidth = Math.Ceiling(builder.MeasureText(surface.Presentation.Name, style).Width) + 1;
        if (!sizeKey.Fits || colorWidth < titleWidth) return false;
        var samples = surface.Presentation.ScaleValues(new[] { surface.Scale!.EffectiveMinimum(surface.Minimum), surface.Scale.EffectiveMaximum(surface.Maximum) });
        if (samples.Sum(value => Math.Ceiling(builder.MeasureText(value, style).Width) + 1) + gap > colorWidth) return false;
        var headingHeight = builder.MeasureText("Mg", style).Height;
        var keyHeight = headingHeight + gap + VisualColorScaleLegendScene.Height(builder, surface.Scale!, surface.Presentation.Labels,
            style, gap, colorWidth, double.MaxValue, surface.Missing, surface.Presentation.ScaleValues, surface.HasDomain,
            compact: true, min: surface.Minimum, max: surface.Maximum, legendStyle: surface.Presentation.LegendStyle);
        var height = Math.Max(keyHeight, sizeKey.Height);
        if (height > budget + .000001) return false;
        var top = viewport.Bottom - height;
        sizeKey.Bounds = new ChartRect(viewport.Right - sizeWidth, top, sizeWidth, sizeKey.Height);
        // A single numeric color source is already identified by its categorical entry and complete semantic source caption.
        DrawNumericColorKey(chart, context, builder, surface, index, new ChartRect(viewport.Left, top, colorWidth, keyHeight),
            surface.Presentation.Name, style, style, gap, headingHeight, compact: true, missingWidth: 0);
        content = new ChartRect(viewport.Left, viewport.Top, viewport.Width, Math.Max(0, viewport.Height - height));
        return true;
    }

    private static void DrawNumericColorKey(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartSeriesColorSurface surface,
        int index, ChartRect area, string title, TextStyle titleStyle, TextStyle keyStyle, double gap, double titleHeight, bool compact, double missingWidth) {
        var prefix = "color-scale-" + index; var scale = surface.Scale!;
        var heading = new ChartRect(area.Left, area.Top, Math.Max(0, area.Width - missingWidth - (missingWidth > 0 ? gap : 0)), titleHeight);
        using (builder.PushGroup(prefix + "-source", "color-scale-source", new Dictionary<string, string> {
            ["data-cfx-series"] = Number(index), ["data-cfx-series-key"] = chart.Series[index].InteractionIdentityKey,
            ["data-cfx-label"] = surface.SeriesName + " · " + surface.Presentation.Name,
            ["data-cfx-label-color"] = surface.Presentation.Name, ["data-cfx-label-no-data"] = surface.Presentation.NoData
        })) {
            VisualStateSceneTools.Text(builder, title, heading, titleStyle, "color-scale-title", prefix + "-title");
            VisualColorScaleLegendScene.Draw(builder, scale, surface.Presentation.Labels, keyStyle, gap,
                new ChartRect(area.Left, area.Top + titleHeight, area.Width, Math.Max(0, area.Height - titleHeight - gap)),
                surface.Minimum, surface.Maximum, prefix, surface.Missing, ChartColorScaleSurface.NoData(scale, context.Theme.Resolve(context.ThemeMode)),
                surface.Presentation.ScaleValues, "cartesian.color-scale-overflow", surface.HasDomain, compact,
                missingWidth > 0 ? new ChartRect(area.Right - missingWidth, area.Top, missingWidth, titleHeight) : null,
                valueName: surface.Presentation.HasOverrides ? surface.Presentation.Name : null, legendStyle: surface.Presentation.LegendStyle);
        }
    }
}

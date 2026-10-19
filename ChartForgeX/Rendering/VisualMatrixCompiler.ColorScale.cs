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

    private static double ColorScaleHeight(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, double width, double maximum, ChartColorPresentation presentation,
        bool missing = false, bool hasDomain = true, double min = 0, double max = 0) =>
        VisualColorScaleLegendScene.Height(builder, chart.Options.HeatmapColorScale!, presentation.Labels, ColorScaleStyle(chart, context),
            context.Theme.Spacing / 2, width, maximum, missing, presentation.ScaleValues, hasDomain: hasDomain, min: min, max: max,
            heading: presentation.HasName ? presentation.Name : null, legendStyle: presentation.LegendStyle);

    private static void ColorScale(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect bounds,
        double min, double max, string prefix, ChartColorPresentation presentation, bool missing = false, ChartColorBlend? missingBlend = null, bool hasDomain = true) {
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        var scale = chart.Options.HeatmapColorScale!;
        VisualColorScaleLegendScene.Draw(builder, scale, presentation.Labels, ColorScaleStyle(chart, context), context.Theme.Spacing / 2,
            bounds, min, max, prefix, missing, missingBlend ?? ChartColorScaleSurface.NoData(scale, context.Theme.Resolve(context.ThemeMode)),
            presentation.ScaleValues, hasDomain: hasDomain, heading: presentation.HasName ? presentation.Name : null, valueName: presentation.HasOverrides ? presentation.Name : null, legendStyle: presentation.LegendStyle);
    }
}

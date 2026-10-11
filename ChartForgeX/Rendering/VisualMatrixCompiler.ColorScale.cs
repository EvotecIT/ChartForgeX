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

    private static double ColorScaleHeight(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, double width, double maximum, bool missing = false) =>
        VisualColorScaleLegendScene.Height(builder, chart.Options.HeatmapColorScale!, chart.Options.Labels, ColorScaleStyle(chart, context),
            context.Theme.Spacing / 2, width, maximum, missing, values => ChartNumericFormatter.FormatScaleValues(chart.Options, values));

    private static void ColorScale(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect bounds,
        double min, double max, string prefix, bool missing = false, ChartColorBlend? missingBlend = null) {
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        var scale = chart.Options.HeatmapColorScale!;
        VisualColorScaleLegendScene.Draw(builder, scale, chart.Options.Labels, ColorScaleStyle(chart, context), context.Theme.Spacing / 2,
            bounds, min, max, prefix, missing, missingBlend ?? ChartColorScaleSurface.NoData(scale, context.Theme.Resolve(context.ThemeMode)),
            values => ChartNumericFormatter.FormatScaleValues(chart.Options, values));
    }
}

using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualMapCompiler {
    private static ChartColorLegendStyle CaptionedMapLegendStyle(Chart chart, ChartColorPresentation presentation) =>
        chart.Options.MapScaleLegendPosition == ChartMapScaleLegendPosition.Right && presentation.LegendStyle == ChartColorLegendStyle.Auto
            ? chart.Options.MapColorScale!.HasPositionedStops ? ChartColorLegendStyle.Swatches : ChartColorLegendStyle.Gradient
            : presentation.LegendStyle;

    private static MapLayout CaptionedScaleLayout(Chart chart, VisualRenderContext context, VisualSceneBuilder builder,
        ChartRect plot, double min, double max, ChartColorPresentation presentation) {
        var scale = chart.Options.MapColorScale!; var style = TickStyle(chart, context); var gap = context.Theme.Spacing;
        var hasDomain = MapHasDomain(chart, scale, min, max); var missing = HasMissingMapValues(chart);
        var legendStyle = CaptionedMapLegendStyle(chart, presentation);
        if (chart.Options.MapScaleLegendPosition == ChartMapScaleLegendPosition.Right) {
            var title = presentation.HasName ? presentation.Name : chart.Series[0].Name;
            var width = VisualColorScaleLegendScene.CaptionedVerticalWidth(builder, scale, presentation.Labels, style, gap / 2,
                plot.Width * .4, min, max, hasDomain, presentation.ScaleValues, title, missing, legendStyle);
            return new MapLayout(new ChartRect(plot.Left, plot.Top, System.Math.Max(0, plot.Width - width - gap), plot.Height),
                new ChartRect(plot.Right - width, plot.Top, width, plot.Height), System.Array.Empty<string>(), true);
        }
        var height = VisualColorScaleLegendScene.Height(builder, scale, presentation.Labels, style, gap / 2,
            plot.Width, plot.Height * .35, missing, presentation.ScaleValues, hasDomain, min: min, max: max,
            heading: presentation.HasName ? presentation.Name : null, legendStyle: legendStyle);
        return new MapLayout(new ChartRect(plot.Left, plot.Top, plot.Width, System.Math.Max(0, plot.Height - height - gap)),
            new ChartRect(plot.Left, plot.Bottom - height, plot.Width, height), System.Array.Empty<string>(), false);
    }

    private static void DrawCaptionedScale(Chart chart, VisualRenderContext context, VisualSceneBuilder builder,
        MapLayout layout, VisualThemeColors colors, double min, double max, ChartColorPresentation presentation) {
        var scale = chart.Options.MapColorScale!;
        VisualColorScaleLegendScene.DrawCaptioned(builder, scale, presentation.Labels, TickStyle(chart, context), context.Theme.Spacing / 2,
            layout.Scale, min, max, "map-scale", HasMissingMapValues(chart), ChartColorScaleSurface.NoData(scale, colors),
            presentation.ScaleValues, "map.scale-overflow", MapHasDomain(chart, scale, min, max), null,
            presentation.HasName ? presentation.Name : layout.Right ? chart.Series[0].Name : null,
            presentation.HasOverrides ? presentation.Name : null, CaptionedMapLegendStyle(chart, presentation), vertical: layout.Right);
    }
}

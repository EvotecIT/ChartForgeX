using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualMapCompiler {
    private static bool MapHasDomain(Chart chart, ChartColorScale scale, double min, double max) =>
        ChartColorScaleSurface.HasDomain(scale, chart.Series[0].RenderPoints.Count > 0 ? min : null, chart.Series[0].RenderPoints.Count > 0 ? max : null);

    private static MapLayout MissingScaleLayout(Chart chart, VisualRenderContext context, VisualSceneBuilder builder,
        ChartRect plot, ChartColorPresentation presentation) {
        var style = TickStyle(chart, context); var gap = context.Theme.Spacing;
        if (chart.Options.MapScaleLegendPosition == ChartMapScaleLegendPosition.Right) {
            var title = presentation.HasName ? presentation.Name : chart.Series[0].Name;
            var width = Math.Min(plot.Width * .4, Math.Max(builder.MeasureText(title, style).Width,
                builder.MeasureText(presentation.NoData, style).Width) + builder.MeasureText("Mg", style).Height + gap * 3);
            return new MapLayout(new ChartRect(plot.Left, plot.Top, Math.Max(0, plot.Width - width - gap), plot.Height),
                new ChartRect(plot.Right - width, plot.Top, width, plot.Height), Array.Empty<string>(), true);
        }
        var height = VisualColorScaleLegendScene.Height(builder, chart.Options.MapColorScale!, presentation.Labels, style,
            gap / 2, plot.Width, plot.Height * .35, true, presentation.ScaleValues, hasDomain: false,
            heading: presentation.HasName ? presentation.Name : null);
        return new MapLayout(new ChartRect(plot.Left, plot.Top, plot.Width, Math.Max(0, plot.Height - height - gap)),
            new ChartRect(plot.Left, plot.Bottom - height, plot.Width, height), Array.Empty<string>(), false);
    }

    private static void DrawMissingScale(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, MapLayout layout,
        VisualThemeColors colors, ChartColorScale scale, ChartColorPresentation presentation) {
        var heading = presentation.HasName ? presentation.Name : layout.Right ? chart.Series[0].Name : null;
        using (builder.PushClip(layout.Scale)) VisualColorScaleLegendScene.Draw(builder, scale, presentation.Labels,
            TickStyle(chart, context), context.Theme.Spacing / 2, layout.Scale, 0, 0, "map-scale", true,
            ChartColorScaleSurface.NoData(scale, colors), presentation.ScaleValues, hasDomain: false,
            heading: heading, legendStyle: presentation.LegendStyle);
    }
}

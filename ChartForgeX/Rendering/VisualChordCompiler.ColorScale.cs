using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualChordCompiler {
    /// <summary>Allocates the shared independent numeric key before computing any weighted ribbon geometry.</summary>
    private static ChartRect ColorScale(VisualRenderContext context, VisualSceneBuilder builder,
        ChartRect plot, ChartChordColorSurface surface, VisualThemeColors colors) {
        if (surface.Scale == null || !surface.Presentation.ShowLegend || !context.Frame.ShowLegend) return plot;
        var style = context.Frame.LegendStyle ?? new TextStyle {
            Font = context.Font, FontSize = context.Theme.Typography.LegendSize, Color = colors.Foreground
        };
        var presentation = surface.Presentation;
        var gap = context.Theme.Spacing / 2;
        var maximum = Math.Max(0, plot.Height * .35);
        var height = VisualColorScaleLegendScene.Height(builder, surface.Scale, presentation.Labels, style,
            gap, plot.Width, maximum, surface.HasMissing, presentation.ScaleValues, hasDomain: surface.HasDomain,
            compact: true, min: surface.Minimum, max: surface.Maximum, heading: presentation.Name, legendStyle: presentation.LegendStyle);
        var scale = new ChartRect(plot.Left, plot.Bottom - height, plot.Width, height);
        VisualColorScaleLegendScene.Draw(builder, surface.Scale, presentation.Labels, style, gap, scale,
            surface.Minimum, surface.Maximum, "chord-color-scale", surface.HasMissing, ChartColorScaleSurface.NoData(surface.Scale, colors),
            presentation.ScaleValues, "chord.scale-overflow", hasDomain: surface.HasDomain, compact: true,
            heading: presentation.Name, valueName: presentation.Name, legendStyle: presentation.LegendStyle);
        return new ChartRect(plot.Left, plot.Top, plot.Width, Math.Max(0, plot.Height - height - Math.Min(gap, plot.Height * .05)));
    }
}

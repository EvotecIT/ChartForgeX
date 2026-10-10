using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

/// <summary>Shared numeric-scale defaults and missing-value paint, with native and SVG provenance.</summary>
internal static class ChartColorScaleSurface {
    internal static ChartColorScale Default(VisualThemeColors colors) => colors.SequentialRamp.Count >= 2
        ? ChartColorScale.Sequential(colors.SequentialRamp)
        : colors.SequentialRamp.Count == 1 ? ChartColorScale.Sequential(colors.SequentialRamp[0], colors.SequentialRamp[0])
            : ChartColorScale.Sequential(colors.Surface, colors.Palette[0]);

    internal static ChartColorBlend NoData(ChartColorScale? scale, VisualThemeColors colors) => scale?.NoDataColor is ChartColor color
        ? ChartColorBlend.Solid(color, SvgColorRole.Ramp)
        : new ChartColorBlend(colors.Surface, SvgColorRole.Surface, colors.Border, SvgColorRole.Grid, .46);
}

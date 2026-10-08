using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static class ChartDottedMapSurface {
    public static ChartColor LandDotColor(ChartColor plotBackground, ChartColor mutedText) => LandDotBlend(plotBackground, mutedText).Color;

    /// <summary>Retains the surface and text operands used by land-dot tinting.</summary>
    internal static ChartColorBlend LandDotBlend(ChartColor plotBackground, ChartColor mutedText) {
        var weight = IsLightSurface(plotBackground) ? 0.62 : 0.58;
        return new ChartColorBlend(plotBackground, SvgColorRole.Surface, mutedText, SvgColorRole.Text, weight);
    }

    public static double LandDotOpacity(ChartColor plotBackground) => IsLightSurface(plotBackground) ? 0.26 : 0.50;

    public static ChartColor LandAreaColor(ChartColor plotBackground, ChartColor mutedText) => LandAreaBlend(plotBackground, mutedText).Color;

    /// <summary>Retains the surface and text operands used by filled land outlines.</summary>
    internal static ChartColorBlend LandAreaBlend(ChartColor plotBackground, ChartColor mutedText) {
        var weight = IsLightSurface(plotBackground) ? 0.24 : 0.28;
        return new ChartColorBlend(plotBackground, SvgColorRole.Surface, mutedText, SvgColorRole.Text, weight);
    }

    public static double LandAreaOpacity(ChartColor plotBackground) => IsLightSurface(plotBackground) ? 0.92 : 0.30;

    public static ChartColor BoundaryColor(ChartColor plotBackground, ChartColor mutedText) => BoundaryBlend(plotBackground, mutedText).Color;

    /// <summary>Retains the surface and text operands used by geographic boundary strokes.</summary>
    internal static ChartColorBlend BoundaryBlend(ChartColor plotBackground, ChartColor mutedText) {
        var weight = IsLightSurface(plotBackground) ? 0.68 : 0.70;
        return new ChartColorBlend(plotBackground, SvgColorRole.Surface, mutedText, SvgColorRole.Text, weight);
    }

    public static double BoundaryOpacity(ChartColor plotBackground) => IsLightSurface(plotBackground) ? 0.30 : 0.30;

    public static bool IsLightSurface(ChartColor color) => ChartColorMath.RelativeLuminance(color) > 0.70;
}

using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static class ChartDottedMapSurface {
    public static ChartColor LandDotColor(ChartColor plotBackground, ChartColor mutedText) {
        var weight = IsLightSurface(plotBackground) ? 0.62 : 0.58;
        return ChartColorMath.Blend(plotBackground, mutedText, weight);
    }

    public static double LandDotOpacity(ChartColor plotBackground) => IsLightSurface(plotBackground) ? 0.26 : 0.50;

    public static ChartColor LandAreaColor(ChartColor plotBackground, ChartColor mutedText) {
        var weight = IsLightSurface(plotBackground) ? 0.24 : 0.28;
        return ChartColorMath.Blend(plotBackground, mutedText, weight);
    }

    public static double LandAreaOpacity(ChartColor plotBackground) => IsLightSurface(plotBackground) ? 0.92 : 0.30;

    public static ChartColor BoundaryColor(ChartColor plotBackground, ChartColor mutedText) {
        var weight = IsLightSurface(plotBackground) ? 0.68 : 0.70;
        return ChartColorMath.Blend(plotBackground, mutedText, weight);
    }

    public static double BoundaryOpacity(ChartColor plotBackground) => IsLightSurface(plotBackground) ? 0.30 : 0.30;

    public static bool IsLightSurface(ChartColor color) => ChartColorMath.RelativeLuminance(color) > 0.70;
}

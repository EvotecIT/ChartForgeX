using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static class ChartSurfacePolish {
    public static string CssGradient(ChartColor color) {
        if (color.A == 0) return "transparent";
        return "linear-gradient(180deg," + GradientTop(color).ToCss() + " 0%," + GradientBottom(color).ToCss() + " 100%)";
    }

    public static ChartColor GradientTop(ChartColor color) {
        if (color.A == 0) return color;
        var blend = ChartColorMath.RelativeLuminance(color) > 0.42
            ? ChartVisualPrimitives.SurfaceGradientLightTopBlend
            : ChartVisualPrimitives.SurfaceGradientDarkTopBlend;
        return PreserveAlpha(ChartColorMath.Blend(color, ChartColor.White, blend), color);
    }

    public static ChartColor GradientBottom(ChartColor color) {
        if (color.A == 0) return color;
        var target = ChartColorMath.RelativeLuminance(color) > 0.42
            ? ChartColor.FromRgb(15, 23, 42)
            : ChartColor.FromRgb(0, 0, 0);
        var blend = ChartColorMath.RelativeLuminance(color) > 0.42
            ? ChartVisualPrimitives.SurfaceGradientLightBottomBlend
            : ChartVisualPrimitives.SurfaceGradientDarkBottomBlend;
        return PreserveAlpha(ChartColorMath.Blend(color, target, blend), color);
    }

    public static ChartColor PreserveAlpha(ChartColor color, ChartColor source) =>
        ChartColor.FromRgba(color.R, color.G, color.B, source.A);

    public static double EdgeSafeSurfaceInset(double width, double height) =>
        Math.Max(12, Math.Round(Math.Min(width, height) * 0.015));
}

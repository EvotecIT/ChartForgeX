using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static class ChartColorMath {
    public static ChartColor Blend(ChartColor a, ChartColor b, double amount) {
        amount = Clamp01(amount);
        var r = (byte)Math.Round(a.R + (b.R - a.R) * amount);
        var g = (byte)Math.Round(a.G + (b.G - a.G) * amount);
        var bl = (byte)Math.Round(a.B + (b.B - a.B) * amount);
        var alpha = (byte)Math.Round(a.A + (b.A - a.A) * amount);
        return new ChartColor(r, g, bl, alpha);
    }

    public static ChartColor WithOpacity(ChartColor color, double opacity) {
        var alpha = (byte)Math.Round(color.A * Clamp01(opacity));
        return ChartColor.FromRgba(color.R, color.G, color.B, alpha);
    }

    public static double RelativeLuminance(ChartColor color) =>
        (0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B) / 255.0;

    public static ChartColor TextOnBackground(ChartColor background, double lightThreshold = 0.54) =>
        RelativeLuminance(background) > lightThreshold ? ChartColor.FromRgb(15, 23, 42) : ChartColor.White;

    private static double Clamp01(double value) {
        if (double.IsNaN(value)) return 0;
        if (value < 0) return 0;
        return value > 1 ? 1 : value;
    }
}

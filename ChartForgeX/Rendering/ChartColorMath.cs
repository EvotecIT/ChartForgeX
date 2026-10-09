using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static class ChartColorMath {
    /// <summary>Interpolates RGBA paints in premultiplied sRGB, matching CSS color-mix().</summary>
    internal static ChartColor BlendPremultiplied(ChartColor from, ChartColor to, double amount) {
        amount = Clamp01(amount);
        var a = from.A * (1 - amount); var b = to.A * amount; var alpha = a + b;
        if (alpha <= 0) return ChartColor.Transparent;
        byte Channel(byte first, byte second) => (byte)Math.Round((first * a + second * b) / alpha);
        return ChartColor.FromRgba(Channel(from.R, to.R), Channel(from.G, to.G), Channel(from.B, to.B), (byte)Math.Round(alpha));
    }

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

    /// <summary>Composites an RGBA paint onto an already resolved opaque surface in sRGB.</summary>
    internal static ChartColor CompositeOverOpaque(ChartColor top, ChartColor bottom) {
        var alpha = top.A / 255.0;
        return ChartColor.FromRgb(
            (byte)Math.Round(top.R * alpha + bottom.R * (1 - alpha)),
            (byte)Math.Round(top.G * alpha + bottom.G * (1 - alpha)),
            (byte)Math.Round(top.B * alpha + bottom.B * (1 - alpha)));
    }

    public static double RelativeLuminance(ChartColor color) =>
        (0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B) / 255.0;

    public static ChartColor TextOnBackground(ChartColor background, double lightThreshold = 0.54) =>
        RelativeLuminance(background) > lightThreshold ? ChartColor.FromRgb(15, 23, 42) : ChartColor.White;

    /// <summary>Chooses opaque black or white by WCAG contrast, guaranteeing at least 4.5:1 on an opaque fill.</summary>
    public static ChartColor AccessibleTextOnBackground(ChartColor background) =>
        ContrastRatio(ChartColor.Black, background) >= ContrastRatio(ChartColor.White, background) ? ChartColor.Black : ChartColor.White;

    /// <summary>Returns the WCAG 2 contrast ratio (1 to 21) of two opaque colours; alpha is ignored.</summary>
    public static double ContrastRatio(ChartColor first, ChartColor second) {
        var a = WcagLuminance(first);
        var b = WcagLuminance(second);
        return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
    }

    private static double WcagLuminance(ChartColor color) =>
        0.2126 * LinearChannel(color.R) + 0.7152 * LinearChannel(color.G) + 0.0722 * LinearChannel(color.B);

    private static double LinearChannel(byte value) {
        var channel = value / 255.0;
        return channel <= 0.03928 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
    }

    private static double Clamp01(double value) {
        if (double.IsNaN(value)) return 0;
        if (value < 0) return 0;
        return value > 1 ? 1 : value;
    }
}

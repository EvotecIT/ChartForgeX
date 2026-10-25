using System;

namespace ChartForgeX.Raster;

/// <summary>Normalized nonseparable channel math; each caller retains its own color space and conversion precision.</summary>
internal static partial class RasterBlendFunctions {
    internal readonly struct Rgb {
        internal Rgb(double r, double g, double b) { R = r; G = g; B = b; }
        internal readonly double R, G, B;
        internal double Lum => 0.3 * R + 0.59 * G + 0.11 * B;
        internal double Sat => Math.Max(R, Math.Max(G, B)) - Math.Min(R, Math.Min(G, B));
    }
    internal static Rgb Nonseparable(Rgb s, Rgb d, int mode) => mode == 24 ? SetLum(SetSat(s, d.Sat), d.Lum) :
        mode == 25 ? SetLum(SetSat(d, s.Sat), d.Lum) : mode == 26 ? SetLum(s, d.Lum) : SetLum(d, s.Lum);
    private static Rgb SetSat(Rgb color, double saturation) {
        var minimum = Math.Min(color.R, Math.Min(color.G, color.B)); var span = color.Sat;
        return span <= 0 ? new Rgb(0, 0, 0) : new Rgb((color.R - minimum) * saturation / span, (color.G - minimum) * saturation / span, (color.B - minimum) * saturation / span);
    }
    private static Rgb SetLum(Rgb color, double luminosity) {
        var delta = luminosity - color.Lum; var r = color.R + delta; var g = color.G + delta; var b = color.B + delta;
        var minimum = Math.Min(r, Math.Min(g, b)); var maximum = Math.Max(r, Math.Max(g, b));
        if (minimum < 0) { var factor = luminosity / (luminosity - minimum); r = luminosity + (r - luminosity) * factor; g = luminosity + (g - luminosity) * factor; b = luminosity + (b - luminosity) * factor; }
        if (maximum > 1) { var factor = (1 - luminosity) / (maximum - luminosity); r = luminosity + (r - luminosity) * factor; g = luminosity + (g - luminosity) * factor; b = luminosity + (b - luminosity) * factor; }
        return new Rgb(r, g, b);
    }
}

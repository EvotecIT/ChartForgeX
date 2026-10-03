using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

/// <summary>Premultiplied linear-light sRGB for colour-font gradients and isolated composition groups.</summary>
internal readonly struct LinearRgba {
    internal readonly float R, G, B, A;
    internal LinearRgba(double r, double g, double b, double a) { R = (float)r; G = (float)g; B = (float)b; A = (float)a; }
    private static readonly double[] Linear = CreateLinearTable();
    private static double[] CreateLinearTable() {
        var table = new double[256];
        for (var i = 0; i < table.Length; i++) { var c = i / 255.0; table[i] = c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4); }
        return table;
    }
    internal static LinearRgba From(ChartColor color, double opacity = 1) {
        var alpha = color.A / 255.0 * opacity; return new LinearRgba(Linear[color.R] * alpha, Linear[color.G] * alpha, Linear[color.B] * alpha, alpha);
    }
    internal LinearRgba Opacity(double amount) => new(R * amount, G * amount, B * amount, A * amount);
    internal ChartColor ToColor(double opacity = 1) => A <= 0 ? new ChartColor(0, 0, 0, 0) :
        new ChartColor(Srgb(R / A), Srgb(G / A), Srgb(B / A), Byte(A * opacity));
    private static byte Srgb(double value) => Byte(value <= 0.0031308 ? value * 12.92 : 1.055 * Math.Pow(Math.Max(0, value), 1 / 2.4) - 0.055);
    private static byte Byte(double value) => (byte)Math.Round(Math.Max(0, Math.Min(1, value)) * 255);
    internal static LinearRgba Mix(LinearRgba left, LinearRgba right, double amount) =>
        new(left.R + (right.R - left.R) * amount, left.G + (right.G - left.G) * amount, left.B + (right.B - left.B) * amount, left.A + (right.A - left.A) * amount);
    internal static LinearRgba Over(LinearRgba source, LinearRgba backdrop) => Weighted(source, backdrop, 1, 1 - source.A);
    private static LinearRgba Weighted(LinearRgba source, LinearRgba backdrop, double sourceFactor, double backdropFactor) =>
        new(source.R * sourceFactor + backdrop.R * backdropFactor, source.G * sourceFactor + backdrop.G * backdropFactor,
            source.B * sourceFactor + backdrop.B * backdropFactor, source.A * sourceFactor + backdrop.A * backdropFactor);

    /// <summary>The 28 COLR composition modes, including Porter-Duff and separable/nonseparable blends.</summary>
    internal static LinearRgba Composite(LinearRgba source, LinearRgba backdrop, int mode) {
        var sa = source.A; var da = backdrop.A;
        switch (mode) {
            case 0: return default;
            case 1: return source;
            case 2: return backdrop;
            case 3: return Over(source, backdrop);
            case 4: return Over(backdrop, source);
            case 5: return source.Opacity(da);
            case 6: return backdrop.Opacity(sa);
            case 7: return source.Opacity(1 - da);
            case 8: return backdrop.Opacity(1 - sa);
            case 9: return Weighted(source, backdrop, da, 1 - sa);
            case 10: return Weighted(source, backdrop, 1 - da, sa);
            case 11: return Weighted(source, backdrop, 1 - da, 1 - sa);
            case 12: return new LinearRgba(Math.Min(1, source.R + backdrop.R), Math.Min(1, source.G + backdrop.G), Math.Min(1, source.B + backdrop.B), Math.Min(1, sa + da));
        }
        if (mode > 27 || mode < 13) return default;
        var s = new Rgb(sa > 0 ? source.R / sa : 0, sa > 0 ? source.G / sa : 0, sa > 0 ? source.B / sa : 0);
        var d = new Rgb(da > 0 ? backdrop.R / da : 0, da > 0 ? backdrop.G / da : 0, da > 0 ? backdrop.B / da : 0);
        var blended = mode >= 24 ? Nonseparable(s, d, mode) : new Rgb(Blend(s.R, d.R, mode), Blend(s.G, d.G, mode), Blend(s.B, d.B, mode));
        var outside = Weighted(source, backdrop, 1 - da, 1 - sa); var overlap = sa * da;
        return new LinearRgba(outside.R + blended.R * overlap, outside.G + blended.G * overlap, outside.B + blended.B * overlap, sa + da - overlap);
    }
    private static double Blend(double s, double d, int mode) {
        switch (mode) {
            case 13: return s + d - s * d;
            case 14: return d <= 0.5 ? 2 * s * d : 1 - 2 * (1 - s) * (1 - d);
            case 15: return Math.Min(s, d);
            case 16: return Math.Max(s, d);
            case 17: return d <= 0 ? 0 : s >= 1 ? 1 : Math.Min(1, d / (1 - s));
            case 18: return d >= 1 ? 1 : s <= 0 ? 0 : 1 - Math.Min(1, (1 - d) / s);
            case 19: return s <= 0.5 ? 2 * s * d : 1 - 2 * (1 - s) * (1 - d);
            case 20: return s <= 0.5 ? d - (1 - 2 * s) * d * (1 - d) : d + (2 * s - 1) * ((d <= 0.25 ? ((16 * d - 12) * d + 4) * d : Math.Sqrt(d)) - d);
            case 21: return Math.Abs(d - s);
            case 22: return d + s - 2 * d * s;
            default: return d * s;
        }
    }
    private readonly struct Rgb {
        internal Rgb(double r, double g, double b) { R = r; G = g; B = b; }
        internal readonly double R, G, B;
        internal double Lum => 0.3 * R + 0.59 * G + 0.11 * B;
        internal double Sat => Math.Max(R, Math.Max(G, B)) - Math.Min(R, Math.Min(G, B));
    }
    private static Rgb Nonseparable(Rgb s, Rgb d, int mode) => mode == 24 ? SetLum(SetSat(s, d.Sat), d.Lum) :
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

using System;
using ChartForgeX.Primitives;
using Rgb = ChartForgeX.Raster.RasterBlendFunctions.Rgb;

namespace ChartForgeX.Raster;

/// <summary>Premultiplied linear-light sRGB for colour-font gradients and isolated composition groups.</summary>
internal readonly struct LinearRgba {
    internal readonly float R, G, B, A;
    internal LinearRgba(double r, double g, double b, double a) { R = (float)r; G = (float)g; B = (float)b; A = (float)a; }
    internal static LinearRgba From(ChartColor color, double opacity = 1) {
        var alpha = color.A / 255.0 * opacity; return new LinearRgba(RasterColorTransfer.Linear(color.R) * alpha, RasterColorTransfer.Linear(color.G) * alpha, RasterColorTransfer.Linear(color.B) * alpha, alpha);
    }
    internal LinearRgba Opacity(double amount) => new(R * amount, G * amount, B * amount, A * amount);
    internal ChartColor ToColor(double opacity = 1) => A <= 0 ? new ChartColor(0, 0, 0, 0) :
        new ChartColor(Srgb(R / A), Srgb(G / A), Srgb(B / A), Byte(A * opacity));
    private static byte Srgb(double value) => Byte(RasterColorTransfer.Encoded(value));
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
        var blended = mode >= 24 ? RasterBlendFunctions.Nonseparable(s, d, mode) : new Rgb(RasterBlendFunctions.Separable(s.R, d.R, mode), RasterBlendFunctions.Separable(s.G, d.G, mode), RasterBlendFunctions.Separable(s.B, d.B, mode));
        var outside = Weighted(source, backdrop, 1 - da, 1 - sa); var overlap = sa * da;
        return new LinearRgba(outside.R + blended.R * overlap, outside.G + blended.G * overlap, outside.B + blended.B * overlap, sa + da - overlap);
    }

}

using System;
using ChartForgeX.Effects;

namespace ChartForgeX.Raster;

internal static partial class RasterEffectPixels {
    internal static RasterEffectPixel16 Read16(ushort[] pixels, int at) =>
        new(pixels[at], pixels[at + 1], pixels[at + 2], pixels[at + 3]);

    /// <summary>Every primitive publishes valid premultiplied channels; hidden RGB is discarded at zero alpha.</summary>
    internal static void Store16(ushort[] target, int at, RasterEffectPixel16 pixel) {
        var alpha = Math.Max(0, Math.Min(65535, pixel.A));
        target[at] = (ushort)Math.Max(0, Math.Min(alpha, pixel.R));
        target[at + 1] = (ushort)Math.Max(0, Math.Min(alpha, pixel.G));
        target[at + 2] = (ushort)Math.Max(0, Math.Min(alpha, pixel.B)); target[at + 3] = (ushort)alpha;
    }
    internal static RasterEffectPixel16 Straight16(double r, double g, double b, double alpha) {
        var a = Unit16(alpha);
        int Channel(double value) => (int)Math.Floor(Math.Max(0, Math.Min(1, value)) * a + .5);
        return new RasterEffectPixel16(Channel(r), Channel(g), Channel(b), a);
    }

    /// <summary>Transfers only unassociated RGB; premultiplied alpha coverage stays exact.</summary>
    internal static RasterEffectPixel16 Convert16(RasterEffectPixel16 pixel, bool linearRgb) {
        if (pixel.A == 0) return default;
        int Channel(int value) {
            var straight = Math.Max(0, Math.Min(1, value / (double)pixel.A));
            var converted = linearRgb ? RasterColorTransfer.Linear(straight) : RasterColorTransfer.Encoded(straight);
            return (int)Math.Floor(converted * pixel.A + .5);
        }
        return new RasterEffectPixel16(Channel(pixel.R), Channel(pixel.G), Channel(pixel.B), pixel.A);
    }

    internal static RasterEffectPixel16 Blend16(RasterEffectPixel16 source, RasterEffectPixel16 destination, VisualBlendMode mode) {
        double Straight(int value, int alpha) => alpha == 0 ? 0 : value / (double)alpha;
        return BlendChannels16(source, destination,
            new RasterBlendFunctions.Rgb(Straight(source.R, source.A), Straight(source.G, source.A), Straight(source.B, source.A)),
            new RasterBlendFunctions.Rgb(Straight(destination.R, destination.A), Straight(destination.G, destination.A), Straight(destination.B, destination.A)), mode);
    }

    /// <summary>The normalized blend and coverage law shared by byte wrappers and retained RGBA16 consumers.</summary>
    private static RasterEffectPixel16 BlendChannels16(RasterEffectPixel16 source, RasterEffectPixel16 destination,
        RasterBlendFunctions.Rgb straightSource, RasterBlendFunctions.Rgb straightDestination, VisualBlendMode mode) {
        var sa = source.A; var da = destination.A;
        var a = mode == VisualBlendMode.PlusLighter ? Math.Min(65535, sa + da) : sa + da - Multiply(sa, da);
        var nonseparable = mode >= VisualBlendMode.Hue
            ? RasterBlendFunctions.Nonseparable(straightSource, straightDestination, 24 + (int)mode - (int)VisualBlendMode.Hue) : default;
        int Channel(int sp, int dp, double s, double d, double combined) {
            if (mode == VisualBlendMode.PlusLighter) return Math.Min(65535, sp + dp);
            if (mode == VisualBlendMode.Normal) return sp + Multiply(dp, 65535 - sa);
            var normalized = mode >= VisualBlendMode.Hue ? Math.Max(0, Math.Min(1, combined))
                : RasterBlendFunctions.Separable(s, d, SeparableFunction(mode));
            var blend = (int)Math.Floor(normalized * 65535 + .5);
            return Math.Min(65535, Multiply(sp, 65535 - da) + Multiply(dp, 65535 - sa) + Multiply(blend, Multiply(sa, da)));
        }
        return new RasterEffectPixel16(
            Channel(source.R, destination.R, straightSource.R, straightDestination.R, nonseparable.R),
            Channel(source.G, destination.G, straightSource.G, straightDestination.G, nonseparable.G),
            Channel(source.B, destination.B, straightSource.B, straightDestination.B, nonseparable.B), a);
    }

    /// <summary>Finite arithmetic over premultiplied channels, including coverage generation at transparent inputs.</summary>
    internal static RasterEffectPixel16 Arithmetic16(RasterEffectPixel16 source, RasterEffectPixel16 destination,
        double k1, double k2, double k3, double k4) {
        int Channel(int first, int second) {
            var s = first / 65535d; var d = second / 65535d;
            var a = k1 * s * d; var b = k2 * s; var c = k3 * d; var e = k4;
            var value = a + b + c + e;
            if (double.IsInfinity(value) || double.IsNaN(value)) {
                // Products are finite because input channels are in [0,1]. Cancel opposite finite terms
                // before same-sign sums, avoiding an Infinity-Infinity artifact for valid finite coefficients.
                for (var pass = 0; pass < 3; pass++) {
                    Cancel(ref a, ref b); Cancel(ref a, ref c); Cancel(ref a, ref e);
                    Cancel(ref b, ref c); Cancel(ref b, ref e); Cancel(ref c, ref e);
                }
                value = a + b + c + e; // Remaining same-sign overflow has a unique clamped value.
            }
            return Unit16(value);
        }
        var alpha = Channel(source.A, destination.A);
        return new RasterEffectPixel16(Math.Min(alpha, Channel(source.R, destination.R)), Math.Min(alpha, Channel(source.G, destination.G)),
            Math.Min(alpha, Channel(source.B, destination.B)), alpha);
    }
    private static void Cancel(ref double first, ref double second) {
        if ((first > 0 && second < 0) || (first < 0 && second > 0)) { first += second; second = 0; }
    }
}

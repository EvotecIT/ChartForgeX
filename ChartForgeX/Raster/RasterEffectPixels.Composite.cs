using System;
using ChartForgeX.Effects;

namespace ChartForgeX.Raster;

internal static partial class RasterEffectPixels {
    /// <summary>Changes only alpha. Positive output alpha retains original unassociated RGB bytes exactly.</summary>
    internal static void WriteMasked(byte[] target, int output, byte[] source, int input, int coverage, int x, int y, bool dither) {
        var alpha = Multiply(source[input + 3] * 257, coverage);
        var a = QuantizeMaskAlpha(alpha, x, y, dither);
        if (a == 0) return; // New result is already transparent black.
        target[output] = source[input]; target[output + 1] = source[input + 1]; target[output + 2] = source[input + 2]; target[output + 3] = a;
    }
    /// <summary>One final alpha-only conversion shared by native masks and typed SVG mask materialization.</summary>
    internal static byte QuantizeMaskAlpha(int alpha, int x, int y, bool dither) {
        var bias = dither ? ((Bayer8[((y & 7) << 3) | (x & 7)] * 2 + 1) * 257) / 128 : 128;
        return Quantize(alpha, bias);
    }

    /// <summary>Finite alpha-aware blend in gamma sRGB. PlusLighter is a distinct additive operator, never source-over.</summary>
    internal static void Composite(byte[] target, int output, byte sr, byte sg, byte sb, byte sa8,
        byte dr, byte dg, byte db, byte da8, VisualBlendMode mode, int x, int y, bool dither) {
        var sa = sa8 * 257; var da = da8 * 257;
        var a = mode == VisualBlendMode.PlusLighter ? Math.Min(65535, sa + da) : sa + da - Multiply(sa, da);
        var nonseparable = mode >= VisualBlendMode.Hue
            ? RasterBlendFunctions.Nonseparable(new RasterBlendFunctions.Rgb(sr / 255.0, sg / 255.0, sb / 255.0),
                new RasterBlendFunctions.Rgb(dr / 255.0, dg / 255.0, db / 255.0), 24 + (int)mode - (int)VisualBlendMode.Hue)
            : default;
        int Channel(byte s, byte d, double combined) {
            var sp = Premultiply(s, sa); var dp = Premultiply(d, da);
            if (mode == VisualBlendMode.PlusLighter) return Math.Min(65535, sp + dp);
            if (mode == VisualBlendMode.Normal) return sp + Multiply(dp, 65535 - sa);
            var normalized = mode >= VisualBlendMode.Hue ? Math.Max(0, Math.Min(1, combined))
                : RasterBlendFunctions.Separable(s / 255.0, d / 255.0, SeparableFunction(mode));
            var blend = (int)Math.Floor(normalized * 65535 + .5);
            return Math.Min(65535, Multiply(sp, 65535 - da) + Multiply(dp, 65535 - sa) + Multiply(blend, Multiply(sa, da)));
        }
        Write(target, output, Channel(sr, dr, nonseparable.R), Channel(sg, dg, nonseparable.G),
            Channel(sb, db, nonseparable.B), a, x, y, dither);
    }
}

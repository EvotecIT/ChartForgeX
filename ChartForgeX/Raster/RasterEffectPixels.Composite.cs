using System;
using ChartForgeX.Effects;

namespace ChartForgeX.Raster;

internal static partial class RasterEffectPixels {
    /// <summary>Changes only alpha. Positive output alpha retains original unassociated RGB bytes exactly.</summary>
    internal static void WriteMasked(byte[] target, int output, byte[] source, int input, int coverage, int x, int y, bool dither) {
        var alpha = Multiply(source[input + 3] * 257, coverage);
        var a = QuantizeMaskAlpha(alpha, x, y, dither);
        if (a == 0) {
            // Most callers target a zeroed result. The affine source clip also uses this
            // same alpha conversion in place on its newly owned generated pixel array.
            target[output] = target[output + 1] = target[output + 2] = target[output + 3] = 0;
            return;
        }
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
        var pixel = BlendChannels16(new RasterEffectPixel16(Premultiply(sr, sa), Premultiply(sg, sa), Premultiply(sb, sa), sa),
            new RasterEffectPixel16(Premultiply(dr, da), Premultiply(dg, da), Premultiply(db, da), da),
            new RasterBlendFunctions.Rgb(sr / 255.0, sg / 255.0, sb / 255.0),
            new RasterBlendFunctions.Rgb(dr / 255.0, dg / 255.0, db / 255.0), mode);
        Write(target, output, pixel.R, pixel.G, pixel.B, pixel.A, x, y, dither);
    }
}

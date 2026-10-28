using System;

namespace ChartForgeX.Raster;

internal static partial class RasterEffectPixels {
    /// <summary>Additive premultiplied merge, including alpha; shared with the existing PlusLighter law.</summary>
    internal static void AddSource(ref int r, ref int g, ref int b, ref int a, byte[] source, int offset) {
        var alpha = source[offset + 3] * 257;
        r = Math.Min(65535, r + Premultiply(source[offset], alpha));
        g = Math.Min(65535, g + Premultiply(source[offset + 1], alpha));
        b = Math.Min(65535, b + Premultiply(source[offset + 2], alpha));
        a = Math.Min(65535, a + alpha);
    }

    /// <summary>Uses the canonical Screen channel function without quantizing its premultiplied bright input first.</summary>
    internal static void ScreenSource(ref int r, ref int g, ref int b, ref int a, byte[] source, int offset) {
        var sourceAlpha = a;
        var backdropAlpha = offset < 0 ? 0 : source[offset + 3] * 257;
        int Channel(int bright, int channel) {
            var color = offset < 0 ? (byte)0 : source[offset + channel];
            var backdrop = Premultiply(color, backdropAlpha);
            if (sourceAlpha == 0) return backdrop;
            var blend = RasterBlendFunctions.Separable(Unpremultiply(bright, sourceAlpha) / 65535.0, color / 255.0, 13);
            var normalized = (int)Math.Floor(blend * 65535 + .5);
            return Math.Min(65535, Multiply(bright, 65535 - backdropAlpha) +
                Multiply(backdrop, 65535 - sourceAlpha) + Multiply(normalized, Multiply(sourceAlpha, backdropAlpha)));
        }
        r = Channel(r, 0);
        g = Channel(g, 1);
        b = Channel(b, 2);
        a = sourceAlpha + backdropAlpha - Multiply(sourceAlpha, backdropAlpha);
    }

    /// <summary>Coverage replaces the explicit backdrop, including alpha; RGB and alpha have distinct quantizers.</summary>
    internal static void ReplaceCoverage(byte[] target, int output, byte[] original, int input,
        byte[] filtered, int blur, int coverage, int x, int y, bool alphaDither) {
        var originalAlpha = original[input + 3] * 257;
        var filteredAlpha = filtered[blur + 3] * 257;
        var inverse = 65535 - coverage;
        var alpha = Multiply(originalAlpha, inverse) + Multiply(filteredAlpha, coverage);
        var quantized = QuantizeMaskAlpha(alpha, x, y, alphaDither);
        if (alpha == 0 || quantized == 0) return;
        for (var channel = 0; channel < 3; channel++) {
            var color = Multiply(Premultiply(original[input + channel], originalAlpha), inverse) +
                Multiply(Premultiply(filtered[blur + channel], filteredAlpha), coverage);
            target[output + channel] = Quantize(Unpremultiply(color, alpha), 128);
        }
        target[output + 3] = quantized;
    }
}

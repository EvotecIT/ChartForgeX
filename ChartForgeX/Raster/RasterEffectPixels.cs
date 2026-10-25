namespace ChartForgeX.Raster;

/// <summary>Gamma-encoded sRGB premultiplied16 conversions for explicit effects. Color-font LinearRgba remains linear-light and unchanged.</summary>
internal static partial class RasterEffectPixels {
    private static readonly byte[] Bayer8 = {
         0,48,12,60, 3,51,15,63, 32,16,44,28,35,19,47,31,
         8,56, 4,52,11,59, 7,55, 40,24,36,20,43,27,39,23,
         2,50,14,62, 1,49,13,61, 34,18,46,30,33,17,45,29,
        10,58, 6,54, 9,57, 5,53, 42,26,38,22,41,25,37,21
    };

    internal static int Premultiply(int color8, int alpha16) => (color8 * alpha16 + 127) / 255;
    internal static int Multiply(int first16, int second16) => (int)(((long)first16 * second16 + 32767) / 65535);

    /// <summary>Composites the original source once over a premultiplied16 shadow before the single final quantization.</summary>
    internal static void SourceOver(ref int r, ref int g, ref int b, ref int a, byte[] source, int offset) {
        var alpha = source[offset + 3] * 257; var inverse = 65535 - alpha;
        r = Premultiply(source[offset], alpha) + Multiply(r, inverse);
        g = Premultiply(source[offset + 1], alpha) + Multiply(g, inverse);
        b = Premultiply(source[offset + 2], alpha) + Multiply(b, inverse);
        a = alpha + Multiply(a, inverse);
    }

    /// <summary>Unpremultiplies only at final RGBA8 output; optional ordered dither is anchored to device coordinates.</summary>
    internal static void Write(byte[] target, int offset, int r, int g, int b, int a, int x, int y, bool orderedDither) {
        var bias = orderedDither ? ((Bayer8[((y & 7) << 3) | (x & 7)] * 2 + 1) * 257) / 128 : 128;
        var alpha = Quantize(a, bias);
        if (a == 0 || alpha == 0) { target[offset] = target[offset + 1] = target[offset + 2] = target[offset + 3] = 0; return; }
        target[offset] = Quantize(Unpremultiply(r, a), bias); target[offset + 1] = Quantize(Unpremultiply(g, a), bias);
        target[offset + 2] = Quantize(Unpremultiply(b, a), bias); target[offset + 3] = alpha;
    }

    private static int Unpremultiply(int color, int alpha) => color >= alpha ? 65535 : (int)(((long)color * 65535 + alpha / 2) / alpha);
    private static byte Quantize(int value, int bias) {
        var result = (value + bias) / 257; return (byte)(result > 255 ? 255 : result);
    }
}

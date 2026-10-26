using System;

namespace ChartForgeX.Raster;

internal static partial class RasterEffectPixels {
    /// <summary>Quantizes native generated straight channels once without a premultiply/unmultiply roundtrip.</summary>
    internal static void WriteGenerated(byte[] target, int at, double r, double g, double b, double a) {
        var alpha = Quantize(Unit16(a), 128);
        if (alpha == 0) { target[at] = target[at + 1] = target[at + 2] = target[at + 3] = 0; return; }
        target[at] = Quantize(Unit16(r), 128);
        target[at + 1] = Quantize(Unit16(g), 128);
        target[at + 2] = Quantize(Unit16(b), 128);
        target[at + 3] = alpha;
    }
    internal static int Unit16(double value) => (int)Math.Floor(Math.Max(0, Math.Min(1, value)) * 65535 + .5);
}

using System;
using System.Threading;

namespace ChartForgeX.Raster;

internal static partial class RasterEffectPixels {
    /// <summary>Transforms straight working-space channels; zero-alpha source has zero RGB, while constants may create new coverage.</summary>
    internal static void TransformColor(byte[] target, int output, byte[] source, int input, RasterColorTransform transform, CancellationToken cancellation) {
        var hasColor = input >= 0 && source[input + 3] != 0;
        double Read(int channel) => !hasColor ? 0 : transform.LinearRgb
            ? RasterColorTransfer.Linear(source[input + channel]) : source[input + channel] / 255d;
        var a = input < 0 ? 0 : source[input + 3] / 255d;
        transform.Apply(Read(0), Read(1), Read(2), a, out var r, out var g, out var b, out var alpha, cancellation);
        if (transform.OutputLinearRgb) {
            r = RasterColorTransfer.Encoded(r); g = RasterColorTransfer.Encoded(g); b = RasterColorTransfer.Encoded(b);
        }
        // Same unassociated output conversion as the native procedural source; no premultiply/unmultiply byte roundtrip.
        WriteGenerated(target, output, r, g, b, alpha);
    }
}

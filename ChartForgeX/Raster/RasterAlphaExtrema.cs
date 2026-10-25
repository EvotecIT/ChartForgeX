using System;
using System.Threading;

namespace ChartForgeX.Raster;

/// <summary>One monotonic-deque owner for byte alpha dilation/erosion, including legacy text strokes.</summary>
internal static class RasterAlphaExtrema {
    internal static byte[] Filter(byte[] source, int width, int height, int stride, int channel, int radiusX, int radiusY,
        bool maximize, CancellationToken cancellation = default) {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (width <= 0 || height <= 0 || stride <= 0 || channel < 0 || channel >= stride || radiusX < 0 || radiusY < 0
            || source.LongLength < checked((long)width * height * stride)) throw new ArgumentOutOfRangeException(nameof(width));
        cancellation.ThrowIfCancellationRequested();
        var horizontal = new byte[checked(width * height)];
        cancellation.ThrowIfCancellationRequested();
        var result = new byte[horizontal.Length];
        cancellation.ThrowIfCancellationRequested();
        var deque = new int[Math.Max(width, height)];
        Pass(source, horizontal, width, height, stride, channel, radiusX, maximize, true, deque, cancellation);
        Pass(horizontal, result, width, height, 1, 0, radiusY, maximize, false, deque, cancellation);
        return result;
    }

    private static void Pass(byte[] source, byte[] target, int width, int height, int stride, int channel, int radius,
        bool maximize, bool horizontal, int[] deque, CancellationToken cancellation) {
        var lines = horizontal ? height : width; var length = horizontal ? width : height;
        for (var line = 0; line < lines; line++) {
            cancellation.ThrowIfCancellationRequested();
            var head = 0; var tail = 0; var added = -1;
            for (var output = 0; output < length; output++) {
                var right = (long)output + radius; var left = (long)output - radius;
                // Visit actual samples only: a large erosion radius never adds an unbounded padding loop.
                while (added + 1 < length && added + 1 <= right) {
                    added++; var value = source[At(line, added) * stride + channel];
                    while (tail > head && Prefer(value, source[At(line, deque[tail - 1]) * stride + channel], maximize)) tail--;
                    deque[tail++] = added;
                }
                while (tail > head && deque[head] < left) head++;
                target[At(line, output)] = !maximize && (left < 0 || right >= length)
                    ? (byte)0 : source[At(line, deque[head]) * stride + channel];
            }
        }
        int At(int line, int sample) => horizontal ? line * width + sample : sample * width + line;
    }
    private static bool Prefer(byte candidate, byte existing, bool maximize) => maximize ? candidate >= existing : candidate <= existing;
}

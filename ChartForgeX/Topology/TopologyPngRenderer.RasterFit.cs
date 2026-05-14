using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

public sealed partial class TopologyPngRenderer {
    private static byte[] FitPixels(byte[] source, int sourceWidth, int sourceHeight, int targetWidth, int targetHeight, ChartColor background) {
        var target = new byte[targetWidth * targetHeight * 4];
        for (var i = 0; i < target.Length; i += 4) {
            target[i] = background.R;
            target[i + 1] = background.G;
            target[i + 2] = background.B;
            target[i + 3] = background.A;
        }

        var scale = Math.Min(targetWidth / (double)sourceWidth, targetHeight / (double)sourceHeight);
        scale = Math.Min(1, scale);
        var fittedWidth = Math.Max(1, (int)Math.Round(sourceWidth * scale));
        var fittedHeight = Math.Max(1, (int)Math.Round(sourceHeight * scale));
        for (var y = 0; y < fittedHeight; y++) {
            var sourceY = scale >= 1 ? y : ((y + 0.5) / scale) - 0.5;
            for (var x = 0; x < fittedWidth; x++) {
                var sourceX = scale >= 1 ? x : ((x + 0.5) / scale) - 0.5;
                var dst = (y * targetWidth + x) * 4;
                SampleBilinear(source, sourceWidth, sourceHeight, sourceX, sourceY, target, dst);
            }
        }

        return target;
    }

    private static void SampleBilinear(byte[] source, int sourceWidth, int sourceHeight, double sourceX, double sourceY, byte[] target, int dst) {
        sourceX = Clamp(sourceX, 0, sourceWidth - 1);
        sourceY = Clamp(sourceY, 0, sourceHeight - 1);
        var x0 = (int)Math.Floor(sourceX);
        var y0 = (int)Math.Floor(sourceY);
        var x1 = Math.Min(sourceWidth - 1, x0 + 1);
        var y1 = Math.Min(sourceHeight - 1, y0 + 1);
        var wx = sourceX - x0;
        var wy = sourceY - y0;
        var topLeft = (y0 * sourceWidth + x0) * 4;
        var topRight = (y0 * sourceWidth + x1) * 4;
        var bottomLeft = (y1 * sourceWidth + x0) * 4;
        var bottomRight = (y1 * sourceWidth + x1) * 4;
        for (var channel = 0; channel < 4; channel++) {
            var top = source[topLeft + channel] * (1 - wx) + source[topRight + channel] * wx;
            var bottom = source[bottomLeft + channel] * (1 - wx) + source[bottomRight + channel] * wx;
            target[dst + channel] = (byte)Clamp((int)Math.Round(top * (1 - wy) + bottom * wy), 0, 255);
        }
    }

    private static double Clamp(double value, double min, double max) {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    private static int Clamp(int value, int min, int max) {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}

// Independently authored scalar implementation of the retained W3C algorithm descriptions.
// Normalized-square gradients with zero-vector retry; no unit-disk rejection.
// See docs/third-party/W3C-Turbulence-NOTICE.txt for source attribution.
using System;
using System.Threading;

namespace ChartForgeX.Raster;

/// <summary>One four-channel lattice, shared across all rows/octaves of a preflighted procedural operation.</summary>
internal sealed class RasterTurbulenceLattice {
    private readonly int[] _permutation = new int[512];
    private readonly double[] _gradients = new double[4 * 256 * 2];

    internal RasterTurbulenceLattice(double seed, CancellationToken cancellation) {
        cancellation.ThrowIfCancellationRequested();
        var state = NormalizeSeed(seed);
        for (var channel = 0; channel < 4; channel++) {
            for (var index = 0; index < 256; index++) {
                cancellation.ThrowIfCancellationRequested();
                double gx, gy, square;
                do {
                    gx = (Next(ref state) % 512 - 256) / 256d;
                    gy = (Next(ref state) % 512 - 256) / 256d;
                    square = gx * gx + gy * gy;
                } while (square == 0); // Zero retry keeps normalization defined without changing square membership.
                var length = Math.Sqrt(square); var offset = (channel * 256 + index) * 2;
                _gradients[offset] = gx / length; _gradients[offset + 1] = gy / length;
            }
        }
        for (var index = 0; index < 256; index++) _permutation[index] = index;
        for (var index = 255; index > 0; index--) {
            var other = Next(ref state) % 256;
            var old = _permutation[index]; _permutation[index] = _permutation[other]; _permutation[other] = old;
        }
        for (var index = 0; index < 256; index++) _permutation[index + 256] = _permutation[index];
    }

    internal static int NormalizeSeed(double seed) {
        if (double.IsNaN(seed) || double.IsInfinity(seed)) throw new ArgumentOutOfRangeException(nameof(seed));
        var integer = Math.Truncate(seed);
        const int limit = 2147483646;
        // Take the double remainder BEFORE narrowing negative large finite values. Positive large seeds clamp.
        if (integer <= 0) return (int)(1 - integer % limit);
        return integer >= limit ? limit : (int)integer;
    }

    internal static int Next(ref int state) {
        var next = 16807 * (state % 127773) - 2836 * (state / 127773);
        state = next <= 0 ? next + 2147483647 : next;
        return state;
    }

    /// <summary>Counts the exact seeded retry stream before arrays, with the existing cumulative work ceiling.</summary>
    internal static long SetupWork(double seed, CancellationToken cancellation) {
        var state = NormalizeSeed(seed); long visits = 0;
        for (var index = 0; index < 4 * 256; index++) {
            int gx, gy;
            do {
                cancellation.ThrowIfCancellationRequested();
                gx = Next(ref state) % 512 - 256; gy = Next(ref state) % 512 - 256;
                visits = checked(visits + 16);
                if (visits > RasterEffectPreflight.MaximumWorkSamples)
                    throw new ArgumentOutOfRangeException(nameof(seed), "Seeded lattice setup exceeds the common work budget before arrays allocate.");
            } while (gx == 0 && gy == 0);
        }
        // Scalar admission and actual construction both visit this stream; include normalization, shuffle and zeroed arrays.
        return checked(visits * 2 + 4L * 256 * 16 + 512L * 4 + 255L * 8);
    }

    internal double Noise(int channel, double x, double y, bool stitch, int periodX, int periodY, int wrapX, int wrapY) {
        var shiftedX = x + 4096; var shiftedY = y + 4096;
        var left = (int)shiftedX; var top = (int)shiftedY;
        var right = left + 1; var bottom = top + 1;
        var dx = shiftedX - left; var dy = shiftedY - top;
        if (stitch) {
            if (left >= wrapX) left -= periodX;
            if (right >= wrapX) right -= periodX;
            if (top >= wrapY) top -= periodY;
            if (bottom >= wrapY) bottom -= periodY;
        }
        var first = _permutation[left & 255]; var second = _permutation[right & 255];
        var lowerLeft = _permutation[first + (top & 255)]; var lowerRight = _permutation[second + (top & 255)];
        var upperLeft = _permutation[first + (bottom & 255)]; var upperRight = _permutation[second + (bottom & 255)];
        var tx = dx * dx * (3 - 2 * dx); var ty = dy * dy * (3 - 2 * dy);
        var lower = Mix(Dot(channel, lowerLeft, dx, dy), Dot(channel, lowerRight, dx - 1, dy), tx);
        var upper = Mix(Dot(channel, upperLeft, dx, dy - 1), Dot(channel, upperRight, dx - 1, dy - 1), tx);
        return Mix(lower, upper, ty);
    }
    private double Dot(int channel, int gradient, double x, double y) {
        var at = (channel * 256 + gradient) * 2;
        return x * _gradients[at] + y * _gradients[at + 1];
    }
    private static double Mix(double first, double second, double amount) => first + amount * (second - first);
}

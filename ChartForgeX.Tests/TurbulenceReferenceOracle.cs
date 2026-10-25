// Copyright ©2011/2018 W3C®. See Fixtures/Effects/W3C-Turbulence-NOTICE.txt.
using System;
using System.Linq;

namespace ChartForgeX.Tests;

/// <summary>Independent test-only comparison of the two retained primary-source gradient laws; never a product fallback.</summary>
/// <remarks>Derived from W3C SVG1.1 section15.24 and Filter Effects1 section9.21 algorithm descriptions.
/// Research source/license receipts accompany this held test packet. No browser implementation code is imported.</remarks>
internal sealed class TurbulenceReferenceOracle {
    private readonly int[] _lattice = new int[514];
    private readonly double[,,] _gradient = new double[4, 514, 2];
    private int _state;

    internal TurbulenceReferenceOracle(double seed, bool unitDisk) {
        var integer = (long)Math.Truncate(seed);
        if (integer <= 0) integer = -(integer % 2147483646) + 1;
        if (integer > 2147483646) integer = 2147483646;
        _state = (int)integer;
        for (var channel = 0; channel < 4; channel++) {
            for (var index = 0; index < 256; index++) {
                _lattice[index] = index;
                double x, y, length;
                do {
                    x = (Next() % 512 - 256) / 256.0;
                    y = (Next() % 512 - 256) / 256.0;
                    length = Math.Sqrt(x * x + y * y);
                } while (length == 0 || unitDisk && length > 1);
                _gradient[channel, index, 0] = x / length;
                _gradient[channel, index, 1] = y / length;
            }
        }
        for (var index = 255; index > 0; index--) {
            var other = Next() % 256;
            var value = _lattice[index];
            _lattice[index] = _lattice[other];
            _lattice[other] = value;
        }
        for (var index = 0; index < 258; index++) {
            _lattice[256 + index] = _lattice[index];
            for (var channel = 0; channel < 4; channel++)
                for (var axis = 0; axis < 2; axis++)
                    _gradient[channel, 256 + index, axis] = _gradient[channel, index, axis];
        }
    }

    private int Next() {
        var result = 16807 * (_state % 127773) - 2836 * (_state / 127773);
        _state = result <= 0 ? result + 2147483647 : result;
        return _state;
    }

    internal double[] Sample(double x, double y, double fx, double fy, int octaves,
        bool fractal, bool stitch, bool linearRgb, double tileWidth = 64, double tileHeight = 48) {
        if (stitch) { fx = StitchFrequency(fx, tileWidth); fy = StitchFrequency(fy, tileHeight); }
        var output = new double[4];
        for (var channel = 0; channel < 4; channel++) {
            double sx = x * fx, sy = y * fy, amplitude = 1, sum = 0;
            var width = (int)(tileWidth * fx + .5);
            var height = (int)(tileHeight * fy + .5);
            var wrapX = 4096 + width;
            var wrapY = 4096 + height;
            for (var octave = 0; octave < octaves; octave++) {
                var noise = Noise(channel, sx, sy, stitch, width, height, wrapX, wrapY);
                sum += (fractal ? noise : Math.Abs(noise)) * amplitude;
                sx *= 2; sy *= 2; amplitude *= .5;
                width *= 2; height *= 2; wrapX = wrapX * 2 - 4096; wrapY = wrapY * 2 - 4096;
            }
            var value = Math.Max(0, Math.Min(1, fractal ? (sum + 1) * .5 : sum));
            if (linearRgb && channel < 3)
                value = value <= .0031308 ? value * 12.92 : 1.055 * Math.Pow(value, 1 / 2.4) - .055;
            output[channel] = value * 255;
        }
        return output;
    }

    private static double StitchFrequency(double frequency, double extent) {
        if (frequency == 0) return 0;
        var low = Math.Floor(extent * frequency) / extent;
        var high = Math.Ceiling(extent * frequency) / extent;
        return frequency / low < high / frequency ? low : high;
    }

    private double Noise(int channel, double x, double y, bool stitch, int width, int height, int wrapX, int wrapY) {
        var px = x + 4096; var py = y + 4096;
        var x0 = (int)px; var x1 = x0 + 1; var y0 = (int)py; var y1 = y0 + 1;
        var rx = px - x0; var ry = py - y0;
        if (stitch) {
            if (x0 >= wrapX) x0 -= width;
            if (x1 >= wrapX) x1 -= width;
            if (y0 >= wrapY) y0 -= height;
            if (y1 >= wrapY) y1 -= height;
        }
        var i = _lattice[x0 & 255]; var j = _lattice[x1 & 255];
        var b00 = _lattice[i + (y0 & 255)]; var b10 = _lattice[j + (y0 & 255)];
        var b01 = _lattice[i + (y1 & 255)]; var b11 = _lattice[j + (y1 & 255)];
        double Dot(int index, double dx, double dy) => dx * _gradient[channel, index, 0] + dy * _gradient[channel, index, 1];
        static double Mix(double amount, double a, double b) => a + amount * (b - a);
        var tx = rx * rx * (3 - 2 * rx); var ty = ry * ry * (3 - 2 * ry);
        return Mix(ty, Mix(tx, Dot(b00, rx, ry), Dot(b10, rx - 1, ry)),
            Mix(tx, Dot(b01, rx, ry - 1), Dot(b11, rx - 1, ry - 1)));
    }
}

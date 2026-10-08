using System;

namespace ChartForgeX.Raster;

internal readonly struct RasterFrameDelay {
    internal RasterFrameDelay(int numerator, int denominator) {
        Numerator = numerator;
        Denominator = denominator;
    }

    internal int Numerator { get; }
    internal int Denominator { get; }

    internal static int GifCentiseconds(TimeSpan duration) {
        if (duration <= TimeSpan.Zero || duration > TimeSpan.FromMilliseconds(655350)) {
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "GIF frame duration must be positive and at most 655.35 seconds.");
        }
        return Math.Max(1, (int)Math.Round(duration.TotalMilliseconds / 10, MidpointRounding.AwayFromZero));
    }

    internal static RasterFrameDelay Apng(TimeSpan duration) {
        if (duration <= TimeSpan.Zero || duration > TimeSpan.FromSeconds(65535)) {
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "APNG frame duration must be positive and at most 65,535 seconds.");
        }
        var divisor = GreatestCommonDivisor(duration.Ticks, TimeSpan.TicksPerSecond);
        var numerator = duration.Ticks / divisor;
        var denominator = TimeSpan.TicksPerSecond / divisor;
        if (numerator <= 65535 && denominator <= 65535) {
            return new RasterFrameDelay((int)numerator, (int)denominator);
        }
        var supportedDenominator = (int)Math.Min(65535, Math.Floor(65535 / duration.TotalSeconds));
        var supportedNumerator = Math.Max(1, (int)Math.Round(duration.TotalSeconds * supportedDenominator, MidpointRounding.AwayFromZero));
        return new RasterFrameDelay(supportedNumerator, supportedDenominator);
    }

    private static long GreatestCommonDivisor(long left, long right) {
        while (right != 0) {
            var next = left % right;
            left = right;
            right = next;
        }
        return left;
    }
}

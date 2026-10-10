using System;

namespace ChartForgeX;

/// <summary>Projects bounded authored playback onto the same tick clock as prepared frames.</summary>
internal static class StoryPlaybackClock {
    internal static long Ticks(double seconds) => checked((long)Math.Round(seconds * TimeSpan.TicksPerSecond, MidpointRounding.AwayFromZero));

    internal static bool Started(double elapsed, double start) => Ticks(elapsed) >= Ticks(start);

    internal static double Progress(double elapsed, double start, double duration) {
        var startTicks = Ticks(start);
        var durationTicks = Ticks(start + duration) - startTicks;
        return durationTicks <= 0 ? 1 : Math.Max(0, Math.Min(1, (Ticks(elapsed) - startTicks) / (double)durationTicks));
    }

    internal static int Elements(int count, double elapsed, double start, double duration) {
        var startTicks = Ticks(start);
        return Elements(count, Ticks(elapsed) - startTicks, Ticks(start + duration) - startTicks);
    }

    internal static int Elements(int count, long elapsedTicks, long durationTicks) {
        if (elapsedTicks < 0) return 0;
        if (durationTicks <= 0 || elapsedTicks >= durationTicks) return count;
        // Decimal keeps the discrete quotient exact without overflowing a long product.
        return (int)((decimal)elapsedTicks * count / durationTicks);
    }
}

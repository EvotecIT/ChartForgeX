using System;

namespace ChartForgeX.Raster;

/// <summary>Projects a frame sequence onto GIF's clock without accumulating per-frame rounding error.</summary>
internal struct GifFrameClock {
    private const long TicksPerCentisecond = TimeSpan.TicksPerMillisecond * 10;
    private long _remainderTicks;

    internal int Next(TimeSpan duration) {
        RasterFrameDelay.GifCentiseconds(duration);
        if (duration == TimeSpan.Zero) return 0;
        var projected = duration.Ticks + _remainderTicks;
        var rounded = (int)Math.Round(projected / (double)TicksPerCentisecond, MidpointRounding.AwayFromZero);
        var delay = Math.Max(1, Math.Min(65535, rounded));
        // A clamped duration cannot follow the requested clock. Do not shorten later frames to repay it.
        _remainderTicks = rounded == delay ? projected - delay * TicksPerCentisecond : 0;
        return delay;
    }
}

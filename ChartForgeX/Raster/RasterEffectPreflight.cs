using System;
using System.Collections.Generic;

namespace ChartForgeX.Raster;

/// <summary>One operation batch's conservative preflight, completed before allocating any effect buffers.</summary>
internal sealed class RasterEffectPreflight {
    internal const long MaximumWorkSamples = 1_000_000_000;
    internal const long MaximumLiveBytes = 256L * 1024 * 1024;
    private readonly RasterEffectPlan[] _plans;
    private readonly RgbaImage[] _sources;
    private readonly bool[] _taken;
    internal long WorkSamples { get; }
    internal long ConservativeLiveBytes { get; }

    private RasterEffectPreflight(RasterEffectPlan[] plans, RgbaImage[] sources, long maximumWorkSamples, long maximumLiveBytes, long additionalPixelBytes, long additionalWorkSamples) {
        if (plans == null || sources == null) throw new ArgumentNullException(nameof(plans));
        if (plans.Length == 0 || plans.Length > 16 || plans.Length != sources.Length) throw new ArgumentOutOfRangeException(nameof(plans));
        if (maximumWorkSamples <= 0 || maximumWorkSamples > MaximumWorkSamples || maximumLiveBytes <= 0 || maximumLiveBytes > MaximumLiveBytes)
            throw new ArgumentOutOfRangeException(nameof(maximumWorkSamples), "Caller limits may tighten, but never raise, the raster safety ceilings.");
        if (additionalPixelBytes < 0 || additionalWorkSamples < 0) throw new ArgumentOutOfRangeException(nameof(additionalPixelBytes));
        if (additionalPixelBytes > maximumLiveBytes || additionalWorkSamples > maximumWorkSamples)
            throw new ArgumentOutOfRangeException(nameof(additionalPixelBytes), "Composition exceeds its raster preflight before execution.");
        _plans = (RasterEffectPlan[])plans.Clone(); _sources = (RgbaImage[])sources.Clone(); _taken = new bool[plans.Length];
        var borrowed = new HashSet<byte[]>(); long work = additionalWorkSamples, bytes = additionalPixelBytes;
        for (int index = 0; index < plans.Length; index++) {
            var plan = _plans[index] ?? throw new ArgumentNullException(nameof(plans)); var source = _sources[index];
            if (source.Pixels == null || source.Width <= 0 || source.Height <= 0 || source.Pixels.LongLength < checked((long)source.Width * source.Height * 4))
                throw new ArgumentException("Effect sources require validated RGBA8 images.", nameof(sources));
            // The entire borrowed allocation counts, not just a clipped region. Duplicate source arrays count once.
            if (borrowed.Add(source.Pixels)) bytes = checked(bytes + source.Pixels.LongLength);
            work = checked(work + plan.WorkSamples); bytes = checked(bytes + plan.AllocatedBytes);
            if (work > maximumWorkSamples || bytes > maximumLiveBytes)
                throw new ArgumentOutOfRangeException(nameof(plans), "Effects exceed cumulative work or conservative live-buffer limits before execution.");
        }
        WorkSamples = work; ConservativeLiveBytes = bytes;
    }

    /// <summary>Safety limits, not performance results. Sum of every planned allocation avoids assuming collection or reuse between operations.</summary>
    internal static RasterEffectPreflight Prepare(RasterEffectPlan[] plans, RgbaImage[] sources,
        long maximumWorkSamples = MaximumWorkSamples, long maximumLiveBytes = MaximumLiveBytes,
        long additionalPixelBytes = 0, long additionalWorkSamples = 0) =>
        new(plans, sources, maximumWorkSamples, maximumLiveBytes, additionalPixelBytes, additionalWorkSamples);

    internal RasterEffectPlan Take(int index, out RgbaImage source) {
        lock (_taken) {
            if (index < 0 || index >= _plans.Length) throw new ArgumentOutOfRangeException(nameof(index));
            if (_taken[index]) throw new InvalidOperationException("A preflighted effect operation executes at most once.");
            _taken[index] = true; source = _sources[index]; return _plans[index];
        }
    }
}

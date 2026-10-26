using System;
using System.Collections.Generic;

namespace ChartForgeX.Raster;

/// <summary>One operation batch's conservative preflight, completed before allocating any effect buffers.</summary>
internal sealed partial class RasterEffectPreflight {
    internal const long MaximumWorkSamples = 1_000_000_000;
    internal const long MaximumLiveBytes = 256L * 1024 * 1024;
    private readonly RasterEffectPlan[] _plans;
    private readonly RgbaImage[] _sources;
    private readonly bool[] _taken;
    private readonly bool _sourceFree;
    internal long WorkSamples { get; }
    internal long ConservativeLiveBytes { get; }

    private RasterEffectPreflight(RasterEffectPlan[] plans, RgbaImage[] sources, long maximumWorkSamples, long maximumLiveBytes, long additionalPixelBytes, long additionalWorkSamples) {
        if (plans == null || sources == null) throw new ArgumentNullException(nameof(plans));
        if (plans.Length == 0 || plans.Length > 16 || plans.Length != sources.Length) throw new ArgumentOutOfRangeException(nameof(plans));
        ValidateLimits(maximumWorkSamples, maximumLiveBytes, additionalPixelBytes, additionalWorkSamples);
        _plans = (RasterEffectPlan[])plans.Clone(); _sources = (RgbaImage[])sources.Clone(); _taken = new bool[plans.Length];
        var borrowed = new HashSet<byte[]>(); long work = additionalWorkSamples, bytes = additionalPixelBytes;
        for (int index = 0; index < plans.Length; index++) {
            var plan = _plans[index] ?? throw new ArgumentNullException(nameof(plans));
            if (plan.TurbulenceData != null)
                throw new ArgumentException("A procedural plan has no SourceGraphic; use the common source-free admission.", nameof(plans));
            var source = _sources[index];
            if (source.Pixels == null || source.Width <= 0 || source.Height <= 0 || source.Pixels.LongLength < checked((long)source.Width * source.Height * 4))
                throw new ArgumentException("Effect sources require validated RGBA8 images.", nameof(sources));
            // The entire borrowed allocation counts, not just a clipped region. Duplicate source arrays count once.
            if (borrowed.Add(source.Pixels)) bytes = checked(bytes + source.Pixels.LongLength);
            if (plan.Operand is RgbaImage operand) {
                _ = new RgbaImage(operand.Width, operand.Height, operand.Pixels);
                _ = new RasterEffectRegion(0, 0, operand.Width, operand.Height);
                if (borrowed.Add(operand.Pixels)) bytes = checked(bytes + operand.Pixels.LongLength);
            }
            work = checked(work + plan.WorkSamples); bytes = checked(bytes + plan.AllocatedBytes);
            ValidateTotals(work, bytes, maximumWorkSamples, maximumLiveBytes);
        }
        WorkSamples = work; ConservativeLiveBytes = bytes;
    }

    private RasterEffectPreflight(RasterEffectPlan procedural, long maximumWorkSamples, long maximumLiveBytes,
        long additionalPixelBytes, long additionalWorkSamples) {
        if (procedural == null) throw new ArgumentNullException(nameof(procedural));
        if (procedural.TurbulenceData == null)
            throw new ArgumentException("Only a represented procedural operation has no SourceGraphic.", nameof(procedural));
        ValidateCapture(procedural, 0, additionalPixelBytes, additionalWorkSamples, out var work, out var bytes,
            maximumWorkSamples, maximumLiveBytes);
        WorkSamples = work; ConservativeLiveBytes = bytes;
        _plans = new[] { procedural }; _sources = Array.Empty<RgbaImage>(); _taken = new bool[1];
        _sourceFree = true;
    }

    /// <summary>Admits the one native procedural source without creating a dummy source image.</summary>
    internal static RasterEffectPreflight PrepareSourceFree(RasterEffectPlan plan,
        long maximumWorkSamples = MaximumWorkSamples, long maximumLiveBytes = MaximumLiveBytes,
        long additionalPixelBytes = 0, long additionalWorkSamples = 0) =>
        new(plan, maximumWorkSamples, maximumLiveBytes, additionalPixelBytes, additionalWorkSamples);

    /// <summary>Checks one planned source capture before its canvas, downsample, effect or private snapshot buffers allocate.</summary>
    /// <remarks>The existing renderer's font/geometry work and encoder allocations remain governed by their own owners;
    /// these counts cover declared capture/effect pixel arrays and explicit array visits, not process memory or latency.</remarks>
    internal static void ValidateCapture(RasterEffectPlan? plan, long sourcePixelBytes, long additionalPixelBytes, long additionalWorkSamples,
        out long workSamples, out long conservativeLiveBytes,
        long maximumWorkSamples = MaximumWorkSamples, long maximumLiveBytes = MaximumLiveBytes) {
        if (sourcePixelBytes < 0) throw new ArgumentOutOfRangeException(nameof(sourcePixelBytes));
        ValidateLimits(maximumWorkSamples, maximumLiveBytes, additionalPixelBytes, additionalWorkSamples);
        workSamples = checked(additionalWorkSamples + (plan?.WorkSamples ?? 0));
        conservativeLiveBytes = checked(sourcePixelBytes + additionalPixelBytes + (plan?.AllocatedBytes ?? 0) + (plan?.Operand?.Pixels.LongLength ?? 0));
        ValidateTotals(workSamples, conservativeLiveBytes, maximumWorkSamples, maximumLiveBytes);
    }

    private static void ValidateLimits(long maximumWorkSamples, long maximumLiveBytes, long additionalPixelBytes, long additionalWorkSamples) {
        if (maximumWorkSamples <= 0 || maximumWorkSamples > MaximumWorkSamples || maximumLiveBytes <= 0 || maximumLiveBytes > MaximumLiveBytes)
            throw new ArgumentOutOfRangeException(nameof(maximumWorkSamples), "Caller limits may tighten, but never raise, the raster safety ceilings.");
        if (additionalPixelBytes < 0 || additionalWorkSamples < 0) throw new ArgumentOutOfRangeException(nameof(additionalPixelBytes));
        if (additionalPixelBytes > maximumLiveBytes || additionalWorkSamples > maximumWorkSamples)
            throw new ArgumentOutOfRangeException(nameof(additionalPixelBytes), "Composition exceeds its raster preflight before execution.");
    }

    private static void ValidateTotals(long work, long bytes, long maximumWorkSamples, long maximumLiveBytes) {
        if (work > maximumWorkSamples || bytes > maximumLiveBytes)
            throw new ArgumentOutOfRangeException("plans", "Effects exceed cumulative work or conservative live-buffer limits before execution.");
    }

    /// <summary>Safety limits, not performance results. Sum of every planned allocation avoids assuming collection or reuse between operations.</summary>
    internal static RasterEffectPreflight Prepare(RasterEffectPlan[] plans, RgbaImage[] sources,
        long maximumWorkSamples = MaximumWorkSamples, long maximumLiveBytes = MaximumLiveBytes,
        long additionalPixelBytes = 0, long additionalWorkSamples = 0) =>
        new(plans, sources, maximumWorkSamples, maximumLiveBytes, additionalPixelBytes, additionalWorkSamples);

    internal RasterEffectPlan Take(int index, out RgbaImage source) {
        if (_surfaceMode || _sourceFree) throw new InvalidOperationException("A procedural operation has no SourceGraphic input.");
        var plan = Claim(index); source = _sources[index]; return plan;
    }

    internal RasterEffectPlan TakeSourceFree(int index) {
        if (_surfaceMode || !_sourceFree) throw new InvalidOperationException("A source-bearing operation must retain its actual SourceGraphic.");
        return Claim(index);
    }

    private RasterEffectPlan Claim(int index) {
        lock (_taken) {
            if (index < 0 || index >= _plans.Length) throw new ArgumentOutOfRangeException(nameof(index));
            if (_taken[index]) throw new InvalidOperationException("A preflighted effect operation executes at most once.");
            _taken[index] = true; return _plans[index];
        }
    }
}

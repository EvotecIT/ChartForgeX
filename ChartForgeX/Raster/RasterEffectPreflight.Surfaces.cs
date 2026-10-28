using System;
using System.Collections.Generic;

namespace ChartForgeX.Raster;

internal sealed partial class RasterEffectPreflight {
    private readonly bool _surfaceMode;
    private readonly RasterEffectSurface? _surfaceSource;

    private RasterEffectPreflight(RasterEffectPlan plan, RasterEffectSurface? source, long maximumWorkSamples,
        long maximumLiveBytes, long additionalPixelBytes, long additionalWorkSamples) {
        if (plan == null || plan.SurfaceOperation == RasterSurfaceOperation.None || plan.SurfaceOperation == RasterSurfaceOperation.Capture)
            throw new ArgumentException("A represented high-precision operation is required.", nameof(plan));
        if (source.HasValue == (plan.SurfaceOperation == RasterSurfaceOperation.Flood))
            throw new ArgumentException("Only a typed flood is source-free at this kernel seam.", nameof(source));
        ValidateLimits(maximumWorkSamples, maximumLiveBytes, additionalPixelBytes, additionalWorkSamples);
        var borrowed = new HashSet<ushort[]>(); var bytes = additionalPixelBytes;
        void Add(RasterEffectSurface surface) {
            _ = new RasterEffectSurface(surface.Region, surface.Pixels, surface.LinearRgb, surface.AlphaOnly);
            if (surface.Region.X != plan.SourceRegion.X || surface.Region.Y != plan.SourceRegion.Y
                || surface.Region.Width != plan.SourceRegion.Width || surface.Region.Height != plan.SourceRegion.Height)
                throw new ArgumentException("Kernel inputs must share the admitted frame.", nameof(source));
            if (surface.LinearRgb != plan.SurfaceLinearRgb && plan.SurfaceOperation != RasterSurfaceOperation.Convert)
                throw new ArgumentException("Working color space changes require the shared conversion operation.", nameof(source));
            if (borrowed.Add(surface.Pixels)) bytes = checked(bytes + surface.ByteCount);
        }
        if (source.HasValue) Add(source.Value);
        if (plan.SurfaceOperand.HasValue) Add(plan.SurfaceOperand.Value);
        bytes = checked(bytes + plan.AllocatedBytes); var work = checked(additionalWorkSamples + plan.WorkSamples);
        ValidateTotals(work, bytes, maximumWorkSamples, maximumLiveBytes);
        ConservativeLiveBytes = bytes; WorkSamples = work;
        _plans = new[] { plan }; _sources = Array.Empty<RgbaImage>(); _taken = new bool[1];
        _surfaceMode = true; _surfaceSource = source;
    }

    internal static RasterEffectPreflight PrepareSurface(RasterEffectPlan plan, RasterEffectSurface? source,
        long maximumWorkSamples = MaximumWorkSamples, long maximumLiveBytes = MaximumLiveBytes,
        long additionalPixelBytes = 0, long additionalWorkSamples = 0) =>
        new(plan, source, maximumWorkSamples, maximumLiveBytes, additionalPixelBytes, additionalWorkSamples);

    internal RasterEffectPlan TakeSurface(out RasterEffectSurface? source) {
        if (!_surfaceMode) throw new InvalidOperationException("The permit does not hold a high-precision surface operation.");
        var plan = Claim(0); source = _surfaceSource; return plan;
    }
}

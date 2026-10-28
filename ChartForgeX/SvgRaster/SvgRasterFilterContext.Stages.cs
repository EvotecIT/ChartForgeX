using System;
using System.Threading;
using ChartForgeX.Raster;

namespace ChartForgeX.SvgRaster;

internal sealed partial class SvgRasterFilterContext {
    /// <summary>One filter application reserves its optional actual byte capture and final viewport before either allocates.</summary>
    internal void BeginStages(int sourceWidth, int sourceHeight, int outputWidth, int outputHeight) {
        Cancellation.ThrowIfCancellationRequested();
        if (_applications >= _options.MaximumApplications) throw Limit("Native filter instance count exceeds the document limit.");
        var sourcePixels = checked((long)sourceWidth * sourceHeight); var outputPixels = checked((long)outputWidth * outputHeight);
        Debit(checked((sourcePixels + outputPixels) * 4), checked((sourcePixels + outputPixels) * 4)); _applications++;
    }

    /// <summary>Admits the byte-to-RGBA16 capture through the existing common preflight without charging the borrowed byte array twice.</summary>
    internal SvgRasterFilterPermit ReserveStageCapture(RasterEffectPlan plan, RgbaImage source) {
        Cancellation.ThrowIfCancellationRequested();
        var priorBytes = _debit.Bytes; var priorWork = _debit.WorkSamples;
        if (source.Pixels.LongLength > priorBytes) throw new InvalidOperationException("The captured byte surface must already belong to this document debit.");
        Debit(plan.AllocatedBytes, plan.WorkSamples);
        return new SvgRasterFilterPermit(plan, checked(priorBytes - source.Pixels.LongLength), priorWork, _options);
    }

    /// <summary>Debits only new arrays/work; common preflight adds the real already-charged unique borrowed surfaces back once.</summary>
    internal SvgRasterFilterPermit ReserveSurface(RasterEffectPlan plan, RasterEffectSurface? source) {
        Cancellation.ThrowIfCancellationRequested();
        var borrowed = source?.ByteCount ?? 0;
        if (plan.SurfaceOperand is RasterEffectSurface operand && (!source.HasValue || !ReferenceEquals(source.Value.Pixels, operand.Pixels)))
            borrowed = checked(borrowed + operand.ByteCount);
        var priorBytes = _debit.Bytes; var priorWork = _debit.WorkSamples;
        if (borrowed > priorBytes) throw new InvalidOperationException("Every borrowed kernel surface must already belong to this document debit.");
        Debit(plan.AllocatedBytes, plan.WorkSamples);
        return new SvgRasterFilterPermit(plan, source, checked(priorBytes - borrowed), priorWork, _options);
    }
}

internal sealed partial class SvgRasterFilterPermit {
    private readonly RasterEffectSurface? _surfaceSource;
    private int _used;
    internal SvgRasterFilterPermit(RasterEffectPlan plan, RasterEffectSurface? source, long bytes, long work, SvgRasterFilterOptions options)
        : this(plan, bytes, work, options) { _surfaceSource = source; }

    private void ClaimPermit() {
        if (Interlocked.Exchange(ref _used, 1) != 0) throw new InvalidOperationException("A document filter permit executes at most once.");
    }
    internal RasterEffectSurface ExecuteSurfaceCapture(RgbaImage source) {
        ClaimPermit();
        var preflight = RasterEffectPreflight.Prepare(new[] { _plan }, new[] { source },
            _options.MaximumWorkSamples, _options.MaximumLiveBytes, _additionalBytes, _additionalWork);
        return RasterEffects.CaptureSurface(preflight, _options.CancellationToken);
    }
    internal RasterEffectSurface ExecuteSurface() {
        ClaimPermit();
        var preflight = RasterEffectPreflight.PrepareSurface(_plan, _surfaceSource,
            _options.MaximumWorkSamples, _options.MaximumLiveBytes, _additionalBytes, _additionalWork);
        return RasterEffects.ExecuteSurface(preflight, _options.CancellationToken);
    }
}

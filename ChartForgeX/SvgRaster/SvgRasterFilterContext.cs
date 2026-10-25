using System;
using System.Threading;
using ChartForgeX.Raster;

namespace ChartForgeX.SvgRaster;

/// <summary>Single rasterization's cumulative filter debit; use and embedded-document instances never reset it.</summary>
internal sealed class SvgRasterFilterContext {
    private readonly SvgRasterFilterOptions _options;
    private long _bytes, _work;
    private int _applications;
    internal SvgRasterFilterContext(SvgRasterFilterOptions options) { _options = options; }
    internal CancellationToken Cancellation => _options.CancellationToken;

    /// <summary>Debits the existing renderer's next ordinary canvas before its pixel array allocates.</summary>
    internal void ReserveCanvas(int width, int height) {
        var allocation = RasterAllocationGuard.Calculate(width, height, 1, 1);
        ReservePixelArray(allocation.ByteCount);
    }

    /// <summary>Debits one declared RGBA creation/copy; prior arrays remain charged without assuming collection.</summary>
    internal void ReservePixelArray(long byteCount) {
        Cancellation.ThrowIfCancellationRequested();
        Debit(byteCount, byteCount);
    }

    /// <summary>Charges declared row visits without creating an additional pixel buffer.</summary>
    internal void ReserveWorkSamples(long samples) {
        Cancellation.ThrowIfCancellationRequested();
        Debit(0, samples);
    }

    internal SvgRasterFilterPermit Reserve(RasterEffectPlan plan, int sourceWidth, int sourceHeight,
        int outputWidth, int outputHeight) {
        Cancellation.ThrowIfCancellationRequested();
        if (_applications >= _options.MaximumApplications) throw Limit("Native filter instance count exceeds the document limit.");
        var sourcePixels = checked((long)sourceWidth * sourceHeight);
        var outputPixels = checked((long)outputWidth * outputHeight);
        var nonKernelBytes = checked(outputPixels * 4);
        var nonKernelWork = checked(sourcePixels * 4 + outputPixels * 4);
        var priorBytes = _bytes; var priorWork = _work;
        // These exact capture/kernel/one-output arrays are reserved before capture and are not charged by AllocateCanvas.
        // Ordinary surrounding compositors debit their real instances separately. No GC timing is assumed.
        Debit(checked(sourcePixels * 4 + plan.AllocatedBytes + nonKernelBytes), checked(plan.WorkSamples + nonKernelWork));
        _applications++;
        return new SvgRasterFilterPermit(plan, checked(priorBytes + nonKernelBytes), checked(priorWork + nonKernelWork), _options);
    }

    private void Debit(long bytes, long work) {
        if (bytes < 0 || work < 0 || bytes > _options.MaximumLiveBytes - _bytes || work > _options.MaximumWorkSamples - _work)
            throw Limit("Native filter import exceeds cumulative declared pixel arrays or sample visits before allocation.");
        _bytes += bytes; _work += work;
    }

    // NotSupportedException passes through the legacy Try-render catch; limits never become an unfiltered fallback.
    private static NotSupportedException Limit(string message) => new("SVG filter safety limit: " + message);
}

/// <summary>Retains the existing one-use effect preflight and accounts for previously reserved document buffers.</summary>
internal sealed class SvgRasterFilterPermit {
    private readonly RasterEffectPlan _plan;
    private readonly long _additionalBytes, _additionalWork;
    private readonly SvgRasterFilterOptions _options;
    internal SvgRasterFilterPermit(RasterEffectPlan plan, long bytes, long work, SvgRasterFilterOptions options) {
        _plan = plan; _additionalBytes = bytes; _additionalWork = work; _options = options;
    }
    internal RasterEffectResult Execute(RgbaImage source) {
        var preflight = RasterEffectPreflight.Prepare(new[] { _plan }, new[] { source },
            _options.MaximumWorkSamples, _options.MaximumLiveBytes, _additionalBytes, _additionalWork);
        // No new quantizer or convolution. False fixes the existing native kernel's final dither policy explicitly.
        return RasterEffects.Execute(preflight, 0, false, _options.CancellationToken);
    }
}

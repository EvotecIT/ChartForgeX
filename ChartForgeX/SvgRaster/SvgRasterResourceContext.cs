using System;
using System.Collections.Generic;
using System.Threading;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.SvgRaster;

/// <summary>One compilation's exact immutable artwork captures and declared pixel-array/sample debit; never enables filters.</summary>
internal sealed class SvgRasterResourceContext {
    private const int MaximumSvgCacheEntries = 1024;
    private readonly Dictionary<string, VisualSceneImageData> _rasters = new(StringComparer.Ordinal);
    private readonly HashSet<string> _invalidRasters = new(StringComparer.Ordinal);
    private readonly Dictionary<SvgRasterCaptureKey, SvgRasterCapturedImage> _svg = new();
    private readonly CancellationToken _cancellation;

    internal SvgRasterResourceContext(SvgRasterFilterContext? filters = null) {
        Budget = filters?.Budget ?? new RasterCumulativeDebit(RasterEffectPreflight.MaximumLiveBytes, RasterEffectPreflight.MaximumWorkSamples);
        _cancellation = filters?.Cancellation ?? default;
    }
    internal RasterCumulativeDebit Budget { get; }
    internal CancellationToken Cancellation => _cancellation;
    internal string? ItemId { get; set; }
    internal void CheckCancellation() => _cancellation.ThrowIfCancellationRequested();

    internal void ReserveCanvas(int width, int height) => ReservePixelArray(RasterAllocationGuard.Calculate(width, height, 1, 1).ByteCount);
    internal void ReservePixelArray(long bytes) => Reserve(bytes, bytes);
    internal void ReserveWorkSamples(long samples) => Reserve(0, samples);
    internal void Reserve(long bytes, long samples) {
        CheckCancellation();
        if (bytes < 0 || bytes > Budget.MaximumBytes - Budget.Bytes)
            throw new VisualResourceLimitException("NativeRasterResourceBytes", Budget.MaximumBytes, checked(Budget.Bytes + bytes), ItemId);
        if (samples < 0 || samples > Budget.MaximumWorkSamples - Budget.WorkSamples)
            throw new VisualResourceLimitException("NativeRasterResourceWorkSamples", Budget.MaximumWorkSamples, checked(Budget.WorkSamples + samples), ItemId);
        // The checks and reservation are one synchronous compilation's operations, never shared mutable renderer state.
        if (!Budget.TryReserve(bytes, samples)) throw new InvalidOperationException("The artwork reservation changed during capture.");
    }

    internal bool TryCaptureRaster(string href, int width, int height, string? aspect, out VisualSceneImageData resource) {
        CheckCancellation();
        if (_rasters.TryGetValue(href, out resource!)) return true;
        resource = null!;
        if (_invalidRasters.Contains(href)) return false;
        if (href.Length > VisualArtifactInterchangeEnvelope.MaximumArtworkImageDataUriCharacters)
            throw new VisualResourceLimitException(nameof(VisualArtifactInterchangeEnvelope.MaximumArtworkImageDataUriCharacters),
                VisualArtifactInterchangeEnvelope.MaximumArtworkImageDataUriCharacters, href.Length, ItemId);
        try { VisualArtifactInterchangeArtworkText.ValidateImageHref(href); }
        catch (ArgumentException) { _invalidRasters.Add(href); return false; }
        const int bytesPerPixel = 8; // Existing conservative decoded RGBA plus immutable-copy policy.
        var remaining = (Budget.MaximumBytes - Budget.Bytes) / bytesPerPixel;
        var limits = new RasterDecodeOptions { MaximumPixels = (int)Math.Max(1, Math.Min(remaining, RasterDecodeLimits.Default.MaximumPixels)) };
        // Dimension de-duplication belongs to this exact href, never to all resources with the same dimensions.
        var observation = new RasterDecodeObservation(remaining, Budget);
        var previousBytes = Budget.Bytes; var previousWork = Budget.WorkSamples;
        var decoded = SvgRasterRenderer.TryDecodeObservedRasterResource(href, width, height, aspect, limits, observation, out var pixels);
        if (observation.PixelLimitExceeded)
            throw new VisualResourceLimitException("NativeRasterResourceBytes", Budget.MaximumBytes,
                checked(previousBytes + observation.ObservedPixels * bytesPerPixel), ItemId);
        if (observation.WorkLimitExceeded)
            throw new VisualResourceLimitException("NativeRasterResourceWorkSamples", Budget.MaximumWorkSamples,
                checked(previousWork + observation.ObservedPixels * bytesPerPixel), ItemId);
        if (!decoded) { _invalidRasters.Add(href); return false; }
        ReserveWorkSamples(checked((long)pixels.Width * pixels.Height)); // Immutable snapshot's alpha-extent scan.
        resource = new VisualSceneImageData(pixels, _cancellation); _rasters.Add(href, resource); return true;
    }

    internal bool TryGetSvg(SvgRasterCaptureKey key, out SvgRasterCapturedImage image) {
        CheckCancellation();
        return _svg.TryGetValue(key, out image!);
    }

    internal SvgRasterCapturedImage CaptureSvg(SvgRasterCaptureKey key, RgbaImage image, bool capturedEffects) {
        ReservePixelArray(checked((long)image.Width * image.Height * 4));
        ReserveWorkSamples(checked((long)image.Width * image.Height));
        var captured = new SvgRasterCapturedImage(new VisualSceneImageData(image, _cancellation), capturedEffects);
        // An exhausted cache keeps rendering/admission exact; uncached captures still pay every real allocation.
        if (_svg.Count < MaximumSvgCacheEntries) _svg.Add(key, captured);
        return captured;
    }
}

/// <summary>Only immutable rendered pixels are cached; no parsed XML, mutable canvas or renderer state escapes.</summary>
internal sealed class SvgRasterCapturedImage {
    internal SvgRasterCapturedImage(VisualSceneImageData resource, bool capturedEffects) { Resource = resource; CapturedEffects = capturedEffects; }
    internal VisualSceneImageData Resource { get; }
    internal bool CapturedEffects { get; }
}

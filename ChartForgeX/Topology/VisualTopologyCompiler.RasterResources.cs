using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.SvgRaster;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private readonly Dictionary<string, VisualSceneImageData> _rasterResources = new(StringComparer.Ordinal);
    private readonly HashSet<string> _invalidRasterResources = new(StringComparer.Ordinal);
    private long _declaredRasterResourceBytes;

    private static bool IsEmbeddedRasterResource(string href) => href.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase)
        && !href.StartsWith("data:image/svg+xml", StringComparison.OrdinalIgnoreCase);

    private bool TryCaptureRasterResource(TopologyNode node, string href, ChartRect bounds, string? aspect, out VisualSceneImageData resource) {
        if (_rasterResources.TryGetValue(href, out resource!)) return true;
        resource = null!;
        if (_invalidRasterResources.Contains(href)) return false;
        if (href.Length > VisualArtifactInterchangeEnvelope.MaximumArtworkImageDataUriCharacters)
            throw new VisualResourceLimitException(nameof(VisualArtifactInterchangeEnvelope.MaximumArtworkImageDataUriCharacters),
                VisualArtifactInterchangeEnvelope.MaximumArtworkImageDataUriCharacters, href.Length, node.Id);
        // Large qualified raster strings share the exact portable MIME/Base64/signature owner; small legacy text policies stay unchanged.
        try { VisualArtifactInterchangeArtworkText.ValidateImageHref(href); }
        catch (ArgumentException) { _invalidRasterResources.Add(href); return false; }
        const int rgbaAndImmutableCopyBytesPerPixel = 8;
        var remaining = (RasterEffectPreflight.MaximumLiveBytes - _declaredRasterResourceBytes) / rgbaAndImmutableCopyBytesPerPixel;
        // Keep malformed/no-dimensions fallback distinct even when the declared pixel allowance is exhausted.
        // Public decode options remain positive; the internal observation can admit exactly zero further pixels.
        var limits = new RasterDecodeOptions { MaximumPixels = (int)Math.Max(1, Math.Min(remaining, RasterDecodeLimits.Default.MaximumPixels)) };
        var observed = new RasterDecodeObservation(remaining); var size = ArtworkSamplingSize(bounds);
        var decoded = SvgRasterRenderer.TryDecodeObservedRasterResource(href, size.Width, size.Height, aspect, limits, observed, out var pixels);
        var previous = _declaredRasterResourceBytes;
        _declaredRasterResourceBytes += observed.DeclaredPixels * rgbaAndImmutableCopyBytesPerPixel;
        if (observed.PixelLimitExceeded)
            throw new VisualResourceLimitException("NativeRasterResourceBytes", RasterEffectPreflight.MaximumLiveBytes,
                previous + observed.ObservedPixels * rgbaAndImmutableCopyBytesPerPixel, node.Id);
        if (!decoded) { _invalidRasterResources.Add(href); return false; }
        resource = new VisualSceneImageData(pixels); _rasterResources.Add(href, resource); return true;
    }
}

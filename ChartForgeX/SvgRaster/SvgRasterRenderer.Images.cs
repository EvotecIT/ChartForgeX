using System;
using System.Text;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.SvgRaster;

internal static partial class SvgRasterRenderer {
    /// <summary>Decodes embedded image data only, retaining the same bounded SVG recursion used by image elements.</summary>
    internal static bool TryDecodeImageResource(string? href, int width, int height, string? preserveAspectRatio, out RgbaImage image) =>
        TryDecodeImage(href, 0, width, height, preserveAspectRatio, out image);

    internal static bool TryDecodeObservedRasterResource(string href, int width, int height, string? preserveAspectRatio,
        RasterDecodeOptions limits, RasterDecodeObservation observation, out RgbaImage image) =>
        TryDecodeImage(href, 0, width, height, preserveAspectRatio, out image, out _, rasterLimits: limits, observation: observation);

    private static bool TryDecodeImage(string? href, int imageDepth, int targetWidth, int targetHeight, string? preserveAspectRatio,
        out RgbaImage image, SvgRasterDiagnostics? diagnostics = null, SvgRasterFilterContext? filters = null) =>
        TryDecodeImage(href, imageDepth, targetWidth, targetHeight, preserveAspectRatio, out image, out _, diagnostics, filters);

    private static bool TryDecodeImage(string? href, int imageDepth, int targetWidth, int targetHeight, string? preserveAspectRatio,
        out RgbaImage image, out bool capturedEffectPixels, SvgRasterDiagnostics? diagnostics = null, SvgRasterFilterContext? filters = null, RasterDecodeOptions? rasterLimits = null, RasterDecodeObservation? observation = null, SvgRasterResourceContext? resources = null) =>
        TryDecodeImage(href, imageDepth, targetWidth, targetHeight, preserveAspectRatio, out image, out capturedEffectPixels, out _, diagnostics, filters, rasterLimits, observation, resources);

    private static bool TryDecodeImage(string? href, int imageDepth, int targetWidth, int targetHeight, string? preserveAspectRatio,
        out RgbaImage image, out bool capturedEffectPixels, out VisualSceneImageData? capturedResource,
        SvgRasterDiagnostics? diagnostics = null, SvgRasterFilterContext? filters = null, RasterDecodeOptions? rasterLimits = null,
        RasterDecodeObservation? observation = null, SvgRasterResourceContext? resources = null) {
        image = default;
        capturedEffectPixels = false;
        capturedResource = null;
        if (imageDepth >= 4 || string.IsNullOrWhiteSpace(href) || !href!.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase)) return false;
        var comma = href.IndexOf(',');
        if (comma < 0) return false;
        var header = href.Substring(5, comma - 5);
        var payload = href.Substring(comma + 1);
        try {
            if (header.StartsWith("image/svg+xml", StringComparison.OrdinalIgnoreCase)) {
                // The existing depth guard above always wins over a previously captured shallower image.
                var key = new SvgRasterCaptureKey(href, false, null, preserveAspectRatio, targetWidth, targetHeight, imageDepth, filters, diagnostics);
                if (resources != null && resources.TryGetSvg(key, out var prior)) {
                    capturedResource = prior.Resource; image = capturedResource.Image; capturedEffectPixels = prior.CapturedEffects; return true;
                }
                var markup = header.IndexOf(";base64", StringComparison.OrdinalIgnoreCase) >= 0
                    ? Encoding.UTF8.GetString(Convert.FromBase64String(payload))
                    : Uri.UnescapeDataString(payload);
                var document = SvgRasterParser.ParseDocument(markup);
                document.Diagnostics = diagnostics;
                document.Filters = filters;
                document.Resources = resources;
                var embeddedWidth = Math.Max(1, targetWidth);
                var embeddedHeight = Math.Max(1, targetHeight);
                var rgba = RenderDocument(document, preserveAspectRatio, embeddedWidth, embeddedHeight, imageDepth + 1, out capturedEffectPixels);
                image = new RgbaImage(embeddedWidth, embeddedHeight, rgba);
                if (resources != null) {
                    var captured = resources.CaptureSvg(key, image, capturedEffectPixels);
                    capturedResource = captured.Resource; image = capturedResource.Image;
                }
                return true;
            }
            if (resources != null) {
                if (!resources.TryCaptureRaster(href, targetWidth, targetHeight, preserveAspectRatio, out var resource)) return false;
                capturedResource = resource; image = resource.Image; return true;
            }
            var data = header.IndexOf(";base64", StringComparison.OrdinalIgnoreCase) >= 0
                ? Convert.FromBase64String(payload)
                : Encoding.UTF8.GetBytes(Uri.UnescapeDataString(payload));
            return observation == null ? RasterImageDecoder.TryDecode(data, out image)
                : RasterImageDecoder.TryDecodeObserved(data, rasterLimits!, observation, out image);
        } catch (Exception ex) when (ex is not VisualResourceLimitException && (ex is FormatException || ex is ArgumentException || ex is InvalidOperationException || ex is System.Xml.XmlException)) {
            return false;
        }
    }
}

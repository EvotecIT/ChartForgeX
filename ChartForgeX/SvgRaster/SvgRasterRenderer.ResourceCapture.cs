using System;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.SvgRaster;

internal static partial class SvgRasterRenderer {
    internal static bool TryCaptureImageResource(string href, int width, int height, string? aspect,
        SvgRasterResourceContext resources, out VisualSceneImageData resource) {
        var decoded = TryDecodeImage(href, 0, width, height, aspect, out _, out _, out var captured, resources: resources);
        resource = captured!;
        return decoded;
    }

    internal static bool TryCaptureFragment(string body, string? viewBox, string? aspect, int width, int height,
        SvgRasterResourceContext resources, out VisualSceneImageData resource) {
        resource = null!;
        if (string.IsNullOrWhiteSpace(body) || width <= 0 || height <= 0) return false;
        var key = new SvgRasterCaptureKey(body, true, viewBox, aspect, width, height, 0);
        if (resources.TryGetSvg(key, out var prior)) { resource = prior.Resource; return true; }
        try {
            var document = SvgRasterParser.ParseFragment(body, viewBox); document.Resources = resources;
            var rgba = RenderDocument(document, aspect, width, height);
            resources.ReserveWorkSamples(checked((long)width * height));
            if (!HasVisiblePixel(rgba)) return false;
            resource = resources.CaptureSvg(key, new RgbaImage(width, height, rgba), false).Resource;
            return true;
        } catch (Exception ex) when (ex is not VisualResourceLimitException &&
            (ex is FormatException || ex is InvalidOperationException || ex is ArgumentException || ex is System.Xml.XmlException)) {
            return false;
        }
    }
}

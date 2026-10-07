using System;
using System.Text;
using ChartForgeX.Raster;

namespace ChartForgeX.SvgRaster;

internal static partial class SvgRasterRenderer {
    /// <summary>Decodes embedded image data only, retaining the same bounded SVG recursion used by image elements.</summary>
    internal static bool TryDecodeImageResource(string? href, int width, int height, string? preserveAspectRatio, out RgbaImage image) =>
        TryDecodeImage(href, 0, width, height, preserveAspectRatio, out image);

    private static bool TryDecodeImage(string? href, int imageDepth, int targetWidth, int targetHeight, string? preserveAspectRatio, out RgbaImage image, SvgRasterDiagnostics? diagnostics = null) {
        image = default;
        if (imageDepth >= 4 || string.IsNullOrWhiteSpace(href) || !href!.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase)) return false;
        var comma = href.IndexOf(',');
        if (comma < 0) return false;
        var header = href.Substring(5, comma - 5);
        var payload = href.Substring(comma + 1);
        try {
            if (header.StartsWith("image/svg+xml", StringComparison.OrdinalIgnoreCase)) {
                var markup = header.IndexOf(";base64", StringComparison.OrdinalIgnoreCase) >= 0
                    ? Encoding.UTF8.GetString(Convert.FromBase64String(payload))
                    : Uri.UnescapeDataString(payload);
                var document = SvgRasterParser.ParseDocument(markup);
                document.Diagnostics = diagnostics;
                var embeddedWidth = Math.Max(1, targetWidth);
                var embeddedHeight = Math.Max(1, targetHeight);
                var rgba = RenderDocument(document, preserveAspectRatio, embeddedWidth, embeddedHeight, imageDepth + 1);
                image = new RgbaImage(embeddedWidth, embeddedHeight, rgba);
                return true;
            }
            var data = header.IndexOf(";base64", StringComparison.OrdinalIgnoreCase) >= 0
                ? Convert.FromBase64String(payload)
                : Encoding.UTF8.GetBytes(Uri.UnescapeDataString(payload));
            return RasterImageDecoder.TryDecode(data, out image);
        } catch (Exception ex) when (ex is FormatException || ex is ArgumentException || ex is InvalidOperationException || ex is System.Xml.XmlException) {
            return false;
        }
    }
}

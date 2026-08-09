using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Raster;

namespace ChartForgeX.SvgRaster;

/// <summary>Provides deterministic, dependency-free rasterization of SVG documents.</summary>
public static class SvgRasterizer {
    /// <summary>Rasterizes an SVG document to PNG using its declared viewport size.</summary>
    public static byte[] ToPng(string svg, RasterImageOptions? options = null) => ToPng(svg, null, null, options);

    /// <summary>Rasterizes an SVG document to PNG using optional output dimensions.</summary>
    public static byte[] ToPng(string svg, int? width, int? height, RasterImageOptions? options = null) {
        if (string.IsNullOrWhiteSpace(svg)) throw new ArgumentException("SVG content cannot be empty.", nameof(svg));
        if (width.HasValue && width.Value <= 0) throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be greater than zero.");
        if (height.HasValue && height.Value <= 0) throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be greater than zero.");
        XDocument document;
        try {
            var settings = new XmlReaderSettings {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            };
            using var textReader = new StringReader(svg);
            using XmlReader xmlReader = XmlReader.Create(textReader, settings);
            document = XDocument.Load(xmlReader, LoadOptions.PreserveWhitespace);
        } catch (Exception ex) when (ex is System.Xml.XmlException || ex is InvalidOperationException) {
            throw new FormatException("SVG content is not a valid XML document.", ex);
        }
        XElement root = document.Root ?? throw new FormatException("SVG content does not contain a document element.");
        if (!string.Equals(root.Name.LocalName, "svg", StringComparison.OrdinalIgnoreCase)) throw new FormatException("SVG content must have an svg document element.");
        string? viewBox = Attribute(root, "viewBox");
        (double viewWidth, double viewHeight) = Viewport(root, viewBox);
        int targetWidth = width ?? Math.Max(1, (int)Math.Round(viewWidth, MidpointRounding.AwayFromZero));
        int targetHeight = height ?? Math.Max(1, (int)Math.Round(viewHeight, MidpointRounding.AwayFromZero));
        string body = string.Concat(root.Nodes().Select(node => node.ToString(SaveOptions.DisableFormatting)));
        if (!SvgRasterRenderer.TryRenderFragment(body, viewBox, Attribute(root, "preserveAspectRatio"), targetWidth, targetHeight, out byte[] rgba)) {
            throw new NotSupportedException("SVG content could not be rasterized by ChartForgeX.");
        }
        return PngWriter.WriteRgba(targetWidth, targetHeight, rgba, options);
    }

    /// <summary>Rasterizes UTF-8 SVG bytes to PNG.</summary>
    public static byte[] ToPng(byte[] svgBytes, int? width = null, int? height = null, RasterImageOptions? options = null) {
        if (svgBytes == null) throw new ArgumentNullException(nameof(svgBytes));
        return ToPng(Encoding.UTF8.GetString(svgBytes), width, height, options);
    }

    private static (double Width, double Height) Viewport(XElement root, string? viewBox) {
        double? width = Length(Attribute(root, "width"));
        double? height = Length(Attribute(root, "height"));
        if (width.HasValue && height.HasValue) return (width.Value, height.Value);
        if (!string.IsNullOrWhiteSpace(viewBox)) {
            string[] parts = viewBox!.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 4 && Parse(parts[2], out double viewWidth) && Parse(parts[3], out double viewHeight) && viewWidth > 0D && viewHeight > 0D) {
                return (width ?? viewWidth, height ?? viewHeight);
            }
        }
        return (width ?? 800D, height ?? 600D);
    }

    private static string? Attribute(XElement element, string name) => element.Attributes().FirstOrDefault(attribute => string.Equals(attribute.Name.LocalName, name, StringComparison.OrdinalIgnoreCase))?.Value;

    private static double? Length(string? value) {
        if (string.IsNullOrWhiteSpace(value)) return null;
        string text = value!.Trim();
        if (text.EndsWith("px", StringComparison.OrdinalIgnoreCase)) text = text.Substring(0, text.Length - 2).Trim();
        return Parse(text, out double parsed) && parsed > 0D ? parsed : (double?)null;
    }

    private static bool Parse(string text, out double value) => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && !double.IsNaN(value) && !double.IsInfinity(value);
}

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
                XmlResolver = null,
                MaxCharactersInDocument = 16_000_000
            };
            using var textReader = new StringReader(svg);
            using XmlReader xmlReader = XmlReader.Create(textReader, settings);
            document = XDocument.Load(xmlReader, LoadOptions.PreserveWhitespace);
        } catch (Exception ex) when (ex is System.Xml.XmlException || ex is InvalidOperationException) {
            throw new FormatException("SVG content is not a valid XML document.", ex);
        }
        XElement root = document.Root ?? throw new FormatException("SVG content does not contain a document element.");
        if (!string.Equals(root.Name.LocalName, "svg", StringComparison.OrdinalIgnoreCase)) throw new FormatException("SVG content must have an svg document element.");
        SvgRasterParser.ValidateElementDepth(root);
        string? viewBox = Attribute(root, "viewBox");
        (double viewWidth, double viewHeight) = Viewport(root, viewBox);
        int targetWidth = width
            ?? (height.HasValue
                ? OutputDimension(height.Value * viewWidth / viewHeight, nameof(width))
                : OutputDimension(viewWidth, nameof(width)));
        int targetHeight = height
            ?? (width.HasValue
                ? OutputDimension(width.Value * viewHeight / viewWidth, nameof(height))
                : OutputDimension(viewHeight, nameof(height)));
        string effectiveViewBox = string.IsNullOrWhiteSpace(viewBox)
            ? "0 0 " + viewWidth.ToString(CultureInfo.InvariantCulture) + " " + viewHeight.ToString(CultureInfo.InvariantCulture)
            : viewBox!;
        if (string.IsNullOrWhiteSpace(viewBox)) root.SetAttributeValue("viewBox", effectiveViewBox);
        if (!SvgRasterRenderer.TryRenderDocument(root.ToString(SaveOptions.DisableFormatting), Attribute(root, "preserveAspectRatio"), targetWidth, targetHeight, out byte[] rgba)) {
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
            string[] parts = viewBox!.Split(new[] { ' ', '\t', '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 4 && Parse(parts[2], out double viewWidth) && Parse(parts[3], out double viewHeight) && viewWidth > 0D && viewHeight > 0D) {
                if (width.HasValue) return (width.Value, width.Value * viewHeight / viewWidth);
                if (height.HasValue) return (height.Value * viewWidth / viewHeight, height.Value);
                var defaultScale = Math.Min(300D / viewWidth, 150D / viewHeight);
                return (viewWidth * defaultScale, viewHeight * defaultScale);
            }
        }
        return (width ?? 300D, height ?? 150D);
    }

    private static string? Attribute(XElement element, string name) => element.Attributes().FirstOrDefault(attribute => string.Equals(attribute.Name.LocalName, name, StringComparison.OrdinalIgnoreCase))?.Value;

    private static double? Length(string? value) {
        return SvgRasterViewBox.TryParseLength(value, out double parsed) ? parsed : (double?)null;
    }

    private static int OutputDimension(double value, string parameterName) {
        var rounded = Math.Round(value, MidpointRounding.AwayFromZero);
        if (double.IsNaN(rounded) || double.IsInfinity(rounded) || value <= 0 || rounded > int.MaxValue) {
            throw new ArgumentOutOfRangeException(parameterName, value, "SVG raster output dimensions exceed the supported allocation range.");
        }
        return Math.Max(1, (int)rounded);
    }

    private static bool Parse(string text, out double value) => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && !double.IsNaN(value) && !double.IsInfinity(value);
}

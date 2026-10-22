using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.SvgRaster;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Playground;

public static partial class PlaygroundEngine {
    // Browser exports use one bounded request, even when its Markdown contains several fences.
    // These match the existing default native Mermaid export bounds; callers can only tighten them.
    private const long MaximumPngPixels = 16_777_216;
    private const long MaximumPngBytes = 8_388_608;
    private const int MaximumSvgCharacters = 16_000_000;
    private static readonly XNamespace SvgNamespace = "http://www.w3.org/2000/svg";

    private static CompletedComposition ComposeCompletedSvgs(IReadOnlyList<CompletedSvg> parts) {
        if (parts.Count == 0) throw new InvalidOperationException("The source did not produce a visual to export.");
        if (parts.Count == 1) return new CompletedComposition(parts[0].Element, parts[0].Width, parts[0].Height);

        const double gap = 32;
        var width = parts.Max(part => part.Width);
        var height = parts.Sum(part => part.Height) + gap * (parts.Count - 1);
        var outer = new XElement(SvgNamespace + "svg", new XAttribute("width", Number(width)),
            new XAttribute("height", Number(height)), new XAttribute("viewBox", "0 0 " + Number(width) + " " + Number(height)));
        double y = 0;
        foreach (var part in parts) {
            var child = new XElement(part.Element);
            // Match JavaScript Math.round for non-negative centered positions, including half-pixels.
            child.SetAttributeValue("x", Number(Math.Floor((width - part.Width) / 2 + .5)));
            child.SetAttributeValue("y", Number(y));
            outer.Add(child);
            y += part.Height + gap;
        }
        return new CompletedComposition(outer, width, height);
    }

    private static byte[] ExportPng(IReadOnlyList<CompletedSvg> parts, CompletedComposition composition, MermaidResourceLimits limits) {
        var pixels = checked((long)composition.PixelWidth * composition.PixelHeight);
        CheckPixelLimit(pixels, MaximumPngPixels);

        // Preserve each producer's captured policy and cancellation checks. The cloned envelope measures
        // the actual composed 2x destination, so a stricter source policy cannot be lost in composition.
        foreach (var part in parts) {
            var request = part.Artifact.Clone();
            request.RenderSource = composition;
            request.ExportPolicy?.BeforeExport(request, VisualArtifactExportFormat.Png, null);
        }
        CheckPixelLimit(pixels, limits.MaximumRasterPixels);

        var png = composition.RenderRgba().ToPng();
        CheckByteLimit(png.LongLength, MaximumPngBytes);
        foreach (var part in parts) part.Artifact.ExportPolicy?.ValidateOutput(VisualArtifactExportFormat.Png, png.LongLength);
        CheckByteLimit(png.LongLength, limits.MaximumOutputBytes);
        return png;
    }

    private static void CheckPixelLimit(long pixels, long limit) {
        if (pixels > limit) throw new InvalidOperationException("PNG export requires " + pixels.ToString(CultureInfo.InvariantCulture)
            + " pixels; the playground accepts at most " + limit.ToString(CultureInfo.InvariantCulture) + ".");
    }

    private static void CheckByteLimit(long bytes, long limit) {
        if (bytes > limit) throw new InvalidOperationException("PNG export produced " + bytes.ToString(CultureInfo.InvariantCulture)
            + " bytes; the playground accepts at most " + limit.ToString(CultureInfo.InvariantCulture) + ".");
    }

    private static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    private sealed class CompletedSvg {
        internal CompletedSvg(VisualArtifact artifact, string svg) {
            Artifact = artifact;
            if (svg.Length > MaximumSvgCharacters) throw new InvalidOperationException("The completed SVG exceeds the supported native export limit.");
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = MaximumSvgCharacters };
            using var text = new StringReader(svg);
            using var reader = XmlReader.Create(text, settings);
            Element = XElement.Load(reader, LoadOptions.PreserveWhitespace);
            if (Element.Name != SvgNamespace + "svg") throw new InvalidOperationException("The completed visual is not an SVG document.");
            Width = Dimension(Element, "width");
            Height = Dimension(Element, "height");
            // Browser preview zoom is not part of the downloadable native frame.
            Element.Attribute("style")?.Remove();
            Element.SetAttributeValue("width", Number(Width));
            Element.SetAttributeValue("height", Number(Height));
        }

        internal VisualArtifact Artifact { get; }
        internal XElement Element { get; }
        internal double Width { get; }
        internal double Height { get; }

        private static double Dimension(XElement svg, string name) {
            if (!double.TryParse((string?)svg.Attribute(name), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                || !double.IsFinite(value) || value <= 0 || value > int.MaxValue / 2d)
                throw new InvalidOperationException("The completed SVG has an unsupported " + name + ".");
            return value;
        }
    }

    /// <summary>Adapts completed native SVG to existing strict rasterization without repeating layout or theme selection.</summary>
    private sealed class CompletedComposition : IMeasuredStaticVisualSource {
        private readonly XElement _element;
        private readonly double _width;
        private readonly double _height;

        internal CompletedComposition(XElement element, double width, double height) {
            if (!double.IsFinite(width) || !double.IsFinite(height) || width <= 0 || height <= 0
                || width > int.MaxValue / 2d || height > int.MaxValue / 2d)
                throw new InvalidOperationException("The composed export frame exceeds the supported native dimensions.");
            Svg = element.ToString(SaveOptions.DisableFormatting);
            if (Svg.Length > MaximumSvgCharacters) throw new InvalidOperationException("The composed SVG exceeds the supported native export limit.");
            PixelWidth = checked((int)Math.Ceiling(width * 2));
            PixelHeight = checked((int)Math.Ceiling(height * 2));
            _element = element;
            _width = width;
            _height = height;
        }

        internal string Svg { get; }
        internal int PixelWidth { get; }
        internal int PixelHeight { get; }

        public VisualSize MeasureRasterSize() => new(PixelWidth, PixelHeight);
        public string RenderSvg(string idScope) => Svg;

        public RgbaImage RenderRgba() {
            // SVG previews do not need this PNG-only wrapper. Export builds it after the
            // destination pixel bounds and every captured producer policy have been checked.

            // The browser's former canvas scaled by exactly two, then left fractional rounding
            // residual transparent. A pixel-grid outer viewport preserves that mapping instead
            // of centering a fractional frame in a slightly taller integer viewport.
            var child = new XElement(_element);
            child.SetAttributeValue("x", "0");
            child.SetAttributeValue("y", "0");
            child.SetAttributeValue("width", Number(_width * 2));
            child.SetAttributeValue("height", Number(_height * 2));
            var wrapper = new XElement(SvgNamespace + "svg", new XAttribute("width", PixelWidth), new XAttribute("height", PixelHeight),
                new XAttribute("viewBox", "0 0 " + PixelWidth.ToString(CultureInfo.InvariantCulture) + " " + PixelHeight.ToString(CultureInfo.InvariantCulture)), child);
            return SvgRasterizer.Rasterize(wrapper.ToString(SaveOptions.DisableFormatting), PixelWidth, PixelHeight, strict: true).Image;
        }
    }
}

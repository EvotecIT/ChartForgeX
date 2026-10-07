using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace ChartForgeX.SvgRaster;

internal static class SvgRasterParser {
    internal const int MaximumElementDepth = 256;
    private const long MaximumDocumentCharacters = 16_000_000;

    public static SvgRasterDocument ParseFragment(string svgBody, string? viewBox) {
        if (svgBody == null) throw new ArgumentNullException(nameof(svgBody));
        var markup = "<svg viewBox=\"" + EscapeAttribute(viewBox ?? "0 0 24 24") + "\">" + svgBody + "</svg>";
        var root = Load(markup).Root ?? throw new FormatException("SVG fragment did not contain a root element.");
        return FromRoot(root, SvgRasterViewBox.Parse(root.Attribute("viewBox")?.Value));
    }

    public static SvgRasterDocument ParseDocument(string markup) {
        if (markup == null) throw new ArgumentNullException(nameof(markup));
        var root = Load(markup).Root ?? throw new FormatException("SVG document did not contain a root element.");
        if (!string.Equals(root.Name.LocalName, "svg", StringComparison.OrdinalIgnoreCase)) throw new FormatException("SVG document root must be an svg element.");
        return FromRoot(root, SvgRasterViewBox.FromDimensions(root.Attribute("width")?.Value, root.Attribute("height")?.Value));
    }

    /// <summary>Builds a document from an svg root element the caller has already parsed and validated.</summary>
    public static SvgRasterDocument FromDocumentRoot(XElement root) {
        if (root == null) throw new ArgumentNullException(nameof(root));
        return FromRoot(root, SvgRasterViewBox.FromDimensions(root.Attribute("width")?.Value, root.Attribute("height")?.Value));
    }

    private static SvgRasterDocument FromRoot(XElement root, SvgRasterViewBox fallbackViewBox) {
        ValidateElementDepth(root);
        var viewBox = fallbackViewBox;
        if (string.Equals(root.Name.LocalName, "svg", StringComparison.OrdinalIgnoreCase) && root.Attribute("viewBox") != null) {
            viewBox = SvgRasterViewBox.Parse(root.Attribute("viewBox")!.Value);
        }

        return new SvgRasterDocument(viewBox, ReadElement(root));
    }

    private static XDocument Load(string markup) {
        var settings = new XmlReaderSettings {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = MaximumDocumentCharacters
        };
        using var reader = XmlReader.Create(new StringReader(markup), settings);
        return XDocument.Load(reader, LoadOptions.PreserveWhitespace);
    }

    internal static void ValidateElementDepth(XElement root) {
        if (root == null) throw new ArgumentNullException(nameof(root));
        var elements = new Stack<XElement>();
        var depths = new Stack<int>();
        elements.Push(root);
        depths.Push(1);
        while (elements.Count > 0) {
            var element = elements.Pop();
            var depth = depths.Pop();
            if (depth > MaximumElementDepth) {
                throw new FormatException("SVG element nesting exceeds the supported depth of " + MaximumElementDepth + ".");
            }
            foreach (var child in element.Elements()) {
                elements.Push(child);
                depths.Push(depth + 1);
            }
        }
    }

    internal static SvgRasterElement ReadStyleElement(XElement element) {
        var attributes = new Dictionary<string, string>(AttributeCount(element), StringComparer.Ordinal);
        foreach (var attribute in element.Attributes()) {
            if (!attribute.IsNamespaceDeclaration) attributes[attribute.Name.LocalName] = attribute.Value;
        }
        return new SvgRasterElement(element.Name.LocalName, attributes, Array.Empty<SvgRasterElement>(), string.Empty);
    }

    private static SvgRasterElement ReadElement(XElement element) {
        var attributes = new Dictionary<string, string>(AttributeCount(element), StringComparer.Ordinal);
        foreach (var attribute in element.Attributes()) {
            if (attribute.IsNamespaceDeclaration) continue;
            var name = attribute.Name.Namespace == XNamespace.Xml ? "xml:" + attribute.Name.LocalName : attribute.Name.LocalName;
            attributes[name] = attribute.Value;
        }

        var children = new List<SvgRasterElement>();
        var content = new List<SvgRasterContent>();
        foreach (var node in element.Nodes()) {
            if (node is XElement childNode) {
                var child = ReadElement(childNode);
                children.Add(child);
                content.Add(SvgRasterContent.FromElement(child));
            } else if (node is XText textNode) {
                content.Add(SvgRasterContent.FromText(textNode.Value));
            }
        }

        // Only text-bearing elements need aggregate descendant text. Computing it for
        // every group duplicates all captions at each ancestor in dense scenes.
        var text = element.Name.LocalName is "text" or "tspan" or "style" ? element.Value : string.Empty;
        return new SvgRasterElement(element.Name.LocalName, attributes, children, text, content);
    }

    // Sizing the dictionary up front avoids rehashing while attributes are added; lookups and order are unchanged.
    private static int AttributeCount(XElement element) {
        var count = 0;
        for (var attribute = element.FirstAttribute; attribute != null; attribute = attribute.NextAttribute) count++;
        return count;
    }

    private static string EscapeAttribute(string value) =>
        value.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;");
}

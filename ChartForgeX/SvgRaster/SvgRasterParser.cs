using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace ChartForgeX.SvgRaster;

internal static class SvgRasterParser {
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

    private static SvgRasterDocument FromRoot(XElement root, SvgRasterViewBox fallbackViewBox) {
        var viewBox = fallbackViewBox;
        if (string.Equals(root.Name.LocalName, "svg", StringComparison.OrdinalIgnoreCase) && root.Attribute("viewBox") != null) {
            viewBox = SvgRasterViewBox.Parse(root.Attribute("viewBox")!.Value);
        }

        return new SvgRasterDocument(viewBox, ReadElement(root));
    }

    private static XDocument Load(string markup) {
        var settings = new XmlReaderSettings {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        };
        using var reader = XmlReader.Create(new StringReader(markup), settings);
        return XDocument.Load(reader, LoadOptions.PreserveWhitespace);
    }

    private static SvgRasterElement ReadElement(XElement element) {
        var attributes = new Dictionary<string, string>(StringComparer.Ordinal);
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

        return new SvgRasterElement(element.Name.LocalName, attributes, children, element.Value, content);
    }

    private static string EscapeAttribute(string value) =>
        value.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;");
}

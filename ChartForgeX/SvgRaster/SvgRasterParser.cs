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
        return FromRoot(root, SvgRasterViewBox.Parse(root.Attribute("viewBox")?.Value ?? "0 0 24 24"));
    }

    private static SvgRasterDocument FromRoot(XElement root, SvgRasterViewBox fallbackViewBox) {
        var viewBox = fallbackViewBox;
        if (string.Equals(root.Name.LocalName, "svg", StringComparison.OrdinalIgnoreCase) && root.Attribute("viewBox") != null) {
            viewBox = SvgRasterViewBox.Parse(root.Attribute("viewBox")!.Value);
        }

        return new SvgRasterDocument(viewBox, ReadChildren(root));
    }

    private static XDocument Load(string markup) {
        var settings = new XmlReaderSettings {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        };
        using var reader = XmlReader.Create(new StringReader(markup), settings);
        return XDocument.Load(reader, LoadOptions.None);
    }

    private static IReadOnlyList<SvgRasterElement> ReadChildren(XElement parent) {
        var children = new List<SvgRasterElement>();
        foreach (var child in parent.Elements()) children.Add(ReadElement(child));
        return children;
    }

    private static SvgRasterElement ReadElement(XElement element) {
        var attributes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var attribute in element.Attributes()) {
            if (attribute.IsNamespaceDeclaration) continue;
            var name = attribute.Name.LocalName;
            if (attribute.Name.NamespaceName.Length > 0 && !string.IsNullOrWhiteSpace(attribute.Name.NamespaceName)) name = attribute.Name.LocalName;
            attributes[name] = attribute.Value;
        }

        return new SvgRasterElement(element.Name.LocalName, attributes, ReadChildren(element), element.Value);
    }

    private static string EscapeAttribute(string value) =>
        value.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;");
}

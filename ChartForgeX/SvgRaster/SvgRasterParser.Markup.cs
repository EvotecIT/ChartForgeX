using System;
using System.Collections.Generic;
using System.Text;
using ChartForgeX.Rendering;

namespace ChartForgeX.SvgRaster;

/// <summary>
/// Reads label-scene markup (<see cref="SvgMarkupElement"/>) into raster elements exactly as the XLinq readers above
/// read the equivalent <c>XElement</c>: attribute names without their prefix (<c>xml:</c> kept where the full reader
/// keeps it), no namespace declarations, child text and CDATA as content, and aggregate text only for text, tspan and
/// style elements.
/// </summary>
internal static partial class SvgRasterParser {
    /// <summary>
    /// As <see cref="FromDocumentRoot(System.Xml.Linq.XElement)"/>, reading only the root's children <paramref name="include"/>
    /// accepts, or, with <paramref name="filterDescendants"/>, only the descendants it accepts at any depth.
    /// </summary>
    internal static SvgRasterDocument FromMarkupRoot(SvgMarkupElement root, Func<SvgMarkupElement, bool>? include = null, bool filterDescendants = false) {
        if (root == null) throw new ArgumentNullException(nameof(root));
        ValidateElementDepth(root);
        var viewBox = SvgRasterViewBox.FromDimensions(root.Attribute("width"), root.Attribute("height"));
        if (string.Equals(root.LocalName, "svg", StringComparison.OrdinalIgnoreCase) && root.Attribute("viewBox") is { } box) viewBox = SvgRasterViewBox.Parse(box);
        return new SvgRasterDocument(viewBox, ReadMarkupElement(root, include, null, out _, filterDescendants));
    }

    /// <summary>As <see cref="ReadStyleElement(System.Xml.Linq.XElement)"/>.</summary>
    internal static SvgRasterElement ReadStyleElement(SvgMarkupElement element) {
        var attributes = new Dictionary<string, string>(element.AttributeCount, StringComparer.Ordinal);
        for (var i = 0; i < element.AttributeCount; i++) {
            var name = element.AttributeName(i);
            if (IsNamespaceDeclaration(name)) continue;
            attributes[LocalPart(name)] = element.AttributeValue(i);
        }

        return new SvgRasterElement(element.LocalName, attributes, Array.Empty<SvgRasterElement>(), string.Empty);
    }

    /// <summary>As <see cref="ValidateElementDepth(System.Xml.Linq.XElement)"/>.</summary>
    internal static void ValidateElementDepth(SvgMarkupElement root) {
        if (root == null) throw new ArgumentNullException(nameof(root));
        var elements = new Stack<(SvgMarkupElement Element, int Depth)>();
        elements.Push((root, 1));
        while (elements.Count > 0) {
            var (element, depth) = elements.Pop();
            if (depth > MaximumElementDepth) throw new FormatException("SVG element nesting exceeds the supported depth of " + MaximumElementDepth + ".");
            var nodes = element.Nodes;
            for (var i = 0; i < nodes.Count; i++) {
                if (nodes[i] is SvgMarkupElement child) elements.Push((child, depth + 1));
            }
        }
    }

    // ReadElement for markup; text is the element's own Value over the children that were read.
    internal static SvgRasterElement ReadMarkupElement(SvgMarkupElement element, Func<SvgMarkupElement, bool>? include, Func<string, string, string>? attributeValue, out string value, bool filterDescendants = false) {
        var attributes = new Dictionary<string, string>(element.AttributeCount, StringComparer.Ordinal);
        for (var i = 0; i < element.AttributeCount; i++) {
            var name = element.AttributeName(i);
            if (IsNamespaceDeclaration(name)) continue;
            var key = name.StartsWith("xml:", StringComparison.Ordinal) ? name : LocalPart(name);
            var attribute = element.AttributeValue(i);
            attributes[key] = attributeValue == null ? attribute : attributeValue(LocalPart(name), attribute);
        }

        var children = new List<SvgRasterElement>();
        var content = new List<SvgRasterContent>();
        var text = new StringBuilder();
        var nodes = element.Nodes;
        for (var i = 0; i < nodes.Count; i++) {
            switch (nodes[i]) {
                case SvgMarkupElement childElement:
                    if (include != null && !include(childElement)) continue;
                    var child = ReadMarkupElement(childElement, filterDescendants ? include : null, attributeValue, out var childValue, filterDescendants);
                    children.Add(child);
                    content.Add(SvgRasterContent.FromElement(child));
                    text.Append(childValue);
                    break;
                case SvgMarkupText textNode:
                    // A root read with a child filter stands for a copy holding only the accepted children.
                    if (include != null && !filterDescendants) continue;
                    content.Add(SvgRasterContent.FromText(textNode.Value));
                    text.Append(textNode.Value);
                    break;
            }
        }

        value = text.ToString();
        // Only text-bearing elements need aggregate descendant text, as in ReadElement.
        var localName = element.LocalName;
        var aggregate = localName is "text" or "tspan" or "style" ? value : string.Empty;
        return new SvgRasterElement(localName, attributes, children, aggregate, content);
    }

    private static bool IsNamespaceDeclaration(string name) => name == "xmlns" || name.StartsWith("xmlns:", StringComparison.Ordinal);

    private static string LocalPart(string name) {
        var colon = name.IndexOf(':');
        return colon < 0 ? name : name.Substring(colon + 1);
    }
}

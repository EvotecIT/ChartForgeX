using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace ChartForgeX.Svg;

internal sealed class SvgDocument {
    public SvgDocument(SvgElement root) {
        Root = root ?? throw new ArgumentNullException(nameof(root));
    }

    public SvgElement Root { get; }

    public static SvgDocument Create(double width, double height, string? viewBox = null) {
        var parsedViewBox = viewBox == null ? new SvgViewBox(0, 0, width, height) : SvgViewBox.Parse(viewBox);
        var root = new SvgElement("svg")
            .Attribute("xmlns", "http://www.w3.org/2000/svg")
            .Attribute("width", width)
            .Attribute("height", height)
            .ViewBox(parsedViewBox);
        return new SvgDocument(root);
    }

    public static SvgDocument Parse(string markup) {
        if (markup == null) throw new ArgumentNullException(nameof(markup));
        using var reader = new StringReader(markup);
        return Load(reader);
    }

    public static SvgDocument Load(TextReader input) {
        if (input == null) throw new ArgumentNullException(nameof(input));

        var settings = new XmlReaderSettings {
            DtdProcessing = DtdProcessing.Prohibit,
            IgnoreProcessingInstructions = true,
            IgnoreWhitespace = true
        };

        using var reader = XmlReader.Create(input, settings);
        var stack = new Stack<SvgElement>();
        SvgElement? root = null;

        while (reader.Read()) {
            switch (reader.NodeType) {
                case XmlNodeType.Element:
                    var element = ReadElement(reader);
                    if (stack.Count == 0) {
                        if (root != null) throw new InvalidOperationException("SVG markup can only contain one root element.");
                        root = element;
                    } else {
                        stack.Peek().Add(element);
                    }

                    if (!reader.IsEmptyElement) stack.Push(element);
                    break;
                case XmlNodeType.EndElement:
                    if (stack.Count == 0) throw new InvalidOperationException("SVG markup contains an unexpected closing element.");
                    stack.Pop();
                    break;
                case XmlNodeType.Text:
                case XmlNodeType.CDATA:
                    if (stack.Count == 0) throw new InvalidOperationException("SVG markup text must belong to an element.");
                    stack.Peek().Text(reader.Value);
                    break;
                case XmlNodeType.Comment:
                    if (stack.Count > 0) stack.Peek().Comment(reader.Value);
                    break;
            }
        }

        if (root == null) throw new InvalidOperationException("SVG markup did not contain a root element.");
        if (stack.Count != 0) throw new InvalidOperationException("SVG markup ended before all elements were closed.");
        return new SvgDocument(root);
    }

    public SvgElement? FindById(string id) => Root.FindById(id);

    public string ToMarkup() {
        var writer = new SvgMarkupWriter();
        WriteTo(writer);
        return writer.Build();
    }

    public void Save(TextWriter output) {
        if (output == null) throw new ArgumentNullException(nameof(output));
        output.Write(ToMarkup());
    }

    public void WriteTo(SvgMarkupWriter writer) {
        if (writer == null) throw new ArgumentNullException(nameof(writer));
        Root.WriteTo(writer);
    }

    private static SvgElement ReadElement(XmlReader reader) {
        var element = new SvgElement(reader.Name);
        if (!reader.HasAttributes) return element;

        while (reader.MoveToNextAttribute()) {
            element.Attribute(reader.Name, reader.Value);
        }

        reader.MoveToElement();
        return element;
    }
}

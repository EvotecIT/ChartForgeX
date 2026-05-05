using System;
using System.Text;

namespace ChartForgeX.Interactivity.Html;

internal static class HtmlInteractiveMarkup {
    internal static HtmlAttribute Attr(string name, string value) => new HtmlAttribute(name, value, false);

    internal static HtmlAttribute BoolAttr(string name) => new HtmlAttribute(name, string.Empty, true);

    internal static HtmlAttribute? OptionalAttr(string name, string? value) =>
        value == null ? null : new HtmlAttribute(name, value, false);

    internal static void AppendStartTag(StringBuilder sb, string tagName, params HtmlAttribute?[] attributes) {
        sb.Append('<').Append(tagName);
        AppendAttributes(sb, attributes);
        sb.Append('>');
    }

    internal static void AppendEndTag(StringBuilder sb, string tagName) {
        sb.Append("</").Append(tagName).Append('>');
    }

    internal static void AppendElement(StringBuilder sb, string tagName, string content, params HtmlAttribute?[] attributes) {
        AppendStartTag(sb, tagName, attributes);
        sb.Append(content);
        AppendEndTag(sb, tagName);
    }

    internal static string Element(string tagName, string content, params HtmlAttribute?[] attributes) {
        var sb = new StringBuilder();
        AppendElement(sb, tagName, content, attributes);
        return sb.ToString();
    }

    internal static string Button(string content, params HtmlAttribute?[] attributes) =>
        Element("button", HtmlInteractiveChartRenderer.EscapeHtml(content), attributes);

    private static void AppendAttributes(StringBuilder sb, HtmlAttribute?[] attributes) {
        for (var i = 0; i < attributes.Length; i++) {
            if (!attributes[i].HasValue) continue;
            var attribute = attributes[i].GetValueOrDefault();
            sb.Append(' ').Append(attribute.Name);
            if (attribute.IsBoolean) continue;
            sb.Append("=\"").Append(HtmlInteractiveChartRenderer.EscapeHtml(attribute.Value)).Append('"');
        }
    }

    internal readonly struct HtmlAttribute {
        internal HtmlAttribute(string name, string value, bool isBoolean) {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("HTML attribute names cannot be empty.", nameof(name));
            Name = name;
            Value = value;
            IsBoolean = isBoolean;
        }

        internal string Name { get; }

        internal string Value { get; }

        internal bool IsBoolean { get; }
    }
}

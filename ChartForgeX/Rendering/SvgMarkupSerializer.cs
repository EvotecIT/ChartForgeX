using System;
using System.Text;

namespace ChartForgeX.Rendering;

/// <summary>
/// Writes a <see cref="SvgMarkupDocument"/> as equivalent compact XML: double-quoted attributes,
/// <c>&lt;name/&gt;</c> for empty elements, line ends as the platform
/// new line in text and as character references in attributes, and the framework's escaping.
/// </summary>
internal static class SvgMarkupSerializer {
    internal static string Write(SvgMarkupDocument document) {
        var builder = new StringBuilder(4096);
        var newLine = Environment.NewLine;
        foreach (var node in document.Nodes) WriteNode(builder, node, newLine);
        return builder.ToString();
    }

    private static void WriteNode(StringBuilder builder, SvgMarkupNode node, string newLine) {
        switch (node) {
            case SvgMarkupElement element:
                WriteElement(builder, element, newLine);
                break;
            case SvgMarkupText { IsCData: true } cdata:
                builder.Append("<![CDATA[").Append(cdata.Raw).Append("]]>");
                break;
            case SvgMarkupText text:
                WriteText(builder, text.Value, newLine);
                break;
            case SvgMarkupOther other:
                builder.Append(other.Markup);
                break;
        }
    }

    private static void WriteElement(StringBuilder builder, SvgMarkupElement element, string newLine) {
        builder.Append('<').Append(element.Name);
        for (var i = 0; i < element.AttributeCount; i++) {
            builder.Append(' ').Append(element.AttributeName(i)).Append("=\"");
            WriteAttributeValue(builder, element.AttributeValue(i));
            builder.Append('"');
        }

        if (element.IsEmpty) {
            builder.Append("/>");
            return;
        }

        builder.Append('>');
        var nodes = element.Nodes;
        for (var i = 0; i < nodes.Count; i++) WriteNode(builder, nodes[i], newLine);
        builder.Append("</").Append(element.Name).Append('>');
    }

    private static void WriteText(StringBuilder builder, string value, string newLine) {
        var run = 0;
        for (var i = 0; i < value.Length; i++) {
            var c = value[i];
            // Characters written as they are are copied in runs.
            if (c >= 0x20 && c != '<' && c != '>' && c != '&' && (c < 0xD800 || c > 0xDFFF && c < 0xFFFE)) continue;
            builder.Append(value, run, i - run);
            switch (c) {
                case '<': builder.Append("&lt;"); break;
                case '>': builder.Append("&gt;"); break;
                case '&': builder.Append("&amp;"); break;
                case '\n': builder.Append(newLine); break;
                case '\r':
                    if (i + 1 < value.Length && value[i + 1] == '\n') i++;
                    builder.Append(newLine);
                    break;
                default:
                    AppendAllowed(builder, value, ref i);
                    break;
            }

            run = i + 1;
        }

        builder.Append(value, run, value.Length - run);
    }

    // A tab or a surrogate pair; any other character the framework writer refuses is refused the same way.
    private static void AppendAllowed(StringBuilder builder, string value, ref int index) {
        var c = value[index];
        if (c == '\t') {
            builder.Append(c);
            return;
        }

        if (char.IsHighSurrogate(c) && index + 1 < value.Length && char.IsLowSurrogate(value[index + 1])) {
            builder.Append(c).Append(value[index + 1]);
            index++;
            return;
        }

        throw new ArgumentException("The markup contains a character XML does not allow (U+" + ((int)c).ToString("X4", System.Globalization.CultureInfo.InvariantCulture) + ").");
    }

    private static void WriteAttributeValue(StringBuilder builder, string value) {
        var run = 0;
        for (var i = 0; i < value.Length; i++) {
            var c = value[i];
            if (c >= 0x20 && c != '<' && c != '>' && c != '&' && c != '"' && (c < 0xD800 || c > 0xDFFF && c < 0xFFFE)) continue;
            builder.Append(value, run, i - run);
            switch (c) {
                case '<': builder.Append("&lt;"); break;
                case '>': builder.Append("&gt;"); break;
                case '&': builder.Append("&amp;"); break;
                case '"': builder.Append("&quot;"); break;
                case '\t': builder.Append("&#x9;"); break;
                case '\n': builder.Append("&#xA;"); break;
                case '\r': builder.Append("&#xD;"); break;
                default: AppendAllowed(builder, value, ref i); break;
            }

            run = i + 1;
        }

        builder.Append(value, run, value.Length - run);
    }
}

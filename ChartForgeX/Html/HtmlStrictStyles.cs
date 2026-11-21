using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace ChartForgeX.Html;

/// <summary>Moves the inline styles of ChartForgeX HTML into one stylesheet, for pages whose content security policy
/// allows styles only from nonce'd <c>style</c> elements.</summary>
/// <remarks>
/// Every <c>style</c> attribute becomes a generated class (<c>cfx-s-</c> followed by a hash of its declarations), and its
/// declarations become a rule in the stylesheet. The rule repeats the class three times so it outranks the renderers'
/// own stylesheet rules, as the attribute did. Every <c>style</c> element joins the same stylesheet; a <c>media</c>
/// attribute becomes an <c>@media</c> block. Script and raw-text contents are copied unchanged.
/// The interactive runtimes move the generated classes back to inline styles through the CSS object model when they
/// mount, which a strict policy allows, so scripts that read or replace inline styles behave as before.
/// </remarks>
public static class HtmlStrictStyles {
    /// <summary>The prefix of the generated class names.</summary>
    public const string ClassPrefix = "cfx-s-";

    /// <summary>Splits ChartForgeX HTML into markup without inline styles and the stylesheet that replaces them.</summary>
    /// <param name="html">A fragment or document produced by a ChartForgeX renderer.</param>
    /// <returns>The markup and its stylesheet. Equal declarations share one class, also across fragments of one page.</returns>
    public static HtmlStrictStyleFragment Extract(string html) => Extract(html, null);

    // With a nonce, style elements inside an SVG stay in place and carry it, so an SVG exported from the page keeps its
    // fonts and scoped rules. Hosts without a nonce of ours receive every rule in the stylesheet instead.
    private static HtmlStrictStyleFragment Extract(string html, string? svgStyleNonce) {
        if (html == null) throw new ArgumentNullException(nameof(html));
        var svgDepth = 0;
        var markup = new StringBuilder(html.Length);
        var sheet = new StringBuilder();
        var rules = new StringBuilder();
        var classes = new Dictionary<string, string>(StringComparer.Ordinal);
        var position = 0;
        while (position < html.Length) {
            var open = html.IndexOf('<', position);
            if (open < 0) { markup.Append(html, position, html.Length - position); break; }
            markup.Append(html, position, open - position);
            if (Starts(html, open, "<!--")) { position = CopyThrough(html, open, "-->", markup); continue; }
            if (Starts(html, open, "<![CDATA[")) { position = CopyThrough(html, open, "]]>", markup); continue; }
            if (open + 1 >= html.Length || !IsNameStart(html[open + 1])) {
                if (svgDepth > 0 && string.Compare(html, open, "</svg", 0, 5, StringComparison.OrdinalIgnoreCase) == 0) svgDepth--;
                markup.Append('<'); position = open + 1; continue;
            }
            var tag = Tag.Read(html, open);
            if (tag == null) { markup.Append(html, open, html.Length - open); break; }
            if (tag.Name == "svg" && !tag.SelfClosing) svgDepth++;
            if (tag.Name == "style" && svgDepth > 0 && svgStyleNonce != null) {
                var close = IndexOfClose(html, tag.End, "style");
                var end = close < 0 ? html.Length : html.IndexOf('>', close) + 1;
                markup.Append("<style nonce=\"").Append(WebUtility.HtmlEncode(svgStyleNonce)).Append('"').Append(html, open + 6, end - open - 6);
                position = end;
                continue;
            }
            if (tag.Name == "style") {
                var close = IndexOfClose(html, tag.End, "style");
                var css = StripCdata(html.Substring(tag.End, (close < 0 ? html.Length : close) - tag.End));
                var media = tag.Value(html, "media");
                if (css.Trim().Length > 0) {
                    if (sheet.Length > 0) sheet.Append('\n');
                    sheet.Append(media == null ? css : "@media " + media + "{" + css + "}");
                }
                position = close < 0 ? html.Length : html.IndexOf('>', close) + 1;
                continue;
            }
            tag.Write(html, markup, classes, rules);
            position = tag.End;
            if (tag.Name is "script" or "textarea" or "title" && !tag.SelfClosing) {
                var close = IndexOfClose(html, position, tag.Name);
                var end = close < 0 ? html.Length : close;
                markup.Append(html, position, end - position);
                position = end;
            }
        }
        if (rules.Length > 0) {
            if (sheet.Length > 0) sheet.Append('\n');
            sheet.Append(rules);
        }
        return new HtmlStrictStyleFragment(markup.ToString(), sheet.ToString());
    }

    internal static string Apply(string html, string? nonce) => nonce == null ? html : Extract(html, nonce).ToHtml(nonce);

    private static bool Starts(string text, int index, string value) => string.CompareOrdinal(text, index, value, 0, value.Length) == 0;
    private static bool IsNameStart(char c) => c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z';
    private static int CopyThrough(string html, int start, string terminator, StringBuilder markup) {
        var end = html.IndexOf(terminator, start, StringComparison.Ordinal);
        end = end < 0 ? html.Length : end + terminator.Length;
        markup.Append(html, start, end - start);
        return end;
    }
    private static int IndexOfClose(string html, int start, string name) {
        for (var index = html.IndexOf("</", start, StringComparison.Ordinal); index >= 0; index = html.IndexOf("</", index + 2, StringComparison.Ordinal))
            if (string.Compare(html, index + 2, name, 0, name.Length, StringComparison.OrdinalIgnoreCase) == 0) return index;
        return -1;
    }
    private static string StripCdata(string css) {
        var trimmed = css.Trim();
        return trimmed.StartsWith("<![CDATA[", StringComparison.Ordinal) && trimmed.EndsWith("]]>", StringComparison.Ordinal)
            ? trimmed.Substring(9, trimmed.Length - 12) : css;
    }

    private static string ClassFor(string declarations, Dictionary<string, string> classes, StringBuilder rules) {
        if (classes.TryGetValue(declarations, out var name)) return name;
        using var hash = SHA256.Create();
        var bytes = hash.ComputeHash(Encoding.UTF8.GetBytes(declarations));
        var builder = new StringBuilder(ClassPrefix);
        for (var i = 0; i < 6; i++) builder.Append(bytes[i].ToString("x2", CultureInfo.InvariantCulture));
        name = builder.ToString();
        classes.Add(declarations, name);
        rules.Append('.').Append(name).Append('.').Append(name).Append('.').Append(name).Append('{').Append(declarations).Append('}');
        return name;
    }

    // Declarations leave an attribute for a rule in a style element. Escape "<", which could close the element, braces,
    // which could close the rule and start another one, and "/*", which would comment out the rules after it; escaped,
    // they stay characters inside the declaration's value.
    private static string Declarations(string encoded) => WebUtility.HtmlDecode(encoded).Trim()
        .Replace("<", "\\3c ").Replace("{", "\\7b ").Replace("}", "\\7d ").Replace("/*", "/\\*");

    private sealed class Tag {
        private readonly List<(string Name, int Start, int End, int ValueStart, int ValueEnd)> _attributes = new();
        private Tag(string name, int open) { Name = name; Open = open; }
        internal string Name { get; }
        internal int Open { get; }
        internal int End { get; private set; }
        internal bool SelfClosing { get; private set; }

        internal static Tag? Read(string html, int open) {
            var index = open + 1;
            while (index < html.Length && !char.IsWhiteSpace(html[index]) && html[index] != '>' && html[index] != '/') index++;
            var tag = new Tag(html.Substring(open + 1, index - open - 1).ToLowerInvariant(), open);
            while (index < html.Length) {
                while (index < html.Length && char.IsWhiteSpace(html[index])) index++;
                if (index >= html.Length) return null;
                if (html[index] == '>') { tag.End = index + 1; return tag; }
                if (html[index] == '/') {
                    if (index + 1 < html.Length && html[index + 1] == '>') { tag.SelfClosing = true; tag.End = index + 2; return tag; }
                    index++; continue;
                }
                var start = index;
                while (index < html.Length && !char.IsWhiteSpace(html[index]) && html[index] != '=' && html[index] != '>' && html[index] != '/') index++;
                var name = html.Substring(start, index - start);
                var cursor = index;
                while (cursor < html.Length && char.IsWhiteSpace(html[cursor])) cursor++;
                int valueStart = -1, valueEnd = -1;
                if (cursor < html.Length && html[cursor] == '=') {
                    cursor++;
                    while (cursor < html.Length && char.IsWhiteSpace(html[cursor])) cursor++;
                    if (cursor >= html.Length) return null;
                    if (html[cursor] == '"' || html[cursor] == '\'') {
                        var close = html.IndexOf(html[cursor], cursor + 1);
                        if (close < 0) return null;
                        valueStart = cursor + 1; valueEnd = close; index = close + 1;
                    } else {
                        valueStart = cursor;
                        while (cursor < html.Length && !char.IsWhiteSpace(html[cursor]) && html[cursor] != '>') cursor++;
                        valueEnd = cursor; index = cursor;
                    }
                }
                tag._attributes.Add((name, start, index, valueStart, valueEnd));
            }
            return null;
        }

        internal string? Value(string html, string name) {
            foreach (var attribute in _attributes)
                if (string.Equals(attribute.Name, name, StringComparison.OrdinalIgnoreCase))
                    return attribute.ValueStart < 0 ? string.Empty : WebUtility.HtmlDecode(html.Substring(attribute.ValueStart, attribute.ValueEnd - attribute.ValueStart));
            return null;
        }

        internal void Write(string html, StringBuilder markup, Dictionary<string, string> classes, StringBuilder rules) {
            var style = -1; var classIndex = -1;
            for (var i = 0; i < _attributes.Count; i++) {
                if (string.Equals(_attributes[i].Name, "style", StringComparison.OrdinalIgnoreCase)) style = i;
                else if (string.Equals(_attributes[i].Name, "class", StringComparison.OrdinalIgnoreCase)) classIndex = i;
            }
            if (style < 0) { markup.Append(html, Open, End - Open); return; }
            var attribute = _attributes[style];
            var declarations = attribute.ValueStart < 0 ? string.Empty : Declarations(html.Substring(attribute.ValueStart, attribute.ValueEnd - attribute.ValueStart));
            var generated = declarations.Length == 0 ? null : ClassFor(declarations, classes, rules);
            var cursor = Open;
            for (var i = 0; i < _attributes.Count; i++) {
                var current = _attributes[i];
                if (i == style) {
                    // Drop the attribute together with the whitespace before it.
                    var before = current.Start;
                    while (before > cursor && char.IsWhiteSpace(html[before - 1])) before--;
                    markup.Append(html, cursor, before - cursor);
                    if (generated != null && classIndex < 0) markup.Append(" class=\"").Append(generated).Append('"');
                    cursor = current.End;
                } else if (i == classIndex && generated != null) {
                    if (current.ValueStart < 0) {
                        markup.Append(html, cursor, current.End - cursor).Append("=\"").Append(generated).Append('"');
                    } else {
                        markup.Append(html, cursor, current.ValueEnd - cursor);
                        if (current.ValueEnd > current.ValueStart) markup.Append(' ');
                        markup.Append(generated);
                        markup.Append(html, current.ValueEnd, current.End - current.ValueEnd);
                    }
                    cursor = current.End;
                }
            }
            markup.Append(html, cursor, End - cursor);
        }
    }
}

/// <summary>ChartForgeX markup without inline styles, and the stylesheet that replaces them.</summary>
public sealed class HtmlStrictStyleFragment {
    internal HtmlStrictStyleFragment(string markup, string styleSheet) { Markup = markup; StyleSheet = styleSheet; }

    /// <summary>The markup, with generated classes in place of <c>style</c> attributes and no <c>style</c> elements.</summary>
    public string Markup { get; }

    /// <summary>The rules that replace the inline styles and style elements. Hosts that register the stylesheet themselves
    /// add it once per page in a nonce'd <c>style</c> element, or serve it as a file.</summary>
    public string StyleSheet { get; }

    /// <summary>Returns the markup preceded by one <c>style</c> element carrying the nonce. In a complete document the
    /// element goes at the end of <c>head</c>.</summary>
    /// <param name="nonce">The page's style nonce, or null for a host that adds its own nonce'd element.</param>
    public string ToHtml(string? nonce) {
        if (StyleSheet.Length == 0) return Markup;
        var element = new HtmlMarkupWriter().StartElement("style").Attribute("nonce", nonce).Attribute("data-cfx-strict-styles", "true")
            .RawTrusted(StyleSheet.Replace("</", "<\\/")).EndElement().Build();
        var head = Markup.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
        return head < 0 ? element + Markup : Markup.Substring(0, head) + element + Markup.Substring(head);
    }
}

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using ChartForgeX.Html;

namespace ChartForgeX.Interactivity.Html;

/// <summary>
/// Moves every inline script and style of ChartForgeX HTML into asset files, for pages served under a policy that
/// loads scripts and styles only from files, such as <c>script-src 'self'; style-src 'self'</c>.
/// </summary>
/// <remarks>
/// Style attributes become generated <c>cfx-s-</c> classes and, with every <c>style</c> element, move into one
/// stylesheet linked at the end of <c>head</c> (see <see cref="HtmlStrictStyles"/>). Each executable inline script
/// becomes a script file loaded from the same place in the document, so scripts still run in order and find their
/// position through <c>document.currentScript</c>. Data blocks such as <c>&lt;script type="application/json"&gt;</c>
/// stay inline: browsers never execute them, so the policy does not apply to them. File names carry a content hash,
/// so the runtimes shared by several pages are written once. The graph explorer's physics worker becomes its own file,
/// so the policy needs no <c>blob:</c> source for it.
/// </remarks>
public static class HtmlExternalAssets {
    /// <summary>The attribute that marks the stylesheet link added for the page's own styles.</summary>
    public const string StyleSheetAttribute = "data-cfx-external-styles";

    /// <summary>Splits a ChartForgeX page or fragment into markup without inline scripts or styles and the files it loads.</summary>
    /// <param name="html">A page or fragment produced by a ChartForgeX renderer, with or without its own asset references.</param>
    /// <param name="assets">Where the page loads the files from. Write <see cref="HtmlExternalPage.Files"/> there.</param>
    /// <returns>The markup and the asset files it references.</returns>
    public static HtmlExternalPage Split(string html, HtmlAssetReferences assets) {
        if (html == null) throw new ArgumentNullException(nameof(html));
        if (assets == null) throw new ArgumentNullException(nameof(assets));
        var strict = HtmlStrictStyles.Extract(html);
        var files = new List<HtmlAssetFile>();
        var markup = MoveScripts(strict.Markup, assets, files);
        if (strict.StyleSheet.Length == 0) return new HtmlExternalPage(markup, files);
        var sheet = new HtmlAssetFile("cfx-page", "css", strict.StyleSheet);
        files.Insert(0, sheet);
        var link = new HtmlMarkupWriter();
        link.StartElement("link").Attribute("rel", "stylesheet").Attribute("href", assets.Href(sheet)).Attribute(StyleSheetAttribute, "true");
        if (assets.IncludeIntegrity) link.Attribute("integrity", sheet.Integrity).Attribute("crossorigin", "anonymous");
        var element = link.EndVoidElement().Build();
        var head = markup.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
        markup = head < 0 ? element + markup : markup.Substring(0, head) + element + markup.Substring(head);
        return new HtmlExternalPage(markup, files);
    }

    private static string MoveScripts(string html, HtmlAssetReferences assets, List<HtmlAssetFile> files) {
        var markup = new StringBuilder(html.Length);
        var position = 0;
        while (position < html.Length) {
            var open = html.IndexOf('<', position);
            if (open < 0) { markup.Append(html, position, html.Length - position); break; }
            markup.Append(html, position, open - position);
            if (Starts(html, open, "<!--")) { position = CopyThrough(html, open, "-->", markup); continue; }
            if (Starts(html, open, "<![CDATA[")) { position = CopyThrough(html, open, "]]>", markup); continue; }
            if (open + 1 >= html.Length || !(char.IsLetter(html[open + 1]) || html[open + 1] == '/' || html[open + 1] == '!')) {
                markup.Append('<'); position = open + 1; continue;
            }
            var end = TagEnd(html, open);
            if (end < 0) { markup.Append(html, open, html.Length - open); break; }
            var name = TagName(html, open);
            if (!string.Equals(name, "script", StringComparison.OrdinalIgnoreCase)) {
                markup.Append(html, open, end - open);
                position = end;
                // Raw text never holds markup of its own.
                if (name is "textarea" or "title" && html[end - 2] != '/') position = CopyRawText(html, end, name, markup);
                continue;
            }

            var close = IndexOfClose(html, end, "script");
            var contentEnd = close < 0 ? html.Length : close;
            var after = close < 0 ? html.Length : html.IndexOf('>', close) + 1;
            var tag = html.Substring(open, end - open);
            var content = html.Substring(end, contentEnd - end);
            if (!IsExecutable(tag) || Attribute(tag, "src") != null) {
                markup.Append(html, open, after - open);
            } else if (content.Trim().Length > 0) {
                var file = ScriptFile(content);
                AddOnce(files, file);
                markup.Append(tag, 0, tag.Length - 1).Append(" src=\"").Append(WebUtility.HtmlEncode(assets.Href(file))).Append('"');
                if (assets.IncludeIntegrity) markup.Append(" integrity=\"").Append(file.Integrity).Append("\" crossorigin=\"anonymous\"");
                if (ReferenceEquals(file, HtmlInteractiveAssetFiles.GraphExplorerScript)) {
                    AddOnce(files, HtmlInteractiveAssetFiles.GraphExplorerPhysicsWorker);
                    markup.Append(' ').Append(HtmlGraphExplorerRenderer.PhysicsWorkerAttribute).Append("=\"")
                        .Append(WebUtility.HtmlEncode(assets.Href(HtmlInteractiveAssetFiles.GraphExplorerPhysicsWorker))).Append('"');
                }
                markup.Append("></script>");
            }
            position = after;
        }
        return markup.ToString();
    }

    // The shared runtimes keep their usual file names, so a page split here and a page rendered with asset references
    // load the same files.
    private static HtmlAssetFile ScriptFile(string content) {
        if (content == HtmlInteractiveAssetFiles.GraphExplorerScript.Content) return HtmlInteractiveAssetFiles.GraphExplorerScript;
        if (content == HtmlInteractiveAssetFiles.ChartScript.Content) return HtmlInteractiveAssetFiles.ChartScript;
        return new HtmlAssetFile("cfx-script", "js", content);
    }

    private static void AddOnce(List<HtmlAssetFile> files, HtmlAssetFile file) {
        foreach (var existing in files) if (existing.FileName == file.FileName) return;
        files.Add(file);
    }

    // Only classic and module scripts run; data blocks with any other type stay inline.
    private static bool IsExecutable(string tag) {
        var type = Attribute(tag, "type");
        if (type == null) return true;
        type = type.Trim().ToLowerInvariant();
        var parameters = type.IndexOf(';');
        if (parameters >= 0) type = type.Substring(0, parameters).Trim();
        return type.Length == 0 || type == "module" || type.EndsWith("javascript", StringComparison.Ordinal)
            || type.EndsWith("ecmascript", StringComparison.Ordinal) || type.StartsWith("text/jscript", StringComparison.Ordinal)
            || type == "text/livescript";
    }

    private static string? Attribute(string tag, string name) {
        var index = TagName(tag, 0).Length + 1;
        while (index < tag.Length) {
            while (index < tag.Length && (char.IsWhiteSpace(tag[index]) || tag[index] == '/')) index++;
            if (index >= tag.Length || tag[index] == '>') return null;
            var start = index;
            while (index < tag.Length && !char.IsWhiteSpace(tag[index]) && tag[index] != '=' && tag[index] != '>' && tag[index] != '/') index++;
            var current = tag.Substring(start, index - start);
            while (index < tag.Length && char.IsWhiteSpace(tag[index])) index++;
            string value = string.Empty;
            if (index < tag.Length && tag[index] == '=') {
                index++;
                while (index < tag.Length && char.IsWhiteSpace(tag[index])) index++;
                if (index < tag.Length && (tag[index] == '"' || tag[index] == '\'')) {
                    var quote = tag[index];
                    var closeQuote = tag.IndexOf(quote, index + 1);
                    if (closeQuote < 0) closeQuote = tag.Length;
                    value = tag.Substring(index + 1, closeQuote - index - 1);
                    index = closeQuote + 1;
                } else {
                    var valueStart = index;
                    while (index < tag.Length && !char.IsWhiteSpace(tag[index]) && tag[index] != '>') index++;
                    value = tag.Substring(valueStart, index - valueStart);
                }
            }
            if (string.Equals(current, name, StringComparison.OrdinalIgnoreCase)) return WebUtility.HtmlDecode(value);
        }
        return null;
    }

    private static string TagName(string html, int open) {
        var index = open + 1;
        while (index < html.Length && (char.IsLetterOrDigit(html[index]) || html[index] == '-' || html[index] == ':')) index++;
        return html.Substring(open + 1, index - open - 1).ToLowerInvariant();
    }

    // Returns the index after the tag's closing ">", honouring quoted attribute values, or -1 when it never closes.
    private static int TagEnd(string html, int open) {
        char quote = '\0';
        for (var index = open + 1; index < html.Length; index++) {
            var character = html[index];
            if (quote != '\0') { if (character == quote) quote = '\0'; continue; }
            if (character == '"' || character == '\'') quote = character;
            else if (character == '>') return index + 1;
        }
        return -1;
    }

    private static bool Starts(string text, int index, string value) => string.CompareOrdinal(text, index, value, 0, value.Length) == 0;

    private static int CopyThrough(string html, int start, string terminator, StringBuilder markup) {
        var end = html.IndexOf(terminator, start, StringComparison.Ordinal);
        end = end < 0 ? html.Length : end + terminator.Length;
        markup.Append(html, start, end - start);
        return end;
    }

    private static int CopyRawText(string html, int start, string name, StringBuilder markup) {
        var close = IndexOfClose(html, start, name);
        var end = close < 0 ? html.Length : close;
        markup.Append(html, start, end - start);
        return end;
    }

    private static int IndexOfClose(string html, int start, string name) {
        for (var index = html.IndexOf("</", start, StringComparison.Ordinal); index >= 0; index = html.IndexOf("</", index + 2, StringComparison.Ordinal))
            if (string.Compare(html, index + 2, name, 0, name.Length, StringComparison.OrdinalIgnoreCase) == 0) return index;
        return -1;
    }
}

/// <summary>ChartForgeX markup that loads every script and style from files, and those files.</summary>
public sealed class HtmlExternalPage {
    internal HtmlExternalPage(string html, IReadOnlyList<HtmlAssetFile> files) { Html = html; Files = files; }

    /// <summary>The markup, with script and stylesheet references in place of inline scripts and styles.</summary>
    public string Html { get; }

    /// <summary>The files the markup references, to write with <see cref="HtmlInteractiveAssetFiles.WriteTo"/> into the
    /// folder the asset references point at.</summary>
    public IReadOnlyList<HtmlAssetFile> Files { get; }
}

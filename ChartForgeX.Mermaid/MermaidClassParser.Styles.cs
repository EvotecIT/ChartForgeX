using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidClassParser {
    private static bool TryStyle(MermaidClassDocument document, Dictionary<string, MermaidClassNode> classes,
        string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        if (MermaidStyleParser.TryDefinition(text, span, result, document.ClassDefinitions, "Class")) {
            document.StyleStatements.Add(new MermaidClassStyleStatement(text, span));
            return true;
        }
        var direct = MermaidParserUtilities.StartsStatement(text, "style");
        var assignment = MermaidParserUtilities.StartsStatement(text, "cssClass");
        if (!direct && !assignment) return false;
        document.StyleStatements.Add(new MermaidClassStyleStatement(text, span));
        var body = text.Substring(direct ? 5 : 8).Trim();
        if (!ReadStyleSelectors(body, span, result, direct, out var ids, out var value)) return true;
        var declarations = new Dictionary<string, string>();
        if (direct) MermaidStyleParser.Read(declarations, value, span, result, "Class");
        var named = direct ? new List<string>() : MermaidStyleParser.Names(value);
        var missing = false;
        foreach (var id in direct ? ids : ExpandStyleSelectors(ids, classes, span)) {
            if (!classes.TryGetValue(CleanEndpoint(id), out var node)) {
                missing = true;
                continue;
            }
            RetainGenericReference(document, node, node.Id, id, span, result);
            if (direct) MermaidStyleParser.Copy(node.Styles, declarations, span);
            else { MermaidResourceGuard.ReserveElements(named.Count, span); node.Classes.AddRange(named); }
        }
        if (missing) MermaidParserUtilities.RetainUnsupported(document, text, span, result, "class style target not yet declared");
        return true;
    }

    // Use the class identity reader so commas and spaces inside a finite generic
    // argument list never split a selector or become the start of style declarations.
    private static bool ReadStyleSelectors(string body, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result,
        bool direct, out List<string> ids, out string value) {
        value = string.Empty;
        if (!ReadSelectorIdentities(body, span, result, direct, out ids, out var position)) return false;
        value = body.Substring(position).Trim().TrimEnd(';').TrimEnd();
        if (value.Length > 0) return true;
        InvalidClass(result, span, "Class style statements require a selector and a value.");
        return false;
    }

    private static bool ReadSelectorIdentities(string body, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result,
        bool direct, out List<string> ids, out int position) {
        ids = new List<string>(); position = 0;
        while (true) {
            if (!ReadIdentity(body, ref position, span, result, out var id)) return false;
            if (position < body.Length && !char.IsWhiteSpace(body[position]) && body[position] != ',') {
                InvalidClass(result, span, "Class style selectors require a complete identity before their value.");
                return false;
            }
            ids.Add(id); SkipSpace(body, ref position);
            if (direct || position >= body.Length || body[position] != ',') return true;
            position++;
        }
    }

    private static IEnumerable<string> ExpandStyleSelectors(List<string> ids, Dictionary<string, MermaidClassNode> classes, MermaidSourceSpan span) {
        foreach (var id in ids) {
            // A complete quoted identity wins, including literal commas and tildes.
            // Older Mermaid sources also quote the whole comma-separated cssClass list.
            if (classes.ContainsKey(CleanEndpoint(id)) || id.Length < 2 || id[0] != '"' && id[0] != '`' || id.IndexOf(',') < 0) { yield return id; continue; }
            var inner = id.Substring(1, id.Length - 2); var parsed = new MermaidParseResult<MermaidDocument>();
            if (ReadSelectorIdentities(inner, span, parsed, false, out var expanded, out var position)
                && position == inner.Length) {
                foreach (var selector in expanded) yield return selector;
            } else yield return id;
        }
    }

    private static string ReadInlineClass(string body, out string? style) {
        style = null;
        var suffix = MermaidStyleParser.Suffix(body);
        if (suffix < 0) return body;
        var end = suffix + 3;
        while (end < body.Length && !char.IsWhiteSpace(body[end]) && body[end] != '{') end++;
        style = body.Substring(suffix + 3, end - suffix - 3).TrimEnd(';');
        return body.Substring(0, suffix) + body.Substring(end);
    }

    private static void ApplyStyles(MermaidClassDocument document) {
        var definitions = MermaidStyleParser.Definitions(document.ClassDefinitions);
        foreach (var node in document.Classes) {
            var resolved = MermaidStyleParser.Resolve(definitions, node.Classes, node.Styles, node.Span);
            node.Styles.Clear();
            MermaidStyleParser.Copy(node.Styles, resolved);
        }
    }
}

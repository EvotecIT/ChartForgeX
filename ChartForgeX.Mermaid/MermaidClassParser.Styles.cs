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
        if (!MermaidStyleParser.SplitHead(body, out var selector, out var value)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error,
                "Class style statements require a selector and a value.", MermaidDiagnosticCodes.InvalidStatement);
            return true;
        }
        var declarations = new Dictionary<string, string>();
        if (direct) MermaidStyleParser.Read(declarations, value, span, result, "Class");
        var ids = direct ? new List<string> { MermaidParserUtilities.Unquote(selector) }
            : MermaidStyleParser.Names(MermaidParserUtilities.Unquote(selector));
        var named = direct ? new List<string>() : MermaidStyleParser.Names(value);
        var missing = false;
        foreach (var id in ids) {
            if (!classes.TryGetValue(CleanEndpoint(id), out var node)) {
                missing = true;
                continue;
            }
            if (direct) MermaidStyleParser.Copy(node.Styles, declarations, span);
            else { MermaidResourceGuard.ReserveElements(named.Count, span); node.Classes.AddRange(named); }
        }
        if (missing) MermaidParserUtilities.RetainUnsupported(document, text, span, result, "class style target not yet declared");
        return true;
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

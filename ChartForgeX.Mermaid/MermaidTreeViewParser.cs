using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

internal static class MermaidTreeViewParser {
    public static void ParseStatements(MermaidTreeViewDocument document, string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        var stack = new List<MermaidTreeViewNode>();
        for (var line = Math.Max(1, startLine); line <= lines.Length; line++) {
            var raw = lines[line - 1];
            var trimmed = MermaidParserUtilities.StripInlineComment(raw.Trim());
            if (MermaidParserUtilities.IsSkippable(trimmed)) continue;

            var indent = MermaidParserUtilities.LeadingWhitespace(raw);
            var span = new MermaidSourceSpan(line, indent + 1, trimmed.Length);
            document.Statements.Add(new MermaidRawStatement(trimmed, span));

            var label = ParseLabel(trimmed, span, result);
            if (label.Length == 0) continue;

            while (stack.Count > 0 && stack[stack.Count - 1].Indent >= indent) stack.RemoveAt(stack.Count - 1);
            var parent = stack.Count == 0 ? null : stack[stack.Count - 1];
            var node = new MermaidTreeViewNode(MermaidParserUtilities.StableId("treeview-node", document.Nodes.Count), label, indent, stack.Count, parent, span);
            if (parent == null) document.Roots.Add(node);
            else parent.Children.Add(node);
            document.Nodes.Add(node);
            stack.Add(node);
        }

        if (document.Nodes.Count == 0) MermaidParserUtilities.Add(result, document.HeaderSpan, MermaidDiagnosticSeverity.Error, "Mermaid tree view diagrams require at least one node.");
    }

    private static string ParseLabel(string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        if (text.Length == 0) return string.Empty;
        if (text[0] != '"') return MermaidParserUtilities.Unquote(text);

        var close = FindClosingQuote(text, 1);
        if (close < 0) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid tree view node labels must close quoted strings.");
            return string.Empty;
        }

        var remainder = text.Substring(close + 1).Trim();
        if (remainder.Length > 0) MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning, "Mermaid tree view node suffix was retained but is not rendered exactly: " + remainder);
        return text.Substring(1, close - 1).Replace("\\\"", "\"").Replace("\\\\", "\\");
    }

    private static int FindClosingQuote(string text, int start) {
        var escaped = false;
        for (var i = start; i < text.Length; i++) {
            var ch = text[i];
            if (escaped) {
                escaped = false;
                continue;
            }

            if (ch == '\\') {
                escaped = true;
                continue;
            }

            if (ch == '"') return i;
        }

        return -1;
    }
}

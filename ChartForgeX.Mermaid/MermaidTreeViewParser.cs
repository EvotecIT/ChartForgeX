using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidTreeViewParser {
    public static void ParseStatements(MermaidTreeViewDocument document, string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        var stack = new List<MermaidTreeViewNode>();
        for (var line = Math.Max(1, startLine); line <= lines.Length; line++) {
            MermaidResourceGuard.Checkpoint();
            var raw = lines[line - 1];
            var statement = StripTreeViewComment(raw).TrimEnd();
            if (MermaidParserUtilities.IsSkippable(statement.Trim())) continue;
            var leading = MermaidParserUtilities.LeadingWhitespace(statement);
            var span = new MermaidSourceSpan(line, leading + 1, statement.Length - leading);
            document.Statements.Add(new MermaidRawStatement(statement.Substring(leading), span));
            if (!TryPrefix(statement, line, result, out var start, out var indent)) continue;
            var content = statement.Substring(start);
            var contentSpan = new MermaidSourceSpan(line, start + 1, content.Length);
            if (!TryAnnotations(content, contentSpan, result, out var parsed)) continue;
            while (stack.Count > 0 && stack[stack.Count - 1].Indent >= indent) stack.RemoveAt(stack.Count - 1);
            var parent = stack.Count == 0 ? null : stack[stack.Count - 1];
            // A box-drawing child at its root's first column still has an explicit parent level.
            if (parsed.Label.Length == 0) continue;
            if (stack.Count >= 128) {
                Error(result, contentSpan, "Tree view source supports at most 128 levels."); continue;
            }
            if (document.Nodes.Count >= 10000) { Error(result, contentSpan, "Tree view source supports at most 10000 rows."); break; }
            MermaidResourceGuard.CheckDepth(stack.Count + 1, contentSpan);
            var node = new MermaidTreeViewNode(MermaidParserUtilities.StableId("treeview-node", document.Nodes.Count), parsed.Label,
                indent, stack.Count, parent, contentSpan, parsed.Description, parsed.Classes, parsed.Icon);
            if (parent == null) document.Roots.Add(node); else parent.Children.Add(node);
            document.Nodes.Add(node); stack.Add(node);
        }
        if (document.Nodes.Count == 0 && !result.HasErrors)
            Error(result, document.HeaderSpan, "Mermaid tree view diagrams require at least one node.");
    }

    // Source offsets count original UTF-16 characters; logical indentation expands tabs to four columns.
    private static bool TryPrefix(string text, int line, MermaidParseResult<MermaidDocument> result, out int start, out int indent) {
        start = 0; indent = 0;
        while (start < text.Length && (text[start] == ' ' || text[start] == '\t')) {
            indent += text[start] == '\t' ? 4 - indent % 4 : 1; start++;
        }
        var scan = start; var column = indent; var pipe = false;
        while (scan < text.Length) {
            var ch = text[scan];
            if (ch == ' ' || ch == '\t' || ch == '│' || ch == '┃') {
                pipe |= ch == '│' || ch == '┃'; column += ch == '\t' ? 4 - column % 4 : 1; scan++; continue;
            }
            if (ch == '├' || ch == '└' || ch == '┣' || ch == '┗') {
                var branch = scan++; var dashes = 0;
                while (scan < text.Length && (text[scan] == '─' || text[scan] == '━')) { dashes++; scan++; }
                if (dashes < 2 || scan >= text.Length || !char.IsWhiteSpace(text[scan])) {
                    Error(result, new MermaidSourceSpan(line, branch + 1, Math.Max(1, scan - branch)), "Tree view branches require a standard or heavy two-stroke prefix followed by whitespace and a label.");
                    return false;
                }
                while (scan < text.Length && char.IsWhiteSpace(text[scan])) scan++;
                if (scan == text.Length) { Error(result, new MermaidSourceSpan(line, branch + 1, text.Length - branch), "Tree view branches require a label."); return false; }
                start = scan; indent = column + 1; return true;
            }
            break;
        }
        if (pipe) { Error(result, new MermaidSourceSpan(line, start + 1, Math.Max(1, scan - start)), "Tree view connector prefixes require a branch marker."); return false; }
        return true;
    }
    // Bare filenames may contain apostrophes. Only the supported double-quoted form protects percent comments.
    private static string StripTreeViewComment(string text) {
        var quoted = false; var escaped = false;
        for (var i = 0; i < text.Length; i++) {
            if (quoted && escaped) { escaped = false; continue; }
            if (quoted && text[i] == '\\') { escaped = true; continue; }
            if (text[i] == '"') { quoted = !quoted; continue; }
            if (!quoted && text[i] == '%' && i + 1 < text.Length && text[i + 1] == '%') return text.Substring(0, i);
        }
        return text;
    }
    private static void Error(MermaidParseResult<MermaidDocument> result, MermaidSourceSpan span, string message) =>
        MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, message, MermaidDiagnosticCodes.InvalidStatement);
}

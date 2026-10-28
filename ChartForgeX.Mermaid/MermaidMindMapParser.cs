using System;
using System.Collections.Generic;
using System.Text;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidMindMapParser {
    public static void ParseStatements(MermaidMindMapDocument document, string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        var stack = new List<MermaidMindMapNode>();
        var indents = new List<int>();
        var nativeIconNodes = new HashSet<MermaidMindMapNode>();
        var starts = MermaidParserUtilities.OriginalLineStarts(document.SourceText);
        for (var line = Math.Max(1, startLine); line <= lines.Length; line++) {
            MermaidResourceGuard.Checkpoint();
            var first = line;
            var indent = MermaidParserUtilities.LeadingWhitespace(lines[line - 1]);
            var text = ReadStatement(lines, ref line, indent, out var closed);
            if (MermaidParserUtilities.IsSkippable(text)) continue;
            var length = starts[line - 1] + LastPhysicalLength(lines[line - 1], text) - starts[first - 1] - indent;
            var span = new MermaidSourceSpan(first, indent + 1, Math.Max(0, length));
            if (!closed) {
                Invalid(span, result, "Mindmap quoted captions must have a closing quote or backtick.");
                document.RawStatements.Add(new MermaidRawStatement(text, span)); continue;
            }
            if (IsModifier(text, 0)) {
                if (document.Nodes.Count == 0) {
                    Invalid(span, result, "Mindmap modifiers require a preceding node.");
                    document.RawStatements.Add(new MermaidRawStatement(text, span));
                } else if (!ReadModifiers(text, 0, document.Nodes[document.Nodes.Count - 1], span, result, nativeIconNodes))
                    document.RawStatements.Add(new MermaidRawStatement(text, span));
                continue;
            }
            while (indents.Count > 0 && indent <= indents[indents.Count - 1]) {
                indents.RemoveAt(indents.Count - 1);
                stack.RemoveAt(stack.Count - 1);
            }
            MermaidResourceGuard.CheckDepth(stack.Count + 1, span);
            var node = ParseNode(text, document.Nodes.Count, stack.Count, span, result, nativeIconNodes);
            if (node == null) { document.RawStatements.Add(new MermaidRawStatement(text, span)); continue; }
            MermaidResourceGuard.ReserveElements(1, span);
            node.ParentId = stack.Count == 0 ? null : stack[stack.Count - 1].Id;
            if (node.ParentId != null) MermaidResourceGuard.ReserveEdges(1, span);
            document.Nodes.Add(node);
            stack.Add(node);
            indents.Add(indent);
        }
        if (document.Nodes.Count == 0 && !result.HasErrors)
            MermaidParserUtilities.Add(result, document.HeaderSpan, MermaidDiagnosticSeverity.Error, "Mermaid mindmaps require at least one node.");
    }

    /// <summary>Consumes one quoted caption for the family parser and shared opaque-line prepasses.</summary>
    internal static string ReadStatement(string[] lines, ref int line, int indent, out bool closed) {
        var text = new StringBuilder(); var quote = '\0';
        while (true) {
            MermaidResourceGuard.Checkpoint();
            var raw = lines[line - 1]; var start = text.Length == 0 ? indent : 0;
            for (var index = start; index < raw.Length; index++) {
                if ((index & 255) == 0) MermaidResourceGuard.Checkpoint();
                var ch = raw[index];
                if (quote == '\0' && ch == '%' && index + 1 < raw.Length && raw[index + 1] == '%') break;
                if ((ch == '"' || ch == '`') && !MermaidFlowchartText.IsEscaped(raw, index)) {
                    if (quote == '\0') quote = ch;
                    else if (quote == ch) quote = '\0';
                }
                text.Append(ch);
            }
            if (quote == '\0' || line >= lines.Length) break;
            text.Append('\n'); line++;
        }
        closed = quote == '\0';
        return text.ToString().TrimEnd();
    }

    private static int LastPhysicalLength(string raw, string logical) {
        var newline = logical.LastIndexOf('\n');
        // The first physical line starts at its authored indentation; continuations retain theirs.
        return newline >= 0 ? logical.Length - newline - 1 : MermaidParserUtilities.LeadingWhitespace(raw) + logical.Length;
    }

    private static void Invalid(MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result, string message) {
        MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, message, MermaidDiagnosticCodes.InvalidStatement);
    }
}

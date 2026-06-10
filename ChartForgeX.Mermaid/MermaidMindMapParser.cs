using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

internal static class MermaidMindMapParser {
    public static void ParseStatements(MermaidMindMapDocument document, string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        var stack = new List<MermaidMindMapNode>();
        var indents = new List<int>();
        for (var line = Math.Max(1, startLine); line <= lines.Length; line++) {
            var raw = lines[line - 1];
            var trimmed = MermaidParserUtilities.StripInlineComment(raw.Trim());
            if (MermaidParserUtilities.IsSkippable(trimmed)) continue;
            var indent = MermaidParserUtilities.LeadingWhitespace(raw);
            var span = new MermaidSourceSpan(line, indent + 1, trimmed.Length);

            while (indents.Count > 0 && indent <= indents[indents.Count - 1]) {
                indents.RemoveAt(indents.Count - 1);
                stack.RemoveAt(stack.Count - 1);
            }

            var node = ParseNode(trimmed, document.Nodes.Count, stack.Count, span);
            node.ParentId = stack.Count == 0 ? null : stack[stack.Count - 1].Id;
            document.Nodes.Add(node);
            stack.Add(node);
            indents.Add(indent);
        }

        if (document.Nodes.Count == 0) MermaidParserUtilities.Add(result, document.HeaderSpan, MermaidDiagnosticSeverity.Error, "Mermaid mindmaps require at least one node.");
    }

    private static MermaidMindMapNode ParseNode(string text, int index, int level, MermaidSourceSpan span) {
        var classes = new List<string>();
        var icons = new List<string>();
        ExtractIcons(ref text, icons);
        ExtractClasses(ref text, classes);
        var shape = "default";
        var label = ExtractShape(text, out shape);
        var node = new MermaidMindMapNode(MermaidParserUtilities.StableId("mindmap-node", index), label, level, span) { Shape = shape };
        foreach (var item in classes) node.Classes.Add(item);
        foreach (var item in icons) node.Icons.Add(item);
        return node;
    }

    private static void ExtractIcons(ref string text, List<string> icons) {
        var index = text.IndexOf("::icon(", StringComparison.Ordinal);
        while (index >= 0) {
            var end = text.IndexOf(')', index);
            if (end < 0) break;
            icons.Add(text.Substring(index + 7, end - index - 7).Trim());
            text = (text.Substring(0, index) + text.Substring(end + 1)).Trim();
            index = text.IndexOf("::icon(", StringComparison.Ordinal);
        }
    }

    private static void ExtractClasses(ref string text, List<string> classes) {
        var index = text.IndexOf(":::", StringComparison.Ordinal);
        if (index < 0) return;
        var classText = text.Substring(index + 3).Trim();
        text = text.Substring(0, index).Trim();
        foreach (var item in classText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)) classes.Add(item);
    }

    private static string ExtractShape(string text, out string shape) {
        shape = "default";
        if (TryDelimiters(text, "((", "))", out var label)) {
            shape = "circle";
            return label;
        }

        if (TryDelimiters(text, "(", ")", out label)) {
            shape = "rounded";
            return label;
        }

        if (TryDelimiters(text, "[", "]", out label)) {
            shape = "square";
            return label;
        }

        if (TryDelimiters(text, "{{", "}}", out label)) {
            shape = "hexagon";
            return label;
        }

        if (TryDelimiters(text, ")", "(", out label)) {
            shape = "bang";
            return label;
        }

        if (TryDelimiters(text, "(-", "-)", out label)) {
            shape = "cloud";
            return label;
        }

        return MermaidParserUtilities.Unquote(text);
    }

    private static bool TryDelimiters(string value, string start, string end, out string found) {
        found = string.Empty;
        var startIndex = value.IndexOf(start, StringComparison.Ordinal);
        if (startIndex < 0 || !value.EndsWith(end, StringComparison.Ordinal)) return false;
        found = value.Substring(startIndex + start.Length, value.Length - startIndex - start.Length - end.Length).Trim();
        return found.Length > 0;
    }
}

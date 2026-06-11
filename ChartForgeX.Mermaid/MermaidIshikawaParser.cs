using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

internal static class MermaidIshikawaParser {
    public static void ParseStatements(MermaidIshikawaDocument document, string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        var stack = new List<StackEntry>();
        int? baseIndent = null;
        for (var line = Math.Max(1, startLine); line <= lines.Length; line++) {
            var raw = lines[line - 1];
            var trimmed = raw.Trim();
            if (MermaidParserUtilities.IsSkippable(trimmed)) continue;
            var indent = MermaidParserUtilities.LeadingWhitespace(raw);
            var span = new MermaidSourceSpan(line, indent + 1, trimmed.Length);
            document.Statements.Add(new MermaidRawStatement(trimmed, span));

            if (document.Root == null) {
                document.Root = new MermaidIshikawaNode(trimmed, 0, span);
                stack.Add(new StackEntry(0, document.Root));
                continue;
            }

            if (!baseIndent.HasValue) baseIndent = indent;
            var level = indent - baseIndent.Value + 1;
            if (level <= 0) level = 1;
            while (stack.Count > 1 && stack[stack.Count - 1].Level >= level) stack.RemoveAt(stack.Count - 1);
            var parent = stack[stack.Count - 1].Node;
            var node = new MermaidIshikawaNode(trimmed, level, span);
            parent.AddChild(node);
            stack.Add(new StackEntry(level, node));
        }

        if (document.Root == null) MermaidParserUtilities.Add(result, document.HeaderSpan, MermaidDiagnosticSeverity.Error, "Mermaid Ishikawa diagrams require a root effect line.");
        else if (document.Root.Children.Count == 0) MermaidParserUtilities.Add(result, document.Root.Span, MermaidDiagnosticSeverity.Error, "Mermaid Ishikawa diagrams require at least one cause under the root effect.");
    }

    private readonly struct StackEntry {
        public StackEntry(int level, MermaidIshikawaNode node) {
            Level = level;
            Node = node;
        }

        public int Level { get; }
        public MermaidIshikawaNode Node { get; }
    }
}

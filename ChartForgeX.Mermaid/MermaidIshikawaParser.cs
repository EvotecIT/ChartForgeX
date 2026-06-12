using System;
using System.Collections.Generic;
using System.Globalization;

namespace ChartForgeX.Mermaid;

internal static class MermaidIshikawaParser {
    private const int MaximumFishboneNodes = 512;
    private const int MaximumFishboneDepth = 12;

    public static void ParseStatements(MermaidIshikawaDocument document, string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        var stack = new List<StackEntry>();
        int? baseIndent = null;
        var nodeCount = 0;
        for (var line = Math.Max(1, startLine); line <= lines.Length; line++) {
            var raw = lines[line - 1];
            var trimmed = MermaidParserUtilities.StripInlineComment(raw.Trim());
            if (MermaidParserUtilities.IsSkippable(trimmed)) continue;
            var indent = MermaidParserUtilities.LeadingWhitespace(raw);
            var span = new MermaidSourceSpan(line, indent + 1, trimmed.Length);
            document.Statements.Add(new MermaidRawStatement(trimmed, span));

            if (document.Root == null) {
                document.Root = new MermaidIshikawaNode(trimmed, 0, span);
                stack.Add(new StackEntry(0, document.Root));
                nodeCount = 1;
                continue;
            }

            if (!baseIndent.HasValue) baseIndent = indent;
            while (stack.Count > 1 && stack[stack.Count - 1].Indent >= indent) stack.RemoveAt(stack.Count - 1);
            var level = stack.Count;
            if (level > MaximumFishboneDepth) {
                MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid Ishikawa cause depth must not exceed " + MaximumFishboneDepth.ToString(CultureInfo.InvariantCulture) + ".");
                continue;
            }

            if (nodeCount >= MaximumFishboneNodes) {
                MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid Ishikawa diagrams must contain no more than " + MaximumFishboneNodes.ToString(CultureInfo.InvariantCulture) + " nodes.");
                continue;
            }

            var parent = stack[stack.Count - 1].Node;
            var node = new MermaidIshikawaNode(trimmed, level, span);
            parent.AddChild(node);
            stack.Add(new StackEntry(indent, node));
            nodeCount++;
        }

        if (document.Root == null) MermaidParserUtilities.Add(result, document.HeaderSpan, MermaidDiagnosticSeverity.Error, "Mermaid Ishikawa diagrams require a root effect line.");
        else if (document.Root.Children.Count == 0) MermaidParserUtilities.Add(result, document.Root.Span, MermaidDiagnosticSeverity.Error, "Mermaid Ishikawa diagrams require at least one cause under the root effect.");
    }

    private readonly struct StackEntry {
        public StackEntry(int indent, MermaidIshikawaNode node) {
            Indent = indent;
            Node = node;
        }

        public int Indent { get; }
        public MermaidIshikawaNode Node { get; }
    }
}

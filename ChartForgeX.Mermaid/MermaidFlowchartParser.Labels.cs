using System;
using System.Text.RegularExpressions;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidFlowchartParser {
    private static void AddLabelDiagnostics(MermaidFlowchartDocument document, MermaidParseResult<MermaidDocument> result) {
        foreach (var node in document.Nodes) {
            if (node.Text != null) MermaidInlineCaption.Diagnose(node.Text, node.LabelSpan, result, "Flowchart");
        }
        foreach (var edge in document.Edges) Check(edge.Label, edge.Span);
        foreach (var group in document.Subgraphs) Check(group.Title, group.Span);

        void Check(string? label, MermaidSourceSpan span) {
            if (label == null) return;
            MermaidInlineCaption.Diagnose(label, span, result, "Flowchart");
        }
    }
}

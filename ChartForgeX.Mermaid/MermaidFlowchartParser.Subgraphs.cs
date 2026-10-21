using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidFlowchartParser {
    private static void ValidateSubgraphs(MermaidFlowchartDocument document, MermaidParseResult<MermaidDocument> result) {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var group in document.Subgraphs) {
            if (!ids.Add(group.Id)) MermaidParserUtilities.Add(result, group.Span, MermaidDiagnosticSeverity.Error,
                "Flowchart subgraph id '" + group.Id + "' is already declared.");
        }
    }

    private static bool TryParseSubgraphDirection(MermaidFlowchartDocument document, string text, MermaidSourceSpan span,
        Stack<MermaidFlowchartSubgraph> subgraphs, MermaidParseResult<MermaidDocument> result) {
        if (!text.StartsWith("direction ", StringComparison.OrdinalIgnoreCase)) return false;
        var token = text.Substring("direction ".Length).Trim().ToUpperInvariant();
        var direction = token switch {
            "TB" => MermaidFlowchartDirection.TopToBottom,
            "TD" => MermaidFlowchartDirection.TopDown,
            "BT" => MermaidFlowchartDirection.BottomToTop,
            "LR" => MermaidFlowchartDirection.LeftToRight,
            "RL" => MermaidFlowchartDirection.RightToLeft,
            _ => MermaidFlowchartDirection.None
        };
        if (direction == MermaidFlowchartDirection.None)
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Unknown flowchart direction '" + token + "'.");
        else if (subgraphs.Count == 0) document.Direction = direction;
        else subgraphs.Peek().Direction = direction;
        return true;
    }

    // References may precede their subgraph declaration. The group remains the endpoint,
    // without introducing a second visible node that happens to have the group's id.
    private static void RemoveSubgraphEndpointNodes(MermaidFlowchartDocument document) {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var group in document.Subgraphs) ids.Add(group.Id);
        document.Nodes.RemoveAll(node => ids.Contains(node.Id));
        foreach (var group in document.Subgraphs) group.NodeIds.RemoveAll(ids.Contains);
    }
}

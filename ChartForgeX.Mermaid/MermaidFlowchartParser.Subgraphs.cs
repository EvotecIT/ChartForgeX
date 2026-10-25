using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidFlowchartParser {
    /// <summary>Owns canonical group identity and first-closed node/group membership after all source occurrences close.</summary>
    private static void ResolveSubgraphs(MermaidFlowchartDocument document, IReadOnlyList<MermaidFlowchartSubgraph> closedSubgraphs,
        MermaidParseResult<MermaidDocument> result) {
        var sourceIds = new List<string>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var declaration in document.Subgraphs) {
            MermaidResourceGuard.Checkpoint();
            if (ids.Add(declaration.Id)) sourceIds.Add(declaration.Id);
        }
        var owners = new Dictionary<string, string>(StringComparer.Ordinal);
        var groups = new Dictionary<string, MermaidFlowchartSubgraph>(StringComparer.Ordinal);
        var factLengths = new Dictionary<string, long>(StringComparer.Ordinal);
        // The primary DB reduces groups at close, excludes already claimed members and
        // self references, then merges a repeated ID into its first completed record.
        foreach (var declaration in closedSubgraphs) {
            MermaidResourceGuard.Checkpoint();
            factLengths.TryGetValue(declaration.Id, out var factLength);
            foreach (var nodeId in declaration.NodeIds) {
                MermaidResourceGuard.Checkpoint();
                if (nodeId != declaration.Id && !owners.ContainsKey(nodeId)) owners.Add(nodeId, declaration.Id);
                if (ids.Contains(nodeId)) continue; // Group endpoints are not serialized as raw node-reference facts.
                factLength += nodeId.Length + (factLength > 0 ? 1L : 0);
                MermaidResourceGuard.CheckInterchangeFactLength(factLength, declaration.Span);
            }
            factLengths[declaration.Id] = factLength;
            if (groups.TryGetValue(declaration.Id, out var first)) first.NodeIds.AddRange(declaration.NodeIds);
            else groups.Add(declaration.Id, declaration);
        }
        document.Subgraphs.Clear();
        foreach (var id in sourceIds) {
            var group = groups[id];
            group.ParentSubgraphId = owners.TryGetValue(id, out var parent) ? parent : null;
            document.Subgraphs.Add(group);
        }
        foreach (var node in document.Nodes) {
            MermaidResourceGuard.Checkpoint();
            node.SubgraphId = owners.TryGetValue(node.Id, out var owner) ? owner : null;
        }
        ValidateSubgraphParents(document, groups, result);
    }

    private static void ValidateSubgraphParents(MermaidFlowchartDocument document,
        IReadOnlyDictionary<string, MermaidFlowchartSubgraph> groups, MermaidParseResult<MermaidDocument> result) {
        var completed = new HashSet<string>(StringComparer.Ordinal);
        foreach (var group in document.Subgraphs) {
            var path = new List<string>(); var visited = new HashSet<string>(StringComparer.Ordinal);
            MermaidFlowchartSubgraph? current = group;
            while (current != null && !completed.Contains(current.Id)) {
                MermaidResourceGuard.Checkpoint();
                if (!visited.Add(current.Id)) {
                    MermaidParserUtilities.Add(result, current.Span, MermaidDiagnosticSeverity.Error,
                        "Flowchart subgraph parent references form a cycle at '" + current.Id + "'.", MermaidDiagnosticCodes.InvalidStatement);
                    break;
                }
                path.Add(current.Id);
                current = current.ParentSubgraphId != null && groups.TryGetValue(current.ParentSubgraphId, out var parent) ? parent : null;
            }
            foreach (var id in path) completed.Add(id);
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
        foreach (var group in document.Subgraphs) {
            ids.Add(group.Id);
            var placeholder=document.Nodes.Find(node=>node.Id==group.Id);
            if(placeholder==null)continue;
            MermaidResourceGuard.ReserveElements(placeholder.Classes.Count,placeholder.Span);
            group.Classes.AddRange(placeholder.Classes);
            MermaidStyleParser.Copy(group.Styles,placeholder.Styles,placeholder.Span);
            foreach (var pair in placeholder.Properties) group.Properties[pair.Key] = pair.Value;
            foreach (var pair in placeholder.PropertyTokens) group.PropertyTokens[pair.Key] = pair.Value;
            foreach (var pair in placeholder.PropertyValueSpans) group.PropertyValueSpans[pair.Key] = pair.Value;
        }
        document.Nodes.RemoveAll(node => ids.Contains(node.Id));
        foreach (var group in document.Subgraphs) group.NodeIds.RemoveAll(ids.Contains);
    }
}

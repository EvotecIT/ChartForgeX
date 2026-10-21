using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Mermaid;

public static partial class MermaidFlowchartRendering {
    // Mermaid inherits the parent direction when a member node links outside its
    // subgraph. Links to the group shell itself leave the local direction intact.
    private static MermaidFlowchartDirection EffectiveSubgraphDirection(MermaidFlowchartDocument document, MermaidFlowchartSubgraph subgraph) {
        var groups = document.Subgraphs.ToDictionary(group => group.Id, StringComparer.Ordinal);
        var inherited = new HashSet<string>(StringComparer.Ordinal);
        while (inherited.Add(subgraph.Id)) {
            var members = new HashSet<string>(document.Nodes.Where(node => IsMember(node.SubgraphId)).Select(node => node.Id), StringComparer.Ordinal);
            var crossesBoundary = document.Edges.Any(edge => members.Contains(edge.SourceId) != members.Contains(edge.TargetId));
            if (!crossesBoundary && subgraph.Direction != MermaidFlowchartDirection.None) return subgraph.Direction;
            if (subgraph.ParentSubgraphId == null || !groups.TryGetValue(subgraph.ParentSubgraphId, out var parent)) return document.Direction;
            subgraph = parent;
        }
        throw new ArgumentException("Flowchart subgraph parents must not contain a cycle.", nameof(document));

        bool IsMember(string? groupId) {
            var visited = new HashSet<string>(StringComparer.Ordinal);
            while (groupId != null && visited.Add(groupId)) {
                if (groupId == subgraph.Id) return true;
                groupId = groups.TryGetValue(groupId, out var group) ? group.ParentSubgraphId : null;
            }
            return false;
        }
    }
}

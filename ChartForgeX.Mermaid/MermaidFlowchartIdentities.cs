using System;
using System.Collections.Generic;
using System.Globalization;

namespace ChartForgeX.Mermaid;

/// <summary>Shares authored/generated identities between parsing, native selection and interchange.</summary>
internal static class MermaidFlowchartIdentities {
    internal static string EdgeId(MermaidFlowchartEdge edge, int index) => edge.Id ?? "mermaid-edge-" + index.ToString(CultureInfo.InvariantCulture);
    internal static void Validate(MermaidFlowchartDocument document, Action<MermaidSourceSpan, string> error) {
        if (!document.Edges.Exists(edge => edge.Id != null)) return;
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in document.Nodes) ids.Add(node.Id);
        foreach (var group in document.Subgraphs) ids.Add(group.Id);
        for (var index = 0; index < document.Edges.Count; index++) {
            var edge = document.Edges[index]; var id = EdgeId(edge, index);
            MermaidResourceGuard.CheckInterchangeFactLength(id.Length, edge.IdentitySpan ?? edge.Span);
            if (!ids.Add(id)) error(edge.IdentitySpan ?? edge.Span,
                "Authored and generated edge identities must be distinct from nodes, boundaries and other edges: " + id + ".");
        }
    }
}

using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Infers deterministic topology layers from Mermaid flowchart edges.
/// </summary>
internal static class MermaidFlowchartLayering {
    public static IReadOnlyDictionary<string, int> Infer(MermaidFlowchartDocument document) {
        if (document == null) throw new ArgumentNullException(nameof(document));

        var order = new Dictionary<string, int>(StringComparer.Ordinal);
        var adjacency = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var incoming = new Dictionary<string, int>(StringComparer.Ordinal);
        var layers = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var index = 0; index < document.Nodes.Count; index++) {
            var id = document.Nodes[index].Id;
            if (order.ContainsKey(id)) continue;
            order[id] = order.Count;
            adjacency[id] = new List<string>();
            incoming[id] = 0;
            layers[id] = 0;
        }

        var edges = new HashSet<string>(StringComparer.Ordinal);
        foreach (var edge in document.Edges) {
            var sourceId = edge.SourceId;
            var targetId = edge.TargetId;
            if (IsBackwardOnly(edge.Operator)) {
                sourceId = edge.TargetId;
                targetId = edge.SourceId;
            }

            if (!adjacency.ContainsKey(sourceId)
                || !adjacency.ContainsKey(targetId)
                || string.Equals(sourceId, targetId, StringComparison.Ordinal)) {
                continue;
            }

            var key = sourceId + "\u001f" + targetId;
            if (!edges.Add(key)) continue;
            adjacency[sourceId].Add(targetId);
            incoming[targetId]++;
        }

        var ready = new SortedSet<NodeOrder>(NodeOrderComparer.Instance);
        foreach (var node in order) {
            if (incoming[node.Key] == 0) ready.Add(new NodeOrder(node.Key, node.Value));
        }

        var processed = new HashSet<string>(StringComparer.Ordinal);
        while (processed.Count < order.Count) {
            if (ready.Count == 0) {
                // Mermaid permits cycles. Break the next cycle at its first declared node,
                // retaining a deterministic forward flow for the remaining edges.
                foreach (var node in order) {
                    if (processed.Contains(node.Key)) continue;
                    ready.Add(new NodeOrder(node.Key, node.Value));
                    break;
                }
            }

            var current = ready.Min;
            ready.Remove(current);
            if (!processed.Add(current.Id)) continue;

            foreach (var target in adjacency[current.Id]) {
                if (processed.Contains(target)) continue;
                layers[target] = Math.Max(layers[target], layers[current.Id] + 1);
                incoming[target] = Math.Max(0, incoming[target] - 1);
                if (incoming[target] == 0) ready.Add(new NodeOrder(target, order[target]));
            }
        }

        return layers;
    }

    private static bool IsBackwardOnly(string edgeOperator) =>
        edgeOperator.IndexOf('<') >= 0 && edgeOperator.IndexOf('>') < 0;

    private readonly struct NodeOrder {
        public NodeOrder(string id, int order) {
            Id = id;
            Order = order;
        }

        public string Id { get; }

        public int Order { get; }
    }

    private sealed class NodeOrderComparer : IComparer<NodeOrder> {
        public static NodeOrderComparer Instance { get; } = new NodeOrderComparer();

        public int Compare(NodeOrder left, NodeOrder right) {
            var order = left.Order.CompareTo(right.Order);
            return order != 0 ? order : StringComparer.Ordinal.Compare(left.Id, right.Id);
        }
    }
}

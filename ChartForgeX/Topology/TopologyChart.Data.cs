using System;
using System.Collections.Generic;

namespace ChartForgeX.Topology;

public sealed partial class TopologyChart {
    /// <summary>
    /// Maps product-owned node and edge records into a deterministic, renderer-independent topology chart.
    /// </summary>
    /// <typeparam name="TNode">The caller's node record type.</typeparam>
    /// <typeparam name="TEdge">The caller's edge record type.</typeparam>
    /// <param name="nodes">The node records in deterministic input order.</param>
    /// <param name="edges">The edge records in deterministic input order.</param>
    /// <param name="nodeId">Selects the stable node id.</param>
    /// <param name="nodeLabel">Selects the visible node label.</param>
    /// <param name="edgeId">Selects the stable edge id.</param>
    /// <param name="sourceNodeId">Selects the source node id for an edge.</param>
    /// <param name="targetNodeId">Selects the target node id for an edge.</param>
    /// <param name="configureNode">Optionally maps product-neutral visual properties onto each created node.</param>
    /// <param name="configureEdge">Optionally maps product-neutral visual properties onto each created edge.</param>
    /// <param name="layoutMode">The deterministic topology layout applied by renderers.</param>
    /// <returns>A topology chart containing mapped copies of the source records.</returns>
    public static TopologyChart FromData<TNode, TEdge>(
        IEnumerable<TNode> nodes,
        IEnumerable<TEdge> edges,
        Func<TNode, string> nodeId,
        Func<TNode, string> nodeLabel,
        Func<TEdge, string> edgeId,
        Func<TEdge, string> sourceNodeId,
        Func<TEdge, string> targetNodeId,
        Action<TopologyNode, TNode>? configureNode = null,
        Action<TopologyEdge, TEdge>? configureEdge = null,
        TopologyLayoutMode layoutMode = TopologyLayoutMode.Layered) {
        if (nodes == null) throw new ArgumentNullException(nameof(nodes));
        if (edges == null) throw new ArgumentNullException(nameof(edges));
        if (nodeId == null) throw new ArgumentNullException(nameof(nodeId));
        if (nodeLabel == null) throw new ArgumentNullException(nameof(nodeLabel));
        if (edgeId == null) throw new ArgumentNullException(nameof(edgeId));
        if (sourceNodeId == null) throw new ArgumentNullException(nameof(sourceNodeId));
        if (targetNodeId == null) throw new ArgumentNullException(nameof(targetNodeId));
        TopologyModelGuards.EnumDefined(layoutMode, nameof(layoutMode));

        var chart = Create();
        chart.LayoutMode = layoutMode;
        var nodeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var record in nodes) {
            if ((object?)record == null) throw new ArgumentException("Node records cannot contain null values.", nameof(nodes));
            var id = RequiredSelectorText(nodeId(record), nameof(nodeId), "Node ids");
            var label = RequiredSelectorText(nodeLabel(record), nameof(nodeLabel), "Node labels");
            if (!nodeIds.Add(id)) throw new ArgumentException("Duplicate topology node id '" + id + "'.", nameof(nodes));
            chart.AddAutoNode(id, label);
            var mapped = chart.Nodes[chart.Nodes.Count - 1];
            configureNode?.Invoke(mapped, record);
            if (!string.Equals(mapped.Id, id, StringComparison.Ordinal)) throw new InvalidOperationException("Node configuration must not change the stable id selected by nodeId.");
        }

        var edgeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var record in edges) {
            if ((object?)record == null) throw new ArgumentException("Edge records cannot contain null values.", nameof(edges));
            var id = RequiredSelectorText(edgeId(record), nameof(edgeId), "Edge ids");
            var source = RequiredSelectorText(sourceNodeId(record), nameof(sourceNodeId), "Edge source node ids");
            var target = RequiredSelectorText(targetNodeId(record), nameof(targetNodeId), "Edge target node ids");
            if (!edgeIds.Add(id)) throw new ArgumentException("Duplicate topology edge id '" + id + "'.", nameof(edges));
            if (!nodeIds.Contains(source)) throw new ArgumentException("Topology edge '" + id + "' references unknown source node '" + source + "'.", nameof(edges));
            if (!nodeIds.Contains(target)) throw new ArgumentException("Topology edge '" + id + "' references unknown target node '" + target + "'.", nameof(edges));
            chart.AddEdge(id, source, target);
            var mapped = chart.Edges[chart.Edges.Count - 1];
            configureEdge?.Invoke(mapped, record);
            if (!string.Equals(mapped.Id, id, StringComparison.Ordinal) || !string.Equals(mapped.SourceNodeId, source, StringComparison.Ordinal) || !string.Equals(mapped.TargetNodeId, target, StringComparison.Ordinal)) {
                throw new InvalidOperationException("Edge configuration must not change identity or endpoints selected by the mapping delegates.");
            }
        }

        return chart;
    }

    private static string RequiredSelectorText(string? value, string parameterName, string description) {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(description + " must not be empty.", parameterName);
        return value!.Trim();
    }
}

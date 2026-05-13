using System;

namespace ChartForgeX.Topology;

public static partial class TopologyChartExtensions {
    /// <summary>
    /// Sets an optional node accent color independent from the node health status.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="nodeId">The node id.</param>
    /// <param name="color">The node accent color.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithNodeColor(this TopologyChart chart, string nodeId, string? color) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        nodeId = RequiredText(nodeId, nameof(nodeId), "Topology node ids");
        foreach (var node in chart.Nodes) {
            if (!string.Equals(node.Id, nodeId, StringComparison.Ordinal)) continue;
            node.Color = color;
            return chart;
        }

        throw new ArgumentException("Topology node '" + nodeId + "' was not found.", nameof(nodeId));
    }

    /// <summary>
    /// Sets an optional node surface fill color independent from the node accent color.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="nodeId">The node id.</param>
    /// <param name="backgroundColor">The node surface fill color.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithNodeBackground(this TopologyChart chart, string nodeId, string? backgroundColor) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        nodeId = RequiredText(nodeId, nameof(nodeId), "Topology node ids");
        foreach (var node in chart.Nodes) {
            if (!string.Equals(node.Id, nodeId, StringComparison.Ordinal)) continue;
            node.BackgroundColor = backgroundColor;
            return chart;
        }

        throw new ArgumentException("Topology node '" + nodeId + "' was not found.", nameof(nodeId));
    }

    /// <summary>
    /// Sets reusable node styling for all nodes of a kind.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="kind">The node kind to update.</param>
    /// <param name="color">The optional node accent color.</param>
    /// <param name="backgroundColor">The optional node surface fill color.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithNodesOfKind(this TopologyChart chart, TopologyNodeKind kind, string? color = null, string? backgroundColor = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        ValidateEnum(typeof(TopologyNodeKind), kind, nameof(kind), "Topology node kinds");
        foreach (var node in chart.Nodes) {
            if (node.Kind != kind) continue;
            if (color != null) node.Color = color;
            if (backgroundColor != null) node.BackgroundColor = backgroundColor;
        }

        return chart;
    }

    /// <summary>
    /// Sets reusable node styling and optional icon assignment for all nodes of a kind in one pass.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="kind">The node kind to update.</param>
    /// <param name="color">The optional node accent color.</param>
    /// <param name="backgroundColor">The optional node surface fill color.</param>
    /// <param name="iconId">The optional icon id, for example <c>common:certificate</c>.</param>
    /// <param name="catalog">Optional icon catalog. When omitted, built-in packs are used.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithNodesOfKindStyle(this TopologyChart chart, TopologyNodeKind kind, string? color = null, string? backgroundColor = null, string? iconId = null, TopologyIconCatalog? catalog = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        ValidateEnum(typeof(TopologyNodeKind), kind, nameof(kind), "Topology node kinds");
        var icon = string.IsNullOrWhiteSpace(iconId) ? null : ResolveIcon(iconId!, catalog);
        foreach (var node in chart.Nodes) {
            if (node.Kind != kind) continue;
            if (color != null) node.Color = color;
            if (backgroundColor != null) node.BackgroundColor = backgroundColor;
            if (icon != null) ApplyIcon(node, icon, updateKind: true);
        }

        return chart;
    }

    /// <summary>
    /// Sets an explicit edge color independent from health status.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="edgeId">The edge id.</param>
    /// <param name="color">The edge color.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithEdgeColor(this TopologyChart chart, string edgeId, string? color) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        edgeId = RequiredText(edgeId, nameof(edgeId), "Topology edge ids");
        foreach (var edge in chart.Edges) {
            if (!string.Equals(edge.Id, edgeId, StringComparison.Ordinal)) continue;
            edge.Color = color;
            return chart;
        }

        throw new ArgumentException("Topology edge '" + edgeId + "' was not found.", nameof(edgeId));
    }
}

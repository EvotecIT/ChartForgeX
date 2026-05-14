using System;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

public static partial class TopologyChartExtensions {
    /// <summary>
    /// Sets the rendered label position for a specific edge and keeps the leader anchored to the edge route.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="edgeId">The edge id.</param>
    /// <param name="offsetX">The horizontal label offset in pixels.</param>
    /// <param name="offsetY">The vertical label offset in pixels.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithEdgeLabelAnnotationOffset(this TopologyChart chart, string edgeId, double offsetX, double offsetY) {
        return chart.WithEdgeLabelOffset(edgeId, offsetX, offsetY).ClearEdgeLabelAnchor(edgeId);
    }

    /// <summary>
    /// Sets an explicit point that a displaced edge label should point to.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="edgeId">The edge id.</param>
    /// <param name="anchor">The canvas-space point the label leader should point to.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithEdgeLabelAnchor(this TopologyChart chart, string edgeId, ChartPoint anchor) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        edgeId = RequiredText(edgeId, nameof(edgeId), "Topology edge ids");
        TopologyModelGuards.Finite(anchor.X, nameof(anchor));
        TopologyModelGuards.Finite(anchor.Y, nameof(anchor));
        foreach (var edge in chart.Edges) {
            if (!string.Equals(edge.Id, edgeId, StringComparison.Ordinal)) continue;
            edge.LabelAnchorX = anchor.X;
            edge.LabelAnchorY = anchor.Y;
            edge.HasLabelAnchorOverride = true;
            edge.LabelAnchorNodeId = null;
            return chart;
        }

        throw new ArgumentException("Topology edge '" + edgeId + "' was not found.", nameof(edgeId));
    }

    /// <summary>
    /// Sets an explicit point that a displaced edge label should point to.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="edgeId">The edge id.</param>
    /// <param name="x">The canvas-space horizontal point.</param>
    /// <param name="y">The canvas-space vertical point.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithEdgeLabelAnchor(this TopologyChart chart, string edgeId, double x, double y) {
        return chart.WithEdgeLabelAnchor(edgeId, new ChartPoint(x, y));
    }

    /// <summary>
    /// Anchors a displaced edge label leader to the rendered boundary of a node.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="edgeId">The edge id.</param>
    /// <param name="nodeId">The node id the label leader should point to.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithEdgeLabelAnchorNode(this TopologyChart chart, string edgeId, string nodeId) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        edgeId = RequiredText(edgeId, nameof(edgeId), "Topology edge ids");
        nodeId = RequiredText(nodeId, nameof(nodeId), "Topology node ids");
        if (!chart.Nodes.Any(node => string.Equals(node.Id, nodeId, StringComparison.Ordinal))) throw new ArgumentException("Topology node '" + nodeId + "' was not found.", nameof(nodeId));
        foreach (var edge in chart.Edges) {
            if (!string.Equals(edge.Id, edgeId, StringComparison.Ordinal)) continue;
            edge.LabelAnchorNodeId = nodeId;
            edge.HasLabelAnchorOverride = false;
            return chart;
        }

        throw new ArgumentException("Topology edge '" + edgeId + "' was not found.", nameof(edgeId));
    }

    /// <summary>
    /// Clears an explicit edge label leader anchor so the label points back to the edge route.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="edgeId">The edge id.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart ClearEdgeLabelAnchor(this TopologyChart chart, string edgeId) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        edgeId = RequiredText(edgeId, nameof(edgeId), "Topology edge ids");
        foreach (var edge in chart.Edges) {
            if (!string.Equals(edge.Id, edgeId, StringComparison.Ordinal)) continue;
            edge.LabelAnchorX = 0;
            edge.LabelAnchorY = 0;
            edge.HasLabelAnchorOverride = false;
            edge.LabelAnchorNodeId = null;
            return chart;
        }

        throw new ArgumentException("Topology edge '" + edgeId + "' was not found.", nameof(edgeId));
    }
}

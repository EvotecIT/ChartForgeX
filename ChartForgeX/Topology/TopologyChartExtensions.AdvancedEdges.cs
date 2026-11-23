using ChartForgeX.Primitives;
using System;
using System.Linq;

namespace ChartForgeX.Topology;

public static partial class TopologyChartExtensions {
    /// <summary>Adds a named attachment port to a topology node.</summary>
    public static TopologyChart AddNodePort(this TopologyChart chart, string nodeId, string portId, TopologyEdgePort side, double offset = 0.5, string? label = null) {
        var node = RequiredNode(chart, nodeId);
        portId = RequiredText(portId, nameof(portId), "Topology node port ids");
        if (node.Ports.Any(port => string.Equals(port.Id, portId, StringComparison.Ordinal))) throw new ArgumentException("Topology node '" + node.Id + "' already contains port '" + portId + "'.", nameof(portId));
        node.Ports.Add(new TopologyNodePort { Id = portId, Side = side, Offset = offset, Label = label });
        return chart;
    }

    /// <summary>Adds a typed detail row to a card-like topology node.</summary>
    public static TopologyChart AddNodeDetail(this TopologyChart chart, string nodeId, string label, string value, TopologyHealthStatus? status = null, ChartColor? color = null, string? iconId = null) {
        var node = RequiredNode(chart, nodeId);
        label = RequiredText(label, nameof(label), "Topology node detail labels");
        value = RequiredText(value, nameof(value), "Topology node detail values");
        if (status.HasValue) ValidateEnum(typeof(TopologyHealthStatus), status.Value, nameof(status), "Topology health statuses");
        node.Details.Add(new TopologyNodeDetail { Label = label, Value = value, Status = status, Color = color, IconId = iconId });
        return chart;
    }

    /// <summary>Applies edge-specific stroke width, opacity, and an optional alternating dash/gap pattern.</summary>
    public static TopologyChart EdgeStroke(this TopologyChart chart, string edgeId, double? width = null, double? opacity = null, params double[] dashPattern) {
        var edge = RequiredEdge(chart, edgeId);
        edge.StrokeWidth = width;
        edge.Opacity = opacity;
        edge.DashPattern.Clear();
        if (dashPattern != null) edge.DashPattern.AddRange(dashPattern);
        return chart;
    }

    private static TopologyNode RequiredNode(TopologyChart chart, string nodeId) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        nodeId = RequiredText(nodeId, nameof(nodeId), "Topology node ids");
        return chart.Nodes.FirstOrDefault(node => string.Equals(node.Id, nodeId, StringComparison.Ordinal)) ?? throw new ArgumentException("Topology node '" + nodeId + "' was not found.", nameof(nodeId));
    }

    private static TopologyEdge RequiredEdge(TopologyChart chart, string edgeId) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        edgeId = RequiredText(edgeId, nameof(edgeId), "Topology edge ids");
        return chart.Edges.FirstOrDefault(edge => string.Equals(edge.Id, edgeId, StringComparison.Ordinal)) ?? throw new ArgumentException("Topology edge '" + edgeId + "' was not found.", nameof(edgeId));
    }
}

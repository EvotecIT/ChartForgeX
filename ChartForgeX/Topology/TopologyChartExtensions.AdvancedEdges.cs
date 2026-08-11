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
    public static TopologyChart AddNodeDetail(this TopologyChart chart, string nodeId, string label, string value, TopologyHealthStatus? status = null, string? color = null, string? iconId = null) {
        var node = RequiredNode(chart, nodeId);
        label = RequiredText(label, nameof(label), "Topology node detail labels");
        value = RequiredText(value, nameof(value), "Topology node detail values");
        if (status.HasValue) ValidateEnum(typeof(TopologyHealthStatus), status.Value, nameof(status), "Topology health statuses");
        node.Details.Add(new TopologyNodeDetail { Label = label, Value = value, Status = status, Color = color, IconId = iconId });
        return chart;
    }

    /// <summary>Connects an edge through named ports on its source and target nodes.</summary>
    public static TopologyChart WithEdgeNamedPorts(this TopologyChart chart, string edgeId, string? sourcePortId, string? targetPortId) {
        var edge = RequiredEdge(chart, edgeId);
        edge.SourcePortId = string.IsNullOrWhiteSpace(sourcePortId) ? null : sourcePortId!.Trim();
        edge.TargetPortId = string.IsNullOrWhiteSpace(targetPortId) ? null : targetPortId!.Trim();
        return chart;
    }

    /// <summary>Applies edge-specific stroke width, opacity, and an optional alternating dash/gap pattern.</summary>
    public static TopologyChart WithEdgeStroke(this TopologyChart chart, string edgeId, double? width = null, double? opacity = null, params double[] dashPattern) {
        var edge = RequiredEdge(chart, edgeId);
        edge.StrokeWidth = width;
        edge.Opacity = opacity;
        edge.DashPattern.Clear();
        if (dashPattern != null) edge.DashPattern.AddRange(dashPattern);
        return chart;
    }

    /// <summary>Sets explicit markers at both edge endpoints. Null keeps direction-derived behavior.</summary>
    public static TopologyChart WithEdgeMarkers(this TopologyChart chart, string edgeId, TopologyMarkerKind? source, TopologyMarkerKind? target) {
        var edge = RequiredEdge(chart, edgeId);
        if (source.HasValue) ValidateEnum(typeof(TopologyMarkerKind), source.Value, nameof(source), "Topology marker kinds");
        if (target.HasValue) ValidateEnum(typeof(TopologyMarkerKind), target.Value, nameof(target), "Topology marker kinds");
        edge.SourceMarker = source;
        edge.TargetMarker = target;
        return chart;
    }

    /// <summary>Sets optional labels near the source and target edge endpoints.</summary>
    public static TopologyChart WithEdgeEndpointLabels(this TopologyChart chart, string edgeId, string? sourceLabel, string? targetLabel) {
        var edge = RequiredEdge(chart, edgeId);
        edge.SourceLabel = sourceLabel;
        edge.TargetLabel = targetLabel;
        return chart;
    }

    /// <summary>Sets soft layout intent without promising exact rendered edge length.</summary>
    public static TopologyChart WithEdgeLayoutHints(this TopologyChart chart, string edgeId, double? preferredLength = null, int minimumRankSpan = 1, int routingPriority = 0) {
        var edge = RequiredEdge(chart, edgeId);
        edge.PreferredLength = preferredLength;
        edge.MinimumRankSpan = minimumRankSpan;
        edge.RoutingPriority = routingPriority;
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

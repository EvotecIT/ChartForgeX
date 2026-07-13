using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChartForgeX.Interactivity;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlGraphExplorerRenderer {
    private static void WriteEdges(StringBuilder writer, GraphScene scene, IReadOnlyDictionary<string, Point> positions, IReadOnlyDictionary<string, string> clusterMembership, IReadOnlyDictionary<string, Point> collapsedNodePositions, IReadOnlyDictionary<string, double> collapsedNodeRadii, string markerId, HashSet<string> collapsedEdgeIds, bool focusableGraphItems) {
        var nodesById = scene.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        foreach (var edge in scene.Edges) {
            if (!positions.TryGetValue(edge.SourceNodeId, out var source) || !positions.TryGetValue(edge.TargetNodeId, out var target)) continue;
            nodesById.TryGetValue(edge.SourceNodeId, out var sourceNode);
            nodesById.TryGetValue(edge.TargetNodeId, out var targetNode);
            var renderSource = collapsedNodePositions.TryGetValue(edge.SourceNodeId, out var collapsedSource) ? collapsedSource : source;
            var renderTarget = collapsedNodePositions.TryGetValue(edge.TargetNodeId, out var collapsedTarget) ? collapsedTarget : target;
            var sourceBoundary = collapsedNodeRadii.TryGetValue(edge.SourceNodeId, out var sourceRadius) ? sourceRadius : (double?)null;
            var targetBoundary = collapsedNodeRadii.TryGetValue(edge.TargetNodeId, out var targetRadius) ? targetRadius : (double?)null;
            var path = EdgePath(edge, renderSource, renderTarget, sourceNode, targetNode, targetBoundary, sourceBoundary);
            writer.Append("<path class=\"cfx-graph-edge");
            if (collapsedEdgeIds.Contains(edge.Id)) writer.Append(" cfx-graph-cluster-collapsed-member");
            if (edge.Style.Hidden) writer.Append(" cfx-graph-hidden");
            writer.Append("\" tabindex=\"");
            writer.Append("-1");
            writer.Append("\" data-cfx-role=\"graph-edge\"");
            if (focusableGraphItems) {
                Attribute(writer, "role", "button");
                Attribute(writer, "aria-pressed", "false");
            }
            Attribute(writer, "data-edge-id", edge.Id);
            Attribute(writer, "data-edge-label", edge.Label);
            Attribute(writer, "data-edge-kind", edge.Kind);
            Attribute(writer, "data-cfx-status", edge.Status);
            Attribute(writer, "data-source-node-id", edge.SourceNodeId);
            Attribute(writer, "data-target-node-id", edge.TargetNodeId);
            Attribute(writer, "data-source-cluster-id", sourceNode == null ? null : NodeClusterId(sourceNode, clusterMembership));
            Attribute(writer, "data-target-cluster-id", targetNode == null ? null : NodeClusterId(targetNode, clusterMembership));
            Attribute(writer, "data-edge-weight", Number(edge.Weight));
            Attribute(writer, "data-edge-length", Number(edge.Length));
            Attribute(writer, "data-edge-directed", edge.Directed ? "true" : "false");
            Attribute(writer, "data-edge-source-arrow", HasSourceArrow(edge) ? "true" : "false");
            Attribute(writer, "data-edge-target-arrow", HasTargetArrow(edge) ? "true" : "false");
            Attribute(writer, "data-edge-shape", EdgeShape(edge.Shape));
            Attribute(writer, "data-edge-curvature", Number(edge.Curvature));
            Attribute(writer, "data-edge-route-points", RoutePointsData(edge));
            Attribute(writer, "data-edge-dashed", edge.Dashed ? "true" : "false");
            Attribute(writer, "data-edge-dash-pattern", edge.Style.DashPattern);
            Attribute(writer, "data-edge-show-label", edge.ShowLabel ? "true" : "false");
            Attribute(writer, "data-edge-width", edge.Style.Width.HasValue ? Number(edge.Style.Width.Value) : null);
            Attribute(writer, "data-edge-color", edge.Style.Color);
            Attribute(writer, "data-edge-label-color", edge.Style.LabelColor);
            Attribute(writer, "data-edge-physics", edge.Style.Physics ? "true" : "false");
            Attribute(writer, "data-edge-hidden", edge.Style.Hidden ? "true" : "false");
            Attribute(writer, "data-cfx-search", SearchText(edge.Metadata));
            Attribute(writer, "data-cfx-metadata", MetadataJson(edge.Metadata));
            Attribute(writer, "aria-label", EdgeAccessibleName(edge, sourceNode, targetNode));
            Attribute(writer, "d", path);
            var edgeStyle = EdgeStyle(edge);
            if (!string.IsNullOrWhiteSpace(edgeStyle)) Attribute(writer, "style", edgeStyle);
            var edgeMarkerId = ArrowMarkerId(markerId, edge.Style.Color);
            if (HasSourceArrow(edge)) Attribute(writer, "marker-start", "url(#" + edgeMarkerId + ")");
            if (HasTargetArrow(edge)) Attribute(writer, "marker-end", "url(#" + edgeMarkerId + ")");
            writer.Append("></path>");
        }
    }

    private static void WriteArrowMarkers(StringBuilder writer, GraphScene scene, string markerId) {
        writer.Append("<defs>");
        WriteArrowMarker(writer, markerId, null);
        foreach (var color in scene.Edges.Where(edge => HasSourceArrow(edge) || HasTargetArrow(edge)).Select(edge => edge.Style.Color).Where(color => !string.IsNullOrWhiteSpace(color)).Distinct(StringComparer.Ordinal)) WriteArrowMarker(writer, ArrowMarkerId(markerId, color), color);
        writer.Append("</defs>");
    }

    private static void WriteArrowMarker(StringBuilder writer, string markerId, string? color) {
        writer.Append("<marker");
        Attribute(writer, "id", markerId);
        writer.Append(" viewBox=\"0 0 10 10\" refX=\"9\" refY=\"5\" markerWidth=\"6\" markerHeight=\"6\" orient=\"auto-start-reverse\"><path d=\"M 0 0 L 10 5 L 0 10 z\"");
        if (!string.IsNullOrWhiteSpace(color)) Attribute(writer, "style", "fill:" + color + ";stroke:" + color);
        writer.Append("></path></marker>");
    }

    private static string ArrowMarkerId(string markerId, string? color) => string.IsNullOrWhiteSpace(color) ? markerId : markerId + "-" + SafeId(color!);

    private static string[] ClusterMemberIds(GraphScene scene, GraphSceneCluster cluster, IReadOnlyDictionary<string, string> clusterMembership) {
        var members = new HashSet<string>(cluster.NodeIds, StringComparer.Ordinal);
        foreach (var node in scene.Nodes) if (clusterMembership.TryGetValue(node.Id, out var clusterId) && string.Equals(cluster.Id, clusterId, StringComparison.Ordinal)) members.Add(node.Id);
        return members.OrderBy(id => id, StringComparer.Ordinal).ToArray();
    }

    private static void WriteEdgeLabels(StringBuilder writer, GraphScene scene, IReadOnlyDictionary<string, Point> positions, IReadOnlyDictionary<string, Point> collapsedNodePositions, IReadOnlyDictionary<string, double> collapsedNodeRadii, HashSet<string> collapsedEdgeIds) {
        var nodesById = scene.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        foreach (var edge in scene.Edges.Where(edge => edge.ShowLabel && !edge.Style.Hidden && !string.IsNullOrWhiteSpace(edge.Label))) {
            if (!positions.TryGetValue(edge.SourceNodeId, out var source) || !positions.TryGetValue(edge.TargetNodeId, out var target)) continue;
            nodesById.TryGetValue(edge.SourceNodeId, out var sourceNode);
            nodesById.TryGetValue(edge.TargetNodeId, out var targetNode);
            var renderSource = collapsedNodePositions.TryGetValue(edge.SourceNodeId, out var collapsedSource) ? collapsedSource : source;
            var renderTarget = collapsedNodePositions.TryGetValue(edge.TargetNodeId, out var collapsedTarget) ? collapsedTarget : target;
            var sourceBoundary = collapsedNodeRadii.TryGetValue(edge.SourceNodeId, out var sourceRadius) ? sourceRadius : (double?)null;
            var targetBoundary = collapsedNodeRadii.TryGetValue(edge.TargetNodeId, out var targetRadius) ? targetRadius : (double?)null;
            var point = EdgeLabelPoint(edge, renderSource, renderTarget, sourceNode, targetNode, targetBoundary, sourceBoundary);
            writer.Append("<text class=\"cfx-graph-edge-label");
            if (collapsedEdgeIds.Contains(edge.Id)) writer.Append(" cfx-graph-cluster-collapsed-member");
            writer.Append("\" data-cfx-role=\"graph-edge-label\"");
            Attribute(writer, "data-edge-label-for", edge.Id);
            Attribute(writer, "x", Number(point.X));
            Attribute(writer, "y", Number(point.Y));
            var labelStyle = EdgeLabelStyle(edge);
            if (!string.IsNullOrWhiteSpace(labelStyle)) Attribute(writer, "style", labelStyle);
            writer.Append('>');
            writer.Append(Text(edge.Label!));
            writer.Append("</text>");
        }
    }
}

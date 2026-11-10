using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChartForgeX.Interactivity;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlGraphExplorerRenderer {
    private static void WriteEdges(StringBuilder writer, GraphScene scene, IReadOnlyDictionary<string, Point> positions, IReadOnlyDictionary<string, string> clusterMembership, IReadOnlyDictionary<string, Point> collapsedNodePositions, IReadOnlyDictionary<string, double> collapsedNodeRadii, HashSet<string> collapsedEdgeIds, bool focusableGraphItems, HtmlChartLocalization? text = null) {
        var nodesById = scene.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var valueWidths = EdgeValueWidths(scene);
        foreach (var edge in scene.Edges) {
            if (!positions.TryGetValue(edge.SourceNodeId, out var source) || !positions.TryGetValue(edge.TargetNodeId, out var target)) continue;
            nodesById.TryGetValue(edge.SourceNodeId, out var sourceNode);
            nodesById.TryGetValue(edge.TargetNodeId, out var targetNode);
            var width = ResolvedEdgeWidth(edge, valueWidths);
            var renderSource = collapsedNodePositions.TryGetValue(edge.SourceNodeId, out var collapsedSource) ? collapsedSource : source;
            var renderTarget = collapsedNodePositions.TryGetValue(edge.TargetNodeId, out var collapsedTarget) ? collapsedTarget : target;
            var sourceBoundary = collapsedNodeRadii.TryGetValue(edge.SourceNodeId, out var sourceRadius) ? sourceRadius : (double?)null;
            var targetBoundary = collapsedNodeRadii.TryGetValue(edge.TargetNodeId, out var targetRadius) ? targetRadius : (double?)null;
            var path = EdgePath(edge, renderSource, renderTarget, sourceNode, targetNode, targetBoundary, sourceBoundary);
            var edgeClass = "cfx-graph-edge" + (collapsedEdgeIds.Contains(edge.Id) ? " cfx-graph-cluster-collapsed-member" : string.Empty) + (edge.Style.Hidden ? " cfx-graph-hidden" : string.Empty);
            writer.Append("<path class=\"").Append(edgeClass).Append("\" tabindex=\"");
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
            Attribute(writer, "data-edge-width", width.HasValue ? Number(width.Value) : null);
            Attribute(writer, "data-edge-arrows", ArrowStyleData(edge));
            Attribute(writer, "data-edge-hover-width", edge.Style.HoverWidth.HasValue ? Number(edge.Style.HoverWidth.Value) : null);
            Attribute(writer, "data-edge-selection-width", edge.Style.SelectionWidth.HasValue ? Number(edge.Style.SelectionWidth.Value) : null);
            Attribute(writer, "data-cfx-hover-text", edge.Tooltip);
            Attribute(writer, "data-edge-color", edge.Style.Color);
            Attribute(writer, "data-edge-label-color", edge.Style.LabelColor);
            Attribute(writer, "data-edge-physics", edge.Style.Physics ? "true" : "false");
            Attribute(writer, "data-edge-hidden", edge.Style.Hidden ? "true" : "false");
            Attribute(writer, "data-cfx-search", SearchText(edge.Metadata));
            Attribute(writer, "data-cfx-metadata", MetadataJson(edge.Metadata));
            Attribute(writer, "aria-label", EdgeAccessibleName(edge, sourceNode, targetNode, text));
            Attribute(writer, "d", path);
            var edgeStyle = EdgeStyle(edge, width);
            if (!string.IsNullOrWhiteSpace(edgeStyle)) Attribute(writer, "style", edgeStyle);
            writer.Append("></path>");
            if (!edge.Style.Hidden) WriteEdgeArrowMarks(writer, edge, edgeClass, width ?? DefaultArrowWidth, renderSource, renderTarget, sourceNode, targetNode, targetBoundary, sourceBoundary);
        }
    }

    private static string[] ClusterMemberIds(GraphScene scene, GraphSceneCluster cluster, IReadOnlyDictionary<string, string> clusterMembership) {
        var members = new HashSet<string>(cluster.NodeIds, StringComparer.Ordinal);
        foreach (var node in scene.Nodes) if (clusterMembership.TryGetValue(node.Id, out var clusterId) && string.Equals(cluster.Id, clusterId, StringComparison.Ordinal)) members.Add(node.Id);
        return members.OrderBy(id => id, StringComparer.Ordinal).ToArray();
    }

    private static void WriteEdgeLabels(StringBuilder writer, GraphScene scene, IReadOnlyDictionary<string, Point> positions, IReadOnlyDictionary<string, Point> collapsedNodePositions, IReadOnlyDictionary<string, double> collapsedNodeRadii, HashSet<string> collapsedEdgeIds) {
        var nodesById = scene.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var labelObstacles = BuildEdgeLabelObstacles(scene, positions, collapsedNodePositions, collapsedNodeRadii);
        foreach (var edge in scene.Edges.Where(edge => edge.ShowLabel && !edge.Style.Hidden && !string.IsNullOrWhiteSpace(edge.Label))) {
            if (!positions.TryGetValue(edge.SourceNodeId, out var source) || !positions.TryGetValue(edge.TargetNodeId, out var target)) continue;
            nodesById.TryGetValue(edge.SourceNodeId, out var sourceNode);
            nodesById.TryGetValue(edge.TargetNodeId, out var targetNode);
            var renderSource = collapsedNodePositions.TryGetValue(edge.SourceNodeId, out var collapsedSource) ? collapsedSource : source;
            var renderTarget = collapsedNodePositions.TryGetValue(edge.TargetNodeId, out var collapsedTarget) ? collapsedTarget : target;
            var sourceBoundary = collapsedNodeRadii.TryGetValue(edge.SourceNodeId, out var sourceRadius) ? sourceRadius : (double?)null;
            var targetBoundary = collapsedNodeRadii.TryGetValue(edge.TargetNodeId, out var targetRadius) ? targetRadius : (double?)null;
            var point = EdgeLabelPoint(edge, renderSource, renderTarget, sourceNode, targetNode, labelObstacles, targetBoundary, sourceBoundary);
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

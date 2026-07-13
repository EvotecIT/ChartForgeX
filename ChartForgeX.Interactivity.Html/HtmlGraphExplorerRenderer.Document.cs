using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ChartForgeX.Interactivity;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlGraphExplorerRenderer {
    private static void WriteGraphDocument(StringBuilder writer, GraphScene scene, IReadOnlyDictionary<string, Point> positions, IReadOnlyDictionary<string, string> clusterMembership, ISet<string> collapsedNodeIds, ISet<string> collapsedEdgeIds, string markerId) {
        var nodesById = scene.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        writer.Append("<script type=\"application/json\" data-cfx-role=\"graph-document\">{\"n\":[");
        for (var index = 0; index < scene.Nodes.Count; index++) {
            if (index > 0) writer.Append(',');
            var node = scene.Nodes[index];
            var point = positions[node.Id];
            writer.Append('[');
            writer.Append(JsonString(node.Id)); Value(writer, node.Label); Value(writer, node.Kind); Value(writer, node.GroupId); Value(writer, NodeClusterId(node, clusterMembership)); Value(writer, node.ParentId); Value(writer, node.Status);
            NumberValue(writer, SafeNodeSize(node)); BooleanValue(writer, node.Fixed); BooleanValue(writer, node.Hidden); NullableNumberValue(writer, node.Level); Value(writer, NodeShape(node)); Value(writer, node.ImageUrl); Value(writer, node.ImageAlt); Value(writer, node.IconText); Value(writer, node.SecondaryLabel); Value(writer, node.BadgeText);
            Value(writer, node.Style.BackgroundColor); Value(writer, node.Style.BorderColor); Value(writer, node.Style.LabelColor); Value(writer, node.Style.LabelBackgroundColor); BooleanValue(writer, node.Style.Shadow); Value(writer, SearchText(node.Metadata)); Value(writer, MetadataJson(node.Metadata)); NumberValue(writer, point.X); NumberValue(writer, point.Y); BooleanValue(writer, collapsedNodeIds.Contains(node.Id));
            writer.Append(']');
        }

        writer.Append("],\"e\":[");
        for (var index = 0; index < scene.Edges.Count; index++) {
            if (index > 0) writer.Append(',');
            var edge = scene.Edges[index];
            nodesById.TryGetValue(edge.SourceNodeId, out var sourceNode);
            nodesById.TryGetValue(edge.TargetNodeId, out var targetNode);
            var edgeMarkerId = ArrowMarkerId(markerId, edge.Style.Color);
            writer.Append('[');
            writer.Append(JsonString(edge.Id)); Value(writer, edge.Label); Value(writer, edge.Kind); Value(writer, edge.Status); Value(writer, edge.SourceNodeId); Value(writer, edge.TargetNodeId);
            Value(writer, sourceNode == null ? null : NodeClusterId(sourceNode, clusterMembership)); Value(writer, targetNode == null ? null : NodeClusterId(targetNode, clusterMembership));
            NumberValue(writer, edge.Weight); NumberValue(writer, edge.Length); BooleanValue(writer, edge.Directed); BooleanValue(writer, HasSourceArrow(edge)); BooleanValue(writer, HasTargetArrow(edge)); Value(writer, EdgeShape(edge.Shape)); NumberValue(writer, edge.Curvature); Value(writer, RoutePointsData(edge)); BooleanValue(writer, edge.Dashed); Value(writer, edge.Style.DashPattern); BooleanValue(writer, edge.ShowLabel); NullableNumberValue(writer, edge.Style.Width); Value(writer, edge.Style.Color); Value(writer, edge.Style.LabelColor); BooleanValue(writer, edge.Style.Physics); BooleanValue(writer, edge.Style.Hidden); Value(writer, SearchText(edge.Metadata)); Value(writer, MetadataJson(edge.Metadata)); BooleanValue(writer, collapsedEdgeIds.Contains(edge.Id)); Value(writer, EdgeAccessibleName(edge, sourceNode, targetNode));
            Value(writer, HasSourceArrow(edge) ? "url(#" + edgeMarkerId + ")" : null); Value(writer, HasTargetArrow(edge) ? "url(#" + edgeMarkerId + ")" : null);
            writer.Append(']');
        }

        writer.Append("]}</script>");
    }

    private static void Value(StringBuilder writer, string? value) {
        writer.Append(',');
        writer.Append(value == null ? "null" : JsonString(value));
    }

    private static void NumberValue(StringBuilder writer, double value) {
        writer.Append(',');
        writer.Append(Number(value));
    }

    private static void NullableNumberValue(StringBuilder writer, int? value) {
        writer.Append(',');
        writer.Append(value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "null");
    }

    private static void NullableNumberValue(StringBuilder writer, double? value) {
        writer.Append(',');
        writer.Append(value.HasValue ? Number(value.Value) : "null");
    }

    private static void BooleanValue(StringBuilder writer, bool value) {
        writer.Append(',');
        writer.Append(value ? "true" : "false");
    }
}

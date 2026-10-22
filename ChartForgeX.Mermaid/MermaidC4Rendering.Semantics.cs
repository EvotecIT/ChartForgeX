using System;
using System.Globalization;
using System.Linq;
using ChartForgeX.Topology;

namespace ChartForgeX.Mermaid;

public static partial class MermaidC4Rendering {
    private static string? RelationshipLabel(MermaidC4Document document, MermaidC4Relationship relationship, int index) =>
        string.Equals(document.DiagramType, "C4Dynamic", StringComparison.OrdinalIgnoreCase) || relationship.Kind == "relindex"
            ? (index + 1).ToString(CultureInfo.InvariantCulture) + ": " + relationship.Label : relationship.Label;

    private static void ApplyC4Styles(TopologyChart chart, MermaidC4Document document) {
        foreach (var style in document.ElementStyles) {
            var node = chart.Nodes.FirstOrDefault(item => item.Id == style.Name);
            var group = chart.Groups.FirstOrDefault(item => item.Id == style.Name);
            foreach (var entry in style.Styles) {
                if (node != null) { if (entry.Key == "fill") node.BackgroundColor = entry.Value; if (entry.Key == "color") node.LabelColor = entry.Value; if (entry.Key == "stroke") node.Color = entry.Value; }
                if (group != null) { if (entry.Key == "fill") group.BackgroundColor = entry.Value; if (entry.Key == "color") group.LabelColor = entry.Value; if (entry.Key == "stroke") group.Color = entry.Value; }
            }
        }
        foreach (var style in document.RelationshipStyles) foreach (var edge in chart.Edges.Where(item => item.SourceNodeId == style.SourceAlias && item.TargetNodeId == style.TargetAlias)) {
            if (style.LineColor != null) edge.Color = style.LineColor;
            if (style.TextColor != null) edge.LabelColor = style.TextColor;
            if (style.OffsetX.HasValue) edge.LabelOffsetX = style.OffsetX.Value;
            if (style.OffsetY.HasValue) edge.LabelOffsetY = style.OffsetY.Value;
        }
    }
}

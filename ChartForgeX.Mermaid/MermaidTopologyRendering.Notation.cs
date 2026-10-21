using System;
using ChartForgeX.Topology;

namespace ChartForgeX.Mermaid;

public static partial class MermaidTopologyRendering {
    private static TopologyLayoutDirection StateDirection(MermaidStateDocument document, MermaidStateNode node, TopologyLayoutDirection fallback) {
        var visited = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        var parentId = node.ParentId;
        while (parentId != null && visited.Add(parentId)) {
            var parent = document.States.Find(state => state.Id == parentId);
            if (parent == null) break;
            if (parent.Direction != null) return DiagramDirection(parent.Direction, fallback);
            parentId = parent.ParentId;
        }
        return fallback;
    }

    private static void AddClassNamespaces(TopologyChart chart, MermaidClassDocument document) {
        var namespaces = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        foreach (var item in document.Classes) {
            if (item.Namespace == null) continue;
            string? parent = null;
            foreach (var part in item.Namespace.Split('.')) {
                var id = parent == null ? part : parent + "." + part;
                if (namespaces.Add(id)) {
                    chart.AddAutoGroup(id, part);
                    chart.Groups[chart.Groups.Count - 1].ParentGroupId = parent;
                }
                parent = id;
            }
        }
    }

    private static double MemberWidth(MermaidClassNode item) {
        var length = (item.Label ?? item.Id).Length;
        foreach (var member in item.Members) length = Math.Max(length, MermaidNotationText.Generics(member.Text).Length);
        return Math.Max(180, Math.Min(600, 28 + length * 6.5));
    }

    private static string EntityAttributeText(MermaidEntityAttribute attribute) =>
        attribute.Type + " " + attribute.Name + (string.IsNullOrWhiteSpace(attribute.Key) ? "" : "  " + attribute.Key) +
        (string.IsNullOrWhiteSpace(attribute.Comment) ? "" : "  " + attribute.Comment);

    private static double EntityWidth(MermaidEntityNode item) {
        var length = (item.Label ?? item.Id).Length;
        foreach (var attribute in item.Attributes) length = Math.Max(length, EntityAttributeText(attribute).Length);
        return Math.Max(180, Math.Min(600, 28 + length * 6.5));
    }

    private static VisualLinkDirection ClassDirection(string connector) {
        var source = ClassMarker(connector, true) != TopologyMarkerKind.None;
        var target = ClassMarker(connector, false) != TopologyMarkerKind.None;
        return source && target ? VisualLinkDirection.Bidirectional : source ? VisualLinkDirection.Backward : target ? VisualLinkDirection.Forward : VisualLinkDirection.None;
    }

    private static TopologyMarkerKind ClassMarker(string connector, bool source) {
        if (source ? connector.StartsWith("<|", StringComparison.Ordinal) : connector.EndsWith("|>", StringComparison.Ordinal)) return TopologyMarkerKind.OpenTriangle;
        if (source ? connector.StartsWith("*", StringComparison.Ordinal) : connector.EndsWith("*", StringComparison.Ordinal)) return TopologyMarkerKind.Diamond;
        if (source ? connector.StartsWith("o", StringComparison.Ordinal) : connector.EndsWith("o", StringComparison.Ordinal)) return TopologyMarkerKind.OpenDiamond;
        if (source ? connector.StartsWith("<", StringComparison.Ordinal) : connector.EndsWith(">", StringComparison.Ordinal)) return TopologyMarkerKind.Arrow;
        return TopologyMarkerKind.None;
    }

    private static TopologyMarkerKind CardinalityMarker(string connector, bool source) {
        var end = source ? connector.Substring(0, 2) : connector.Substring(connector.Length - 2);
        if (end == "||") return TopologyMarkerKind.ExactlyOne;
        if (end.IndexOf('o') >= 0) return end.IndexOf('|') >= 0 ? TopologyMarkerKind.ZeroOrOne : TopologyMarkerKind.ZeroOrMany;
        return TopologyMarkerKind.OneOrMany;
    }

    private static void AddNotes(TopologyChart chart, System.Collections.Generic.IEnumerable<MermaidDiagramNote> notes) {
        var ids = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        foreach (var node in chart.Nodes) ids.Add(node.Id);
        foreach (var group in chart.Groups) ids.Add(group.Id);
        foreach (var edge in chart.Edges) ids.Add(edge.Id);
        var index = 0;
        foreach (var note in notes) {
            string id;
            do { id = "mermaid-note-" + (index++).ToString(System.Globalization.CultureInfo.InvariantCulture); } while (!ids.Add(id));
            chart.Annotations.Add(new TopologyAnnotation(id, note.Text, note.NodeId, note.LeftOfNode ? TopologyAnnotationSide.Left : TopologyAnnotationSide.Right));
        }
    }
}

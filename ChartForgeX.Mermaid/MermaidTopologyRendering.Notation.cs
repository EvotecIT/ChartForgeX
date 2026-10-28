using System;
using System.Linq;
using ChartForgeX.Topology;

namespace ChartForgeX.Mermaid;

public static partial class MermaidTopologyRendering {
    private static double MemberWidth(MermaidClassNode item) {
        var length = CaptionWidth(MermaidLabelText.DisplayCaption(item.Label ?? item.Id));
        foreach (var member in item.Members) length = Math.Max(length, CaptionWidth(MermaidLabelText.DisplayCaption(member.DisplayText)));
        return Math.Max(180, Math.Min(600, 28 + length * 6.5));
    }

    private static string EntityAttributeText(MermaidEntityAttribute attribute) =>
        attribute.Type + " " + attribute.Name + (string.IsNullOrWhiteSpace(attribute.Key) ? "" : "  " + attribute.Key) +
        (string.IsNullOrWhiteSpace(attribute.Comment) ? "" : "  " + MermaidLabelText.EntityCaption(attribute.Comment!));

    private static ChartForgeX.Typography.TextContent EntityAttributeContent(MermaidEntityAttribute attribute) {
        var prefix = attribute.Type + " " + attribute.Name + (string.IsNullOrWhiteSpace(attribute.Key) ? "" : "  " + attribute.Key);
        if (string.IsNullOrWhiteSpace(attribute.Comment)) return ChartForgeX.Typography.TextContent.Plain(prefix);
        var comment = MermaidInlineCaption.Content(attribute.Comment!, markdownByDefault: true);
        return comment.IsStructured ? ChartForgeX.Typography.TextContent.FromRuns(
            new[] { new ChartForgeX.Typography.TextContentRun(prefix + "  ") }.Concat(comment.Runs))
            : ChartForgeX.Typography.TextContent.Plain(prefix + "  " + comment.PlainText);
    }

    private static ChartForgeX.Typography.TextContent? RelationshipContent(string? source, bool entity = false) {
        if (source == null) return null;
        var text = source.Length >= 2 && source[0] == '"' && source[source.Length - 1] == '"'
            ? MermaidParserUtilities.Unquote(source) : source;
        return MermaidInlineCaption.Content(text, markdownByDefault: entity);
    }

    private static double EntityWidth(MermaidEntityNode item) {
        var length = CaptionWidth(MermaidLabelText.EntityCaption(item.Label ?? item.Id));
        foreach (var attribute in item.Attributes) length = Math.Max(length, CaptionWidth(EntityAttributeText(attribute)));
        return Math.Max(180, Math.Min(600, 28 + length * 6.5));
    }

    private static string? DisplayRelationshipCaption(string? source, bool entity = false) {
        if (source == null) return null;
        var text = source.Length >= 2 && source[0] == '"' && source[source.Length - 1] == '"'
            ? MermaidParserUtilities.Unquote(source) : source;
        return entity ? MermaidLabelText.EntityCaption(text) : MermaidLabelText.DisplayCaption(text);
    }

    private static int CaptionWidth(string text) {
        var length = 0;
        foreach (var line in text.Split('\n')) length = Math.Max(length, line.Length);
        return length;
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

    internal static void AddNotes(TopologyChart chart, System.Collections.Generic.IEnumerable<MermaidDiagramNote> notes) {
        var ids = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        foreach (var node in chart.Nodes) ids.Add(node.Id);
        foreach (var group in chart.Groups) ids.Add(group.Id);
        foreach (var edge in chart.Edges) ids.Add(edge.Id);
        var source = new System.Collections.Generic.List<MermaidDiagramNote>(notes);
        // Reserve authored identities first so an earlier attached note cannot consume one.
        foreach (var note in source) if (note.Id != null && !ids.Add(note.Id))
            throw new ArgumentException("Diagram note '" + note.Id + "' shares an existing graph or note identity.", nameof(notes));
        var index = 0;
        foreach (var note in source) {
            var id = note.Id;
            if (id == null) {
                do { id = "mermaid-note-" + (index++).ToString(System.Globalization.CultureInfo.InvariantCulture); } while (!ids.Add(id));
            }
            chart.Annotations.Add(new TopologyAnnotation(id, note.Text, note.NodeId, note.LeftOfNode ? TopologyAnnotationSide.Left : TopologyAnnotationSide.Right));
        }
    }
}

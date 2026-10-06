using System;
using ChartForgeX.Topology;

namespace ChartForgeX.Mermaid;

public static partial class MermaidTopologyRendering {
    private static double MemberWidth(MermaidClassNode item) {
        var length = (item.Label ?? item.Id).Length;
        foreach (var member in item.Members) length = Math.Max(length, member.Text.Length);
        return Math.Max(180, Math.Min(600, 28 + length * 6.5));
    }

    private static string EntityAttributeText(MermaidEntityAttribute attribute) =>
        attribute.Type + " " + attribute.Name + (string.IsNullOrWhiteSpace(attribute.Key) ? "" : "  " + attribute.Key) +
        (string.IsNullOrWhiteSpace(attribute.Comment) ? "" : "  " + attribute.Comment);

    private static double EntityWidth(MermaidEntityNode item) {
        var length = item.Id.Length;
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
}

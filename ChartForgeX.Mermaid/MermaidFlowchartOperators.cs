using ChartForgeX.Topology;

namespace ChartForgeX.Mermaid;

/// <summary>Reads one link token and maps its endpoint semantics independently of caption text.</summary>
internal static class MermaidFlowchartOperators {
    public static int ReadEnd(string text, int start) {
        var end = start;
        if (end < text.Length && (text[end] == '<' || text[end] == 'o' || text[end] == 'x')) end++;
        var body = end;
        while (end < text.Length && (text[end] == '-' || text[end] == '.' || text[end] == '=')) end++;
        if (end == body) return start;
        // A single endpoint belongs to the link; a following o/x starts the target id.
        if (end < text.Length && (text[end] == '>' || text[end] == 'o' || text[end] == 'x')) end++;
        return end;
    }

    public static VisualLinkDirection Direction(string edgeOperator) {
        var source = Marker(edgeOperator, true) != TopologyMarkerKind.None;
        var target = Marker(edgeOperator, false) != TopologyMarkerKind.None;
        return source && target ? VisualLinkDirection.Bidirectional : source ? VisualLinkDirection.Backward
            : target ? VisualLinkDirection.Forward : VisualLinkDirection.None;
    }

    public static TopologyMarkerKind Marker(string edgeOperator, bool source) {
        if (edgeOperator.Length == 0) return TopologyMarkerKind.None;
        var marker = edgeOperator[source ? 0 : edgeOperator.Length - 1];
        if (source) {
            var target = edgeOperator[edgeOperator.Length - 1];
            // Circle/cross sources are meaningful only in matching double-ended links.
            if ((marker == 'o' || marker == 'x') && marker != target) return TopologyMarkerKind.None;
            if (marker == '<' && (target == 'o' || target == 'x')) return TopologyMarkerKind.None;
        }
        if (marker == (source ? '<' : '>')) return TopologyMarkerKind.Arrow;
        return marker == 'o' ? TopologyMarkerKind.Circle : marker == 'x' ? TopologyMarkerKind.Cross : TopologyMarkerKind.None;
    }
}

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

    /// <summary>Reads the open half of a labelled link without consuming a complete link token.</summary>
    public static int ReadLabelPrefixEnd(string text, int start) {
        var body = start;
        if (body < text.Length && (text[body] == '<' || text[body] == 'o' || text[body] == 'x')) body++;
        if (body + 1 >= text.Length) return start;
        var first = text[body]; var second = text[body + 1];
        if (!((first == '-' && (second == '-' || second == '.')) || (first == '=' && second == '='))) return start;
        var end = body + 2;
        if (end < text.Length && (text[end] == '-' || text[end] == '.' || text[end] == '='
            || text[end] == '>' || text[end] == 'o' || text[end] == 'x')) return start;
        return end;
    }

    /// <summary>Reads the closing half in the same normal, thick or dotted link family.</summary>
    public static int ReadLabelSuffixEnd(string text, int start, char family) {
        var end = start;
        if (family == '.') {
            if (end < text.Length && text[end] == '-') end++;
            var dots = end;
            while (end < text.Length && text[end] == '.') end++;
            if (end == dots || end >= text.Length || text[end] != '-') return start;
            end++;
        } else {
            while (end < text.Length && text[end] == family) end++;
            if (end - start < 2) return start;
        }
        if (end < text.Length && (text[end] == '>' || text[end] == 'o' || text[end] == 'x')) return end + 1;
        return family == '.' || end - start >= 3 ? end : start;
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

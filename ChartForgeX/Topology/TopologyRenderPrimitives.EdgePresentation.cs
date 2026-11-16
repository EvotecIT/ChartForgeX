using System;

namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    internal const double EdgeStripeLength = 12, EdgeChipHeight = 16, EdgeChipFontSize = 9.5, EdgeChipGap = 4, EdgeChipPadding = 6, EdgeChipIconSize = 10;
    internal const string UplinkMarker = "↑";

    /// <summary>Returns the edge tooltip with its evidence appended, so static and interactive hosts show the same text.</summary>
    internal static string? EdgeTooltip(TopologyEdge edge) {
        if (edge.Evidence == TopologyEdgeEvidence.Unspecified) return edge.Tooltip;
        var evidence = edge.Evidence == TopologyEdgeEvidence.Assumed ? "Assumed link" : "Confirmed link";
        if (!string.IsNullOrWhiteSpace(edge.EvidenceText)) evidence += ": " + edge.EvidenceText!.Trim();
        return string.IsNullOrWhiteSpace(edge.Tooltip) ? evidence : edge.Tooltip + Environment.NewLine + evidence;
    }

    /// <summary>Prefixes the child endpoint label with the uplink marker. Applied once to the prepared layout copy.</summary>
    internal static void ApplyUplinkMarkers(TopologyChart chart) {
        foreach (var edge in chart.Edges) {
            if (edge.UplinkEnd == TopologyEdgeEnd.Source) edge.SourceLabel = WithUplink(edge.SourceLabel);
            else if (edge.UplinkEnd == TopologyEdgeEnd.Target) edge.TargetLabel = WithUplink(edge.TargetLabel);
        }
    }

    private static string WithUplink(string? label) {
        if (string.IsNullOrWhiteSpace(label)) return UplinkMarker;
        return label!.StartsWith(UplinkMarker, StringComparison.Ordinal) ? label : UplinkMarker + " " + label.Trim();
    }
}

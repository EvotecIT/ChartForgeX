namespace ChartForgeX.Mermaid;

public sealed partial class MermaidParser {
    // These are deliberate source-to-native semantic projections, independent of
    // configurable appearance or the presence of an unsupported statement.
    private static void ReportApproximateMapping(MermaidDocument document, MermaidParseResult<MermaidDocument> result) {
        if (result.HasErrors) return;
        var limitation = document.Kind switch {
            MermaidDiagramKind.Journey => "Journey task scores render as bars; actor lanes and journey notation remain in the source model.",
            MermaidDiagramKind.Timeline => "Timeline periods render as ordinal event ranges; their labels are not parsed into calendar durations.",
            MermaidDiagramKind.Venn => "Venn sets and intersections retain their authored values, but their drawn areas are not proportional to those values.",
            MermaidDiagramKind.C4 => "C4 elements and relationships render through native topology; specialized C4 notation and layout are approximate.",
            _ => null
        };
        if (limitation != null) MermaidParserUtilities.Add(result, document.HeaderSpan, MermaidDiagnosticSeverity.Warning,
            limitation, MermaidDiagnosticCodes.ApproximateMapping);
    }
}

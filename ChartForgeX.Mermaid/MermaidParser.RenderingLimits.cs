namespace ChartForgeX.Mermaid;

public sealed partial class MermaidParser {
    // These are deliberate source-to-native semantic projections, independent of
    // configurable appearance or the presence of an unsupported statement.
    private static void ReportApproximateMapping(MermaidDocument document, MermaidParseResult<MermaidDocument> result) {
        if (result.HasErrors) return;
        var limitation = document.Kind switch {
            MermaidDiagramKind.C4 => "C4 elements and relationships render through native topology; specialized C4 notation and layout are approximate.",
            _ => null
        };
        if (limitation != null) MermaidParserUtilities.Add(result, document.HeaderSpan, MermaidDiagnosticSeverity.Warning,
            limitation, MermaidDiagnosticCodes.ApproximateMapping);
    }
}

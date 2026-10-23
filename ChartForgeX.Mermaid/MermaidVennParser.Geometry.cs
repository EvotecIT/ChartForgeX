using System;
using System.Linq;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidVennParser {
    private static void ValidateNativeMemberships(MermaidVennDocument document, MermaidParseResult<MermaidDocument> result) {
        try {
            var layout = VisualBlockRendering.VennMembershipGeometry(document.ToVennDiagramBlock());
            if (!layout.IsAreaAccurate) MermaidParserUtilities.Add(result, document.HeaderSpan, MermaidDiagnosticSeverity.Warning,
                "The requested three-set overlaps cannot be matched exactly by this circular layout. The native layout exposes requested and actual region areas; maximum error relative to the largest set is "
                + layout.MaximumRelativeAreaError.ToString("P2", System.Globalization.CultureInfo.InvariantCulture) + ".", MermaidDiagnosticCodes.ApproximateMapping);
        } catch (VennDiagramValidationException exception) {
            var region = document.Intersections.LastOrDefault(item => item.SetIds.Count == exception.SetIds.Count && exception.SetIds.All(item.SetIds.Contains));
            var span = region?.Span ?? document.Intersections.LastOrDefault()?.Span ?? document.HeaderSpan;
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, exception.Message, MermaidDiagnosticCodes.InvalidStatement);
        }
    }
}

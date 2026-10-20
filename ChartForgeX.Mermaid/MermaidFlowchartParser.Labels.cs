using System;
using System.Text.RegularExpressions;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidFlowchartParser {
    private static void AddLabelDiagnostics(MermaidFlowchartDocument document, MermaidParseResult<MermaidDocument> result) {
        foreach (var node in document.Nodes) Check(node.Text, node.Span);
        foreach (var edge in document.Edges) Check(edge.Label, edge.Span);
        foreach (var group in document.Subgraphs) Check(group.Title, group.Span);

        void Check(string? label, MermaidSourceSpan span) {
            if (label == null) return;
            if (MermaidFlowchartText.IsMarkdown(label)) MermaidParserUtilities.Add(result, span,
                MermaidDiagnosticSeverity.Warning, "Flowchart Markdown labels render as plain text; emphasis uses the native caption style.",
                MermaidDiagnosticCodes.ApproximateMapping);
            var withoutBreaks = Regex.Replace(label, "<br\\s*/?>", "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            // UML stereotypes such as <<include>> are notation, not HTML tags.
            if (Regex.IsMatch(withoutBreaks, "(?<!<)</?[a-zA-Z][^>]*>(?!>)") || label.IndexOf("$$", StringComparison.Ordinal) >= 0)
                MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning,
                    "Flowchart HTML and math labels are retained and displayed as literal text.", MermaidDiagnosticCodes.UnsupportedStatement);
        }
    }
}

using System;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidFlowchartParser {
    /// <summary>Dispatches metadata to an already declared edge identity before the ordinary node parser.</summary>
    private static bool TryParseEdgeProperties(MermaidFlowchartDocument document, string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        if (!MermaidFlowchartCurves.Applies(document.Kind)) return false;
        var position = 0; SkipWhitespace(text, ref position); var start = position;
        while (position < text.Length && IsNodeIdCharacter(text[position])) position++;
        if (position == start) return false;
        var id = text.Substring(start, position - start); SkipWhitespace(text, ref position);
        if (!StartsWith(text, position, "@{")) return false;
        var edge = document.Edges.Find(item => string.Equals(item.Id, id, StringComparison.Ordinal));
        if (edge == null) return false; // Before declaration, the incumbent node/identity policy remains authoritative.
        ReadFlowchartProperties(text, ref position, span, result, (property, propertySpan) => {
            if (!TryReadFlowchartProperty(property, propertySpan, result, "edge", out var key, out var value, out var scalar, out _)) return;
            edge.Properties[key] = value; edge.PropertySpans[key] = propertySpan; edge.PropertyTokens[key] = scalar;
            if (key != "curve" || !MermaidFlowchartCurves.TryKind(value, out _))
                MermaidParserUtilities.Add(result, propertySpan, MermaidDiagnosticSeverity.Warning,
                    "Flowchart edge property '" + key + "' is retained without applying an unsupported static capability.", MermaidDiagnosticCodes.UnsupportedStatement);
        }, "edge");
        ParseClassSuffix(text, ref position, edge.Classes, span, result); SkipWhitespace(text, ref position);
        if (position < text.Length) MermaidParserUtilities.Add(result, MermaidFlowchartText.AtOffset(text, span, position, text.Length - position),
            MermaidDiagnosticSeverity.Error, "Flowchart edge metadata has unsupported trailing syntax.", MermaidDiagnosticCodes.InvalidStatement);
        return true;
    }
}

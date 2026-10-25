namespace ChartForgeX.Mermaid;

internal static partial class MermaidFlowchartParser {
    /// <summary>Reads an optional authored identity before the existing canonical link token.</summary>
    private static bool TryParseNamedEdgeOperator(string text, ref int position, MermaidSourceSpan span,
        MermaidFlowchartInlineLabels inlineLabels, bool useCase, out string edgeOperator, out string? label,
        out string? edgeId, out MermaidSourceSpan? identitySpan) {
        edgeId = null; identitySpan = null;
        var saved = position; SkipWhitespace(text, ref position);
        var start = position;
        while (position < text.Length && (text[position] is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '_')) position++;
        if (position > start && position < text.Length && text[position] == '@') {
            edgeId = text.Substring(start, position - start);
            identitySpan = MermaidFlowchartText.AtOffset(text, span, start, position - start);
            position++; SkipWhitespace(text, ref position);
        } else position = saved;
        if (useCase && StartsWith(text, position, "--|>")) {
            edgeOperator = "--|>"; label = null; position += 4; return true;
        }
        if (TryParseEdgeOperator(text, ref position, inlineLabels, out edgeOperator, out label)) return true;
        position = saved; edgeId = null; identitySpan = null; return false;
    }
}

using System;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidFlowchartParser {
    private static string ReadLinkInterpolation(MermaidFlowchartLinkStyle style, string declarations, string statement,
        MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        var position = 0; SkipWhitespace(declarations, ref position); const string token = "interpolate";
        if (!StartsWith(declarations, position, token) || position + token.Length < declarations.Length
            && !char.IsWhiteSpace(declarations[position + token.Length])) return declarations;
        position += token.Length; SkipWhitespace(declarations, ref position); var start = position;
        while (position < declarations.Length && declarations[position] != ',' && !char.IsWhiteSpace(declarations[position])) position++;
        var choice = declarations.Substring(start, position - start); SkipWhitespace(declarations, ref position);
        var propertyOffset = statement.Length - declarations.Length + start;
        var choiceSpan = MermaidFlowchartText.AtOffset(statement, span, propertyOffset, Math.Max(1, choice.Length));
        if (choice.Length == 0 || position < declarations.Length && declarations[position] != ',') {
            MermaidParserUtilities.Add(result, choiceSpan, MermaidDiagnosticSeverity.Error,
                "Flowchart link interpolation requires a name followed by an optional comma and paint declarations.", MermaidDiagnosticCodes.InvalidStatement);
            return string.Empty;
        }
        style.Interpolation = choice; style.InterpolationSpan = choiceSpan;
        if (!MermaidFlowchartCurves.TryKind(choice, out _)) MermaidParserUtilities.Add(result, choiceSpan, MermaidDiagnosticSeverity.Warning,
            "Flowchart interpolation '" + choice + "' is retained with its unsupported source token.", MermaidDiagnosticCodes.UnsupportedStatement);
        return position == declarations.Length ? string.Empty : declarations.Substring(position + 1);
    }
}

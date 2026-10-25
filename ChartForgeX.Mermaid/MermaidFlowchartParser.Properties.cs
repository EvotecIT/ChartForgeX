using System;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidFlowchartParser {
    private static void ReadFlowchartProperties(string text, ref int position, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result, Action<string,MermaidSourceSpan> add, string owner) {
        var start = position;
        position += 2;
        var propertyStart = position;
        char quote = '\0';
        while (position < text.Length) {
            var ch = text[position];
            if (MermaidFlowchartText.ReadQuotedCharacter(text, ref position, ref quote, metadata: true)) { position++; continue; }
            if (ch == ',' || ch == '}') {
                add(text.Substring(propertyStart, position - propertyStart), MermaidFlowchartText.AtOffset(text, span, propertyStart, position - propertyStart));
                position++;
                if (ch == '}') return;
                propertyStart = position;
                continue;
            }
            position++;
        }
        MermaidParserUtilities.Add(result, MermaidFlowchartText.AtOffset(text, span, start, text.Length - start), MermaidDiagnosticSeverity.Error, "Flowchart " + owner + " metadata must close with '}'.");
    }

    private static bool TryReadFlowchartProperty(string property, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result,
        string owner, out string key, out string value, out string scalar, out int scalarStart) {
        key = value = scalar = string.Empty; scalarStart = 0;
        if (string.IsNullOrWhiteSpace(property)) return false;
        var colon = property.IndexOf(':');
        if (colon <= 0) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Flowchart " + owner + " properties require 'key: value' syntax.");
            return false;
        }
        scalar = property.Substring(0, colon);
        scalarStart = scalar.Length - scalar.TrimStart().Length;
        scalar = scalar.Trim();
        try {
            key = ReadPropertyScalar(scalar);
            scalar = property.Substring(colon + 1);
            scalarStart = colon + 1 + scalar.Length - scalar.TrimStart().Length;
            scalar = scalar.Trim();
            value = ReadPropertyScalar(scalar);
        } catch (ArgumentException exception) {
            MermaidParserUtilities.Add(result, new MermaidSourceSpan(span.Line, span.Column + scalarStart, Math.Max(1, scalar.Length)),
                MermaidDiagnosticSeverity.Error, exception.Message);
            return false;
        }
        return true;
    }
}

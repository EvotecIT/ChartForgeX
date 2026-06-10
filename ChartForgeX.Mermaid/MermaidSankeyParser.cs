using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ChartForgeX.Mermaid;

internal static class MermaidSankeyParser {
    public static void ParseStatements(MermaidSankeyDocument document, string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        for (var index = Math.Max(0, startLine - 1); index < lines.Length; index++) {
            var raw = lines[index];
            var trimmed = raw.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith("%%", StringComparison.Ordinal)) continue;

            var span = new MermaidSourceSpan(index + 1, LeadingWhitespace(raw) + 1, trimmed.Length);
            document.Statements.Add(new MermaidRawStatement(trimmed, span));
            ParseLink(document, trimmed, span, result);
        }

        if (document.Links.Count == 0) Add(result, document.HeaderSpan.Line, document.HeaderSpan.Column, document.HeaderSpan.Length, MermaidDiagnosticSeverity.Error, "Mermaid Sankey diagrams require at least one CSV link row.");
    }

    private static void ParseLink(MermaidSankeyDocument document, string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        var columns = ParseCsvRow(text, span, result);
        if (columns == null) return;
        if (columns.Count != 3) {
            Add(result, span.Line, span.Column, span.Length, MermaidDiagnosticSeverity.Error, "Mermaid Sankey CSV rows must contain exactly three columns: source,target,value.");
            return;
        }

        var source = columns[0].Trim();
        var target = columns[1].Trim();
        var valueText = columns[2].Trim();
        if (source.Length == 0 || target.Length == 0) {
            Add(result, span.Line, span.Column, span.Length, MermaidDiagnosticSeverity.Error, "Mermaid Sankey source and target columns must not be empty.");
            return;
        }

        if (string.Equals(source, target, StringComparison.Ordinal)) {
            Add(result, span.Line, span.Column, span.Length, MermaidDiagnosticSeverity.Error, "Mermaid Sankey source and target columns must be distinct.");
            return;
        }

        if (!double.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || double.IsNaN(value) || double.IsInfinity(value) || value <= 0) {
            Add(result, span.Line, span.Column, valueText.Length, MermaidDiagnosticSeverity.Error, "Mermaid Sankey values must be positive numbers greater than zero.");
            return;
        }

        document.Links.Add(new MermaidSankeyLink(source, target, value, span));
    }

    private static List<string>? ParseCsvRow(string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        var columns = new List<string>();
        var current = new StringBuilder();
        var inQuote = false;
        for (var i = 0; i < text.Length; i++) {
            var ch = text[i];
            if (inQuote) {
                if (ch == '"') {
                    if (i + 1 < text.Length && text[i + 1] == '"') {
                        current.Append('"');
                        i++;
                    } else {
                        inQuote = false;
                    }
                } else {
                    current.Append(ch);
                }

                continue;
            }

            if (ch == '"') {
                if (current.ToString().Trim().Length > 0) {
                    Add(result, span.Line, span.Column + i, 1, MermaidDiagnosticSeverity.Error, "Mermaid Sankey CSV quotes must start at the beginning of a field.");
                    return null;
                }

                inQuote = true;
                continue;
            }

            if (ch == ',') {
                columns.Add(current.ToString());
                current.Length = 0;
                continue;
            }

            current.Append(ch);
        }

        if (inQuote) {
            Add(result, span.Line, span.Column, span.Length, MermaidDiagnosticSeverity.Error, "Mermaid Sankey CSV quoted fields must close with a double quote.");
            return null;
        }

        columns.Add(current.ToString());
        return columns;
    }

    private static int LeadingWhitespace(string text) {
        var count = 0;
        while (count < text.Length && char.IsWhiteSpace(text[count])) count++;
        return count;
    }

    private static void Add<TDocument>(MermaidParseResult<TDocument> result, int line, int column, int length, MermaidDiagnosticSeverity severity, string message) where TDocument : MermaidDocument {
        result.Diagnostics.Add(new MermaidDiagnostic {
            Span = new MermaidSourceSpan(line, column, length),
            Severity = severity,
            Message = message
        });
    }
}

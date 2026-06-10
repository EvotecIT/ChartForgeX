using System;
using System.Collections.Generic;
using System.Globalization;

namespace ChartForgeX.Mermaid;

internal static class MermaidPieParser {
    public static void ParseStatements(MermaidPieDocument document, string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        for (var index = Math.Max(0, startLine - 1); index < lines.Length; index++) {
            var raw = lines[index];
            var trimmed = raw.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith("%%", StringComparison.Ordinal)) continue;

            var span = new MermaidSourceSpan(index + 1, LeadingWhitespace(raw) + 1, trimmed.Length);
            document.Statements.Add(new MermaidRawStatement(trimmed, span));

            if (StartsWithKeyword(trimmed, "showData")) {
                document.ShowData = true;
                continue;
            }

            if (StartsWithKeyword(trimmed, "title")) {
                document.Title = Unquote(trimmed.Substring(5).Trim());
                continue;
            }

            ParseSlice(document, result, trimmed, span);
        }

        if (document.Slices.Count == 0) Add(result, document.HeaderSpan.Line, document.HeaderSpan.Column, document.HeaderSpan.Length, MermaidDiagnosticSeverity.Error, "Mermaid pie diagrams require at least one positive slice.");
    }

    private static void ParseSlice(MermaidPieDocument document, MermaidParseResult<MermaidDocument> result, string text, MermaidSourceSpan span) {
        var colon = FindSeparator(text);
        if (colon < 0) {
            Add(result, span.Line, span.Column, span.Length, MermaidDiagnosticSeverity.Error, "Pie slice must use Mermaid syntax '\"label\" : positiveNumber'.");
            return;
        }

        var labelText = text.Substring(0, colon).Trim();
        var valueText = text.Substring(colon + 1).Trim();
        if (labelText.Length < 2 || labelText[0] != '"' || labelText[labelText.Length - 1] != '"') {
            Add(result, span.Line, span.Column, labelText.Length, MermaidDiagnosticSeverity.Error, "Pie slice labels must be wrapped in double quotes.");
            return;
        }

        if (!double.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || double.IsNaN(value) || double.IsInfinity(value) || value <= 0) {
            Add(result, span.Line, span.Column + colon + 1, valueText.Length, MermaidDiagnosticSeverity.Error, "Pie slice values must be positive numbers greater than zero.");
            return;
        }

        document.Slices.Add(new MermaidPieSlice(UnescapeQuoted(labelText.Substring(1, labelText.Length - 2)), value, span));
    }

    private static int FindSeparator(string text) {
        var inQuote = false;
        var escaped = false;
        for (var i = 0; i < text.Length; i++) {
            var ch = text[i];
            if (escaped) {
                escaped = false;
                continue;
            }

            if (ch == '\\') {
                escaped = true;
                continue;
            }

            if (ch == '"') {
                inQuote = !inQuote;
                continue;
            }

            if (!inQuote && ch == ':') return i;
        }

        return -1;
    }

    private static bool StartsWithKeyword(string text, string keyword) {
        if (!text.StartsWith(keyword, StringComparison.OrdinalIgnoreCase)) return false;
        return text.Length == keyword.Length || char.IsWhiteSpace(text[keyword.Length]);
    }

    private static string Unquote(string value) {
        if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"') return UnescapeQuoted(value.Substring(1, value.Length - 2));
        return value;
    }

    private static string UnescapeQuoted(string value) {
        if (value.IndexOf('\\') < 0) return value;
        var chars = new List<char>(value.Length);
        var escaped = false;
        foreach (var ch in value) {
            if (escaped) {
                chars.Add(ch);
                escaped = false;
                continue;
            }

            if (ch == '\\') {
                escaped = true;
                continue;
            }

            chars.Add(ch);
        }

        if (escaped) chars.Add('\\');
        return new string(chars.ToArray());
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

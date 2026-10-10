using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ChartForgeX.Mermaid;

internal static class MermaidParserUtilities {
    public static bool IsSkippable(string text) => text.Length == 0 || text.StartsWith("%%", StringComparison.Ordinal);

    public static string StripInlineComment(string text) {
        var quote = '\0';
        var escaped = false;
        for (var index = 0; index < text.Length - 1; index++) {
            var ch = text[index];
            if (quote != '\0' && escaped) {
                escaped = false;
                continue;
            }

            if (quote != '\0' && ch == '\\') {
                escaped = true;
                continue;
            }

            if ((ch == '"' || ch == '\'' || ch == '`') && quote == '\0') {
                quote = ch;
                continue;
            }

            if (quote != '\0' && ch == quote) {
                quote = '\0';
                continue;
            }

            if (quote == '\0' && ch == '%' && text[index + 1] == '%') return text.Substring(0, index).TrimEnd();
        }

        return text;
    }

    public static int LeadingWhitespace(string text) {
        var count = 0;
        while (count < text.Length && char.IsWhiteSpace(text[count])) count++;
        return count;
    }

    public static string Unquote(string value) {
        if (value == null) throw new ArgumentNullException(nameof(value));
        var trimmed = value.Trim();
        if (trimmed.Length >= 2 && ((trimmed[0] == '"' && trimmed[trimmed.Length - 1] == '"') || (trimmed[0] == '`' && trimmed[trimmed.Length - 1] == '`'))) {
            return trimmed.Substring(1, trimmed.Length - 2);
        }

        return trimmed;
    }

    public static bool TryBracketed(string text, out string id, out string label, out string suffix) {
        id = string.Empty;
        label = string.Empty;
        suffix = string.Empty;
        var start = text.IndexOf("[", StringComparison.Ordinal);
        if (start <= 0) return false;
        var end = text.LastIndexOf(']');
        if (end <= start) return false;
        id = text.Substring(0, start).Trim();
        label = text.Substring(start + 1, end - start - 1).Trim();
        suffix = text.Substring(end + 1).Trim();
        return id.Length > 0 && label.Length > 0;
    }

    public static IReadOnlyList<string> SplitCsvLike(string text) {
        var parts = new List<string>();
        var current = string.Empty;
        var quote = '\0';
        for (var index = 0; index < text.Length; index++) {
            var ch = text[index];
            if ((ch == '"' || ch == '\'' || ch == '`') && quote == '\0') {
                quote = ch;
                current += ch;
                continue;
            }

            if (quote != '\0' && ch == quote) {
                quote = '\0';
                current += ch;
                continue;
            }

            if (ch == ',' && quote == '\0') {
                parts.Add(current.Trim());
                current = string.Empty;
                continue;
            }

            current += ch;
        }

        if (current.Length > 0 || text.EndsWith(",", StringComparison.Ordinal)) parts.Add(current.Trim());
        return parts;
    }

    public static string StableId(string prefix, int index) => prefix + "-" + index.ToString(CultureInfo.InvariantCulture);

    public static bool StartsStatement(string text, string keyword, bool ignoreCase = false) => text.StartsWith(keyword, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) &&
        text.Length > keyword.Length && char.IsWhiteSpace(text[keyword.Length]);

    public static bool IsMultilineStateNote(string text) => Regex.IsMatch(text,
        @"^note\s+(?:left|right)\s+of\s+[^:\s]+\s*$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public static bool TryReadDirection(string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result, out string? direction, bool ignoreCase = false) {
        direction = null;
        if (!StartsStatement(text, "direction", ignoreCase)) return false;
        var value = text.Substring(9).Trim().TrimEnd(';').Trim();
        if (ignoreCase) value = value.ToUpperInvariant();
        if (value == "LR" || value == "RL" || value == "TB" || value == "BT") direction = value;
        else Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid direction must be LR, RL, TB or BT.", MermaidDiagnosticCodes.InvalidStatement);
        return true;
    }

    public static void RetainUnsupported(MermaidDocument document, string text, MermaidSourceSpan span,
        MermaidParseResult<MermaidDocument> result, string feature) {
        document.RawStatements.Add(new MermaidRawStatement(text, span));
        Add(result, span, MermaidDiagnosticSeverity.Warning, "Mermaid " + feature + " is retained without exact native rendering.", MermaidDiagnosticCodes.UnsupportedStatement);
    }

    public static List<int> OriginalLineStarts(string source) {
        var starts = new List<int> { 0 };
        for (var index = 0; index < source.Length; index++) {
            if (source[index] == '\r') {
                if (index + 1 < source.Length && source[index + 1] == '\n') index++;
                starts.Add(index + 1);
            } else if (source[index] == '\n') starts.Add(index + 1);
        }
        return starts;
    }

    public static void Add(MermaidParseResult<MermaidDocument> result, MermaidSourceSpan span, MermaidDiagnosticSeverity severity, string message, string? code = null) {
        result.Diagnostics.Add(new MermaidDiagnostic {
            Code = code ?? string.Empty,
            Severity = severity,
            Message = message,
            Span = span
        });
    }
}

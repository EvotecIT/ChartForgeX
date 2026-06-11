using System;
using System.Collections.Generic;
using System.Globalization;

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

    public static void Add(MermaidParseResult<MermaidDocument> result, MermaidSourceSpan span, MermaidDiagnosticSeverity severity, string message) {
        result.Diagnostics.Add(new MermaidDiagnostic {
            Severity = severity,
            Message = message,
            Span = span
        });
    }
}

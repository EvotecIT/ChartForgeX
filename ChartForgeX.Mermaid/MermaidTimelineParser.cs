using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

internal static class MermaidTimelineParser {
    public static void ParseStatements(MermaidTimelineDocument document, string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        string? currentSection = null;
        MermaidTimelinePeriod? currentPeriod = null;

        for (var index = Math.Max(0, startLine - 1); index < lines.Length; index++) {
            var raw = lines[index];
            var trimmed = raw.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith("%%", StringComparison.Ordinal)) continue;

            var span = new MermaidSourceSpan(index + 1, LeadingWhitespace(raw) + 1, trimmed.Length);
            document.Statements.Add(new MermaidRawStatement(trimmed, span));

            if (StartsWithKeyword(trimmed, "title")) {
                document.Title = trimmed.Substring(5).Trim();
                currentPeriod = null;
                continue;
            }

            if (StartsWithKeyword(trimmed, "section")) {
                currentSection = trimmed.Substring(7).Trim();
                document.Sections.Add(new MermaidTimelineSection(currentSection, span));
                currentPeriod = null;
                continue;
            }

            if (trimmed.StartsWith(":", StringComparison.Ordinal)) {
                if (currentPeriod == null) {
                    Add(result, span.Line, span.Column, span.Length, MermaidDiagnosticSeverity.Error, "Timeline continuation events require a preceding time period.");
                    continue;
                }

                foreach (var eventText in SplitEvents(trimmed.Substring(1))) currentPeriod.Events.Add(new MermaidTimelineEvent(eventText, span));
                continue;
            }

            var parts = SplitEvents(trimmed);
            if (parts.Count < 2) {
                Add(result, span.Line, span.Column, span.Length, MermaidDiagnosticSeverity.Error, "Timeline entries must use Mermaid syntax 'period : event'.");
                currentPeriod = null;
                continue;
            }

            var period = new MermaidTimelinePeriod(parts[0], currentSection, span);
            for (var i = 1; i < parts.Count; i++) period.Events.Add(new MermaidTimelineEvent(parts[i], span));
            document.Periods.Add(period);
            currentPeriod = period;
        }

        if (document.Periods.Count == 0) Add(result, document.HeaderSpan.Line, document.HeaderSpan.Column, document.HeaderSpan.Length, MermaidDiagnosticSeverity.Error, "Mermaid timeline diagrams require at least one time period.");
    }

    private static List<string> SplitEvents(string text) {
        var values = new List<string>();
        var current = new System.Text.StringBuilder();
        var escaped = false;
        for (var i = 0; i < text.Length; i++) {
            var ch = text[i];
            if (escaped) {
                current.Append(ch);
                escaped = false;
                continue;
            }

            if (ch == '\\') {
                escaped = true;
                continue;
            }

            if (ch == ':') {
                AddPart(values, current);
                continue;
            }

            current.Append(ch);
        }

        AddPart(values, current);
        return values;
    }

    private static void AddPart(List<string> values, System.Text.StringBuilder current) {
        var value = current.ToString().Trim();
        current.Length = 0;
        if (value.Length > 0) values.Add(value);
    }

    private static bool StartsWithKeyword(string text, string keyword) {
        if (!text.StartsWith(keyword, StringComparison.OrdinalIgnoreCase)) return false;
        return text.Length == keyword.Length || char.IsWhiteSpace(text[keyword.Length]);
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

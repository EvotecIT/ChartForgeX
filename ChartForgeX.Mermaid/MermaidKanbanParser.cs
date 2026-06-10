using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

internal static class MermaidKanbanParser {
    public static void ParseStatements(MermaidKanbanDocument document, string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        MermaidKanbanColumn? activeColumn = null;
        for (var line = Math.Max(1, startLine); line <= lines.Length; line++) {
            var raw = lines[line - 1];
            var trimmed = MermaidParserUtilities.StripInlineComment(raw.Trim());
            if (MermaidParserUtilities.IsSkippable(trimmed)) continue;
            var indent = MermaidParserUtilities.LeadingWhitespace(raw);
            var span = new MermaidSourceSpan(line, indent + 1, trimmed.Length);
            if (!MermaidParserUtilities.TryBracketed(trimmed, out var id, out var title, out var suffix)) {
                MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning, "Unrecognized kanban statement was retained but not rendered exactly: " + trimmed);
                continue;
            }

            if (indent == 0 || activeColumn == null) {
                activeColumn = new MermaidKanbanColumn(id, title, span);
                document.Columns.Add(activeColumn);
                continue;
            }

            var task = new MermaidKanbanTask(id, title, span);
            ParseMetadata(suffix, task.Metadata);
            activeColumn.Tasks.Add(task);
        }

        if (document.Columns.Count == 0) MermaidParserUtilities.Add(result, document.HeaderSpan, MermaidDiagnosticSeverity.Error, "Mermaid kanban diagrams require at least one column.");
    }

    private static void ParseMetadata(string text, Dictionary<string, string> metadata) {
        var start = text.IndexOf("@{", StringComparison.Ordinal);
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start) return;
        var body = text.Substring(start + 2, end - start - 2).Trim();
        foreach (var part in MermaidParserUtilities.SplitCsvLike(body)) {
            var separator = part.IndexOf(':');
            if (separator < 0) separator = part.IndexOf('=');
            if (separator <= 0) continue;
            var key = part.Substring(0, separator).Trim();
            var value = MermaidParserUtilities.Unquote(part.Substring(separator + 1).Trim());
            if (key.Length > 0) metadata[key] = value;
        }
    }
}

using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidKanbanParser {
    public static void ParseStatements(MermaidKanbanDocument document, string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        MermaidKanbanColumn? activeColumn = null; int? columnIndent = null; int? taskIndent = null;
        var identities = new HashSet<string>(StringComparer.Ordinal);
        for (var line = Math.Max(1, startLine); line <= lines.Length; line++) {
            MermaidResourceGuard.Checkpoint();
            var raw = lines[line - 1]; var text = MermaidParserUtilities.StripInlineComment(raw.Trim());
            if (MermaidParserUtilities.IsSkippable(text)) continue;
            var indent = MermaidParserUtilities.LeadingWhitespace(raw);
            var span = new MermaidSourceSpan(line, indent + 1, text.Length);
            if (!ReadDeclaration(text, span, result, out var id, out var title, out var suffix)) { Retain(); continue; }
            if (!identities.Add(id)) { Invalid(span, result, "Kanban column and task identities must be unique: '" + id + "'."); Retain(); continue; }
            if (columnIndent.HasValue && (indent < columnIndent.Value || indent > columnIndent.Value && taskIndent.HasValue && indent != taskIndent.Value)) {
                Invalid(span, result, "Kanban columns share one indentation; tasks share a deeper indentation per column and cannot nest."); Retain(); continue;
            }
            MermaidResourceGuard.ReserveElements(1, span);
            if (!columnIndent.HasValue || indent == columnIndent.Value) {
                columnIndent = indent; taskIndent = null;
                var column = new MermaidKanbanColumn(id, title, span);
                if (!ReadMetadata(text, suffix, column.Metadata, span, result)) { Retain(); continue; }
                Diagnose(title, column.Metadata, span, result);
                column.MetadataSource = suffix < text.Length ? text.Substring(suffix).Trim() : null;
                document.Columns.Add(column); activeColumn = column;
            } else {
                taskIndent = indent;
                var task = new MermaidKanbanTask(id, title, span);
                if (!ReadMetadata(text, suffix, task.Metadata, span, result)) { Retain(); continue; }
                Diagnose(title, task.Metadata, span, result);
                task.MetadataSource = suffix < text.Length ? text.Substring(suffix).Trim() : null;
                activeColumn!.Tasks.Add(task);
            }
            void Retain() => document.RawStatements.Add(new MermaidRawStatement(text, span));
        }
        if (document.Columns.Count == 0 && !result.HasErrors) Invalid(document.HeaderSpan, result, "Mermaid kanban diagrams require at least one column.");
    }

    private static bool ReadDeclaration(string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result,
        out string id, out string title, out int suffix) {
        id = title = string.Empty; suffix = text.Length; var opening = text.IndexOf('[');
        if (opening <= 0) return Fail("Kanban declarations require id[Title] and optional inline metadata.");
        id = text.Substring(0, opening).Trim();
        if (!MermaidKanbanMetadata.Identity(id)) return Fail("Kanban identities contain only letters, digits, underscore, hyphen or dot.");
        var quote = '\0'; var closing = -1;
        for (var index = opening + 1; index < text.Length; index++) {
            if ((index & 255) == 0) MermaidResourceGuard.Checkpoint();
            if (ReadQuoted(text, ref index, ref quote)) continue;
            if (text[index] == ']') { closing = index; break; }
            if (text[index] == '[') return Fail("Quote literal brackets inside Kanban titles.");
        }
        if (closing < 0 || quote != '\0') return Fail("Kanban titles require balanced brackets and quotes.");
        try { title = Scalar(text.Substring(opening + 1, closing - opening - 1).Trim()); }
        catch (ArgumentException exception) { return Fail(exception.Message); }
        try { MermaidKanbanMetadata.Scalar("label", title); }
        catch (ArgumentException exception) { return Fail(exception.Message); }
        suffix = closing + 1; return true;
        bool Fail(string message) { Invalid(span, result, message); return false; }
    }

    private static void Diagnose(string title, Dictionary<string, string> values, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        var caption = MermaidKanbanMetadata.Get(values, "label") ?? title;
        MermaidLabelText.Diagnose(caption, span, result, "Kanban");
        if (string.IsNullOrWhiteSpace(MermaidLabelText.DisplayCaption(caption))) Invalid(span, result, "Kanban display captions must not be empty.");

    }

    private static void Invalid(MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result, string message) =>
        MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, message, MermaidDiagnosticCodes.InvalidStatement);
}

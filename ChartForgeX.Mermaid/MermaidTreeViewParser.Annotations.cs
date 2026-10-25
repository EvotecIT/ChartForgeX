using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidTreeViewParser {
    private sealed class ParsedTreeRow {
        internal string Label = string.Empty;
        internal string Description = string.Empty;
        internal readonly List<string> Classes = new();
        internal string? Icon;
    }
    private static bool TryAnnotations(string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result, out ParsedTreeRow row) {
        row = new ParsedTreeRow(); var position = 0;
        if (text.Length > 0 && text[0] == '"') {
            position = ClosingQuote(text, 1);
            if (position < 0) { Error(result, span, "Mermaid tree view node labels must close quoted strings."); return false; }
            row.Label = text.Substring(1, position - 1).Replace("\\\"", "\"").Replace("\\\\", "\\"); position++;
        } else {
            position = NextAnnotation(text, 0);
            row.Label = text.Substring(0, position).TrimEnd();
        }
        if (string.IsNullOrWhiteSpace(row.Label)) { Error(result, span, "Tree view rows require a non-empty label."); return false; }
        while (position < text.Length) {
            MermaidResourceGuard.Checkpoint();
            while (position < text.Length && char.IsWhiteSpace(text[position])) position++;
            if (position == text.Length) break;
            var start = position;
            if (At(text, position, ":::")) {
                position += 3; var nameStart = position;
                while (position < text.Length && (char.IsLetterOrDigit(text[position]) || text[position] == '_' || text[position] == '-')) position++;
                if (position == nameStart) { Error(result, Slice(span, start, 3), "Tree view class annotations require a name."); return false; }
                var name = text.Substring(nameStart, position - nameStart);
                if (row.Classes.Count >= 64) { Error(result, Slice(span, start, position - start), "Tree view rows support at most 64 class annotations."); return false; }
                row.Classes.Add(name);
                if (name != "highlight") Warn(result, Slice(span, start, position - start), "Tree view class '" + name + "' is retained as inert metadata; external CSS is not applied.");
            } else if (At(text, position, "icon(")) {
                var end = text.IndexOf(')', position + 5);
                if (end < 0) { Error(result, Slice(span, start, text.Length - start), "Tree view icon annotations must close with ')'."); return false; }
                row.Icon = text.Substring(position + 5, end - position - 5).Trim(); position = end + 1;
                if (row.Icon.IndexOf('(') >= 0 || row.Icon.Length > 256) { Error(result, Slice(span, start, position - start), "Tree view icon references must be a simple name of at most 256 characters."); return false; }
                if (row.Icon != "" && row.Icon != "none" && row.Icon != "file" && row.Icon != "folder")
                    Warn(result, Slice(span, start, position - start), "Tree view icon '" + row.Icon + "' is retained with a native question-mark fallback; external icon packs are not loaded.");
            } else if (At(text, position, "##")) {
                position += 2; var end = NextAnnotation(text, position);
                var description = text.Substring(position, end - position).Trim();
                row.Description = row.Description.Length == 0 ? description : row.Description + " " + description;
                position = end;
            } else {
                var end = NextAnnotation(text, position + 1);
                Warn(result, Slice(span, start, end - start), "Tree view node suffix is retained without native paint: " + text.Substring(start, end - start)); position = end;
            }
        }
        return true;
    }
    private static int NextAnnotation(string text, int start) {
        for (var i = start; i < text.Length; i++) {
            if ((i & 1023) == 0) MermaidResourceGuard.Checkpoint();
            if (At(text, i, ":::") || At(text, i, "##") || (i == 0 || char.IsWhiteSpace(text[i - 1])) && At(text, i, "icon(")) return i;
        }
        return text.Length;
    }
    private static int ClosingQuote(string text, int start) {
        var escaped = false;
        for (var i = start; i < text.Length; i++) {
            if (escaped) { escaped = false; continue; }
            if (text[i] == '\\') { escaped = true; continue; }
            if (text[i] == '"') return i;
        }
        return -1;
    }
    private static bool At(string text, int index, string value) => index <= text.Length - value.Length && string.CompareOrdinal(text, index, value, 0, value.Length) == 0;
    private static MermaidSourceSpan Slice(MermaidSourceSpan span, int offset, int length) => new(span.Line, span.Column + offset, length);
    private static void Warn(MermaidParseResult<MermaidDocument> result, MermaidSourceSpan span, string message) =>
        MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning, message, MermaidDiagnosticCodes.UnsupportedStatement);
}

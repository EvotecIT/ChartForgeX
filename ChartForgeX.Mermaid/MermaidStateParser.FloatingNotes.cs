using System;
using System.Collections.Generic;
using System.Text;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidStateParser {
    /// <summary>Reads only the existing quoted floating-note production, without interpreting its body as diagram syntax.</summary>
    private static bool TryFloatingNote(MermaidStateDocument document, string text, MermaidSourceSpan span,
        MermaidParseResult<MermaidDocument> result) {
        var position = 4;
        while (position < text.Length && char.IsWhiteSpace(text[position])) position++;
        if (position >= text.Length || text[position] != '"') return false;
        position++;
        var caption = new StringBuilder();
        var closed = false;
        while (position < text.Length) {
            if ((position & 255) == 0) MermaidResourceGuard.Checkpoint();
            var character = text[position++];
            if (character == '"') { closed = true; break; }
            if (character == '\\' && position < text.Length) {
                var next = text[position];
                if (next == '"' || next == '\\' || next == 'n') {
                    position++;
                    caption.Append(next == 'n' ? '\n' : next);
                    continue;
                }
            }
            caption.Append(character);
        }
        if (!closed || position >= text.Length || !char.IsWhiteSpace(text[position])) {
            Invalid("Floating state notes require a complete quoted caption followed by 'as' and one identity.");
            return true;
        }
        var tail = text.Substring(position).TrimStart();
        if (!MermaidParserUtilities.StartsStatement(tail, "as", ignoreCase: true)) {
            Invalid("Floating state notes require one identity after 'as'.");
            return true;
        }
        var id = tail.Substring(2).Trim();
        if (id.Length == 0) {
            Invalid("Floating state notes require a nonempty identity.");
            return true;
        }
        foreach (var character in id) {
            if (char.IsWhiteSpace(character) || char.IsControl(character) || "\"'`{}[]:;,<>".IndexOf(character) >= 0) {
                Invalid("Floating state note identities must be one unquoted token without diagram delimiters.");
                return true;
            }
        }
        MermaidResourceGuard.ReserveElements(1, span);
        document.Notes.Add(new MermaidDiagramNote(caption.ToString(), null, false, span, id));
        return true;

        void Invalid(string message) {
            document.Statements.Add(new MermaidStateStatement(text, span));
            document.RawStatements.Add(new MermaidRawStatement(text, span));
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, message, MermaidDiagnosticCodes.InvalidStatement);
        }
    }

    private static void ValidateNoteIdentities(MermaidStateDocument document, Dictionary<string, MermaidStateNode> states,
        MermaidParseResult<MermaidDocument> result) {
        var ids = new HashSet<string>(states.Keys, StringComparer.Ordinal);
        foreach (var note in document.Notes) {
            MermaidResourceGuard.Checkpoint();
            if (note.Id != null && !ids.Add(note.Id)) MermaidParserUtilities.Add(result, note.Span, MermaidDiagnosticSeverity.Error,
                "Floating state note identities must be distinct from states, composites and other named notes; transitions to notes are unsupported.",
                MermaidDiagnosticCodes.InvalidStatement);
        }
    }
}

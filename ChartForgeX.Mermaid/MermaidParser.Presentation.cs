using System;
using System.Text;

namespace ChartForgeX.Mermaid;

public sealed partial class MermaidParser {
    // Blanking shared declarations keeps family parsers focused and preserves original line/column locations.
    private static MermaidDocument ReadPresentation(string[] lines, int firstBodyLine, MermaidDiagramKind kind, MermaidParseResult<MermaidDocument> result) {
        var presentation = new MermaidDocument();
        var insideStateNote = false;
        for (var i = firstBodyLine - 1; i < lines.Length; i++) {
            var text = lines[i].Trim();
            var statement = MermaidParserUtilities.StripInlineComment(text);
            if (insideStateNote) {
                if (MermaidParserUtilities.IsStateNoteTerminator(statement)) insideStateNote = false;
                continue;
            }
            if (kind == MermaidDiagramKind.State && MermaidParserUtilities.IsMultilineStateNote(statement)) {
                insideStateNote = true;
                continue;
            }
            if (text.StartsWith("accTitle:", StringComparison.Ordinal)) {
                presentation.Accessibility.Name = text.Substring(9).Trim();
                lines[i] = string.Empty;
            } else if (text.StartsWith("accDescr:", StringComparison.Ordinal)) {
                presentation.Accessibility.Description = text.Substring(9).Trim();
                lines[i] = string.Empty;
            } else if (text.StartsWith("accDescr", StringComparison.Ordinal) && text.Substring(8).TrimStart().StartsWith("{", StringComparison.Ordinal)) {
                var start = i;
                var description = new StringBuilder();
                var part = text.Substring(text.IndexOf('{') + 1);
                var closed = false;
                while (true) {
                    var close = part.IndexOf('}');
                    description.Append(close >= 0 ? part.Substring(0, close) : part);
                    lines[i] = string.Empty;
                    if (close >= 0) { closed = true; break; }
                    if (++i >= lines.Length) break;
                    description.Append('\n');
                    part = lines[i].Trim();
                }
                presentation.Accessibility.Description = description.ToString();
                if (!closed) Add(result, start + 1, 1, text.Length, MermaidDiagnosticSeverity.Error, "Mermaid accDescr must close with '}'.");
            }
        }
        return presentation;
    }
}

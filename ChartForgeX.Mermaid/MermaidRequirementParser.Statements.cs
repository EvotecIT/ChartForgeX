using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidRequirementParser {
    /// <summary>Separates compact blocks and fields without moving their original line or column.</summary>
    private static IEnumerable<MermaidRawStatement> ReadStatements(string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        var depth = 0;
        for (var line = Math.Max(1, startLine); line <= lines.Length; line++) {
            MermaidResourceGuard.Checkpoint();
            var text = MermaidParserUtilities.StripInlineComment(lines[line - 1]);
            var start = 0; var quote = '\0';
            for (var index = 0; index < text.Length; index++) {
                if (quote != '\0') {
                    if (text[index] == quote && !MermaidFlowchartText.IsEscaped(text, index)) quote = '\0';
                    continue;
                }
                if (text[index] == '"' || text[index] == '`') { quote = text[index]; continue; }
                if (text[index] == '{') {
                    if (Read(start, index + 1) is { } opening) yield return opening;
                    start = index + 1; depth++;
                } else if (text[index] == '}') {
                    if (Read(start, index) is { } field) yield return field;
                    yield return new MermaidRawStatement("}", new MermaidSourceSpan(line, index + 1, 1));
                    start = index + 1; depth = Math.Max(0, depth - 1);
                } else if (depth > 0 && (text[index] == ';' || IsFieldStart(text, index, start))) {
                    if (Read(start, index) is { } field) yield return field;
                    start = text[index] == ';' ? index + 1 : index;
                }
            }
            if (Read(start, text.Length) is { } tail) yield return tail;
            if (quote != '\0') MermaidParserUtilities.Add(result, new MermaidSourceSpan(line, start + 1, Math.Max(1, text.Length - start)),
                MermaidDiagnosticSeverity.Error, "A requirement string must close on its source line.", MermaidDiagnosticCodes.InvalidStatement);

            MermaidRawStatement? Read(int first, int end) {
                while (first < end && (char.IsWhiteSpace(text[first]) || text[first] == ';')) first++;
                while (end > first && char.IsWhiteSpace(text[end - 1])) end--;
                return end <= first ? null : new MermaidRawStatement(text.Substring(first, end - first), new MermaidSourceSpan(line, first + 1, end - first));
            }
        }
    }

    private static bool IsFieldStart(string text, int index, int start) {
        if (index <= start || !char.IsWhiteSpace(text[index - 1]) || !char.IsLetter(text[index])) return false;
        var end = index;
        while (end < text.Length && char.IsLetter(text[end])) end++;
        var key = text.Substring(index, end - index);
        while (end < text.Length && char.IsWhiteSpace(text[end])) end++;
        return end < text.Length && text[end] == ':' && IsOneOf(key, "id", "text", "risk", "verifymethod", "type", "docref");
    }
}

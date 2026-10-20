using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

/// <summary>Splits flowchart statements without splitting quoted labels, shapes, or edge labels.</summary>
internal static class MermaidFlowchartStatements {
    internal static IEnumerable<MermaidRawStatement> Read(string[] lines, int firstLine, int firstOffset) {
        foreach (var logical in LogicalLines(lines, firstLine, firstOffset)) {
            var text = logical.Text;
            var start = 0;
            var openings = new Stack<char>();
            char quote = '\0';
            var pipe = false;
            var inlineLabels = new MermaidFlowchartInlineLabels(text);
            for (var index = start; index <= text.Length; index++) {
                var end = index == text.Length;
                if (!end) {
                    var c = text[index];
                    if (MermaidFlowchartText.ReadQuotedCharacter(text, ref index, ref quote, openings.Count > 0 && openings.Peek() == '@')) continue;
                    if (c == '|' && openings.Count == 0) { pipe = !pipe; continue; }
                    if (openings.Count == 0 && !pipe && inlineLabels.TryRead(index, out _, out _, out var edgeEnd)) {
                        index = edgeEnd - 1;
                        continue;
                    }
                    if (openings.Count == 0 && !pipe && (c == '-' || c == '.' || c == '=' || c == '<')) {
                        var operatorEnd = MermaidFlowchartOperators.ReadEnd(text, index);
                        if (operatorEnd > index) { index = operatorEnd - 1; continue; }
                    }
                    if (c == '[' || c == '(' || c == '{' || (c == '>' && index > start && char.IsLetterOrDigit(text[index - 1])))
                        openings.Push(c == '{' && index > start && text[index - 1] == '@' ? '@' : c);
                    else if (openings.Count > 0 && ((c == ']' && (openings.Peek() == '[' || openings.Peek() == '>')) || (c == ')' && openings.Peek() == '(') || (c == '}' && (openings.Peek() == '{' || openings.Peek() == '@')))) openings.Pop();
                    if (openings.Count != 0 || pipe) continue;
                    if (c == '%' && index + 1 < text.Length && text[index + 1] == '%') end = true;
                    else if (c != ';') continue;
                }
                var trimmedStart = start;
                while (trimmedStart < index && char.IsWhiteSpace(text[trimmedStart])) trimmedStart++;
                var trimmedEnd = index;
                while (trimmedEnd > trimmedStart && char.IsWhiteSpace(text[trimmedEnd - 1])) trimmedEnd--;
                if (trimmedEnd > trimmedStart) yield return new MermaidRawStatement(text.Substring(trimmedStart, trimmedEnd - trimmedStart),
                    MermaidFlowchartText.AtOffset(text, logical.Span, trimmedStart, trimmedEnd - trimmedStart));
                if (end) break;
                start = index + 1;
            }
        }
    }

    // Only quoted captions continue across physical lines. An incomplete unquoted shape
    // remains an error on its own line rather than consuming later independent statements.
    private static IEnumerable<MermaidRawStatement> LogicalLines(string[] lines, int firstLine, int firstOffset) {
        for (var line = firstLine - 1; line < lines.Length; line++) {
            var origin = line;
            var offset = line == firstLine - 1 ? firstOffset : 0;
            var buffer = new System.Text.StringBuilder();
            var quote = '\0';
            var metadata = false;
            do {
                var text = lines[line];
                if (buffer.Length > 0) buffer.Append('\n');
                buffer.Append(text.Substring(offset));
                for (var index = offset; index < text.Length; index++) {
                    if (MermaidFlowchartText.ReadQuotedCharacter(text, ref index, ref quote, metadata)) continue;
                    if (text[index] == '%' && index + 1 < text.Length && text[index + 1] == '%') break;
                    if (text[index] == '{' && index > 0 && text[index - 1] == '@') metadata = true;
                    else if (text[index] == '}') metadata = false;
                }
                offset = 0;
                if (quote == '\0' || line + 1 == lines.Length) break;
                line++;
            } while (true);
            yield return new MermaidRawStatement(buffer.ToString(), new MermaidSourceSpan(origin + 1,
                origin == firstLine - 1 ? firstOffset + 1 : 1, buffer.Length));
        }
    }

}

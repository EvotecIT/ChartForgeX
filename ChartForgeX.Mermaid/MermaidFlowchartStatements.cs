using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

/// <summary>Splits flowchart statements without splitting quoted labels, shapes, or edge labels.</summary>
internal static class MermaidFlowchartStatements {
    internal static IEnumerable<MermaidRawStatement> Read(string[] lines, int firstLine, int firstOffset) {
        for (var line = firstLine - 1; line < lines.Length; line++) {
            var text = lines[line];
            var start = line == firstLine - 1 ? firstOffset : 0;
            var openings = new Stack<char>();
            char quote = '\0';
            var pipe = false;
            for (var index = start; index <= text.Length; index++) {
                var end = index == text.Length;
                if (!end) {
                    var c = text[index];
                    if (quote != '\0') {
                        if (c == quote && !IsEscaped(text, index)) quote = '\0';
                        continue;
                    }
                    if (c == '"' || (c == '\'' && (index == start || char.IsWhiteSpace(text[index - 1]) || "[({=".IndexOf(text[index - 1]) >= 0))) { quote = c; continue; }
                    if (c == '|' && openings.Count == 0) { pipe = !pipe; continue; }
                    if (c == '[' || c == '(' || c == '{' || (c == '>' && index > start && char.IsLetterOrDigit(text[index - 1]))) openings.Push(c);
                    else if (openings.Count > 0 && ((c == ']' && (openings.Peek() == '[' || openings.Peek() == '>')) || (c == ')' && openings.Peek() == '(') || (c == '}' && openings.Peek() == '{'))) openings.Pop();
                    if (openings.Count != 0 || pipe) continue;
                    if (c == '%' && index + 1 < text.Length && text[index + 1] == '%') end = true;
                    else if (c != ';') continue;
                }
                var trimmedStart = start;
                while (trimmedStart < index && char.IsWhiteSpace(text[trimmedStart])) trimmedStart++;
                var trimmedEnd = index;
                while (trimmedEnd > trimmedStart && char.IsWhiteSpace(text[trimmedEnd - 1])) trimmedEnd--;
                if (trimmedEnd > trimmedStart) yield return new MermaidRawStatement(text.Substring(trimmedStart, trimmedEnd - trimmedStart), new MermaidSourceSpan(line + 1, trimmedStart + 1, trimmedEnd - trimmedStart));
                if (end) break;
                start = index + 1;
            }
        }
    }

    internal static bool IsEscaped(string text, int index) {
        var slashes = 0;
        while (index > 0 && text[--index] == '\\') slashes++;
        return (slashes & 1) != 0;
    }
}

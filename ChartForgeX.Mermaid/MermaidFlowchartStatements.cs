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
            var exhaustedFamilies = new HashSet<char>();
            for (var index = start; index <= text.Length; index++) {
                var end = index == text.Length;
                if (!end) {
                    var c = text[index];
                    if (MermaidFlowchartText.ReadQuotedCharacter(text, ref index, ref quote, openings.Count > 0 && openings.Peek() == '@')) continue;
                    if (c == '|' && openings.Count == 0) { pipe = !pipe; continue; }
                    if (openings.Count == 0 && !pipe) {
                        var prefixEnd = MermaidFlowchartOperators.ReadLabelPrefixEnd(text, index);
                        if (prefixEnd > index && !exhaustedFamilies.Contains(text[prefixEnd - 1])) {
                            if (MermaidFlowchartOperators.TryReadInlineLabel(text, index, out _, out _, out var edgeEnd)) {
                                index = edgeEnd - 1;
                                continue;
                            }
                            // This family has no matching suffix anywhere in the remaining
                            // line. Later prefixes cannot succeed; each family scans once.
                            exhaustedFamilies.Add(text[prefixEnd - 1]);
                        }
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
                if (trimmedEnd > trimmedStart) yield return new MermaidRawStatement(text.Substring(trimmedStart, trimmedEnd - trimmedStart), new MermaidSourceSpan(line + 1, trimmedStart + 1, trimmedEnd - trimmedStart));
                if (end) break;
                start = index + 1;
            }
        }
    }

}

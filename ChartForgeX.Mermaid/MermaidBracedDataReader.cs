using System;
using System.Text;

namespace ChartForgeX.Mermaid;

// One bounded quote/brace scanner serves family parsing and opaque shared prepasses.
internal static class MermaidBracedDataReader {
    internal static string Read(string[] lines, ref int line, ref int position, MermaidSourceSpan origin, string family) {
        if (position >= lines[line - 1].Length || lines[line - 1][position] != '{') throw new ArgumentException(family + " data must open with '{'.");
        var initialLine = line; var content = new StringBuilder(); var depth = 1; var quote = '\0'; var escaped = false; position++;
        MermaidResourceGuard.CheckDepth(depth, origin);
        for (; line <= lines.Length; line++) {
            var raw = lines[line - 1];
            for (; position < raw.Length; position++) {
                MermaidResourceGuard.Checkpoint(); var character = raw[position];
                if (quote != '\0') {
                    content.Append(character);
                    if (escaped) escaped = false;
                    else if (character == '\\' && quote != '\'') escaped = true;
                    else if (character == quote) {
                        if (quote == '\'' && position + 1 < raw.Length && raw[position + 1] == '\'') content.Append(raw[++position]);
                        else quote = '\0';
                    }
                } else if (character is '\'' or '"' or '`') { quote = character; content.Append(character); }
                else if (character == '{') { MermaidResourceGuard.CheckDepth(++depth, new MermaidSourceSpan(origin.Line + line - initialLine, position + 1, 1)); content.Append(character); }
                else if (character == '}') {
                    if (--depth == 0) { position++; return content.ToString(); }
                    content.Append(character);
                } else content.Append(character);
            }
            if (line < lines.Length) content.Append('\n');
            position = 0;
        }
        line = lines.Length;
        throw new ArgumentException(family + " data body must close its quotes and braces.");
    }
}

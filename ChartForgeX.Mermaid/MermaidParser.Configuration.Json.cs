using System;
using System.Text;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

public sealed partial class MermaidParser {
    // Mermaid's legacy directives accept single-quoted JSON strings. Normalize only their quoting;
    // the core JSON reader still owns structure, escapes, numbers, duplicate keys and resource limits.
    private static string NormalizeConfigurationJson(string text, List<int>? sourceOffsets = null) {
        var normalized = new StringBuilder(text.Length);
        void Append(char character, int position) { normalized.Append(character); sourceOffsets?.Add(position); }
        var quote = '\0';
        for (var index = 0; index < text.Length; index++) {
            var character = text[index];
            if (quote == '\0') {
                if (character == '\'' || character == '"') { quote = character; Append('"', index); }
                else Append(character, index);
            } else if (character == '\\') {
                if (++index == text.Length) throw new ArgumentException("Mermaid init configuration has an incomplete string escape.");
                var escaped = text[index];
                if (quote == '\'' && escaped == '\'') Append('\'', index);
                else { Append('\\', index - 1); Append(escaped, index); }
            } else if (character == quote) {
                Append('"', index); quote = '\0';
            } else if (quote == '\'' && character == '"') { Append('\\', index); Append('"', index); }
            else Append(character, index);
        }
        if (quote != '\0') throw new ArgumentException("Mermaid init configuration has an unterminated string.");
        return normalized.ToString();
    }
}

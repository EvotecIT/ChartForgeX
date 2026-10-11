using System;
using System.Text;

namespace ChartForgeX.Mermaid;

public sealed partial class MermaidParser {
    // Mermaid's legacy directives accept single-quoted JSON strings. Normalize only their quoting;
    // the core JSON reader still owns structure, escapes, numbers, duplicate keys and resource limits.
    private static string NormalizeConfigurationJson(string text) {
        var normalized = new StringBuilder(text.Length);
        var quote = '\0';
        for (var index = 0; index < text.Length; index++) {
            var character = text[index];
            if (quote == '\0') {
                if (character == '\'' || character == '"') { quote = character; normalized.Append('"'); }
                else normalized.Append(character);
            } else if (character == '\\') {
                if (++index == text.Length) throw new ArgumentException("Mermaid init configuration has an incomplete string escape.");
                var escaped = text[index];
                if (quote == '\'' && escaped == '\'') normalized.Append('\'');
                else normalized.Append('\\').Append(escaped);
            } else if (character == quote) {
                normalized.Append('"'); quote = '\0';
            } else if (quote == '\'' && character == '"') normalized.Append("\\\"");
            else normalized.Append(character);
        }
        if (quote != '\0') throw new ArgumentException("Mermaid init configuration has an unterminated string.");
        return normalized.ToString();
    }
}

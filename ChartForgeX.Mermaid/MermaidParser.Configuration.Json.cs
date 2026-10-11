using System;
using System.Text;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

public sealed partial class MermaidParser {
    // Legacy directives use JSON escapes; YAML flow mappings preserve single-quoted backslashes.
    // The core JSON reader still owns structure, escapes, numbers, duplicate keys and resource limits.
    private static string NormalizeConfigurationJson(string text, List<int>? sourceOffsets = null, bool yamlFlowMapping = false) {
        var normalized = new StringBuilder(text.Length);
        void Append(char character, int position) { normalized.Append(character); sourceOffsets?.Add(position); }
        void FoldBreak(ref int position, bool escaped) {
            var origin = position;
            if (!escaped) while (normalized.Length > 0 && (normalized[normalized.Length - 1] == ' ' || normalized[normalized.Length - 1] == '\t')) {
                normalized.Length--; sourceOffsets?.RemoveAt(sourceOffsets.Count - 1);
            }
            var breaks = 0;
            while (position < text.Length && (text[position] == '\n' || text[position] == '\r')) {
                if (text[position] == '\r' && position + 1 < text.Length && text[position + 1] == '\n') position++;
                breaks++; position++;
                while (position < text.Length && (text[position] == ' ' || text[position] == '\t')) position++;
            }
            position--;
            if (breaks == 1 && !escaped) Append(' ', origin);
            else for (var line = 1; line < breaks; line++) { Append('\\', origin); Append('n', origin); }
        }
        var quote = '\0';
        for (var index = 0; index < text.Length; index++) {
            var character = text[index];
            if (quote == '\0') {
                if (character == '\'' || character == '"') { quote = character; Append('"', index); }
                else Append(character, index);
            } else if (yamlFlowMapping && (character == '\n' || character == '\r')) FoldBreak(ref index, false);
            else if (yamlFlowMapping && quote == '\'') {
                if (character == '\'') {
                    if (index + 1 < text.Length && text[index + 1] == '\'') { Append('\'', index); index++; }
                    else { Append('"', index); quote = '\0'; }
                } else if (character == '\\' || character == '"') { Append('\\', index); Append(character, index); }
                else if (character == '\t') { Append('\\', index); Append('t', index); }
                else Append(character, index);
            } else if (character == '\\') {
                if (++index == text.Length) throw new ArgumentException("Mermaid init configuration has an incomplete string escape.");
                var escaped = text[index];
                if (yamlFlowMapping && quote == '"' && (escaped == '\n' || escaped == '\r')) FoldBreak(ref index, true);
                else if (yamlFlowMapping && escaped == '\t') { Append('\\', index - 1); Append('t', index); }
                else if (quote == '\'' && escaped == '\'') Append('\'', index);
                else { Append('\\', index - 1); Append(escaped, index); }
            } else if (character == quote) {
                Append('"', index); quote = '\0';
            } else if (yamlFlowMapping && character == '\t') { Append('\\', index); Append('t', index); }
            else if (quote == '\'' && character == '"') { Append('\\', index); Append('"', index); }
            else Append(character, index);
        }
        if (quote != '\0') throw new ArgumentException("Mermaid init configuration has an unterminated string.");
        return normalized.ToString();
    }
}

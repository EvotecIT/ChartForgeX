using System.Text;

namespace ChartForgeX.Mermaid;

public sealed partial class MermaidParser {
    // Root flow mappings may span physical YAML lines. Keep collection bounded;
    // the shared JSON reader still owns syntax, duplicate keys and depth limits.
    private static string ReadConfigurationFlowMapping(string[] lines, ref int index, string value) {
        var collected = new StringBuilder();
        var depth = 0;
        var quote = '\0';
        while (true) {
            if (collected.Length > 0) collected.Append('\n');
            collected.Append(value);
            for (var position = 0; position < value.Length; position++) {
                var character = value[position];
                if (quote != '\0') {
                    if (character == '\\') position++;
                    else if (character == quote) quote = '\0';
                } else if (character == '\'' || character == '"') quote = character;
                else if (character == '{') depth++;
                else if (character == '}') depth--;
            }
            if (depth <= 0 || collected.Length > MaximumConfigurationLength || index + 1 == lines.Length) break;
            value = TrimYamlComment(lines[++index]).Trim();
        }
        return collected.ToString();
    }
}

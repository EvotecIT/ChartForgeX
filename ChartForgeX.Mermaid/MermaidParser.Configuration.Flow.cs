using System.Text;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

public sealed partial class MermaidParser {
    // Root flow mappings may span physical YAML lines. Keep collection bounded;
    // the shared JSON reader still owns syntax, duplicate keys and depth limits.
    private static string ReadConfigurationFlowMapping(string[] lines, ref int index, string value,
        List<ConfigurationJsonLine>? sourceLines = null, int firstColumn = 1) {
        var collected = new StringBuilder();
        var depth = 0;
        var quote = '\0';
        var column = firstColumn;
        while (true) {
            if (value.Length > 0 || quote != '\0') {
                // Retain one overflow character so the budget pre-scan still detects rejection.
                var available = MaximumConfigurationLength + 1 - collected.Length;
                if (collected.Length > 0 && available > 0) { collected.Append('\n'); available--; }
                sourceLines?.Add(new ConfigurationJsonLine(collected.Length, index + 2, column));
                if (available > 0) collected.Append(value, 0, System.Math.Min(value.Length, available));
            }
            if (collected.Length > MaximumConfigurationLength) break;
            ScanConfigurationYamlFlow(value, ref depth, ref quote);
            if (depth <= 0 || index + 1 == lines.Length) break;
            while (quote == '\0' && index + 1 < lines.Length && lines[index + 1].TrimStart(' ', '\t').StartsWith("#", System.StringComparison.Ordinal)) index++;
            if (index + 1 == lines.Length) break;
            value = TrimConfigurationYaml(TrimYamlComment(lines[++index], true, quote));
            column = ConfigurationYamlIndent(lines[index]) + 1;
        }
        return collected.ToString();
    }

    // Shared physical scanning keeps unsupported flow nodes bounded without interpreting them.
    private static void ScanConfigurationYamlFlow(string text, ref int depth, ref char quote) {
        for (var position = 0; position < text.Length; position++) {
            var character = text[position];
            if (quote != '\0') {
                if (quote == '"' && character == '\\') position++;
                else if (quote == '\'' && character == '\'' && position + 1 < text.Length && text[position + 1] == '\'') position++;
                else if (character == quote) quote = '\0';
            } else if (character == '#' && (position == 0 || text[position - 1] == ' ' || text[position - 1] == '\t')) break;
            else if (character == '\'' || character == '"') quote = character;
            else if (character == '{' || character == '[') depth++;
            else if (character == '}' || character == ']') depth--;
        }
    }

}

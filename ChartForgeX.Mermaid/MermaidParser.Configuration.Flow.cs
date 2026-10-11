using System.Text;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

public sealed partial class MermaidParser {
    // An unsupported collection remains opaque until its closing delimiter, regardless
    // of the indentation of its members. Return true for a block scalar whose body
    // must remain literal; actual flow collections reuse the physical root reader.
    private static bool SkipOpaqueConfigurationYamlNode(string[] lines, ref int index, string node) {
        while (IsConfigurationYamlSequenceItem(node)) node = TrimConfigurationYaml(node.Substring(1));
        var separator = ConfigurationYamlValueSeparator(node);
        if (separator >= 0) node = node.Substring(separator + 1);
        node = ConfigurationYamlNodeWithoutProperties(TrimConfigurationYaml(TrimYamlComment(node, true)));
        if (node.StartsWith("{", System.StringComparison.Ordinal) || node.StartsWith("[", System.StringComparison.Ordinal))
            ReadConfigurationFlowMapping(lines, ref index, node);
        return IsConfigurationYamlBlockScalar(node);
    }

    // Root flow mappings may span physical YAML lines. Keep collection bounded;
    // the shared JSON reader still owns syntax, duplicate keys and depth limits.
    private static string ReadConfigurationFlowMapping(string[] lines, ref int index, string value,
        List<ConfigurationJsonLine>? sourceLines = null, int firstColumn = 1) {
        var collected = new StringBuilder();
        var depth = 0;
        var quote = '\0';
        var scalarState = ConfigurationFlowScalar.Node;
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
            ScanConfigurationYamlFlow(value, ref depth, ref quote, ref scalarState);
            if (depth <= 0 || index + 1 == lines.Length) break;
            while (quote == '\0' && index + 1 < lines.Length && lines[index + 1].TrimStart(' ', '\t').StartsWith("#", System.StringComparison.Ordinal)) index++;
            if (index + 1 == lines.Length) break;
            value = TrimConfigurationYaml(TrimYamlComment(lines[++index], true, quote));
            column = ConfigurationYamlIndent(lines[index]) + 1;
        }
        return collected.ToString();
    }

    // Shared physical scanning keeps unsupported flow nodes bounded without interpreting them.
    private static int ScanConfigurationYamlFlow(string text, ref int depth, ref char quote, ref ConfigurationFlowScalar scalarState,
        bool findMappingValue = false) {
        for (var position = 0; position < text.Length; position++) {
            var character = text[position];
            if (quote != '\0') {
                if (quote == '"' && character == '\\') position++;
                else if (quote == '\'' && character == '\'' && position + 1 < text.Length && text[position + 1] == '\'') position++;
                else if (character == quote) { quote = '\0'; scalarState = ConfigurationFlowScalar.Quoted; }
            } else if (scalarState == ConfigurationFlowScalar.VerbatimTag) {
                if (character == '>') scalarState = ConfigurationFlowScalar.Property;
            } else if (scalarState == ConfigurationFlowScalar.Property && "{}[],".IndexOf(character) < 0) {
                if (character == ' ' || character == '\t') scalarState = ConfigurationFlowScalar.Node;
            } else if (scalarState == ConfigurationFlowScalar.Node && (character == '!' || character == '&'))
                scalarState = character == '!' && position + 1 < text.Length && text[position + 1] == '<'
                    ? ConfigurationFlowScalar.VerbatimTag : ConfigurationFlowScalar.Property;
            else if (character == '#' && (position == 0 || text[position - 1] == ' ' || text[position - 1] == '\t')) break;
            else if (findMappingValue && depth == 0 && character == ':' && (scalarState == ConfigurationFlowScalar.Quoted ||
                position + 1 == text.Length || text[position + 1] == ' ' || text[position + 1] == '\t')) return position;
            else if ((character == '\'' || character == '"') && scalarState == ConfigurationFlowScalar.Node) quote = character;
            else if ((character == '{' || character == '[') && (!findMappingValue || depth > 0 || scalarState == ConfigurationFlowScalar.Node)) {
                depth++; scalarState = ConfigurationFlowScalar.Node;
            }
            else if ((character == '}' || character == ']') && (!findMappingValue || depth > 0)) { depth--; scalarState = ConfigurationFlowScalar.Plain; }
            else if (character == ',' || character == ':' && (scalarState == ConfigurationFlowScalar.Quoted || position + 1 == text.Length ||
                text[position + 1] == ' ' || text[position + 1] == '\t')) scalarState = ConfigurationFlowScalar.Node;
            else if (character != ' ' && character != '\t') scalarState = ConfigurationFlowScalar.Plain;
        }
        return -1;
    }

    private enum ConfigurationFlowScalar { Node, Plain, Quoted, Property, VerbatimTag }
}

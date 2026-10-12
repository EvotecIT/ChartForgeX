using System;
using System.Text.RegularExpressions;

namespace ChartForgeX.Mermaid;

public sealed partial class MermaidParser {
    private static Match ConfigurationYamlMapping(string text) => Regex.Match(text,
        @"^(""(?:[^""\\]|\\.)*""|'(?:[^']|'')*'|[A-Za-z_][A-Za-z0-9_-]*(?=[ \t]*:(?:[ \t]|$)))[ \t]*:[ \t]*(.*)$", RegexOptions.CultureInvariant);

    private static string? ConfigurationYamlKey(Match match, out string? error) {
        error = null;
        if (!match.Success) return null;
        var key = match.Groups[1].Value;
        try {
            return key[0] == '"' ? ReadDoubleQuotedConfigurationYaml(key)
                : key[0] == '\'' ? MermaidYamlScalar.ReadSingleQuoted(key) : key;
        } catch (ArgumentException exception) { error = exception.Message; return null; }
    }

    // Unsupported keys can contain flow collections and nonseparating colons.
    // Locate their value without interpreting or applying the key itself.
    private static int ConfigurationYamlValueSeparator(string text) {
        var depth = 0;
        var quote = '\0';
        var scalarState = ConfigurationFlowScalar.Node;
        return ScanConfigurationYamlFlow(text, ref depth, ref quote, ref scalarState, findMappingValue: true);
    }

    // Count physical scalar content before discarding presentation comments or whitespace.
    // Unsupported YAML nodes remain subject to the same bounded configuration input.
    private static long ConfigurationYamlLength(string text) {
        var lines = text.Split('\n');
        var rootIndent = int.MaxValue;
        foreach (var line in lines) {
            if (TrimConfigurationYaml(line).Length > 0 && !line.TrimStart(' ', '\t').StartsWith("#", StringComparison.Ordinal))
                rootIndent = Math.Min(rootIndent, ConfigurationYamlIndent(line));
        }
        long length = 0;
        var inConfiguration = false;
        var scalarIndent = -1;
        var flowDepth = 0;
        var flowQuote = '\0';
        var scalarState = ConfigurationFlowScalar.Node;
        var rootFlow = false;
        void Count(string line) { length += line.Length + (length > 0 ? 1L : 0L); }
        for (var index = 0; index < lines.Length; index++) {
            var line = lines[index];
            var trimmed = TrimConfigurationYaml(line);
            var indent = ConfigurationYamlIndent(line);
            if (flowDepth > 0) {
                if (flowQuote != '\0' || trimmed.Length > 0 && !trimmed.StartsWith("#", StringComparison.Ordinal)) Count(line);
                ScanConfigurationYamlFlow(line, ref flowDepth, ref flowQuote, ref scalarState);
                if (flowDepth <= 0 && rootFlow) inConfiguration = false;
            } else if (scalarIndent >= 0 && (trimmed.Length == 0 || indent > scalarIndent)) Count(line);
            else {
                scalarIndent = -1;
                if (trimmed.Length == 0 || trimmed.StartsWith("#", StringComparison.Ordinal)) continue;
                var mapping = ConfigurationYamlMapping(trimmed);
                if (indent <= rootIndent && (!inConfiguration || !IsConfigurationYamlBlockContinuation(indent, rootIndent, trimmed)))
                    inConfiguration = indent == rootIndent && ConfigurationYamlKey(mapping, out _) == "config";
                if (!inConfiguration) continue;
                Count(line);
                var node = trimmed;
                var ownerIndent = indent;
                while (IsConfigurationYamlSequenceItem(node)) {
                    var prefix = 1;
                    while (prefix < node.Length && (node[prefix] == ' ' || node[prefix] == '\t')) prefix++;
                    ownerIndent += prefix;
                    node = node.Substring(prefix);
                }
                var entry = ConfigurationYamlMapping(node);
                // Opaque keys are not interpreted, but their value still has physical YAML structure.
                var separator = entry.Success ? -1 : ConfigurationYamlValueSeparator(node);
                var value = entry.Success ? entry.Groups[2].Value : separator >= 0 ? node.Substring(separator + 1) : node;
                value = TrimConfigurationYaml(TrimYamlComment(value, true));
                value = ConfigurationYamlNodeWithoutProperties(value);
                if (IsConfigurationYamlBlockScalar(value))
                    scalarIndent = entry.Success || separator >= 0 ? ownerIndent : indent;
                else if (value.StartsWith("{", StringComparison.Ordinal) || value.StartsWith("[", StringComparison.Ordinal)) {
                    rootFlow = indent == rootIndent && ConfigurationYamlKey(mapping, out _) == "config";
                    flowQuote = '\0'; scalarState = ConfigurationFlowScalar.Node;
                    ScanConfigurationYamlFlow(value, ref flowDepth, ref flowQuote, ref scalarState);
                    if (flowDepth <= 0 && rootFlow) inConfiguration = false;
                }
            }
            if (length > MaximumConfigurationLength) return length;
        }
        return length;
    }

    private static string ConfigurationYamlNodeWithoutProperties(string value) =>
        Regex.Replace(value, @"^(?:(?:&[^\s]+|![^\s]+)[ \t]+)+", string.Empty, RegexOptions.CultureInvariant);

    private static bool IsConfigurationYamlBlockScalar(string value) =>
        Regex.IsMatch(value, @"^[|>](?:[1-9][+-]?|[+-][1-9]?)?$", RegexOptions.CultureInvariant);
}

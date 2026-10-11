using System;
using System.Text.RegularExpressions;

namespace ChartForgeX.Mermaid;

public sealed partial class MermaidParser {
    private static Match ConfigurationYamlMapping(string text) => Regex.Match(text,
        @"^(""(?:[^""\\]|\\.)*""|'(?:[^']|'')*'|[A-Za-z_][A-Za-z0-9_-]*)[ \t]*:[ \t]*(.*)$", RegexOptions.CultureInvariant);

    private static string? ConfigurationYamlKey(Match match) {
        if (!match.Success) return null;
        var key = match.Groups[1].Value;
        try {
            return key[0] == '"' ? ReadDoubleQuotedConfigurationYaml(key)
                : key[0] == '\'' ? ReadSingleQuotedConfigurationYaml(key) : key;
        } catch (ArgumentException) { return null; }
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
                    inConfiguration = indent == rootIndent && ConfigurationYamlKey(mapping) == "config";
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
                var separator = entry.Success ? entry : Regex.Match(node, @":(?:[ \t]+|$)(.*)$", RegexOptions.CultureInvariant);
                var value = separator.Success ? separator.Groups[entry.Success ? 2 : 1].Value : node;
                value = TrimConfigurationYaml(TrimYamlComment(value, true));
                value = Regex.Replace(value, @"^(?:(?:&[^\s]+|![^\s]+)[ \t]+)+", string.Empty, RegexOptions.CultureInvariant);
                if (Regex.IsMatch(value, @"^[|>](?:[1-9][+-]?|[+-][1-9]?)?$", RegexOptions.CultureInvariant))
                    scalarIndent = separator.Success ? ownerIndent : indent;
                else if (value.StartsWith("{", StringComparison.Ordinal) || value.StartsWith("[", StringComparison.Ordinal)) {
                    rootFlow = indent == rootIndent && ConfigurationYamlKey(mapping) == "config";
                    flowQuote = '\0'; scalarState = ConfigurationFlowScalar.Node;
                    ScanConfigurationYamlFlow(value, ref flowDepth, ref flowQuote, ref scalarState);
                    if (flowDepth <= 0 && rootFlow) inConfiguration = false;
                }
            }
            if (length > MaximumConfigurationLength) return length;
        }
        return length;
    }
}

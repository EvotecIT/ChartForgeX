using System;
using System.Collections.Generic;
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
        void Count(string line) { length += line.Length + (length > 0 ? 1L : 0L); }
        for (var index = 0; index < lines.Length; index++) {
            var line = lines[index];
            var trimmed = TrimConfigurationYaml(line);
            var indent = ConfigurationYamlIndent(line);
            if (scalarIndent >= 0) {
                if (trimmed.Length == 0 || indent > scalarIndent) {
                    Count(line);
                    if (length > MaximumConfigurationLength) return length;
                    continue;
                }
                scalarIndent = -1;
            }
            if (trimmed.Length == 0 || trimmed.StartsWith("#", StringComparison.Ordinal)) continue;
            var mapping = ConfigurationYamlMapping(trimmed);
            if (indent <= rootIndent && (!inConfiguration || !IsConfigurationYamlBlockContinuation(indent, rootIndent, trimmed)))
                inConfiguration = indent == rootIndent && ConfigurationYamlKey(mapping) == "config";
            if (!inConfiguration) continue;
            Count(line);
            var value = mapping.Success ? TrimConfigurationYaml(TrimYamlComment(mapping.Groups[2].Value, indent == rootIndent)) : string.Empty;
            if (indent == rootIndent && value.StartsWith("{", StringComparison.Ordinal)) {
                var sourceLines = new List<ConfigurationJsonLine>();
                ReadConfigurationFlowMapping(lines, ref index, value, sourceLines);
                // The first physical line was counted above; quoted blank and # lines are data.
                for (var extra = 1; extra < sourceLines.Count; extra++) Count(lines[sourceLines[extra].Line - 2]);
                inConfiguration = false;
            } else {
                var node = trimmed;
                var ownerIndent = indent;
                while (IsConfigurationYamlSequenceItem(node)) {
                    var prefix = 1;
                    while (prefix < node.Length && (node[prefix] == ' ' || node[prefix] == '\t')) prefix++;
                    ownerIndent += prefix;
                    node = node.Substring(prefix);
                }
                var entry = ConfigurationYamlMapping(node);
                var scalar = TrimConfigurationYaml(TrimYamlComment(entry.Success ? entry.Groups[2].Value : node));
                if (Regex.IsMatch(scalar, @"^(?:(?:&[^\s]+|![^\s]+)[ \t]+)*[|>](?:[1-9][+-]?|[+-][1-9]?)?$", RegexOptions.CultureInvariant))
                    scalarIndent = entry.Success ? ownerIndent : indent;
            }
            if (length > MaximumConfigurationLength) return length;
        }
        return length;
    }
}

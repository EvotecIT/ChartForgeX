using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

public sealed partial class MermaidParser {
    /// <summary>Reads the supported root title scalar using the same YAML rules as source configuration.</summary>
    internal static string? ReadFrontMatterTitle(string? frontMatter) {
        if (string.IsNullOrWhiteSpace(frontMatter)) return null;
        var lines = frontMatter!.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var rootIndent = ConfigurationYamlRootIndent(lines);
        for (var index = 0; index < lines.Length; index++) {
            var raw = lines[index];
            if (ConfigurationYamlIndent(raw) != rootIndent || raw.Substring(0, rootIndent).IndexOf('\t') >= 0) continue;
            var mapping = ConfigurationYamlMapping(TrimConfigurationYaml(raw));
            var key = ConfigurationYamlKey(mapping, out var keyError);
            if (key == null || keyError != null) continue;
            var value = TrimConfigurationYaml(TrimYamlComment(mapping.Groups[2].Value));
            try {
                if (key == "title") {
                    if (value.Length == 0 || IsOpaqueConfigurationYamlValue(value) || value[0] == '{') return null;
                    if (value[0] == '"') return ReadDoubleQuotedConfigurationYaml(value);
                    if (value[0] == '\'') return MermaidYamlScalar.ReadSingleQuoted(value);
                    ValidateConfigurationYamlPlainScalar(value);
                    return value;
                }
                // A multiline root flow mapping may contain keys at the root indentation.
                // Consume it as one node so a nested title never becomes a diagram heading.
                SkipOpaqueConfigurationYamlNode(lines, ref index, value);
            } catch (ArgumentException) {
                // Unsupported metadata remains in the raw source rather than becoming a partial heading.
                if (key == "title") return null;
            }
        }
        return null;
    }

    private static int ConfigurationYamlRootIndent(IEnumerable<string> lines) {
        var indent = int.MaxValue;
        foreach (var line in lines) {
            var trimmed = TrimConfigurationYaml(line);
            if (trimmed.Length > 0 && !trimmed.StartsWith("#", StringComparison.Ordinal))
                indent = Math.Min(indent, ConfigurationYamlIndent(line));
        }
        return indent;
    }
}

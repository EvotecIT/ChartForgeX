using System;

namespace ChartForgeX.Mermaid;

/// <summary>Decodes the YAML scalar forms shared by source configuration and node metadata.</summary>
internal static class MermaidYamlScalar {
    internal static string ReadSingleQuoted(string value) {
        if (value.Length < 2 || value[value.Length - 1] != '\'') throw new ArgumentException("Mermaid YAML scalar has an unterminated single-quoted string.");
        for (var index = 1; index < value.Length - 1; index++) {
            if (value[index] != '\'') continue;
            if (index + 1 >= value.Length - 1 || value[index + 1] != '\'') throw new ArgumentException("Mermaid YAML single-quoted strings must escape interior quotes as ''.");
            index++;
        }
        return value.Substring(1, value.Length - 2).Replace("''", "'");
    }
}

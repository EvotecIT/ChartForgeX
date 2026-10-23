using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ChartForgeX.Mermaid;

public sealed partial class MermaidParser {
    // Shared prepasses must not reinterpret metadata instructions as directives or
    // accessibility declarations. The actual family parser owns the untouched body.
    private static HashSet<int> AgentflowMetadataLines(string[] lines, int firstLine = 0) {
        var opaque = new HashSet<int>();
        var active = false; var depth = 0; var quote = '\0'; var scalarIndent = -1;
        for (var index = firstLine; index < lines.Length; index++) {
            MermaidResourceGuard.Checkpoint();
            var line = lines[index]; var text = line.Trim();
            if (!active) {
                if (ConfigurationPrefix(line).Success) {
                    while (!ConfigurationEnding(lines[index]).Success && index + 1 < lines.Length) index++;
                    continue;
                }
                if (text.Length == 0 || IsComment(text)) continue;
                // Only the actual first diagram header enables this family-specific prepass.
                if (ResolveDiagramKind(text.Split(';')[0]).Kind != MermaidDiagramKind.Agentflow) return opaque;
                active = true;
                continue;
            }
            var indentation = MermaidParserUtilities.LeadingWhitespace(line);
            if (depth > 0) opaque.Add(index);
            if (scalarIndent >= 0 && (text.Length == 0 || indentation > scalarIndent)) continue;
            scalarIndent = -1;
            if (depth > 0 && quote == '\0' && Regex.IsMatch(line, @":\s*[|>]\s*$", RegexOptions.CultureInvariant)) scalarIndent = indentation;
            for (var position = 0; position < line.Length; position++) {
                var character = line[position];
                if (quote != '\0') {
                    if (character == '\\' && quote == '"') position++;
                    else if (character == quote) {
                        if (quote == '\'' && position + 1 < line.Length && line[position + 1] == '\'') position++;
                        else quote = '\0';
                    }
                } else if (character is '"' or '\'') quote = character;
                else if (depth == 0 && character == '@' && position + 1 < line.Length && line[position + 1] == '{') { depth = 1; position++; opaque.Add(index); }
                else if (depth > 0 && character == '{') depth++;
                else if (depth > 0 && character == '}') depth--;
            }
            if (depth == 0) { quote = '\0'; scalarIndent = -1; }
        }
        return opaque;
    }
}

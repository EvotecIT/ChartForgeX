using System;
using System.Text;
using System.Text.RegularExpressions;

namespace ChartForgeX.Mermaid;

public sealed partial class MermaidParser {
    // Blanking shared declarations keeps family parsers focused and preserves original line/column locations.
    private static MermaidDocument ReadPresentation(string[] lines, int firstBodyLine, string? frontMatter, MermaidParseResult<MermaidDocument> result) {
        var presentation = new MermaidDocument();
        for (var i = firstBodyLine - 1; i < lines.Length; i++) {
            var text = lines[i].Trim();
            if (text.StartsWith("accTitle:", StringComparison.Ordinal)) {
                presentation.Accessibility.Name = text.Substring(9).Trim();
                lines[i] = string.Empty;
            } else if (text.StartsWith("accDescr:", StringComparison.Ordinal)) {
                presentation.Accessibility.Description = text.Substring(9).Trim();
                lines[i] = string.Empty;
            } else if (text.StartsWith("accDescr", StringComparison.Ordinal) && text.Substring(8).TrimStart().StartsWith("{", StringComparison.Ordinal)) {
                var start = i;
                var description = new StringBuilder();
                var part = text.Substring(text.IndexOf('{') + 1);
                var closed = false;
                while (true) {
                    var close = part.IndexOf('}');
                    description.Append(close >= 0 ? part.Substring(0, close) : part);
                    lines[i] = string.Empty;
                    if (close >= 0) { closed = true; break; }
                    if (++i >= lines.Length) break;
                    description.Append('\n');
                    part = lines[i].Trim();
                }
                presentation.Accessibility.Description = description.ToString();
                if (!closed) Add(result, start + 1, 1, text.Length, MermaidDiagnosticSeverity.Error, "Mermaid accDescr must close with '}'.");
            }
        }
        if (frontMatter != null) {
            var match = Regex.Match(frontMatter, @"(?m)^\s+theme:\s*['""']?([a-zA-Z]+)", RegexOptions.CultureInvariant);
            if (match.Success) presentation.Theme = match.Groups[1].Value;
        }
        // The init directive uses JSON-like keys; only the scalar theme is interpreted here.
        for (var i = 0; i < lines.Length; i++) {
            var line = lines[i];
            if (!line.TrimStart().StartsWith("%%{init", StringComparison.Ordinal)) continue;
            var match = Regex.Match(line, @"['""]theme['""]\s*:\s*['""]([a-zA-Z]+)['""]", RegexOptions.CultureInvariant);
            if (match.Success) presentation.Theme = match.Groups[1].Value;
            foreach (Match key in Regex.Matches(line, @"['""]([\w]+)['""]\s*:", RegexOptions.CultureInvariant)) {
                if (key.Groups[1].Value != "theme") Add(result, i + 1, key.Index + 1, key.Length, MermaidDiagnosticSeverity.Warning, "Mermaid source configuration '" + key.Groups[1].Value + "' is retained but not applied to static rendering.");
            }
        }
        for (var i = 0; i < firstBodyLine - 1 && i < lines.Length; i++) {
            var match = Regex.Match(lines[i], @"^\s{2,}([\w]+):", RegexOptions.CultureInvariant);
            if (match.Success && match.Groups[1].Value != "theme") Add(result, i + 1, 1, lines[i].Length, MermaidDiagnosticSeverity.Warning, "Mermaid source configuration '" + match.Groups[1].Value + "' is retained but not applied to static rendering.");
        }
        if (presentation.Theme != null && presentation.Theme != "dark" && presentation.Theme != "default") {
            Add(result, firstBodyLine - 1, 1, 0, MermaidDiagnosticSeverity.Warning, "Mermaid theme '" + presentation.Theme + "' uses the static light palette; custom source theme variables are not interpreted.");
        }
        return presentation;
    }
}

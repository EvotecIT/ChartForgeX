using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ChartForgeX.Core;

namespace ChartForgeX.Mermaid;

public sealed partial class MermaidParser {
    private static int ConfigurationYamlLength(string text) {
        var lines = text.Split('\n');
        var rootIndent = int.MaxValue;
        foreach (var line in lines) {
            if (line.Trim().Length > 0 && !line.TrimStart().StartsWith("#", StringComparison.Ordinal)) rootIndent = Math.Min(rootIndent, LeadingWhitespace(line));
        }
        var length = 0;
        var inConfiguration = false;
        for (var index = 0; index < lines.Length; index++) {
            var line = lines[index];
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith("#", StringComparison.Ordinal)) continue;
            if (LeadingWhitespace(line) <= rootIndent)
                inConfiguration = LeadingWhitespace(line) == rootIndent && Regex.IsMatch(trimmed, @"^config\s*:", RegexOptions.CultureInvariant);
            if (inConfiguration) {
                length += line.Length + (length > 0 ? 1 : 0);
                var flow = Regex.Match(trimmed, @"^config\s*:\s*(\{.*)$", RegexOptions.CultureInvariant);
                if (LeadingWhitespace(line) == rootIndent && flow.Success) {
                    var first = index;
                    ReadConfigurationFlowMapping(lines, ref index, TrimYamlComment(flow.Groups[1].Value).Trim());
                    for (var extra = first + 1; extra <= index; extra++) {
                        if (!lines[extra].TrimStart().StartsWith("#", StringComparison.Ordinal)) length += lines[extra].Length + 1;
                    }
                    inConfiguration = false;
                }
            }
        }
        return length;
    }

    private static void ReadConfigurationYaml(string text, MermaidSourceConfiguration configuration, MermaidParseResult<MermaidDocument> result) {
        var parents = new List<(int Indent, string Path, int ChildIndent, MermaidSourceSpan Span)>();
        var declared = new HashSet<string>(StringComparer.Ordinal);
        var opaqueIndent = -1;
        var lines = text.Split('\n');
        var rootIndent = int.MaxValue;
        foreach (var line in lines) {
            if (line.Trim().Length > 0 && !line.TrimStart().StartsWith("#", StringComparison.Ordinal)) rootIndent = Math.Min(rootIndent, LeadingWhitespace(line));
        }
        for (var index = 0; index < lines.Length; index++) {
            var raw = lines[index];
            var trimmed = raw.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith("#", StringComparison.Ordinal)) continue;
            var indent = LeadingWhitespace(raw);
            if (opaqueIndent >= 0 && indent > opaqueIndent) continue;
            opaqueIndent = -1;
            while (parents.Count > 0 && indent <= parents[parents.Count - 1].Indent) CompleteConfigurationYamlParent(parents, configuration);
            var match = Regex.Match(trimmed, @"^([A-Za-z_][A-Za-z0-9_-]*)\s*:\s*(.*)$", RegexOptions.CultureInvariant);
            if (parents.Count == 0 && (indent != rootIndent || !match.Success || match.Groups[1].Value != "config")) continue;
            var span = new MermaidSourceSpan(index + 2, indent + 1, trimmed.Length);
            if (parents.Count > 0) {
                var parent = parents[parents.Count - 1];
                if (parent.ChildIndent < 0) parents[parents.Count - 1] = (parent.Indent, parent.Path, indent, parent.Span);
                else if (parent.ChildIndent != indent) {
                    ConfigurationError(result, span, "Mermaid configuration mapping keys must use consistent sibling indentation.");
                    continue;
                }
            }
            if (!match.Success || raw.Substring(0, indent).IndexOf('\t') >= 0) {
                Add(result, span.Line, span.Column, span.Length, MermaidDiagnosticSeverity.Warning,
                    "Mermaid frontmatter configuration supports space-indented mappings with scalar values; this declaration is retained without application.", MermaidDiagnosticCodes.UnsupportedConfiguration);
                continue;
            }
            var path = parents.Count == 0 ? string.Empty : parents[parents.Count - 1].Path;
            var key = match.Groups[1].Value;
            if (parents.Count > 0) path = path.Length == 0 ? key : path + "." + key;
            var valueText = TrimYamlComment(match.Groups[2].Value).Trim();
            if (!declared.Add(path)) { ConfigurationError(result, span, "Mermaid frontmatter configuration repeats '" + (path.Length == 0 ? "config" : path) + "'."); continue; }
            if (declared.Count > 256) { ConfigurationError(result, span, "Mermaid frontmatter configuration exceeds 256 keys."); break; }
            if (valueText.Length == 0) {
                if (parents.Count >= 8) { ConfigurationError(result, span, "Mermaid frontmatter configuration exceeds eight mapping levels."); break; }
                if (IsConfigurationStringPath(path))
                    configuration.Add(new MermaidConfigurationSetting(path, null, false, span));
                parents.Add((indent, path, -1, span));
                continue;
            }
            try {
                if (parents.Count == 0) {
                    if (valueText.StartsWith("{", StringComparison.Ordinal))
                        valueText = ReadConfigurationFlowMapping(lines, ref index, valueText);
                    AddConfigurationJson(GeoJsonValue.Parse(NormalizeConfigurationJson(valueText), StringComparer.Ordinal, ConfigurationJsonLimits).AsObject("Mermaid frontmatter config"), string.Empty, span, configuration);
                } else {
                    var isString = true;
                    string? value = valueText;
                    if (valueText.StartsWith("\"", StringComparison.Ordinal)) {
                        value = GeoJsonValue.Parse(valueText.Replace("\t", "\\t"), StringComparer.Ordinal, ConfigurationJsonLimits).AsString("Mermaid YAML scalar");
                    } else if (valueText.StartsWith("'", StringComparison.Ordinal)) {
                        value = ReadSingleQuotedConfigurationYaml(valueText);
                    } else if (valueText.StartsWith("{", StringComparison.Ordinal) || valueText.StartsWith("[", StringComparison.Ordinal) ||
                        valueText.StartsWith("|", StringComparison.Ordinal) || valueText.StartsWith(">", StringComparison.Ordinal) ||
                        valueText.StartsWith("&", StringComparison.Ordinal) || valueText.StartsWith("*", StringComparison.Ordinal) || valueText.StartsWith("!", StringComparison.Ordinal)) {
                        value = null; isString = false;
                        opaqueIndent = indent;
                    } else if (Regex.IsMatch(valueText, @":(?:\s|$)", RegexOptions.CultureInvariant)) {
                        throw new ArgumentException("Mermaid YAML plain scalars cannot contain a mapping separator; quote strings containing a colon followed by whitespace or an ending colon.");
                    } else if (IsTypedConfigurationYamlScalar(valueText)) isString = false;
                    configuration.Add(new MermaidConfigurationSetting(path, value, isString, span));
                }
            } catch (ArgumentException exception) { ConfigurationError(result, span, exception.Message); }
        }
        while (parents.Count > 0) CompleteConfigurationYamlParent(parents, configuration);
    }

    private static void CompleteConfigurationYamlParent(List<(int Indent, string Path, int ChildIndent, MermaidSourceSpan Span)> parents,
        MermaidSourceConfiguration configuration) {
        var parent = parents[parents.Count - 1];
        if (parent.ChildIndent < 0 && parent.Path.Length > 0 && !IsConfigurationStringPath(parent.Path))
            configuration.Add(new MermaidConfigurationSetting(parent.Path, null, false, parent.Span));
        parents.RemoveAt(parents.Count - 1);
    }

    private static string ReadSingleQuotedConfigurationYaml(string value) {
        if (value.Length < 2 || value[value.Length - 1] != '\'') throw new ArgumentException("Mermaid YAML scalar has an unterminated single-quoted string.");
        for (var index = 1; index < value.Length - 1; index++) {
            if (value[index] != '\'') continue;
            if (index + 1 >= value.Length - 1 || value[index + 1] != '\'') throw new ArgumentException("Mermaid YAML single-quoted strings must escape interior quotes as ''.");
            index++;
        }
        return value.Substring(1, value.Length - 2).Replace("''", "'");
    }

    private static bool IsTypedConfigurationYamlScalar(string value) {
        if (Regex.IsMatch(value, @"^(?:~|null|Null|NULL|true|True|TRUE|false|False|FALSE|[-+]?\.(?:inf|Inf|INF)|\.(?:nan|NaN|NAN))$", RegexOptions.CultureInvariant)) return true;
        return MermaidConfigurationNumber.TryParse(value, out _);
    }

    private static string TrimYamlComment(string value) {
        var quote = '\0';
        for (var index = 0; index < value.Length; index++) {
            var character = value[index];
            if (quote == '\0') {
                if (character == '\'' || character == '"') quote = character;
                else if (character == '#' && (index == 0 || char.IsWhiteSpace(value[index - 1]))) return value.Substring(0, index);
            } else if (character == '\\' && quote == '"') index++;
            else if (character == quote) {
                if (quote == '\'' && index + 1 < value.Length && value[index + 1] == '\'') index++;
                else quote = '\0';
            }
        }
        return value;
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using ChartForgeX.Core;

namespace ChartForgeX.Mermaid;

public sealed partial class MermaidParser {
    private static void ReadConfigurationYaml(string text, MermaidSourceConfiguration configuration, MermaidParseResult<MermaidDocument> result) {
        var parents = new List<(int Indent, string Path, int ChildIndent)>();
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
            while (parents.Count > 0 && indent <= parents[parents.Count - 1].Indent) parents.RemoveAt(parents.Count - 1);
            var match = Regex.Match(trimmed, @"^([A-Za-z_][A-Za-z0-9_-]*)\s*:\s*(.*)$", RegexOptions.CultureInvariant);
            if (parents.Count == 0 && (indent != rootIndent || !match.Success || match.Groups[1].Value != "config")) continue;
            var span = new MermaidSourceSpan(index + 2, indent + 1, trimmed.Length);
            if (parents.Count > 0) {
                var parent = parents[parents.Count - 1];
                if (parent.ChildIndent < 0) parents[parents.Count - 1] = (parent.Indent, parent.Path, indent);
                else if (parent.ChildIndent != indent) {
                    ConfigurationError(result, span, "Mermaid configuration mapping keys must use consistent sibling indentation.");
                    continue;
                }
            }
            if (!match.Success || raw.IndexOf('\t') >= 0) {
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
                parents.Add((indent, path, -1));
                continue;
            }
            try {
                if (parents.Count == 0) {
                    AddConfigurationJson(GeoJsonValue.Parse(NormalizeConfigurationJson(valueText), StringComparer.Ordinal, ConfigurationJsonLimits).AsObject("Mermaid frontmatter config"), string.Empty, span, configuration);
                } else {
                    var isString = true;
                    string? value = valueText;
                    if (valueText.StartsWith("\"", StringComparison.Ordinal)) {
                        value = GeoJsonValue.Parse(valueText, StringComparer.Ordinal, ConfigurationJsonLimits).AsString("Mermaid YAML scalar");
                    } else if (valueText.StartsWith("'", StringComparison.Ordinal)) {
                        if (valueText.Length < 2 || valueText[valueText.Length - 1] != '\'') throw new ArgumentException("Mermaid YAML scalar has an unterminated single-quoted string.");
                        value = valueText.Substring(1, valueText.Length - 2).Replace("''", "'");
                    } else if (valueText.StartsWith("{", StringComparison.Ordinal) || valueText.StartsWith("[", StringComparison.Ordinal) ||
                        valueText.StartsWith("|", StringComparison.Ordinal) || valueText.StartsWith(">", StringComparison.Ordinal) ||
                        valueText.StartsWith("&", StringComparison.Ordinal) || valueText.StartsWith("*", StringComparison.Ordinal) || valueText.StartsWith("!", StringComparison.Ordinal)) {
                        value = null; isString = false;
                        opaqueIndent = indent;
                    } else if (valueText == "true" || valueText == "false" || valueText == "null" || valueText == "~" ||
                        double.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out _)) isString = false;
                    configuration.Add(new MermaidConfigurationSetting(path, value, isString, span));
                }
            } catch (ArgumentException exception) { ConfigurationError(result, span, exception.Message); }
        }
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

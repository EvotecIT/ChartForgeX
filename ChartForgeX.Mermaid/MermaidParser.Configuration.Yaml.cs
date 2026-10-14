using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using ChartForgeX.Core;

namespace ChartForgeX.Mermaid;

public sealed partial class MermaidParser {
    private static void ReadConfigurationYaml(string text, MermaidSourceConfiguration configuration, MermaidParseResult<MermaidDocument> result, IReadOnlyList<int> sourceLineStarts) {
        var parents = new List<(int Indent, string Path, int ChildIndent, MermaidSourceSpan Span)>();
        var declared = new HashSet<string>(StringComparer.Ordinal);
        var opaqueIndent = -1;
        var opaqueScalarIndent = -1;
        var lines = text.Split('\n');
        var rootIndent = ConfigurationYamlRootIndent(lines);
        for (var index = 0; index < lines.Length; index++) {
            var raw = lines[index];
            var trimmed = TrimConfigurationYaml(raw);
            if (trimmed.Length == 0 || trimmed.StartsWith("#", StringComparison.Ordinal)) continue;
            var indent = ConfigurationYamlIndent(raw);
            if (opaqueScalarIndent >= 0 && indent > opaqueScalarIndent) continue;
            opaqueScalarIndent = -1;
            if (opaqueIndent >= 0 && IsConfigurationYamlBlockContinuation(indent, opaqueIndent, trimmed)) {
                if (SkipOpaqueConfigurationYamlNode(lines, ref index, trimmed)) opaqueScalarIndent = indent;
                continue;
            }
            opaqueIndent = -1;
            var sequenceItem = IsConfigurationYamlSequenceItem(trimmed);
            while (parents.Count > 0 && indent <= parents[parents.Count - 1].Indent) {
                var parent = parents[parents.Count - 1];
                if (sequenceItem && indent == parent.Indent && parent.ChildIndent < 0) break;
                CompleteConfigurationYamlParent(parents, configuration);
            }
            if (sequenceItem && parents.Count > 0 && parents[parents.Count - 1].ChildIndent < 0) {
                var parent = parents[parents.Count - 1];
                if (!IsConfigurationScalarPath(parent.Path))
                    configuration.Add(new MermaidConfigurationSetting(parent.Path.Length == 0 ? "config" : parent.Path, null, false, parent.Span));
                parents.RemoveAt(parents.Count - 1);
                opaqueIndent = parent.Indent;
                if (SkipOpaqueConfigurationYamlNode(lines, ref index, trimmed)) opaqueScalarIndent = indent;
                continue;
            }
            var match = ConfigurationYamlMapping(trimmed);
            var key = ConfigurationYamlKey(match, out var keyError);
            var span = new MermaidSourceSpan(index + 2, indent + 1, trimmed.Length);
            if (keyError != null && (parents.Count > 0 || indent == rootIndent)) {
                ConfigurationError(result, span, keyError);
                opaqueIndent = indent;
                if (SkipOpaqueConfigurationYamlNode(lines, ref index, trimmed)) opaqueScalarIndent = indent;
                continue;
            }
            if (parents.Count == 0 && (indent != rootIndent || !match.Success || key != "config")) continue;
            if (parents.Count > 0) {
                var parent = parents[parents.Count - 1];
                if (parent.ChildIndent < 0) parents[parents.Count - 1] = (parent.Indent, parent.Path, indent, parent.Span);
                else if (parent.ChildIndent != indent) {
                    ConfigurationError(result, span, "Mermaid configuration mapping keys must use consistent sibling indentation.");
                    continue;
                }
            }
            if (key == null || raw.Substring(0, indent).IndexOf('\t') >= 0) {
                Add(result, span.Line, span.Column, span.Length, MermaidDiagnosticSeverity.Warning,
                    "Mermaid frontmatter configuration supports space-indented mappings with scalar values; this declaration is retained without application.", MermaidDiagnosticCodes.UnsupportedConfiguration);
                opaqueIndent = indent;
                if (SkipOpaqueConfigurationYamlNode(lines, ref index, trimmed)) opaqueScalarIndent = indent;
                continue;
            }
            var path = parents.Count == 0 ? string.Empty : parents[parents.Count - 1].Path;
            if (parents.Count > 0) path = path.Length == 0 ? ConfigurationPathKey(key!) : path + "." + ConfigurationPathKey(key!);
            var valueText = TrimConfigurationYaml(TrimYamlComment(match.Groups[2].Value, parents.Count == 0));
            if (!declared.Add(path)) { ConfigurationError(result, span, "Mermaid frontmatter configuration repeats '" + (path.Length == 0 ? "config" : path) + "'."); continue; }
            if (declared.Count > 256) { ConfigurationError(result, span, "Mermaid frontmatter configuration exceeds 256 keys."); break; }
            if (valueText.Length == 0) {
                if (parents.Count >= 8) { ConfigurationError(result, span, "Mermaid frontmatter configuration exceeds eight mapping levels."); break; }
                if (IsConfigurationScalarPath(path))
                    configuration.Add(new MermaidConfigurationSetting(path, null, false, span));
                parents.Add((indent, path, -1, span));
                continue;
            }
            try {
                foreach (var character in valueText)
                    if (character < ' ' && character != '\t') throw new ArgumentException("Mermaid YAML scalars cannot contain literal control characters other than tabs.");
                if (parents.Count == 0) {
                    if (IsOpaqueConfigurationYamlValue(valueText)) {
                        configuration.Add(new MermaidConfigurationSetting("config", valueText, false, span));
                        opaqueIndent = indent;
                        if (SkipOpaqueConfigurationYamlNode(lines, ref index, valueText)) opaqueScalarIndent = indent;
                        continue;
                    }
                    var sourceLines = new List<ConfigurationJsonLine>();
                    if (valueText.StartsWith("{", StringComparison.Ordinal))
                        valueText = ReadConfigurationFlowMapping(lines, ref index, valueText, sourceLines, indent + match.Groups[2].Index + 1);
                    var offsets = new List<int>();
                    var normalized = NormalizeConfigurationJson(valueText, offsets, yamlFlowMapping: true);
                    AddConfigurationJson(GeoJsonValue.Parse(normalized, StringComparer.Ordinal, ConfigurationJsonLimits).AsObject("Mermaid frontmatter config"),
                        string.Empty, span, configuration, value => ConfigurationJsonPropertySpan(value, offsets, sourceLines, sourceLineStarts, span));
                } else {
                    var isString = true;
                    string? value = valueText;
                    if (valueText.StartsWith("\"", StringComparison.Ordinal)) {
                        value = ReadDoubleQuotedConfigurationYaml(valueText);
                    } else if (valueText.StartsWith("'", StringComparison.Ordinal)) {
                        value = MermaidYamlScalar.ReadSingleQuoted(valueText);
                    } else if (valueText.StartsWith("{", StringComparison.Ordinal) || IsOpaqueConfigurationYamlValue(valueText)) {
                        value = null; isString = false;
                        opaqueIndent = indent;
                        if (SkipOpaqueConfigurationYamlNode(lines, ref index, valueText)) opaqueScalarIndent = indent;
                    } else {
                        ValidateConfigurationYamlPlainScalar(valueText);
                        if (Regex.IsMatch(valueText, @":(?:[ \t]|$)", RegexOptions.CultureInvariant))
                            throw new ArgumentException("Mermaid YAML plain scalars cannot contain a mapping separator; quote strings containing a colon followed by whitespace or an ending colon.");
                        if (IsTypedConfigurationYamlScalar(valueText)) isString = false;
                    }
                    configuration.Add(new MermaidConfigurationSetting(path, value, isString, span));
                }
            } catch (ArgumentException exception) { ConfigurationError(result, span, exception.Message); }
        }
        while (parents.Count > 0) CompleteConfigurationYamlParent(parents, configuration);
    }

    // Indentless sequence items and flow continuations still belong to their owning
    // opaque YAML node. Only a mapping entry at the owning indentation ends it.
    private static bool IsConfigurationYamlBlockContinuation(int indent, int ownerIndent, string text) =>
        indent > ownerIndent || indent == ownerIndent && !IsConfigurationYamlMappingEntry(text);

    private static bool IsConfigurationYamlSequenceItem(string text) => text.Length > 0 && text[0] == '-' &&
        (text.Length == 1 || text[1] == ' ' || text[1] == '\t');

    private static bool IsConfigurationYamlMappingEntry(string text) => !IsConfigurationYamlSequenceItem(text) &&
        ConfigurationYamlValueSeparator(text) >= 0;

    private static bool IsOpaqueConfigurationYamlValue(string value) => value.Length > 0 && "[|>&*!".IndexOf(value[0]) >= 0;

    private static void ValidateConfigurationYamlPlainScalar(string value) {
        var leading = value[0];
        if ("]},@`%".IndexOf(leading) >= 0 || (leading == '-' || leading == '?' || leading == ':') &&
            (value.Length == 1 || value[1] == ' ' || value[1] == '\t'))
            throw new ArgumentException("Mermaid YAML plain scalars cannot begin with a reserved indicator; quote the intended string.");
        // YAML 1.2 allows all non-C0 characters inside quoted scalars for JSON compatibility.
        // Its printable stream ranges still apply to the plain scalars validated here.
        for (var index = 0; index < value.Length; index++) {
            var character = value[index];
            if (character >= '\u007f' && character <= '\u009f' && character != '\u0085' || character >= '\ufffe')
                throw new ArgumentException("Mermaid YAML plain scalars must contain printable Unicode characters.");
            if (char.IsHighSurrogate(character) && index + 1 < value.Length && char.IsLowSurrogate(value[index + 1])) index++;
            else if (char.IsSurrogate(character))
                throw new ArgumentException("Mermaid YAML plain scalars must contain complete Unicode characters.");
        }
    }

    private static void CompleteConfigurationYamlParent(List<(int Indent, string Path, int ChildIndent, MermaidSourceSpan Span)> parents,
        MermaidSourceConfiguration configuration) {
        var parent = parents[parents.Count - 1];
        if (parent.ChildIndent < 0 && parent.Path.Length > 0 && !IsConfigurationScalarPath(parent.Path))
            configuration.Add(new MermaidConfigurationSetting(parent.Path, null, false, parent.Span));
        parents.RemoveAt(parents.Count - 1);
    }

    private static string ReadDoubleQuotedConfigurationYaml(string value) {
        var normalized = new StringBuilder(value.Length);
        for (var index = 0; index < value.Length; index++) {
            var character = value[index];
            if (character == '\t') normalized.Append("\\t");
            else if (character == '\\' && index + 1 < value.Length) {
                var escaped = value[++index];
                if (escaped == '\t') normalized.Append("\\t");
                else normalized.Append('\\').Append(escaped);
            } else normalized.Append(character);
        }
        return GeoJsonValue.Parse(normalized.ToString(), StringComparer.Ordinal, ConfigurationJsonLimits).AsString("Mermaid YAML scalar");
    }

    private static bool IsTypedConfigurationYamlScalar(string value) {
        if (Regex.IsMatch(value, @"^(?:~|null|Null|NULL|true|True|TRUE|false|False|FALSE|[-+]?\.(?:inf|Inf|INF)|\.(?:nan|NaN|NAN))$", RegexOptions.CultureInvariant)) return true;
        return MermaidConfigurationNumber.TryParse(value, out _);
    }

    private static string TrimConfigurationYaml(string value) => value.Trim(' ', '\t');

    private static int ConfigurationYamlIndent(string value) {
        var index = 0;
        while (index < value.Length && (value[index] == ' ' || value[index] == '\t')) index++;
        return index;
    }

    private static string TrimYamlComment(string value, bool flow = false, char quote = '\0') {
        var first = ConfigurationYamlIndent(value);
        for (var index = 0; index < value.Length; index++) {
            var character = value[index];
            if (quote == '\0') {
                if ((flow || index == first) && (character == '\'' || character == '"')) quote = character;
                else if (character == '#' && (index == 0 || value[index - 1] == ' ' || value[index - 1] == '\t')) return value.Substring(0, index);
            } else if (character == '\\' && quote == '"') index++;
            else if (character == quote) {
                if (quote == '\'' && index + 1 < value.Length && value[index + 1] == '\'') index++;
                else quote = '\0';
            }
        }
        return value;
    }
}

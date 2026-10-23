using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;

namespace ChartForgeX.Mermaid;

public sealed partial class MermaidParser {
    /// <summary>Collects the bounded inert mapping dialect used in body metadata, reusing existing YAML scalar and JSON owners.</summary>
    internal static MermaidMetadataValue ReadMetadata(string text, MermaidSourceSpan span) {
        var trimmed = text.Trim();
        if (trimmed.Length == 0) return MermaidMetadataValue.FromObject(new Dictionary<string, MermaidMetadataValue>(), span);
        if (trimmed.IndexOf('\n') < 0 || trimmed.StartsWith("{", StringComparison.Ordinal) || MetadataHasFlowSeparator(trimmed)) return new MetadataFlowReader(trimmed, span).MappingBody();
        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var position = 0;
        while (position < lines.Length && TrimConfigurationYaml(TrimYamlComment(lines[position], true)).Length == 0) position++;
        if (position == lines.Length) return MermaidMetadataValue.FromObject(new Dictionary<string, MermaidMetadataValue>(), span);
        var value = ReadMetadataBlock(lines, ref position, ConfigurationYamlIndent(lines[position]), span, 1);
        while (position < lines.Length && TrimConfigurationYaml(TrimYamlComment(lines[position], true)).Length == 0) position++;
        if (position != lines.Length || value.Kind != MermaidMetadataKind.Object) throw new ArgumentException("Agentflow metadata requires one consistently indented mapping.");
        return value;
    }

    private static bool MetadataHasFlowSeparator(string text) {
        var depth = 0; var quote = '\0';
        for (var index = 0; index < text.Length; index++) {
            MermaidResourceGuard.Checkpoint();
            var character = text[index];
            if (quote != '\0') {
                if (character == '\\' && quote == '"') index++;
                else if (character == quote) quote = '\0';
            } else if (character is '\'' or '"') quote = character;
            // A block mapping's later literal lines are opaque scalar content;
            // their braces or commas cannot select the inline mapping grammar.
            else if ((character is '\r' or '\n') && depth == 0) return false;
            else if (character is '{' or '[') depth++;
            else if (character is '}' or ']') depth--;
            else if (character == ',' && depth == 0) return true;
        }
        return false;
    }

    private static MermaidMetadataValue ReadMetadataBlock(string[] lines, ref int position, int indent, MermaidSourceSpan origin, int depth, string? firstEntry = null) {
        var mapping = new Dictionary<string, MermaidMetadataValue>(StringComparer.Ordinal);
        var items = new List<MermaidMetadataValue>();
        var sequence = firstEntry == null && TrimConfigurationYaml(lines[position]).StartsWith("- ", StringComparison.Ordinal);
        MermaidResourceGuard.CheckDepth(depth, origin);
        while (position < lines.Length) {
            MermaidResourceGuard.Checkpoint();
            // A sequence's first inline key shares this physical source line. Feed
            // it to the same mapping owner without rewriting the source array.
            var raw = firstEntry == null ? lines[position] : new string(' ', indent) + firstEntry;
            firstEntry = null;
            var text = TrimConfigurationYaml(TrimYamlComment(raw, true));
            if (text.Length == 0) { position++; continue; }
            var actual = ConfigurationYamlIndent(raw);
            if (actual < indent) break;
            if (actual != indent || raw.Substring(0, actual).IndexOf('\t') >= 0) throw new ArgumentException("Metadata mapping entries require consistent space indentation.");
            var span = new MermaidSourceSpan(origin.Line + position, actual + 1, text.Length);
            var item = text.StartsWith("- ", StringComparison.Ordinal);
            if (sequence != item) throw new ArgumentException("Metadata must not mix mapping and sequence entries.");
            string? key = null;
            var scalar = item ? text.Substring(2).Trim() : text;
            if (item && ConfigurationYamlValueSeparator(scalar) > 0) {
                var mappingIndent = actual + text.IndexOf(scalar, StringComparison.Ordinal);
                var entry = ReadMetadataBlock(lines, ref position, mappingIndent, origin, depth + 1, scalar);
                MermaidResourceGuard.ReserveElements(1, span);
                items.Add(entry);
                continue;
            }
            if (!item) {
                var separator = ConfigurationYamlValueSeparator(text);
                if (separator <= 0) throw new ArgumentException("Metadata entries require 'key: value'.");
                key = MetadataString(text.Substring(0, separator).Trim());
                scalar = text.Substring(separator + 1).Trim();
            }
            position++;
            MermaidMetadataValue value;
            if (scalar.Length == 0) {
                while (position < lines.Length && TrimConfigurationYaml(TrimYamlComment(lines[position], true)).Length == 0) position++;
                value = position < lines.Length && ConfigurationYamlIndent(lines[position]) > indent
                    ? ReadMetadataBlock(lines, ref position, ConfigurationYamlIndent(lines[position]), origin, depth + 1)
                    : MermaidMetadataValue.Null(span);
            } else if (scalar == "|" || scalar == ">") {
                var content = new List<string>();
                var contentIndent = position < lines.Length ? ConfigurationYamlIndent(lines[position]) : indent + 1;
                while (position < lines.Length && (string.IsNullOrWhiteSpace(lines[position]) || ConfigurationYamlIndent(lines[position]) > indent)) {
                    content.Add(lines[position].Length >= contentIndent ? lines[position].Substring(contentIndent) : string.Empty); position++;
                }
                value = MermaidMetadataValue.FromString(string.Join(scalar == "|" ? "\n" : " ", content) + "\n", span);
            } else value = new MetadataFlowReader(scalar, span, depth).ValueOnly();
            MermaidResourceGuard.ReserveElements(1, span);
            if (item) items.Add(value);
            else {
                if (mapping.ContainsKey(key!)) throw new ArgumentException("Metadata repeats a mapping key.");
                mapping.Add(key!, value);
            }
        }
        return sequence ? MermaidMetadataValue.FromArray(items, origin) : MermaidMetadataValue.FromObject(mapping, origin);
    }

    private static string MetadataString(string text) => text.StartsWith("'", StringComparison.Ordinal) ? MermaidYamlScalar.ReadSingleQuoted(text)
        : text.StartsWith("\"", StringComparison.Ordinal) ? ReadDoubleQuotedConfigurationYaml(text) : text;

    private sealed class MetadataFlowReader {
        private readonly string _text;
        private readonly MermaidSourceSpan _span;
        private readonly int _baseDepth;
        private int _position;
        internal MetadataFlowReader(string text, MermaidSourceSpan span, int depth = 1) { _text = text; _span = span; _baseDepth = depth; }
        internal MermaidMetadataValue MappingBody() {
            White();
            var wrapped = _position < _text.Length && _text[_position] == '{';
            if (wrapped) _position++;
            var result = Mapping(_baseDepth, wrapped);
            White();
            if (_position != _text.Length) throw new ArgumentException("Unexpected text after metadata mapping.");
            return result;
        }
        internal MermaidMetadataValue ValueOnly() {
            var result = Value(_baseDepth);
            White();
            if (_position != _text.Length) throw new ArgumentException("Unexpected text after metadata value.");
            return result;
        }
        private MermaidMetadataValue Mapping(int depth, bool wrapped) {
            MermaidResourceGuard.CheckDepth(depth, _span);
            var values = new Dictionary<string, MermaidMetadataValue>(StringComparer.Ordinal);
            White();
            if (wrapped && Take('}')) return MermaidMetadataValue.FromObject(values, _span);
            while (_position < _text.Length) {
                var key = Scalar(':');
                if (!Take(':') || key.Length == 0) throw new ArgumentException("Metadata mappings require a key followed by ':'.");
                var value = Value(depth + 1);
                key = MetadataString(key);
                if (values.ContainsKey(key)) throw new ArgumentException("Metadata repeats a mapping key.");
                values.Add(key, value);
                White();
                if (wrapped && Take('}')) return MermaidMetadataValue.FromObject(values, _span);
                if (_position == _text.Length && !wrapped) return MermaidMetadataValue.FromObject(values, _span);
                if (!Take(',')) throw new ArgumentException("Inline metadata mapping entries require ','.");
                White();
            }
            if (wrapped) throw new ArgumentException("Metadata mappings must close with '}'.");
            return MermaidMetadataValue.FromObject(values, _span);
        }
        private MermaidMetadataValue Value(int depth) {
            MermaidResourceGuard.CheckDepth(depth, _span);
            MermaidResourceGuard.ReserveElements(1, _span);
            White();
            if (Take('{')) return Mapping(depth, true);
            if (Take('[')) {
                var items = new List<MermaidMetadataValue>();
                White();
                if (Take(']')) return MermaidMetadataValue.FromArray(items, _span);
                while (_position < _text.Length) {
                    items.Add(Value(depth + 1)); White();
                    if (Take(']')) return MermaidMetadataValue.FromArray(items, _span);
                    if (!Take(',')) throw new ArgumentException("Metadata sequences require ','.");
                }
                throw new ArgumentException("Metadata sequences must close with ']'.");
            }
            var text = Scalar();
            if (text.Length == 0 || text.Equals("null", StringComparison.OrdinalIgnoreCase) || text == "~") return MermaidMetadataValue.Null(_span);
            if (text[0] is '\'' or '"') return MermaidMetadataValue.FromString(MetadataString(text), _span);
            if (text.Equals("true", StringComparison.OrdinalIgnoreCase) || text.Equals("false", StringComparison.OrdinalIgnoreCase))
                return MermaidMetadataValue.FromBoolean(text.Equals("true", StringComparison.OrdinalIgnoreCase), _span);
            if (text[0] == '&' || text[0] == '*' || text[0] == '!') throw new ArgumentException("Metadata tags, anchors and aliases are not interpreted; use an ordinary quoted value.");
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)) return MermaidMetadataValue.FromNumber(number, _span);
            if (text.TrimStart('+', '-').Equals(".inf", StringComparison.OrdinalIgnoreCase) || text.Equals(".nan", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Metadata numbers must be finite.");
            return MermaidMetadataValue.FromString(text, _span);
        }
        private string Scalar(char terminator = '\0') {
            White(); var start = _position; var quote = '\0';
            while (_position < _text.Length) {
                MermaidResourceGuard.Checkpoint();
                var character = _text[_position];
                if (quote != '\0') {
                    if (character == '\\' && quote == '"') { _position += 2; continue; }
                    if (character == quote) {
                        if (quote == '\'' && _position + 1 < _text.Length && _text[_position + 1] == '\'') { _position += 2; continue; }
                        quote = '\0';
                    }
                } else if (character is '\'' or '"') quote = character;
                else if (character == terminator || character is ',' or '}' or ']') break;
                _position++;
            }
            if (quote != '\0' || _position > _text.Length) throw new ArgumentException("Metadata strings must close their quote.");
            return _text.Substring(start, _position - start).Trim();
        }
        private void White() { while (_position < _text.Length && char.IsWhiteSpace(_text[_position])) _position++; }
        private bool Take(char character) { White(); if (_position >= _text.Length || _text[_position] != character) return false; _position++; return true; }
    }
}

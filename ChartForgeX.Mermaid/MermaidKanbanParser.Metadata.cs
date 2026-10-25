using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidKanbanParser {
    private static bool ReadMetadata(string text, int offset, Dictionary<string, string> values, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        while (offset < text.Length && char.IsWhiteSpace(text[offset])) offset++;
        if (offset == text.Length) return true;
        if (text.IndexOf("@{", offset, StringComparison.Ordinal) != offset || !text.EndsWith("}", StringComparison.Ordinal)) return Fail(offset, "Kanban metadata must use one complete inline @{ key: value } block.");
        if (text.Length - offset > VisualArtifactInterchangeValidation.MaximumTextCharacters) return Fail(offset, "Kanban metadata exceeds the native source-fact text budget.");
        var start = offset + 2; var quote = '\0'; var count = 0;
        for (var index = start; index < text.Length; index++) {
            if ((index & 255) == 0) MermaidResourceGuard.Checkpoint();
            if (ReadQuoted(text, ref index, ref quote)) continue;
            if (text[index] == '{' || text[index] == '[') return Fail(index, "Kanban metadata values are scalar text; nested objects and arrays are unsupported.");
            if (text[index] != ',' && text[index] != '}') continue;
            if (text[index] == '}' && index != text.Length - 1) return Fail(index, "Kanban metadata has trailing text after its closing brace.");
            var property = text.Substring(start, index - start).Trim();
            if (property.Length == 0) {
                if (count == 0 && text[index] == '}') return true;
                return Fail(start, "Kanban metadata cannot contain empty entries.");
            }
            var separator = -1; var propertyQuote = '\0';
            for (var at = 0; at < property.Length; at++) {
                if (ReadQuoted(property, ref at, ref propertyQuote)) continue;
                if (property[at] is ':' or '=') { separator = at; break; }
            }
            if (separator <= 0) return Fail(start, "Kanban metadata entries require key: value.");
            var located = MermaidFlowchartText.AtOffset(text, span, start, index - start);
            string key, value;
            try {
                key = Scalar(property.Substring(0, separator).Trim()); value = Scalar(property.Substring(separator + 1).Trim());
                MermaidKanbanMetadata.Scalar(key, value);
            } catch (ArgumentException exception) { return Fail(start, exception.Message); }
            if (values.ContainsKey(key)) return Fail(start, "Kanban metadata keys must be unique, independently of case: '" + key + "'.");
            if (++count > 256) return Fail(start, "Kanban metadata is limited to 256 scalar fields per declaration.");
            MermaidResourceGuard.ReserveElements(1, located); values.Add(key, value);
            try { MermaidKanbanMetadata.Bag(values); }
            catch (ArgumentException exception) { return Fail(start, exception.Message); }
            if (property[separator] == '=') MermaidParserUtilities.Add(result, located, MermaidDiagnosticSeverity.Warning,
                "Kanban '=' metadata separators are a ChartForgeX extension; use ':' for Mermaid source.", MermaidDiagnosticCodes.ApproximateMapping);
            if (!MermaidKanbanMetadata.Known(key)) MermaidParserUtilities.Add(result, located, MermaidDiagnosticSeverity.Warning,
                "Kanban metadata '" + key + "' is visible as a literal fact; no external behavior or assets are activated.", MermaidDiagnosticCodes.UnsupportedStatement);
            start = index + 1;
        }
        if (quote != '\0') return Fail(start, "Kanban metadata has an unclosed quoted scalar.");
        return true;
        bool Fail(int position, string message) { Invalid(MermaidFlowchartText.AtOffset(text, span, position, Math.Max(1, text.Length - position)), result, message); return false; }
    }

    private static bool ReadQuoted(string text, ref int index, ref char quote) {
        // The existing quote owner already handles doubled YAML quotes and JSON escapes.
        // Kanban adds only its bracket-title and retained '=' scalar entry boundaries.
        if (quote == '\0' && text[index] == '\'') {
            var before = index;
            while (before > 0 && char.IsWhiteSpace(text[before - 1])) before--;
            if (before > 0 && (text[before - 1] == '[' || text[before - 1] == '=')) {
                quote = '\''; return true;
            }
        }
        return MermaidFlowchartText.ReadQuotedCharacter(text, ref index, ref quote, metadata: true);
    }

    private static string Scalar(string value) {
        if (value.Length == 0) throw new ArgumentException("Kanban metadata and quoted titles require a scalar value.");
        if (value[0] == '\'') return MermaidYamlScalar.ReadSingleQuoted(value);
        if (value[0] == '"') return GeoJsonValue.Parse(value).AsString("Kanban quoted scalar");
        if (value.IndexOf('"') >= 0 || value.IndexOf('\'') >= 0 || value.IndexOf('{') >= 0 || value.IndexOf('[') >= 0)
            throw new ArgumentException("Quote literal syntax inside Kanban scalar values.");
        return value;
    }
}

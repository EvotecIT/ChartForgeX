using System;
using System.Collections.Generic;
using System.Xml;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

/// <summary>Shares bounded scalar and identity validation between parsed and authored boards.</summary>
internal static class MermaidKanbanMetadata {
    internal static bool Identity(string value) {
        if (string.IsNullOrWhiteSpace(value) || value.Length > VisualArtifactInterchangeValidation.MaximumIdCharacters) return false;
        foreach (var ch in value) if (!char.IsLetterOrDigit(ch) && ch != '_' && ch != '-' && ch != '.') return false;
        return true;
    }
    internal static string? Get(Dictionary<string, string> values, string key) => values.TryGetValue(key, out var value) ? value : null;
    internal static void Set(Dictionary<string, string> values, string key, string? value) {
        if (value == null) { values.Remove(key); return; }
        Scalar(key, value); values[key] = value;
    }
    internal static void Scalar(string key, string value) {
        if (!Identity(key) || key.Length > VisualArtifactInterchangeValidation.MaximumIdCharacters - "mermaid.meta.".Length || value == null || value.Length > 4096 || string.Equals(key, "label", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Kanban metadata requires scalar values of at most 4096 characters and a nonempty display label.");
        try { XmlConvert.VerifyXmlChars(value); }
        catch (XmlException exception) { throw new ArgumentException("Kanban metadata requires complete XML text characters.", exception); }
    }
    internal static void Bag(Dictionary<string, string> values) {
        if (values.Count > 256) throw new ArgumentException("Kanban declarations support at most 256 scalar metadata fields.");
        var total = 0;
        foreach (var field in values) {
            Scalar(field.Key, field.Value);
            total += field.Key.Length + field.Value.Length + 6;
            if (total > VisualArtifactInterchangeValidation.MaximumTextCharacters - 64)
                throw new ArgumentException("Kanban metadata exceeds the native full-fact text budget.");
        }
    }
    internal static bool Known(string key) => string.Equals(key, "assigned", StringComparison.OrdinalIgnoreCase)
        || string.Equals(key, "ticket", StringComparison.OrdinalIgnoreCase) || string.Equals(key, "priority", StringComparison.OrdinalIgnoreCase)
        || string.Equals(key, "label", StringComparison.OrdinalIgnoreCase);
}

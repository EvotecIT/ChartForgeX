using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

internal static class MermaidEntityRelationshipParser {
    private static readonly string[] RelationshipConnectors = {
        "||--||", "||--|{", "||--o{", "||--o|", "|o--||", "|o--|{", "|o--o{", "|o--o|",
        "}o--||", "}o--|{", "}o--o{", "}o--o|", "}|--||", "}|--|{", "}|--o{", "}|--o|",
        "||..||", "||..|{", "||..o{", "||..o|", "|o..||", "|o..|{", "|o..o{", "|o..o|",
        "}o..||", "}o..|{", "}o..o{", "}o..o|", "}|..||", "}|..|{", "}|..o{", "}|..o|"
    };

    public static void ParseStatements(MermaidEntityRelationshipDocument document, string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        var entities = new Dictionary<string, MermaidEntityNode>(StringComparer.Ordinal);
        MermaidEntityNode? activeEntity = null;
        for (var line = Math.Max(1, startLine); line <= lines.Length; line++) {
            var raw = lines[line - 1];
            var trimmed = MermaidParserUtilities.StripInlineComment(raw.Trim());
            if (MermaidParserUtilities.IsSkippable(trimmed)) continue;
            var span = new MermaidSourceSpan(line, MermaidParserUtilities.LeadingWhitespace(raw) + 1, trimmed.Length);

            if (activeEntity != null) {
                if (trimmed == "}") {
                    activeEntity = null;
                    continue;
                }

                ParseAttribute(activeEntity, trimmed, span);
                continue;
            }

            if (trimmed.EndsWith("{", StringComparison.Ordinal)) {
                var id = MermaidParserUtilities.Unquote(trimmed.Substring(0, trimmed.Length - 1).Trim());
                activeEntity = EnsureEntity(document, entities, id, span);
                continue;
            }

            if (TryParseRelationship(trimmed, span, out var relationship)) {
                document.Relationships.Add(relationship);
                EnsureEntity(document, entities, relationship.SourceId, span);
                EnsureEntity(document, entities, relationship.TargetId, span);
            } else {
                EnsureEntity(document, entities, MermaidParserUtilities.Unquote(trimmed), span);
            }
        }

        if (document.Entities.Count == 0 && document.Relationships.Count == 0) MermaidParserUtilities.Add(result, document.HeaderSpan, MermaidDiagnosticSeverity.Error, "Mermaid ER diagrams require at least one entity or relationship.");
    }

    private static bool TryParseRelationship(string text, MermaidSourceSpan span, out MermaidEntityRelationship relationship) {
        relationship = null!;
        foreach (var connector in RelationshipConnectors) {
            var index = text.IndexOf(connector, StringComparison.Ordinal);
            if (index <= 0) continue;
            var left = MermaidParserUtilities.Unquote(text.Substring(0, index).Trim());
            var rightWithLabel = text.Substring(index + connector.Length).Trim();
            var colon = rightWithLabel.IndexOf(':');
            var label = string.Empty;
            if (colon >= 0) {
                label = rightWithLabel.Substring(colon + 1).Trim();
                rightWithLabel = rightWithLabel.Substring(0, colon).Trim();
            }

            var right = MermaidParserUtilities.Unquote(rightWithLabel);
            relationship = new MermaidEntityRelationship(left, right, connector, label, span);
            return true;
        }

        return false;
    }

    private static void ParseAttribute(MermaidEntityNode entity, string text, MermaidSourceSpan span) {
        string? comment = null;
        var quoteIndex = text.IndexOf('"');
        if (quoteIndex >= 0 && text.EndsWith("\"", StringComparison.Ordinal)) {
            comment = text.Substring(quoteIndex + 1, text.Length - quoteIndex - 2);
            text = text.Substring(0, quoteIndex).Trim();
        }

        var parts = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;
        var type = parts[0];
        var name = parts.Length > 1 ? parts[1] : string.Empty;
        var key = parts.Length > 2 ? string.Join(" ", parts, 2, parts.Length - 2) : null;
        entity.Attributes.Add(new MermaidEntityAttribute(type, name, key, comment, span));
    }

    private static MermaidEntityNode EnsureEntity(MermaidEntityRelationshipDocument document, Dictionary<string, MermaidEntityNode> entities, string id, MermaidSourceSpan span) {
        if (entities.TryGetValue(id, out var existing)) return existing;
        var entity = new MermaidEntityNode(id, span);
        entities.Add(id, entity);
        document.Entities.Add(entity);
        return entity;
    }
}

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
        var activeEntityOpening = default(MermaidSourceSpan);
        var subgraphs = new Stack<MermaidSourceSpan>();
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

            if (MermaidParserUtilities.TryReadDirection(trimmed, span, result, out var direction, ignoreCase: true)) {
                if (subgraphs.Count == 0) document.Direction = direction;
                else MermaidParserUtilities.RetainUnsupported(document, trimmed, span, result, "ER subgraph direction");
                continue;
            }
            if (MermaidParserUtilities.StartsStatement(trimmed, "subgraph")) {
                subgraphs.Push(span);
                MermaidParserUtilities.RetainUnsupported(document, trimmed, span, result, "ER subgraph boundary");
                continue;
            }
            if (trimmed == "end") {
                if (subgraphs.Count > 0) {
                    subgraphs.Pop();
                    document.RawStatements.Add(new MermaidRawStatement(trimmed, span));
                } else MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "ER subgraph closing statement has no open subgraph.", MermaidDiagnosticCodes.InvalidStatement);
                continue;
            }
            if (trimmed == "}") {
                MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "ER closing brace has no open entity definition.", MermaidDiagnosticCodes.InvalidStatement);
                continue;
            }
            if (MermaidParserUtilities.StartsStatement(trimmed, "style") || MermaidParserUtilities.StartsStatement(trimmed, "class") || MermaidParserUtilities.StartsStatement(trimmed, "classDef")) {
                MermaidParserUtilities.RetainUnsupported(document, trimmed, span, result, "ER style");
                continue;
            }

            if (trimmed.EndsWith("{", StringComparison.Ordinal)) {
                var id = MermaidParserUtilities.Unquote(trimmed.Substring(0, trimmed.Length - 1).Trim());
                activeEntity = EnsureEntity(document, entities, id, span);
                activeEntityOpening = span;
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

        if (activeEntity != null) MermaidParserUtilities.Add(result, activeEntityOpening, MermaidDiagnosticSeverity.Error,
            "ER entity definitions must close with '}'.", MermaidDiagnosticCodes.InvalidStatement);
        foreach (var opening in subgraphs) MermaidParserUtilities.Add(result, opening, MermaidDiagnosticSeverity.Error,
            "ER subgraphs must close with 'end'.", MermaidDiagnosticCodes.InvalidStatement);

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

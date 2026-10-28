using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidEntityRelationshipParser {
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
        var subgraphs = new Stack<MermaidEntityGroup>();
        for (var line = Math.Max(1, startLine); line <= lines.Length; line++) {
            MermaidResourceGuard.Checkpoint();
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
                else subgraphs.Peek().Direction = direction;
                continue;
            }
            if (TryGroup(document, entities, trimmed, span, subgraphs, result)) continue;
            if (trimmed == "end") {
                if (subgraphs.Count > 0) {
                    subgraphs.Pop();
                } else MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "ER subgraph closing statement has no open subgraph.", MermaidDiagnosticCodes.InvalidStatement);
                continue;
            }
            if (trimmed == "}") {
                MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "ER closing brace has no open entity definition.", MermaidDiagnosticCodes.InvalidStatement);
                continue;
            }
            if (TryStyle(document, entities, trimmed, span, result)) continue;

            if (trimmed.EndsWith("{", StringComparison.Ordinal)) {
                activeEntity = EnsureEntity(document, entities, trimmed.Substring(0, trimmed.Length - 1).Trim(), span, result, allowGroupEndpoint: false);
                GroupMember(activeEntity, subgraphs);
                activeEntityOpening = span;
                continue;
            }

            if (TryParseRelationship(trimmed, span, result, out var relationship, out var sourceText, out var targetText)) {
                if (relationship == null) continue;
                document.Relationships.Add(relationship);
                if (sourceText.IndexOf('[') >= 0 || targetText.IndexOf('[') >= 0)
                    MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "ER aliases belong on entity declarations, not relationship endpoints.", MermaidDiagnosticCodes.InvalidStatement);
                GroupMember(EnsureEntity(document, entities, sourceText, span, result), subgraphs);
                GroupMember(EnsureEntity(document, entities, targetText, span, result), subgraphs);
            } else {
                GroupMember(EnsureEntity(document, entities, trimmed, span, result), subgraphs);
            }
        }

        if (activeEntity != null) MermaidParserUtilities.Add(result, activeEntityOpening, MermaidDiagnosticSeverity.Error,
            "ER entity definitions must close with '}'.", MermaidDiagnosticCodes.InvalidStatement);
        foreach (var opening in subgraphs) MermaidParserUtilities.Add(result, opening.Span, MermaidDiagnosticSeverity.Error,
            "ER subgraphs must close with 'end'.", MermaidDiagnosticCodes.InvalidStatement);

        ResolveGroupEndpoints(document, result);
        if (document.Entities.Count == 0 && document.Relationships.Count == 0 && document.Groups.Count == 0) MermaidParserUtilities.Add(result, document.HeaderSpan, MermaidDiagnosticSeverity.Error, "Mermaid ER diagrams require at least one entity, group or relationship.");
        ApplyStyles(document);
        foreach (var node in document.Entities) {
            MermaidInlineCaption.Diagnose(node.Label ?? node.Id, document.CaptionSpans.TryGetValue(node.Id, out var replaced) ? replaced : node.Span, result, "ER", markdownByDefault: true);
            foreach (var attribute in node.Attributes) if (attribute.Comment != null) MermaidInlineCaption.Diagnose(attribute.Comment, attribute.Span, result, "ER", markdownByDefault: true);
        }
        foreach (var group in document.Groups) MermaidInlineCaption.Diagnose(group.Label, group.Span, result, "ER", markdownByDefault: true);
        foreach (var relationship in document.Relationships) MermaidInlineCaption.Diagnose(relationship.Label, relationship.Span, result, "ER", markdownByDefault: true);
    }

    private static bool TryParseRelationship(string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result,
        out MermaidEntityRelationship? relationship, out string sourceText, out string targetText) {
        relationship = null;
        sourceText = targetText = string.Empty;
        var connector = string.Empty;
        var index = -1;
        foreach (var candidate in RelationshipConnectors) {
            var found = RelationshipConnectorIndex(text, candidate);
            if (found >= 0 && (index < 0 || found < index)) { index = found; connector = candidate; }
        }
        if (index < 0) return false;
        sourceText = text.Substring(0, index).Trim();
        var left = EndpointId(sourceText);
        targetText = text.Substring(index + connector.Length).Trim();
        var colon = RelationshipLabelSeparator(targetText);
        var label = string.Empty;
        if (colon >= 0) {
            label = MermaidParserUtilities.Unquote(targetText.Substring(colon + 1).Trim());
            targetText = targetText.Substring(0, colon).Trim();
        }
        var right = EndpointId(targetText);
        if (MissingEntityIdentity(left) || MissingEntityIdentity(right)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error,
                "ER relationship endpoints require identities before inline classes.", MermaidDiagnosticCodes.InvalidStatement);
            return true;
        }
        relationship = new MermaidEntityRelationship(left, right, connector, label, span);
        return true;
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

    private static MermaidEntityNode? EnsureEntity(MermaidEntityRelationshipDocument document, Dictionary<string, MermaidEntityNode> entities, string id, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result, bool allowGroupEndpoint = true) {
        var original = id;
        var missingAliasIdentity = id.TrimStart().StartsWith("[", StringComparison.Ordinal);
        id = ReadInlineClasses(id, out var classes);
        if (MermaidStyleParser.Suffix(original) >= 0 && (classes.Count == 0 || classes.Exists(name => name.IndexOf(":::", StringComparison.Ordinal) >= 0)))
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error,
                "ER inline classes require a name after ':::'.", MermaidDiagnosticCodes.InvalidStatement);
        string? label = null;
        if (MermaidParserUtilities.TryBracketed(id, out var bracketId, out var alias, out _)) {
            if (alias.IndexOfAny(new[] { ' ', '\t' }) >= 0 && !(alias.StartsWith("\"", StringComparison.Ordinal) && alias.EndsWith("\"", StringComparison.Ordinal)))
                MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "ER display aliases containing spaces must be double-quoted.", MermaidDiagnosticCodes.InvalidStatement);
            id = bracketId;
            label = MermaidParserUtilities.Unquote(alias);
        }
        id = MermaidParserUtilities.Unquote(id);
        if (missingAliasIdentity || MissingEntityIdentity(id)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error,
                "ER declarations require an identity before aliases or inline classes.", MermaidDiagnosticCodes.InvalidStatement);
            return null;
        }
        var group = document.Groups.Find(candidate => candidate.Id == id);
        if (allowGroupEndpoint && group != null && label == null) {
            MermaidResourceGuard.ReserveElements(classes.Count, span);
            group.Classes.AddRange(classes);
            return null;
        }
        if (entities.TryGetValue(id, out var existing)) {
            if (label != null) { existing.Label = label; document.CaptionSpans[id] = span; }
            existing.Classes.AddRange(classes);
            return existing;
        }
        var entity = new MermaidEntityNode(id, span);
        entity.Label = label;
        if (label != null) document.CaptionSpans[id] = span;
        entity.Classes.AddRange(classes);
        entities.Add(id, entity);
        document.Entities.Add(entity);
        return entity;
    }

    private static string EndpointId(string text) {
        text = ReadInlineClasses(text, out _);
        return MermaidParserUtilities.TryBracketed(text, out var id, out _, out _)
            ? MermaidParserUtilities.Unquote(id) : MermaidParserUtilities.Unquote(text);
    }

    private static int RelationshipConnectorIndex(string text, string connector) {
        var quote = '\0';
        var brackets = 0;
        for (var index = 0; index <= text.Length - connector.Length; index++) {
            var ch = text[index];
            if (quote != '\0') {
                if (ch == '\\' && index + 1 < text.Length) index++;
                else if (ch == quote) quote = '\0';
                continue;
            }
            if (ch is '"' or '`') { quote = ch; continue; }
            if (ch == '[') brackets++;
            else if (ch == ']') brackets--;
            else if (brackets == 0 && string.CompareOrdinal(text, index, connector, 0, connector.Length) == 0) return index;
        }
        return -1;
    }

    private static bool MissingEntityIdentity(string id) => string.IsNullOrWhiteSpace(id);

    private static int RelationshipLabelSeparator(string text) {
        var quote = '\0';
        var brackets = 0;
        for (var index = 0; index < text.Length; index++) {
            var ch = text[index];
            if (quote != '\0') {
                if (ch == '\\' && index + 1 < text.Length) index++;
                else if (ch == quote) quote = '\0';
                continue;
            }
            if (ch is '"' or '`') { quote = ch; continue; }
            if (ch == '[') brackets++;
            else if (ch == ']') brackets--;
            else if (ch == ':' && brackets == 0) {
                if (index + 2 < text.Length && text[index + 1] == ':' && text[index + 2] == ':') { index += 2; continue; }
                return index;
            }
        }
        return -1;
    }
}

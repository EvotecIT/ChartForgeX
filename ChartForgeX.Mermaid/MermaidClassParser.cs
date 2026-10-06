using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ChartForgeX.Mermaid;

internal static class MermaidClassParser {
    private static readonly string[] RelationshipConnectors = { "<|--", "--|>", "*--", "--*", "o--", "--o", "..|>", "<|..", "..>", "<..", "-->", "<--", "--", ".." };

    public static void ParseStatements(MermaidClassDocument document, string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        var classes = new Dictionary<string, MermaidClassNode>(StringComparer.Ordinal);
        MermaidClassNode? activeClass = null;
        var namespaces = new Stack<string>();
        for (var line = Math.Max(1, startLine); line <= lines.Length; line++) {
            var raw = lines[line - 1];
            var trimmed = MermaidParserUtilities.StripInlineComment(raw.Trim());
            if (MermaidParserUtilities.IsSkippable(trimmed)) continue;
            var span = new MermaidSourceSpan(line, MermaidParserUtilities.LeadingWhitespace(raw) + 1, trimmed.Length);

            if (activeClass != null) {
                if (trimmed == "}") {
                    activeClass = null;
                    continue;
                }

                AddMember(activeClass, trimmed, span);
                continue;
            }

            if (trimmed.StartsWith("namespace ", StringComparison.Ordinal) && trimmed.EndsWith("{", StringComparison.Ordinal)) {
                var name = trimmed.Substring(10, trimmed.Length - 11).Trim();
                if (name.Length == 0) MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Class namespace names must not be empty.");
                else namespaces.Push(namespaces.Count == 0 ? name : namespaces.Peek() + "." + name);
                continue;
            }
            if (trimmed == "}") {
                if (namespaces.Count > 0) namespaces.Pop();
                else MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Class diagram closing brace has no open class or namespace.");
                continue;
            }

            if (trimmed.StartsWith("classDef ", StringComparison.Ordinal) || trimmed.StartsWith("class ", StringComparison.Ordinal)) {
                if (trimmed.Substring(6).TrimStart().StartsWith("{", StringComparison.Ordinal)) {
                    MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Class declarations require an identifier.");
                    continue;
                }
                ParseClassOrStyle(document, classes, trimmed, span, ref activeClass);
                if (namespaces.Count > 0 && trimmed.StartsWith("class ", StringComparison.Ordinal)) {
                    var ids = trimmed.Substring(6).Split(new[] { ' ', '{', '[' }, StringSplitOptions.RemoveEmptyEntries);
                    if (ids.Length > 0 && classes.TryGetValue(ids[0], out var declared)) declared.Namespace = namespaces.Peek();
                }
                continue;
            }

            if (trimmed.StartsWith("style ", StringComparison.Ordinal) || trimmed.StartsWith("linkStyle ", StringComparison.Ordinal) || trimmed.StartsWith("cssClass ", StringComparison.Ordinal)) {
                document.StyleStatements.Add(new MermaidClassStyleStatement(trimmed, span));
                continue;
            }

            if (trimmed.StartsWith("<<", StringComparison.Ordinal)) {
                ParseAnnotation(document, classes, trimmed, span);
                continue;
            }

            if (TryParseRelationship(trimmed, span, out var relationship)) {
                document.Relationships.Add(relationship);
                EnsureClass(document, classes, relationship.SourceId, span);
                EnsureClass(document, classes, relationship.TargetId, span);
                continue;
            }

            var colonIndex = trimmed.IndexOf(':');
            if (colonIndex > 0) {
                var classId = trimmed.Substring(0, colonIndex).Trim();
                var member = trimmed.Substring(colonIndex + 1).Trim();
                if (classId.Length > 0 && member.Length > 0) AddMember(EnsureClass(document, classes, classId, span), member, span);
                continue;
            }

            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning, "Unrecognized class diagram statement was retained but not rendered exactly: " + trimmed);
            document.StyleStatements.Add(new MermaidClassStyleStatement(trimmed, span));
        }

        if (activeClass != null || namespaces.Count > 0) MermaidParserUtilities.Add(result, document.HeaderSpan, MermaidDiagnosticSeverity.Error, "Class definitions and namespaces must close with '}'.");
        if (document.Classes.Count == 0 && document.Relationships.Count == 0) MermaidParserUtilities.Add(result, document.HeaderSpan, MermaidDiagnosticSeverity.Error, "Mermaid class diagrams require at least one class or relationship.");
    }

    private static void ParseClassOrStyle(MermaidClassDocument document, Dictionary<string, MermaidClassNode> classes, string text, MermaidSourceSpan span, ref MermaidClassNode? activeClass) {
        if (text.StartsWith("classDef ", StringComparison.Ordinal) || text.StartsWith("class ", StringComparison.Ordinal) && text.IndexOf(",", StringComparison.Ordinal) >= 0) {
            document.StyleStatements.Add(new MermaidClassStyleStatement(text, span));
            return;
        }

        var body = text.Substring(6).Trim();
        var assignmentParts = body.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (assignmentParts.Length > 1 && body.IndexOf("[", StringComparison.Ordinal) < 0 && body.IndexOf("{", StringComparison.Ordinal) < 0) {
            document.StyleStatements.Add(new MermaidClassStyleStatement(text, span));
            return;
        }

        var braceIndex = body.IndexOf('{');
        if (braceIndex >= 0) {
            var id = body.Substring(0, braceIndex).Trim();
            activeClass = EnsureClass(document, classes, id, span);
            var remainder = body.Substring(braceIndex + 1).Trim();
            if (remainder.EndsWith("}", StringComparison.Ordinal)) {
                remainder = remainder.Substring(0, remainder.Length - 1).Trim();
                if (remainder.Length > 0) AddMember(activeClass, remainder, span);
                activeClass = null;
            }

            return;
        }

        if (MermaidParserUtilities.TryBracketed(body, out var bracketId, out var label, out _)) {
            EnsureClass(document, classes, bracketId, span).Label = MermaidParserUtilities.Unquote(label);
            return;
        }

        var parts = body.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1) EnsureClass(document, classes, parts[0], span);
        else document.StyleStatements.Add(new MermaidClassStyleStatement(text, span));
    }

    private static void ParseAnnotation(MermaidClassDocument document, Dictionary<string, MermaidClassNode> classes, string text, MermaidSourceSpan span) {
        var end = text.IndexOf(">>", StringComparison.Ordinal);
        if (end <= 2) return;
        var annotation = text.Substring(2, end - 2).Trim();
        var id = text.Substring(end + 2).Trim();
        if (id.Length == 0) return;
        EnsureClass(document, classes, id, span).Annotations.Add(annotation);
    }

    private static bool TryParseRelationship(string text, MermaidSourceSpan span, out MermaidClassRelationship relationship) {
        relationship = null!;
        foreach (var connector in RelationshipConnectors) {
            var index = text.IndexOf(connector, StringComparison.Ordinal);
            if (index <= 0) continue;
            var left = text.Substring(0, index).Trim();
            var rightWithLabel = text.Substring(index + connector.Length).Trim();
            var label = SplitLabel(ref rightWithLabel);
            var source = Regex.Match(left, @"^(\S+?)(?:\s+""([^""]+)"")?$", RegexOptions.CultureInvariant);
            var target = Regex.Match(rightWithLabel, @"^(?:""([^""]+)""\s+)?(\S+)$", RegexOptions.CultureInvariant);
            if (!source.Success || !target.Success) return false;
            relationship = new MermaidClassRelationship(CleanEndpoint(source.Groups[1].Value), CleanEndpoint(target.Groups[2].Value), connector, label, span) {
                SourceMultiplicity = source.Groups[2].Success ? source.Groups[2].Value : null,
                TargetMultiplicity = target.Groups[1].Success ? target.Groups[1].Value : null
            };
            return true;
        }

        return false;
    }

    private static string? SplitLabel(ref string text) {
        var colon = text.IndexOf(':');
        if (colon < 0) return null;
        var label = text.Substring(colon + 1).Trim();
        text = text.Substring(0, colon).Trim();
        return label.Length == 0 ? null : label;
    }

    private static string CleanEndpoint(string value) => MermaidParserUtilities.Unquote(value.Trim().Trim('"', '`'));

    private static MermaidClassNode EnsureClass(MermaidClassDocument document, Dictionary<string, MermaidClassNode> classes, string id, MermaidSourceSpan span) {
        id = CleanEndpoint(id);
        if (classes.TryGetValue(id, out var existing)) return existing;
        var node = new MermaidClassNode(id, span);
        classes.Add(id, node);
        document.Classes.Add(node);
        return node;
    }

    private static void AddMember(MermaidClassNode node, string text, MermaidSourceSpan span) {
        if (text.StartsWith("<<", StringComparison.Ordinal) && text.EndsWith(">>", StringComparison.Ordinal)) {
            node.Annotations.Add(text.Substring(2, text.Length - 4).Trim());
            return;
        }

        node.Members.Add(new MermaidClassMember(text, text.IndexOf("(", StringComparison.Ordinal) >= 0 && text.IndexOf(")", StringComparison.Ordinal) > text.IndexOf("(", StringComparison.Ordinal), span));
    }
}

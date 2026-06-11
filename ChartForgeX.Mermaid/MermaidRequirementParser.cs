using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

internal static class MermaidRequirementParser {
    private static readonly HashSet<string> RequirementTypes = new(StringComparer.OrdinalIgnoreCase) {
        "requirement",
        "functionalRequirement",
        "interfaceRequirement",
        "performanceRequirement",
        "physicalRequirement",
        "designConstraint"
    };

    private static readonly HashSet<string> RelationshipTypes = new(StringComparer.OrdinalIgnoreCase) {
        "contains",
        "copies",
        "derives",
        "satisfies",
        "verifies",
        "refines",
        "traces"
    };

    public static void ParseStatements(MermaidRequirementDocument document, string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        var requirements = new Dictionary<string, MermaidRequirementNode>(StringComparer.Ordinal);
        var elements = new Dictionary<string, MermaidRequirementElement>(StringComparer.Ordinal);
        for (var line = Math.Max(1, startLine); line <= lines.Length; line++) {
            var raw = lines[line - 1];
            var trimmed = MermaidParserUtilities.StripInlineComment(raw.Trim());
            if (MermaidParserUtilities.IsSkippable(trimmed)) continue;
            var span = new MermaidSourceSpan(line, MermaidParserUtilities.LeadingWhitespace(raw) + 1, trimmed.Length);
            document.Statements.Add(new MermaidRawStatement(trimmed, span));

            if (StartsWithKeyword(trimmed, "direction")) {
                var direction = trimmed.Substring(9).Trim();
                document.Direction = direction.Length == 0 ? null : direction;
                continue;
            }

            if (TryParseRequirementStart(trimmed, out var requirementType, out var requirementName, out var requirementClasses)) {
                if (elements.ContainsKey(requirementName)) {
                    MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Requirement name '" + requirementName + "' is already defined as an element.");
                    var shadow = new MermaidRequirementNode(requirementName, requirementType, span);
                    AddClasses(shadow.Classes, requirementClasses);
                    line = ReadRequirementBlock(document, lines, line + 1, shadow, result);
                    continue;
                }

                var requirement = EnsureRequirement(document, requirements, requirementName, requirementType, span);
                AddClasses(requirement.Classes, requirementClasses);
                line = ReadRequirementBlock(document, lines, line + 1, requirement, result);
                continue;
            }

            if (TryParseElementStart(trimmed, out var elementName, out var elementClasses)) {
                if (requirements.ContainsKey(elementName)) {
                    MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Requirement element name '" + elementName + "' is already defined as a requirement.");
                    var shadow = new MermaidRequirementElement(elementName, span);
                    AddClasses(shadow.Classes, elementClasses);
                    line = ReadElementBlock(document, lines, line + 1, shadow, result);
                    continue;
                }

                var element = EnsureElement(document, elements, elementName, span);
                AddClasses(element.Classes, elementClasses);
                line = ReadElementBlock(document, lines, line + 1, element, result);
                continue;
            }

            if (TryParseRelationship(trimmed, span, out var relationship)) {
                ValidateRelationshipEndpoint(relationship.SourceName, requirements, elements, span, "source", result);
                ValidateRelationshipEndpoint(relationship.TargetName, requirements, elements, span, "target", result);
                document.Relationships.Add(relationship);
                continue;
            }

            if (TryParseClassAssignment(trimmed, out var names, out var classes)) {
                ApplyClasses(requirements, elements, names, classes);
                document.StyleStatements.Add(new MermaidRawStatement(trimmed, span));
                continue;
            }

            if (IsStyleStatement(trimmed)) {
                document.StyleStatements.Add(new MermaidRawStatement(trimmed, span));
                continue;
            }

            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning, "Unrecognized requirement diagram statement was retained but not rendered exactly: " + trimmed);
            document.StyleStatements.Add(new MermaidRawStatement(trimmed, span));
        }

        if (document.Requirements.Count == 0 && document.Elements.Count == 0) MermaidParserUtilities.Add(result, document.HeaderSpan, MermaidDiagnosticSeverity.Error, "Mermaid requirement diagrams require at least one requirement or element.");
    }

    private static int ReadRequirementBlock(MermaidRequirementDocument document, string[] lines, int startLine, MermaidRequirementNode requirement, MermaidParseResult<MermaidDocument> result) {
        for (var line = startLine; line <= lines.Length; line++) {
            var raw = lines[line - 1];
            var trimmed = MermaidParserUtilities.StripInlineComment(raw.Trim());
            if (MermaidParserUtilities.IsSkippable(trimmed)) continue;
            var span = new MermaidSourceSpan(line, MermaidParserUtilities.LeadingWhitespace(raw) + 1, trimmed.Length);
            document.Statements.Add(new MermaidRawStatement(trimmed, span));
            if (trimmed == "}") return line;

            if (!TryParseField(trimmed, out var key, out var value)) {
                MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning, "Unrecognized requirement field was retained but not rendered exactly: " + trimmed);
                continue;
            }

            switch (NormalizeKey(key)) {
                case "id":
                    requirement.RequirementId = value;
                    break;
                case "text":
                    requirement.Text = value;
                    break;
                case "risk":
                    requirement.Risk = value;
                    break;
                case "verifymethod":
                    requirement.VerifyMethod = value;
                    break;
                default:
                    MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning, "Unrecognized requirement field was retained but not rendered exactly: " + key);
                    break;
            }
        }

        MermaidParserUtilities.Add(result, requirement.Span, MermaidDiagnosticSeverity.Error, "Requirement block '" + requirement.Name + "' must close with '}'.");
        return lines.Length;
    }

    private static int ReadElementBlock(MermaidRequirementDocument document, string[] lines, int startLine, MermaidRequirementElement element, MermaidParseResult<MermaidDocument> result) {
        for (var line = startLine; line <= lines.Length; line++) {
            var raw = lines[line - 1];
            var trimmed = MermaidParserUtilities.StripInlineComment(raw.Trim());
            if (MermaidParserUtilities.IsSkippable(trimmed)) continue;
            var span = new MermaidSourceSpan(line, MermaidParserUtilities.LeadingWhitespace(raw) + 1, trimmed.Length);
            document.Statements.Add(new MermaidRawStatement(trimmed, span));
            if (trimmed == "}") return line;

            if (!TryParseField(trimmed, out var key, out var value)) {
                MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning, "Unrecognized requirement element field was retained but not rendered exactly: " + trimmed);
                continue;
            }

            switch (NormalizeKey(key)) {
                case "type":
                    element.ElementType = value;
                    break;
                case "docref":
                    element.DocumentReference = value;
                    break;
                default:
                    MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning, "Unrecognized requirement element field was retained but not rendered exactly: " + key);
                    break;
            }
        }

        MermaidParserUtilities.Add(result, element.Span, MermaidDiagnosticSeverity.Error, "Element block '" + element.Name + "' must close with '}'.");
        return lines.Length;
    }

    private static bool TryParseRequirementStart(string text, out string requirementType, out string name, out IReadOnlyList<string> classes) {
        requirementType = string.Empty;
        name = string.Empty;
        classes = Array.Empty<string>();
        var brace = text.IndexOf('{');
        if (brace < 0) return false;
        var prefix = text.Substring(0, brace).Trim();
        var parts = prefix.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !RequirementTypes.Contains(parts[0])) return false;
        requirementType = parts[0];
        name = ExtractClasses(parts[1], out classes);
        return name.Length > 0;
    }

    private static bool TryParseElementStart(string text, out string name, out IReadOnlyList<string> classes) {
        name = string.Empty;
        classes = Array.Empty<string>();
        var brace = text.IndexOf('{');
        if (brace < 0) return false;
        var prefix = text.Substring(0, brace).Trim();
        var parts = prefix.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !string.Equals(parts[0], "element", StringComparison.OrdinalIgnoreCase)) return false;
        name = ExtractClasses(parts[1], out classes);
        return name.Length > 0;
    }

    private static bool TryParseRelationship(string text, MermaidSourceSpan span, out MermaidRequirementRelationship relationship) {
        relationship = null!;
        foreach (var type in RelationshipTypes) {
            var forward = " - " + type + " -> ";
            var forwardIndex = text.IndexOf(forward, StringComparison.OrdinalIgnoreCase);
            if (forwardIndex > 0) {
                relationship = new MermaidRequirementRelationship(CleanName(text.Substring(0, forwardIndex)), CleanName(text.Substring(forwardIndex + forward.Length)), type, span);
                return true;
            }

            var reverse = " <- " + type + " - ";
            var reverseIndex = text.IndexOf(reverse, StringComparison.OrdinalIgnoreCase);
            if (reverseIndex > 0) {
                relationship = new MermaidRequirementRelationship(CleanName(text.Substring(reverseIndex + reverse.Length)), CleanName(text.Substring(0, reverseIndex)), type, span);
                return true;
            }
        }

        return false;
    }

    private static bool TryParseClassAssignment(string text, out IReadOnlyList<string> names, out IReadOnlyList<string> classes) {
        names = Array.Empty<string>();
        classes = Array.Empty<string>();
        if (text.StartsWith("class ", StringComparison.OrdinalIgnoreCase)) {
            var parts = text.Substring(6).Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return false;
            names = SplitTokens(parts[0]);
            classes = SplitTokens(parts[1]);
            return names.Count > 0 && classes.Count > 0;
        }

        var shorthand = text.IndexOf(":::", StringComparison.Ordinal);
        if (shorthand <= 0) return false;
        names = new[] { CleanName(text.Substring(0, shorthand)) };
        classes = SplitTokens(text.Substring(shorthand + 3));
        return names.Count > 0 && classes.Count > 0;
    }

    private static bool IsStyleStatement(string text) =>
        text.StartsWith("style ", StringComparison.OrdinalIgnoreCase) ||
        text.StartsWith("classDef ", StringComparison.OrdinalIgnoreCase);

    private static void ValidateRelationshipEndpoint(string name, Dictionary<string, MermaidRequirementNode> requirements, Dictionary<string, MermaidRequirementElement> elements, MermaidSourceSpan span, string role, MermaidParseResult<MermaidDocument> result) {
        if (requirements.ContainsKey(name) || elements.ContainsKey(name)) return;
        MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Requirement relationship " + role + " '" + name + "' must refer to a declared requirement or element.");
    }

    private static bool TryParseField(string text, out string key, out string value) {
        key = string.Empty;
        value = string.Empty;
        var colon = text.IndexOf(':');
        if (colon <= 0) return false;
        key = text.Substring(0, colon).Trim();
        value = MermaidParserUtilities.Unquote(text.Substring(colon + 1));
        return key.Length > 0;
    }

    private static MermaidRequirementNode EnsureRequirement(MermaidRequirementDocument document, Dictionary<string, MermaidRequirementNode> requirements, string name, string requirementType, MermaidSourceSpan span) {
        if (requirements.TryGetValue(name, out var existing)) {
            existing.RequirementType = requirementType;
            return existing;
        }

        var node = new MermaidRequirementNode(name, requirementType, span);
        requirements.Add(name, node);
        document.Requirements.Add(node);
        return node;
    }

    private static MermaidRequirementElement EnsureElement(MermaidRequirementDocument document, Dictionary<string, MermaidRequirementElement> elements, string name, MermaidSourceSpan span) {
        if (elements.TryGetValue(name, out var existing)) return existing;
        var element = new MermaidRequirementElement(name, span);
        elements.Add(name, element);
        document.Elements.Add(element);
        return element;
    }

    private static void ApplyClasses(Dictionary<string, MermaidRequirementNode> requirements, Dictionary<string, MermaidRequirementElement> elements, IReadOnlyList<string> names, IReadOnlyList<string> classes) {
        foreach (var name in names) {
            if (requirements.TryGetValue(name, out var requirement)) AddClasses(requirement.Classes, classes);
            if (elements.TryGetValue(name, out var element)) AddClasses(element.Classes, classes);
        }
    }

    private static void AddClasses(List<string> target, IReadOnlyList<string> classes) {
        foreach (var className in classes) {
            if (className.Length > 0 && !target.Contains(className)) target.Add(className);
        }
    }

    private static IReadOnlyList<string> SplitTokens(string text) {
        var values = new List<string>();
        foreach (var part in text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)) {
            var value = CleanName(part);
            if (value.Length > 0) values.Add(value);
        }

        return values;
    }

    private static string ExtractClasses(string text, out IReadOnlyList<string> classes) {
        var index = text.IndexOf(":::", StringComparison.Ordinal);
        if (index < 0) {
            classes = Array.Empty<string>();
            return CleanName(text);
        }

        classes = SplitTokens(text.Substring(index + 3));
        return CleanName(text.Substring(0, index));
    }

    private static bool StartsWithKeyword(string text, string keyword) {
        if (!text.StartsWith(keyword, StringComparison.OrdinalIgnoreCase)) return false;
        return text.Length == keyword.Length || char.IsWhiteSpace(text[keyword.Length]);
    }

    private static string NormalizeKey(string key) {
        var chars = new List<char>(key.Length);
        foreach (var ch in key) if (char.IsLetterOrDigit(ch)) chars.Add(char.ToLowerInvariant(ch));
        return new string(chars.ToArray());
    }

    private static string CleanName(string value) => MermaidParserUtilities.Unquote(value.Trim());
}

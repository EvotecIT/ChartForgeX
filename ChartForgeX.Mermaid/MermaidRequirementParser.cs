using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidRequirementParser {
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
        MermaidRequirementNode? requirement = null;
        MermaidRequirementElement? element = null;
        foreach (var statement in ReadStatements(lines, startLine, result)) {
            MermaidResourceGuard.Checkpoint();
            var text = statement.Text; var span = statement.Span;
            document.Statements.Add(statement);
            if (text == "}") {
                if (requirement == null && element == null) Invalid("An unmatched requirement block terminator.", span);
                requirement = null; element = null;
                continue;
            }
            if (requirement != null || element != null) {
                if (text.IndexOf('{') >= 0) { Invalid("Requirement and element blocks cannot be nested.", span); continue; }
                if (!TryParseField(text, out var key, out var value)) { Invalid("Expected a requirement or element field.", span); continue; }
                if (requirement != null) ReadRequirementField(requirement, key, value, span, result);
                else ReadElementField(element!, key, value, span, result);
                continue;
            }
            if (MermaidParserUtilities.TryReadDirection(text, span, result, out var direction, ignoreCase: true)) {
                if (direction != null) document.Direction = direction;
                continue;
            }
            if (TryParseRequirementStart(text, out var type, out var name, out var classes)) {
                MermaidResourceGuard.CheckDepth(1, span);
                if (elements.ContainsKey(name)) Invalid("Requirement name '" + name + "' is already defined as an element.", span);
                requirement = EnsureRequirement(document, requirements, name, type, span);
                AddClasses(requirement.Classes, classes);
                continue;
            }
            if (TryParseElementStart(text, out name, out classes)) {
                MermaidResourceGuard.CheckDepth(1, span);
                if (requirements.ContainsKey(name)) Invalid("Element name '" + name + "' is already defined as a requirement.", span);
                element = EnsureElement(document, elements, name, span);
                AddClasses(element.Classes, classes);
                continue;
            }
            if (TryParseRelationship(text, span, out var relationship)) { document.Relationships.Add(relationship); continue; }
            if (TryReadStyle(document, statement, result)) continue;
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning,
                "Unrecognized requirement statement is retained without native application: " + text, MermaidDiagnosticCodes.UnsupportedStatement);
            document.RawStatements.Add(statement);
        }
        if (requirement != null || element != null) Invalid("Requirement and element blocks must close with '}'.", (requirement as MermaidAstNode ?? element!).Span);
        foreach (var relationship in document.Relationships) {
            ValidateRelationshipEndpoint(relationship.SourceName, requirements, elements, relationship.Span, "source", result);
            ValidateRelationshipEndpoint(relationship.TargetName, requirements, elements, relationship.Span, "target", result);
        }
        ApplyStyles(document, requirements, elements, result);
        if (document.Requirements.Count == 0 && document.Elements.Count == 0) Invalid("Mermaid requirement diagrams require at least one requirement or element.", document.HeaderSpan);
        void Invalid(string message, MermaidSourceSpan span) => MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, message, MermaidDiagnosticCodes.InvalidStatement);
    }

    private static void ReadRequirementField(MermaidRequirementNode requirement, string key, string value, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        switch (NormalizeKey(key)) {
            case "id": requirement.RequirementId = value; return;
            case "text": requirement.Text = value; return;
            case "risk":
                requirement.Risk = value;
                if (!IsOneOf(value, "low", "medium", "high")) Invalid("Requirement risk must be Low, Medium, or High.");
                return;
            case "verifymethod":
                requirement.VerifyMethod = value;
                if (!IsOneOf(value, "analysis", "inspection", "test", "demonstration")) Invalid("Requirement verification method must be Analysis, Inspection, Test, or Demonstration.");
                return;
            default:
                MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning, "Unknown requirement field: " + key, MermaidDiagnosticCodes.UnsupportedStatement);
                return;
        }
        void Invalid(string message) => MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, message, MermaidDiagnosticCodes.InvalidStatement);
    }

    private static void ReadElementField(MermaidRequirementElement element, string key, string value, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        switch (NormalizeKey(key)) {
            case "type": element.ElementType = value; break;
            case "docref": element.DocumentReference = value; break;
            default: MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning, "Unknown element field: " + key, MermaidDiagnosticCodes.UnsupportedStatement); break;
        }
    }

    private static bool TryParseRequirementStart(string text, out string requirementType, out string name, out IReadOnlyList<string> classes) {
        requirementType = string.Empty;
        name = string.Empty;
        classes = Array.Empty<string>();
        var brace = text.IndexOf('{');
        if (brace < 0) return false;
        var prefix = text.Substring(0, brace).Trim();
        if (!MermaidStyleParser.SplitHead(prefix, out var type, out var identity) || !RequirementTypes.Contains(type)) return false;
        requirementType = type;
        name = ExtractClasses(identity, out classes);
        return name.Length > 0;
    }

    private static bool TryParseElementStart(string text, out string name, out IReadOnlyList<string> classes) {
        name = string.Empty;
        classes = Array.Empty<string>();
        var brace = text.IndexOf('{');
        if (brace < 0) return false;
        var prefix = text.Substring(0, brace).Trim();
        if (!MermaidStyleParser.SplitHead(prefix, out var type, out var identity) || !string.Equals(type, "element", StringComparison.OrdinalIgnoreCase)) return false;
        name = ExtractClasses(identity, out classes);
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
            if (!MermaidStyleParser.SplitHead(text.Substring(6).Trim(), out var selectors, out var assigned)) return false;
            names = MermaidStyleParser.Names(selectors);
            classes = SplitTokens(assigned);
            return names.Count > 0 && classes.Count > 0;
        }

        var shorthand = MermaidStyleParser.Suffix(text);
        if (shorthand <= 0) return false;
        names = new[] { CleanName(text.Substring(0, shorthand)) };
        classes = SplitTokens(text.Substring(shorthand + 3));
        return names.Count > 0 && classes.Count > 0;
    }

    private static bool IsOneOf(string value, params string[] allowed) {
        foreach (var item in allowed) if (string.Equals(value, item, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

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
        var index = MermaidStyleParser.Suffix(text);
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

    private static string CleanName(string value) => MermaidParserUtilities.Unquote(value.Trim().TrimEnd(';').TrimEnd());
}

using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidRequirementParser {
    private static bool TryReadStyle(MermaidRequirementDocument document, MermaidRawStatement statement, MermaidParseResult<MermaidDocument> result) {
        var text = statement.Text;
        if (MermaidStyleParser.TryDefinition(text, statement.Span, result, document.ClassDefinitions, "Requirement")) {
            document.StyleStatements.Add(statement); return true;
        }
        if (MermaidParserUtilities.StartsStatement(text, "style", ignoreCase: true)
            || TryParseClassAssignment(text, out _, out _)) {
            document.StyleStatements.Add(statement); return true;
        }
        return false;
    }

    private static void ApplyStyles(MermaidRequirementDocument document, Dictionary<string, MermaidRequirementNode> requirements,
        Dictionary<string, MermaidRequirementElement> elements, MermaidParseResult<MermaidDocument> result) {
        foreach (var statement in document.StyleStatements) {
            MermaidResourceGuard.Checkpoint();
            var direct = MermaidParserUtilities.StartsStatement(statement.Text, "style", ignoreCase: true);
            IReadOnlyList<string> names; IReadOnlyList<string> classes = System.Array.Empty<string>();
            var styles = new Dictionary<string, string>();
            if (direct) {
                if (!MermaidStyleParser.SplitHead(statement.Text.Substring(6).Trim(), out var selector, out var value)) {
                    Invalid("Requirement styles require node names and declarations."); continue;
                }
                names = MermaidStyleParser.Names(selector);
                MermaidStyleParser.Read(styles, value, statement.Span, result, "Requirement");
            } else if (!TryParseClassAssignment(statement.Text, out names, out classes)) continue;
            foreach (var name in names) {
                if (requirements.TryGetValue(name, out var requirement)) {
                    if (direct) MermaidStyleParser.Copy(requirement.Styles, styles); else AddClasses(requirement.Classes, classes);
                } else if (elements.TryGetValue(name, out var element)) {
                    if (direct) MermaidStyleParser.Copy(element.Styles, styles); else AddClasses(element.Classes, classes);
                } else Invalid("Requirement style target '" + name + "' must refer to a declared requirement or element.");
            }

            void Invalid(string message) => MermaidParserUtilities.Add(result, statement.Span, MermaidDiagnosticSeverity.Error,
                message, MermaidDiagnosticCodes.InvalidStatement);
        }
    }
}

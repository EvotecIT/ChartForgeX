using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidEntityRelationshipParser {
    private static bool TryStyle(MermaidEntityRelationshipDocument document, Dictionary<string, MermaidEntityNode> entities,
        string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        if (MermaidStyleParser.TryDefinition(text, span, result, document.ClassDefinitions, "ER")) {
            document.StyleStatements.Add(new MermaidRawStatement(text, span));
            return true;
        }
        var direct = MermaidParserUtilities.StartsStatement(text, "style");
        if (!direct && !MermaidParserUtilities.StartsStatement(text, "class")) return false;
        document.StyleStatements.Add(new MermaidRawStatement(text, span));
        if (!MermaidStyleParser.SplitHead(text.Substring(5).Trim(), out var selector, out var value)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error,
                "ER style statements require a selector and a value.", MermaidDiagnosticCodes.InvalidStatement);
            return true;
        }
        var declarations = new Dictionary<string, string>();
        if (direct) MermaidStyleParser.Read(declarations, value, span, result, "ER");
        var missing = false;
        foreach (var id in MermaidStyleParser.Names(selector)) {
            var group = document.Groups.Find(candidate => candidate.Id == id);
            if (group != null) {
                if (direct) MermaidStyleParser.Copy(group.Styles, declarations);
                else foreach (var name in MermaidStyleParser.Names(value)) group.Classes.Add(name);
            } else if (entities.TryGetValue(id, out var node)) {
                if (direct) MermaidStyleParser.Copy(node.Styles, declarations);
                else foreach (var name in MermaidStyleParser.Names(value)) node.Classes.Add(name);
            } else missing = true;
        }
        if (missing) MermaidParserUtilities.RetainUnsupported(document, text, span, result, "ER style target not yet declared");
        return true;
    }

    private static string ReadInlineClasses(string text, out List<string> classes) {
        classes = new List<string>();
        var suffix = MermaidStyleParser.Suffix(text);
        if (suffix < 0) return text;
        classes = MermaidStyleParser.Names(text.Substring(suffix + 3));
        return text.Substring(0, suffix).TrimEnd();
    }

    private static void ApplyStyles(MermaidEntityRelationshipDocument document) {
        var definitions = MermaidStyleParser.Definitions(document.ClassDefinitions);
        foreach (var group in document.Groups) {
            var resolved = MermaidStyleParser.Resolve(definitions, group.Classes, group.Styles, includeDefault: false);
            group.Styles.Clear();
            MermaidStyleParser.Copy(group.Styles, resolved);
        }
        foreach (var node in document.Entities) {
            var resolved = MermaidStyleParser.Resolve(definitions, node.Classes, node.Styles);
            node.Styles.Clear();
            MermaidStyleParser.Copy(node.Styles, resolved);
        }
    }
}

using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidFlowchartParser {
    private static bool TryUseCaseStyle(MermaidUseCaseDocument document, string text, MermaidSourceSpan span,
        MermaidParseResult<MermaidDocument> result) {
        if (MermaidStyleParser.TryDefinition(text, span, result, document.ClassDefinitions, "UseCase", UseCaseStyle)) return true;
        if (MermaidParserUtilities.StartsStatement(text, "class") || MermaidParserUtilities.StartsStatement(text, "style")) {
            MermaidResourceGuard.ReserveElements(1, span);
            document.SelectorStatements.Add(new MermaidRawStatement(text, span)); return true;
        }
        if (MermaidParserUtilities.StartsStatement(text, "linkStyle")) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error,
                "Pinned UseCase notation selects named associations with 'style ID'; linkStyle is flowchart syntax.", MermaidDiagnosticCodes.InvalidStatement);
            return true;
        }
        return false;
    }

    private static void ReadUseCaseStereotype(string text, ref int position, MermaidFlowchartNode node,
        Dictionary<string, MermaidFlowchartNode> nodes, MermaidSourceSpan statementSpan, MermaidParseResult<MermaidDocument> result) {
        if (!StartsWith(text, position, "<<")) return;
        var start = position; var end = text.IndexOf(">>", position + 2, StringComparison.Ordinal);
        if (end < 0) { Invalid(statementSpan, "UseCase stereotypes must close with '>>'."); position = text.Length; return; }
        position = end + 2;
        var raw = text.Substring(start, position - start); var inner = text.Substring(start + 2, end - start - 2);
        var caption = inner.Trim();
        var span = MermaidFlowchartText.AtOffset(text, statementSpan, start, raw.Length);
        if (caption.Length == 0) { Invalid(span, "UseCase stereotypes require visible text."); return; }
        if (nodes.TryGetValue(node.Id, out var existing) && existing.Properties.TryGetValue("usecase.stereotype", out var previous) && previous != caption)
            Invalid(span, "UseCase element '" + node.Id + "' has conflicting stereotypes.");
        MermaidLabelText.Diagnose(caption, MermaidFlowchartText.AtOffset(text, statementSpan,
            start + 2 + inner.Length - inner.TrimStart().Length, caption.Length), result, "UseCase stereotype");
        node.Properties["usecase.stereotype"] = caption;
        node.Properties["usecase.stereotype.raw"] = raw;
        node.PropertySpans["usecase.stereotype"] = span;
        SkipWhitespace(text, ref position);
        void Invalid(MermaidSourceSpan location, string message) => MermaidParserUtilities.Add(result, location,
            MermaidDiagnosticSeverity.Error, message, MermaidDiagnosticCodes.InvalidStatement);
    }

    internal static void CompleteUseCase(MermaidUseCaseDocument document, MermaidParseResult<MermaidDocument> result) {
        foreach (var node in document.Nodes) ValidateMetadata(node);
        foreach (var statement in document.SelectorStatements) {
            var assignment = statement.Text.StartsWith("class ", StringComparison.Ordinal);
            var body = statement.Text.Substring(6).Trim();
            if (!MermaidStyleParser.SplitHead(body, out var selectors, out var tail)) {
                Invalid(statement.Span, "UseCase class/style statements require a selector and value."); continue;
            }
            var classes = assignment ? MermaidStyleParser.Names(tail) : null;
            foreach (var id in MermaidStyleParser.Names(selectors)) {
                var node = document.Nodes.Find(candidate => candidate.Id == id);
                var edge = document.Edges.Find(candidate => candidate.Id == id);
                if (node == null && edge == null) {
                    if (document.Subgraphs.Exists(group => group.Id == id)) Unsupported(statement.Span,
                        "UseCase boundary class/style is retained without native boundary application.");
                    else Invalid(statement.Span, "UseCase class/style target '" + id + "' is unresolved or anonymous.");
                    continue;
                }
                if (classes != null) {
                    MermaidResourceGuard.ReserveElements(classes.Count, statement.Span);
                    (node?.Classes ?? edge!.Classes).AddRange(classes); continue;
                }
                var styles = node?.Styles ?? edge!.Styles;
                MermaidStyleParser.Read(styles, tail, statement.Span, result, "UseCase", accepts: (property, value) =>
                    node != null ? MermaidNativeStyles.SupportsSurface(property,value) : property != "fill" && UseCaseStyle(property, value));
            }
        }
        var definitions = MermaidStyleParser.Definitions(document.ClassDefinitions);
        foreach (var node in document.Nodes) Apply(node.Classes, node.Styles, node.Span, false);
        foreach (var edge in document.Edges) Apply(edge.Classes, edge.Styles, edge.Span, true);

        void Apply(List<string> classes, Dictionary<string, string> direct, MermaidSourceSpan span, bool edge) {
            var authored = new HashSet<string>(direct.Keys, StringComparer.Ordinal);
            var resolved = MermaidStyleParser.Resolve(definitions, classes, direct, span);
            direct.Clear(); MermaidStyleParser.Copy(direct, resolved);
            foreach (var item in resolved) {
                if (authored.Contains(item.Key) || !UseCaseStyle(item.Key, item.Value)) continue;
                if ((!edge && MermaidNativeStyles.UnsupportedSurface(item.Key)) || (edge && item.Key == "fill"))
                    Unsupported(span, "UseCase " + (edge ? "association" : "node") + " style '" + item.Key + "' is retained without native application.");
            }
        }
        void ValidateMetadata(MermaidFlowchartNode node) {
            var actor = node.Shape == MermaidFlowchartNodeShape.Actor;
            var type = node.Properties.TryGetValue("type", out var authoredType) ? authoredType : "normal";
            var business = node.Properties.TryGetValue("business", out var authoredBusiness) && authoredBusiness == "true";
            var icon = node.Properties.TryGetValue("icon", out var authoredIcon) && authoredIcon.Length > 0;
            foreach (var key in new[] { "type", "business", "icon" }) if (node.Properties.TryGetValue(key, out var value)) {
                var span = node.PropertySpans.TryGetValue(key, out var propertySpan) ? propertySpan : node.Span;
                var token = node.PropertyTokens.TryGetValue(key, out var raw) ? raw : value;
                if (key == "business" && token is not ("true" or "false")) Invalid(span, "UseCase business metadata must be a boolean.");
                else if (!actor && key != "business") Invalid(span, "Only actors accept UseCase '" + key + "' metadata.");
                else if (key == "type" && value is not ("normal" or "hollow" or "awesome")) Invalid(span, "UseCase actor type must be normal, hollow or awesome.");
                else if ((key is "type" or "icon") && !UseCaseStringToken(token)) Invalid(span, "UseCase actor '" + key + "' metadata requires an identifier or quoted string.");
            }
            if (actor && icon && type != "normal") Invalid(node.Span, "UseCase actor icon cannot combine with a non-normal type.");
            if (actor && business && (icon || type == "awesome")) Invalid(node.Span, "Business actors require normal or hollow geometry.");
            if (!actor && business && node.Shape == MermaidFlowchartNodeShape.Rectangle) Invalid(node.Span, "A rectangular use case cannot use business geometry.");
        }
        void Invalid(MermaidSourceSpan span, string message) => MermaidParserUtilities.Add(result, span,
            MermaidDiagnosticSeverity.Error, message, MermaidDiagnosticCodes.InvalidStatement);
        void Unsupported(MermaidSourceSpan span, string message) => MermaidParserUtilities.Add(result, span,
            MermaidDiagnosticSeverity.Warning, message, MermaidDiagnosticCodes.UnsupportedStatement);
    }

    // Pinned metadata accepts Identifier or PlainString, not unquoted qualified ids or Boolean/number tokens.
    private static bool UseCaseStringToken(string token) {
        if (token.Length >= 2 && (token[0] is '\'' or '"') && token[token.Length - 1] == token[0]) {
            for (var index = 1; index < token.Length - 1; index++) if (token[index] == token[0] || token[index] is '\r' or '\n') return false;
            return true;
        }
        if (token.Length == 0 || token is "true" or "false" || !(token[0] is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or '_')) return false;
        foreach (var ch in token) if (!(ch is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '_')) return false;
        return true;
    }

    private static bool UseCaseStyle(string property, string value) => property=="fill"
        ? MermaidStyleValues.IsColor(value):MermaidNativeStyles.SupportsEdge(property,value);
}

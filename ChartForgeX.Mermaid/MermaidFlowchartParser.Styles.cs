using System;
using System.Collections.Generic;
using System.Globalization;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidFlowchartParser {
    private static void ParseClassSuffix(string text, ref int position, List<string> classes, MermaidSourceSpan span,
        MermaidParseResult<MermaidDocument> result) {
        if (StartsWith(text, position, ":::")) {
            position += 3;
            var start = position;
            while (position < text.Length && !char.IsWhiteSpace(text[position]) && !StartsWith(text, position, ":::")
                && !IsClassLinkBoundary(text, position)) position++;
            // Unlike a class statement's comma-separated list, an inline suffix is one class name.
            if (position > start) classes.Add(text.Substring(start, position - start));
            else MermaidParserUtilities.Add(result, MermaidFlowchartText.AtOffset(text, span, start - 3, 3),
                MermaidDiagnosticSeverity.Error, "Flowchart inline classes require a class name after ':::'.", MermaidDiagnosticCodes.InvalidStatement);
            SkipWhitespace(text, ref position);
        }
    }

    private static bool IsClassLinkBoundary(string text, int position) {
        // o/x belong to the class identifier even immediately before a compact link.
        // A single hyphen also belongs to an identifier such as "external-box".
        var ch = text[position];
        if (ch == '<' || ch == '>') return true;
        if (ch != '-' && ch != '.' && ch != '=') return false;
        // Count link-body strokes only. An endpoint in "-only" or "-xml"
        // cannot turn the identifier's single hyphen into a link.
        return position + 1 < text.Length
            && (text[position + 1] == '-' || text[position + 1] == '.' || text[position + 1] == '=');
    }

    private static bool TryParseClassDefinition(MermaidFlowchartDocument document, string text, MermaidSourceSpan span,
        MermaidParseResult<MermaidDocument> result) {
        if (!text.StartsWith("classDef ", StringComparison.Ordinal)) return false;
        var body = text.Substring("classDef ".Length).Trim();
        var split = body.IndexOfAny(new[] { ' ', '\t' });
        if (split <= 0) return false;
        var styles = new Dictionary<string, string>();
        ReadStyles(styles, body.Substring(split + 1), span, result, edge: false);
        foreach (var name in SplitCsv(body.Substring(0, split))) {
            var definition = new MermaidStyleDefinition(name, span);
            CopyStyles(definition.Styles, styles);
            document.ClassDefinitions.Add(definition);
        }
        return true;
    }

    private static bool TryParseStyleAssignment(MermaidFlowchartDocument document, Dictionary<string, MermaidFlowchartNode> nodes,
        string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        if (!text.StartsWith("style ", StringComparison.Ordinal)) return false;
        var body = text.Substring("style ".Length).Trim();
        var split = body.IndexOfAny(new[] { ' ', '\t' });
        if (split <= 0) return false;
        var nodeId = body.Substring(0, split);
        var styles = new Dictionary<string, string>();
        ReadStyles(styles, body.Substring(split + 1), span, result, edge: false);
        CopyStyles(GetOrAddNode(document, nodes, nodeId, span).Styles, styles);
        return true;
    }

    private static bool TryParseLinkStyle(MermaidFlowchartDocument document, string text, MermaidSourceSpan span,
        MermaidParseResult<MermaidDocument> result) {
        if (!text.StartsWith("linkStyle ", StringComparison.Ordinal)) return false;
        var body = text.Substring("linkStyle ".Length).Trim();
        var split = body.IndexOfAny(new[] { ' ', '\t' });
        if (split <= 0) return false;
        var style = new MermaidFlowchartLinkStyle(body.Substring(0, split), span);
        if (style.Selector != "default") {
            foreach (var selector in style.Selector.Split(',')) {
                if (!int.TryParse(selector, NumberStyles.None, CultureInfo.InvariantCulture, out var index) || index >= document.Edges.Count) {
                    MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error,
                        "Flowchart linkStyle must select an existing zero-based edge index or 'default'.", MermaidDiagnosticCodes.InvalidStatement);
                    return true;
                }
            }
        }
        ReadStyles(style.Styles, body.Substring(split + 1), span, result, edge: true);
        document.LinkStyles.Add(style);
        return true;
    }

    private static void ApplyClassDefinitions(MermaidFlowchartDocument document) {
        var definitions = MermaidStyleParser.Definitions(document.ClassDefinitions);
        foreach (var node in document.Nodes) {
            var resolved = new Dictionary<string, string>();
            Apply("default");
            Apply("node");
            foreach (var className in node.Classes) Apply(className);
            // Direct declarations win independently of whether they precede a classDef.
            CopyStyles(resolved, node.Styles);
            node.Styles.Clear();
            CopyStyles(node.Styles, resolved);

            void Apply(string name) {
                if (definitions.TryGetValue(name, out var styles)) CopyStyles(resolved, styles);
            }
        }
    }

    private static void ApplyLinkStyles(MermaidFlowchartDocument document) {
        MermaidFlowchartLinkStyle? defaults = null;
        var selected = new Dictionary<int, MermaidFlowchartLinkStyle>();
        foreach (var style in document.LinkStyles) {
            if (style.Selector == "default") { defaults = style; continue; }
            foreach (var selector in SplitCsv(style.Selector)) {
                // Numeric selectors are validated against the edges present at their declaration.
                var index = int.Parse(selector, NumberStyles.None, CultureInfo.InvariantCulture);
                selected[index] = style;
            }
        }
        for (var index = 0; index < document.Edges.Count; index++) {
            var styles = document.Edges[index].Styles;
            if (defaults != null) CopyStyles(styles, defaults.Styles);
            if (selected.TryGetValue(index, out var explicitStyle)) CopyStyles(styles, explicitStyle.Styles);
        }
    }

    private static void ReadStyles(Dictionary<string, string> styles, string text, MermaidSourceSpan span,
        MermaidParseResult<MermaidDocument> result, bool edge) => MermaidStyleParser.Read(styles, text, span, result, "Flowchart", edge);

    private static void CopyStyles(Dictionary<string, string> target, Dictionary<string, string> source) => MermaidStyleParser.Copy(target, source);
}

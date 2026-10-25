using System;
using System.Collections.Generic;
using System.Globalization;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidFlowchartParser {
    private static void ParseClassSuffix(string text, ref int position, List<string> classes, MermaidSourceSpan span,
        MermaidParseResult<MermaidDocument> result) {
        if (MermaidStyleParser.InlineClass(text,ref position,out var name,out var start)) {
            // Unlike a class statement's comma-separated list, an inline suffix is one class name.
            if (name.Length>0) classes.Add(name);
            else MermaidParserUtilities.Add(result, MermaidFlowchartText.AtOffset(text, span, start - 3, 3),
                MermaidDiagnosticSeverity.Error, "Flowchart inline classes require a class name after ':::'.", MermaidDiagnosticCodes.InvalidStatement);
        }
    }

    private static bool TryParseClassDefinition(MermaidFlowchartDocument document, string text, MermaidSourceSpan span,
        MermaidParseResult<MermaidDocument> result) {
        if (!text.StartsWith("classDef ", StringComparison.Ordinal)) return false;
        var body = text.Substring("classDef ".Length).Trim();
        var split = body.IndexOfAny(new[] { ' ', '\t' });
        if (split <= 0) return false;
        var styles = new Dictionary<string, string>();
        ReadStyles(styles, body.Substring(split + 1), span, result, edge: false,declaration:true);
        var names = SplitCsv(body.Substring(0, split));
        MermaidResourceGuard.ReserveElements(checked((long)names.Count * (1L + styles.Count)), span);
        foreach (var name in names) {
            MermaidResourceGuard.Checkpoint();
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
        if(document.Edges.Exists(edge=>edge.Id==nodeId)) {
            MermaidParserUtilities.Add(result,span,MermaidDiagnosticSeverity.Warning,
                "Flowchart named-edge style is retained without application; use class or linkStyle for edge paints.",MermaidDiagnosticCodes.UnsupportedStatement);
            return true;
        }
        var styles = new Dictionary<string, string>();
        ReadStyles(styles, body.Substring(split + 1), span, result, edge: false);
        CopyStyles(GetOrAddNode(document, nodes, nodeId, span).Styles, styles, span);
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
        var declarations = body.Substring(split + 1);
        if (MermaidFlowchartCurves.Applies(document.Kind)) declarations = ReadLinkInterpolation(style, declarations, text, span, result);
        if (!string.IsNullOrWhiteSpace(declarations)) ReadStyles(style.Styles, declarations, span, result, edge: true);
        document.LinkStyles.Add(style);
        return true;
    }

    private static void ApplyClassDefinitions(MermaidFlowchartDocument document,MermaidParseResult<MermaidDocument> result) {
        var definitions = MermaidStyleParser.Definitions(document.ClassDefinitions);
        foreach (var node in document.Nodes) {
            var classes = new List<string>(node.Classes.Count + 1) { "node" };
            classes.AddRange(node.Classes);
            var authored=new HashSet<string>(node.Styles.Keys,StringComparer.Ordinal);
            var resolved = MermaidStyleParser.Resolve(definitions, classes, node.Styles, node.Span);
            node.Styles.Clear();
            CopyStyles(node.Styles, resolved);
            DiagnoseNodeProperties(node.Styles,authored,node.Span);
        }
        foreach(var group in document.Subgraphs) {
            var authored=new HashSet<string>(group.Styles.Keys,StringComparer.Ordinal);
            var resolved=MermaidStyleParser.Resolve(definitions,group.Classes,group.Styles,group.Span,includeDefault:false);
            group.Styles.Clear();CopyStyles(group.Styles,resolved);DiagnoseNodeProperties(group.Styles,authored,group.Span);
        }
        foreach(var edge in document.Edges) {
            var resolved=MermaidStyleParser.Resolve(definitions,edge.Classes,edge.Styles,edge.Span,includeDefault:false);
            edge.Styles.Clear();CopyStyles(edge.Styles,resolved);
        }
        void DiagnoseNodeProperties(Dictionary<string,string> styles,HashSet<string> authored,MermaidSourceSpan span) {
            foreach(var item in styles)if(!authored.Contains(item.Key)&&MermaidNativeStyles.UnsupportedSurface(item.Key))
                MermaidParserUtilities.Add(result,span,MermaidDiagnosticSeverity.Warning,
                    "Flowchart node/group style '"+item.Key+"' is retained without native application.",MermaidDiagnosticCodes.UnsupportedStatement);
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
            if (defaults != null) CopyStyles(styles, defaults.Styles, document.Edges[index].Span);
            if (selected.TryGetValue(index, out var explicitStyle)) CopyStyles(styles, explicitStyle.Styles, document.Edges[index].Span);
        }
    }

    private static void ReadStyles(Dictionary<string, string> styles, string text, MermaidSourceSpan span,
        MermaidParseResult<MermaidDocument> result, bool edge,bool declaration=false) => MermaidStyleParser.Read(styles, text, span, result, "Flowchart", edge,
            nativeProperty:edge||declaration?MermaidNativeStyles.SupportsEdge:MermaidNativeStyles.SupportsSurface);

    private static void CopyStyles(Dictionary<string, string> target, Dictionary<string, string> source, MermaidSourceSpan? span = null) => MermaidStyleParser.Copy(target, source, span);
}

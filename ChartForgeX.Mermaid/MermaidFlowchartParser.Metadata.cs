using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidFlowchartParser {
    private static bool TryParseNodeGroup(MermaidFlowchartDocument document, Dictionary<string, MermaidFlowchartNode> nodes, string text, ref int position, MermaidSourceSpan span, MermaidFlowchartSubgraph? subgraph, MermaidParseResult<MermaidDocument> result, out List<MermaidFlowchartNode> group) {
        group = new List<MermaidFlowchartNode>();
        do {
            if (!TryParseNode(text, ref position, span, out var node)) {
                if (group.Count > 0) MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Flowchart '&' requires a following node.");
                return false;
            }
            if (StartsWith(text, position, "@{")) ReadNodeProperties(text, ref position, node, span, result);
            SkipWhitespace(text, ref position);
            ParseClassSuffix(text, ref position, node.Classes);
            if (subgraph != null) { node.SubgraphId = subgraph.Id; AddNodeToSubgraph(subgraph, node.Id); }
            AddOrUpdateNode(document, nodes, node);
            group.Add(node);
            SkipWhitespace(text, ref position);
            if (position >= text.Length || text[position] != '&') break;
            position++;
        } while (true);
        return true;
    }

    private static void ReadNodeProperties(string text, ref int position, MermaidFlowchartNode node, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        var start = position;
        position += 2;
        var propertyStart = position;
        char quote = '\0';
        while (position < text.Length) {
            var ch = text[position];
            if (quote != '\0') {
                if (ch == quote && !MermaidFlowchartStatements.IsEscaped(text, position)) quote = '\0';
            } else if (ch == '"' || ch == '\'') quote = ch;
            else if (ch == ',' || ch == '}') {
                AddNodeProperty(text.Substring(propertyStart, position - propertyStart), node, span, result);
                position++;
                if (ch == '}') return;
                propertyStart = position;
                continue;
            }
            position++;
        }
        MermaidParserUtilities.Add(result, new MermaidSourceSpan(span.Line, span.Column + start, text.Length - start), MermaidDiagnosticSeverity.Error, "Flowchart node metadata must close with '}'.");
    }

    private static void AddNodeProperty(string property, MermaidFlowchartNode node, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        if (string.IsNullOrWhiteSpace(property)) return;
        var colon = property.IndexOf(':');
        if (colon <= 0) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Flowchart node properties require 'key: value' syntax.");
            return;
        }
        var key = Unquote(property.Substring(0, colon).Trim());
        var value = Unquote(property.Substring(colon + 1).Trim());
        node.Properties[key] = value;
        if (key == "label") node.Text = value;
        else if (key == "shape") {
            if (TryResolveShape(value, out var shape)) node.Shape = shape;
            else MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning, "Flowchart shape '" + value + "' is retained but rendered as a rectangle.");
        } else MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning, "Flowchart node property '" + key + "' is retained but not rendered.");
    }

    private static bool TryResolveShape(string value, out MermaidFlowchartNodeShape shape) {
        shape = MermaidFlowchartNodeShape.Rectangle;
        switch (value) {
            case "rect": case "rectangle": case "proc": case "process": break;
            case "rounded": case "event": shape = MermaidFlowchartNodeShape.Rounded; break;
            case "stadium": case "pill": case "terminal": shape = MermaidFlowchartNodeShape.Stadium; break;
            case "subproc": case "subroutine": case "subprocess": shape = MermaidFlowchartNodeShape.Subroutine; break;
            case "cyl": case "cylinder": case "db": case "database": shape = MermaidFlowchartNodeShape.Cylinder; break;
            case "circle": case "circ": shape = MermaidFlowchartNodeShape.Circle; break;
            case "dbl-circ": case "double-circle": shape = MermaidFlowchartNodeShape.DoubleCircle; break;
            case "diamond": case "diam": case "decision": shape = MermaidFlowchartNodeShape.Rhombus; break;
            case "hex": case "hexagon": case "prepare": shape = MermaidFlowchartNodeShape.Hexagon; break;
            case "lean-r": case "in-out": shape = MermaidFlowchartNodeShape.Parallelogram; break;
            case "lean-l": case "out-in": shape = MermaidFlowchartNodeShape.ParallelogramAlt; break;
            case "trap-b": case "trapezoid": shape = MermaidFlowchartNodeShape.Trapezoid; break;
            case "trap-t": case "inv-trapezoid": shape = MermaidFlowchartNodeShape.TrapezoidAlt; break;
            case "cloud": shape = MermaidFlowchartNodeShape.Cloud; break;
            default: return false;
        }
        return true;
    }
}

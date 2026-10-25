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
            if (StartsWith(text, position, "@{")) ReadNodeProperties(text, ref position, node, span, result, document is MermaidUseCaseDocument, document.Kind == MermaidDiagramKind.Flowchart);
            SkipWhitespace(text, ref position);
            if (document is MermaidUseCaseDocument) ReadUseCaseStereotype(text, ref position, node, nodes, span, result);
            ParseClassSuffix(text, ref position, node.Classes, span, result);
            if (document is MermaidUseCaseDocument && node.Classes.Count == 1 && node.Classes[0].IndexOf(',') >= 0) {
                var classes = SplitCsv(node.Classes[0]); node.Classes.Clear(); node.Classes.AddRange(classes);
            }
            if (subgraph != null) AddNodeToSubgraph(subgraph, node.Id);
            AddOrUpdateNode(document, nodes, node);
            group.Add(node);
            SkipWhitespace(text, ref position);
            if (position >= text.Length || text[position] != '&') break;
            position++;
        } while (true);
        return true;
    }

    private static void ReadNodeProperties(string text, ref int position, MermaidFlowchartNode node, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result, bool useCase = false, bool assets = false) =>
        ReadFlowchartProperties(text, ref position, span, result, (property, propertySpan) => AddNodeProperty(property, node, propertySpan, result, useCase, assets), "node");

    private static void AddNodeProperty(string property, MermaidFlowchartNode node, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result, bool useCase, bool assets = false) {
        if (!TryReadFlowchartProperty(property, span, result, "node", out var key, out var value, out var scalar, out var scalarStart)) return;
        node.Properties[key] = value;
        node.PropertySpans[key] = span;
        node.PropertyTokens[key] = scalar;
        node.PropertyValueSpans[key] = MermaidFlowchartText.AtOffset(property, span, scalarStart, scalar.Length);
        if (assets && MermaidFlowchartAssets.IsMetadataKey(key)) return;
        if (useCase && key is "type" or "business" or "icon") return;
        if (key == "label") {
            node.Text = value;
            node.LabelSpan = MermaidFlowchartText.AtOffset(property, span, scalarStart, scalar.Length);
        } else if (key == "shape") {
            if (TryResolveShape(value, out var shape)) node.Shape = shape;
            else MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning, "Flowchart shape '" + value + "' is retained but rendered as a rectangle.");
        } else MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning, "Flowchart node property '" + key + "' is retained but not rendered.");
    }

    private static string ReadPropertyScalar(string value) => value.StartsWith("'", StringComparison.Ordinal)
        ? MermaidYamlScalar.ReadSingleQuoted(value) : Unquote(value);

    internal static bool TryResolveShape(string value, out MermaidFlowchartNodeShape shape) {
        shape = MermaidFlowchartNodeShape.Rectangle;
        switch (value) {
            case "rect": case "rectangle": case "proc": case "process": break;
            case "rounded": case "event": shape = MermaidFlowchartNodeShape.Rounded; break;
            case "stadium": case "pill": case "terminal": shape = MermaidFlowchartNodeShape.Stadium; break;
            case "subproc": case "subroutine": case "subprocess": case "fr-rect": case "framed-rectangle": shape = MermaidFlowchartNodeShape.Subroutine; break;
            case "cyl": case "cylinder": case "db": case "database": shape = MermaidFlowchartNodeShape.Cylinder; break;
            case "circle": case "circ": shape = MermaidFlowchartNodeShape.Circle; break;
            case "dbl-circ": case "double-circle": shape = MermaidFlowchartNodeShape.DoubleCircle; break;
            case "diamond": case "diam": case "decision": case "question": shape = MermaidFlowchartNodeShape.Rhombus; break;
            case "hex": case "hexagon": case "prepare": shape = MermaidFlowchartNodeShape.Hexagon; break;
            case "lean-r": case "lean-right": case "in-out": shape = MermaidFlowchartNodeShape.Parallelogram; break;
            case "lean-l": case "lean-left": case "out-in": shape = MermaidFlowchartNodeShape.ParallelogramAlt; break;
            case "trap-b": case "trapezoid": case "trapezoid-bottom": case "priority": shape = MermaidFlowchartNodeShape.Trapezoid; break;
            case "trap-t": case "inv-trapezoid": case "trapezoid-top": case "manual": shape = MermaidFlowchartNodeShape.TrapezoidAlt; break;
            case "odd": shape = MermaidFlowchartNodeShape.Asymmetric; break;
            case "cloud": shape = MermaidFlowchartNodeShape.Cloud; break;
            case "lin-doc": case "lined-document": shape = MermaidFlowchartNodeShape.LinedDocument; break;
            case "bang": shape = MermaidFlowchartNodeShape.ScallopedBurst; break;
            case "h-cyl": case "das": case "horizontal-cylinder": shape = MermaidFlowchartNodeShape.HorizontalCylinder; break;
            case "datastore": case "data-store": shape = MermaidFlowchartNodeShape.DataStore; break;
            case "folder": case "directory": shape = MermaidFlowchartNodeShape.Folder; break;
            case "bucket": shape = MermaidFlowchartNodeShape.Bucket; break;
            case "console": shape = MermaidFlowchartNodeShape.Console; break;
            case "browser": shape = MermaidFlowchartNodeShape.Browser; break;
            case "person": shape = MermaidFlowchartNodeShape.Person; break;
            case "text": shape = MermaidFlowchartNodeShape.TextBlock; break;
            case "notch-rect": case "card": case "notched-rectangle": shape = MermaidFlowchartNodeShape.NotchedRectangle; break;
            case "lin-rect": case "lined-rectangle": case "lined-process": case "lin-proc": case "shaded-process": shape = MermaidFlowchartNodeShape.LinedRectangle; break;
            case "sm-circ": case "start": case "small-circle": case "f-circ": case "junction": case "filled-circle": shape = MermaidFlowchartNodeShape.FilledCircle; break;
            case "fr-circ": case "stop": case "framed-circle": shape = MermaidFlowchartNodeShape.FramedCircle; break;
            case "fork": case "join": shape = MermaidFlowchartNodeShape.SynchronizationBar; break;
            case "hourglass": case "collate": shape = MermaidFlowchartNodeShape.Hourglass; break;
            case "brace": case "comment": case "brace-l": shape = MermaidFlowchartNodeShape.BraceLeft; break;
            case "brace-r": shape = MermaidFlowchartNodeShape.BraceRight; break;
            case "braces": shape = MermaidFlowchartNodeShape.Braces; break;
            case "bolt": case "com-link": case "lightning-bolt": shape = MermaidFlowchartNodeShape.LightningBolt; break;
            case "doc": case "document": shape = MermaidFlowchartNodeShape.Document; break;
            case "delay": case "half-rounded-rectangle": shape = MermaidFlowchartNodeShape.HalfRoundedRectangle; break;
            case "lin-cyl": case "disk": case "lined-cylinder": shape = MermaidFlowchartNodeShape.LinedCylinder; break;
            case "curv-trap": case "curved-trapezoid": case "display": shape = MermaidFlowchartNodeShape.CurvedTrapezoid; break;
            case "div-rect": case "div-proc": case "divided-rectangle": case "divided-process": shape = MermaidFlowchartNodeShape.DividedRectangle; break;
            case "tri": case "extract": case "triangle": shape = MermaidFlowchartNodeShape.Triangle; break;
            case "win-pane": case "internal-storage": case "window-pane": shape = MermaidFlowchartNodeShape.WindowPane; break;
            case "notch-pent": case "loop-limit": case "notched-pentagon": shape = MermaidFlowchartNodeShape.NotchedPentagon; break;
            case "flip-tri": case "manual-file": case "flipped-triangle": shape = MermaidFlowchartNodeShape.TriangleDown; break;
            case "sl-rect": case "manual-input": case "sloped-rectangle": shape = MermaidFlowchartNodeShape.SlopedRectangle; break;
            case "docs": case "documents": case "st-doc": case "stacked-document": shape = MermaidFlowchartNodeShape.StackedDocument; break;
            case "st-rect": case "procs": case "processes": case "stacked-rectangle": shape = MermaidFlowchartNodeShape.StackedRectangle; break;
            case "bow-rect": case "stored-data": case "bow-tie-rectangle": shape = MermaidFlowchartNodeShape.BowTieRectangle; break;
            case "cross-circ": case "summary": case "crossed-circle": shape = MermaidFlowchartNodeShape.CrossedCircle; break;
            case "tag-doc": case "tagged-document": shape = MermaidFlowchartNodeShape.TaggedDocument; break;
            case "tag-rect": case "tagged-rectangle": case "tag-proc": case "tagged-process": shape = MermaidFlowchartNodeShape.TaggedRectangle; break;
            case "flag": case "paper-tape": shape = MermaidFlowchartNodeShape.WavyRectangle; break;
            default: return false;
        }
        return true;
    }
}

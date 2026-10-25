using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.VisualBlocks;
using ChartForgeX.Core;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidBlockParser {
    private const int MaximumBlockLayoutItems = 10000;
    private const int MaximumBlockLayoutEdges = 20000;

    public static void ParseStatements(MermaidBlockDocument document, string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        var ids = new Dictionary<string, MermaidBlockItem>(StringComparer.Ordinal);
        var parents = new Stack<MermaidBlockItem>();
        var pending = new List<(MermaidBlockItem Item, string? Parent)>();
        var styles = new List<(string Text, MermaidSourceSpan Span)>();
        var sequence = 0;
        for (var line = Math.Max(1, startLine); line <= lines.Length; line++) {
            MermaidResourceGuard.Checkpoint();
            var raw = lines[line - 1];
            var text = MermaidParserUtilities.StripInlineComment(raw.Trim());
            if (MermaidParserUtilities.IsSkippable(text)) continue;
            var span = new MermaidSourceSpan(line, MermaidParserUtilities.LeadingWhitespace(raw) + 1, text.Length);
            document.Statements.Add(new MermaidRawStatement(text, span));
            if (StartsWithKeyword(text, "title")) { document.Title = MermaidParserUtilities.Unquote(text.Substring(5).Trim()); continue; }
            if (StartsWithKeyword(text, "columns")) {
                var value = text.Substring(7).Trim(); var auto = value.Equals("auto", StringComparison.OrdinalIgnoreCase);
                int? columns = null;
                if (!auto && (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) || parsed < 1 || parsed > 24)) {
                    Error(span, "Mermaid block column counts must be between one and twenty-four, or auto."); continue;
                } else if (!auto) columns = int.Parse(value, CultureInfo.InvariantCulture);
                if (parents.Count == 0) { document.Columns = columns; document.AutoColumns = auto; }
                else { parents.Peek().Columns = columns; parents.Peek().AutoColumns = auto; }
                continue;
            }
            if (StartsWithKeyword(text, "classDef")) {
                document.StyleStatements.Add(new MermaidRawStatement(text, span));
                MermaidStyleParser.TryDefinition(text, span, result, document.ClassDefinitions, "Block", BlockNativeStyle); continue;
            }
            if (StartsWithKeyword(text, "style") || StartsWithKeyword(text, "class")) {
                document.StyleStatements.Add(new MermaidRawStatement(text, span)); styles.Add((text, span)); continue;
            }
            if (text.Equals("end", StringComparison.OrdinalIgnoreCase)) {
                if (parents.Count == 0) Error(span, "Mermaid block end has no open composite."); else parents.Pop();
                continue;
            }
            if (StartsWithKeyword(text, "block")) {
                var body = text.Substring(5).Trim();
                var token = body.StartsWith(":", StringComparison.Ordinal) ? body.Substring(1).Trim() : body;
                var itemSpan = 1;
                if (token.Length == 0) token = "__cfx-composite-" + (++sequence).ToString(CultureInfo.InvariantCulture);
                if (!ReadSpan(ref token, out itemSpan)) { Error(span, "Composite spans must be between one and twenty-four."); continue; }
                if (!ValidId(token)) { Error(span, "Composite declarations require one identifier and optional span."); continue; }
                var item = new MermaidBlockItem(token, "", itemSpan, BlockLayoutShape.Rectangle, false, span) { IsComposite = true };
                if (!Add(item, span)) continue;
                parents.Push(item); MermaidResourceGuard.CheckDepth(parents.Count, span); continue;
            }
            var links = EdgeParts(text);
            if (links.Count > 1) {
                var initial = links[0].Text; var edgeLabel = InlineEdgeLabel(ref initial);
                var source = Endpoint(initial, span);
                for (var index = 1; index < links.Count; index++) {
                    var part = links[index]; var body = part.Text; var label = edgeLabel;
                    if (body.StartsWith("|", StringComparison.Ordinal)) {
                        var close = body.IndexOf('|', 1);
                        if (close < 0) { Error(span, "Block connector labels must close their pipe delimiter."); break; }
                        label = body.Substring(1, close - 1); body = body.Substring(close + 1).Trim();
                    }
                    var nextLabel = InlineEdgeLabel(ref body);
                    var target = Endpoint(body, span);
                    if (source != null && target != null) {
                        if (document.Edges.Count >= MaximumBlockLayoutEdges) Error(span, "Mermaid block diagrams support no more than 20000 edges.");
                        else {
                            MermaidResourceGuard.ReserveEdges(1, span);
                            var edge = new MermaidBlockEdge(source.Id, target.Id, label, part.Arrow.IndexOf('>') >= 0, span) { Visible = part.Arrow != "~~~" };
                            if (part.Arrow.StartsWith("==", StringComparison.Ordinal)) edge.Stroke = new ChartStrokeStyle(width: 3);
                            else if (part.Arrow.StartsWith("-.", StringComparison.Ordinal)) edge.Stroke = new ChartStrokeStyle(dash: new[] { 4d, 3d });
                            document.Edges.Add(edge);
                        }
                    }
                    source = target; edgeLabel = nextLabel;
                }
                if (edgeLabel.Length > 0) Error(span, "A connector label requires a following connector operator.");
                continue;
            }
            foreach (var token in SplitTokens(text)) {
                if (!TryParseItem(token.Text, new MermaidSourceSpan(line, span.Column + token.Offset, token.Text.Length), ref sequence, out var item, out var diagnostic)) {
                    Error(span, diagnostic); continue;
                }
                Add(item, item.Span);
            }
        }
        foreach (var parent in parents) Error(parent.Span, "Mermaid composite '" + parent.Id + "' is missing end.");
        // Bare edge references are resolved only after declarations, retaining every explicit source position.
        foreach (var reference in pending) if (!ids.ContainsKey(reference.Item.Id)) {
            reference.Item.ParentId = reference.Parent;
            Add(reference.Item, reference.Item.Span, preserveParent: true);
        }
        ApplyStyles(document, ids, styles, result);
        if (!document.Items.Any(item => !item.IsSpace)) Error(document.HeaderSpan, "Mermaid block diagrams require at least one visible block item.");
        ValidateLocalSpans(document, result);

        bool Add(MermaidBlockItem item, MermaidSourceSpan span, bool preserveParent = false) {
            if (ids.ContainsKey(item.Id)) { Error(span, "Mermaid block item ids must be unique: " + item.Id + "."); return false; }
            if (document.Items.Count >= MaximumBlockLayoutItems) { Error(span, "Mermaid block diagrams support no more than 10000 items."); return false; }
            MermaidResourceGuard.ReserveElements(1 + item.Classes.Count, span);
            if (!preserveParent) item.ParentId = parents.Count == 0 ? null : parents.Peek().Id;
            ids.Add(item.Id, item); document.Items.Add(item); return true;
        }
        MermaidBlockItem? Endpoint(string token, MermaidSourceSpan span) {
            if (!TryParseItem(token, span, ref sequence, out var item, out var diagnostic) || item.IsSpace) { Error(span, diagnostic.Length == 0 ? "Connectors cannot target spaces." : diagnostic); return null; }
            if (HasShape(token)) { if (!ids.ContainsKey(item.Id)) Add(item, span); }
            else pending.Add((item, parents.Count == 0 ? null : parents.Peek().Id));
            return item;
        }
        void Error(MermaidSourceSpan span, string message) => MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, message, MermaidDiagnosticCodes.InvalidStatement);
    }

    private static void ValidateLocalSpans(MermaidBlockDocument document, MermaidParseResult<MermaidDocument> result) {
        var columns = new Dictionary<string, int>(StringComparer.Ordinal);
        var groups = document.Items.GroupBy(item => item.ParentId ?? "").ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        int Count(string id, int value, bool auto) => auto && groups.TryGetValue(id, out var children) ? Math.Max(1, children.Sum(child => child.ColumnSpan)) : value;
        var root = Count("", document.Columns ?? 24, document.AutoColumns);
        foreach (var item in document.Items) {
            var inherited = item.ParentId == null ? root : columns[item.ParentId];
            if (item.ColumnSpan > inherited) MermaidParserUtilities.Add(result, item.Span, MermaidDiagnosticSeverity.Error,
                "Block span exceeds the containing grid's column count.", MermaidDiagnosticCodes.InvalidStatement);
            columns[item.Id] = item.IsComposite ? Count(item.Id, item.Columns ?? inherited, item.AutoColumns) : inherited;
        }
    }
}

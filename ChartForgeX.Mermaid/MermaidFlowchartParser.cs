using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidFlowchartParser {
    public static void ParseStatements(MermaidFlowchartDocument document, string[] lines, int firstBodyLine, MermaidParseResult<MermaidDocument> result, int firstOffset = 0) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        if (lines == null) throw new ArgumentNullException(nameof(lines));
        if (result == null) throw new ArgumentNullException(nameof(result));

        var nodes = new Dictionary<string, MermaidFlowchartNode>(StringComparer.Ordinal);
        var subgraphs = new Stack<MermaidFlowchartSubgraph>();
        foreach (var statement in MermaidFlowchartStatements.Read(lines, firstBodyLine, firstOffset)) {
            MermaidResourceGuard.Checkpoint();
            var trimmed = statement.Text;
            if (trimmed.Length == 0 || IsComment(trimmed)) continue;
            var span = statement.Span;
            document.Statements.Add(statement);
            if (TryParseSubgraphStart(document, trimmed, span, subgraphs)) continue;
            if (IsSubgraphEnd(trimmed, subgraphs, span, result)) continue;
            if (TryParseSubgraphDirection(document, trimmed, span, subgraphs, result)) continue;
            if (TryParseClassDefinition(document, trimmed, span, result)) continue;
            if (TryParseClassAssignment(document, nodes, trimmed, span)) continue;
            if (TryParseStyleAssignment(document, nodes, trimmed, span, result)) continue;
            if (TryParseClickAssignment(document, nodes, trimmed, span)) continue;
            if (TryParseLinkStyle(document, trimmed, span, result)) continue;
            ParseStatement(document, nodes, trimmed, span, subgraphs.Count == 0 ? null : subgraphs.Peek(), result);
        }

        foreach (var subgraph in subgraphs) MermaidParserUtilities.Add(result, subgraph.Span, MermaidDiagnosticSeverity.Error, "Flowchart subgraph '" + subgraph.Id + "' must close with 'end'.");
        ValidateSubgraphs(document, result);
        RemoveSubgraphEndpointNodes(document);
        ApplyClassDefinitions(document);
        ApplyLinkStyles(document);
        AddLabelDiagnostics(document, result);
    }

    private static void ParseStatement(MermaidFlowchartDocument document, Dictionary<string, MermaidFlowchartNode> nodes, string text, MermaidSourceSpan span, MermaidFlowchartSubgraph? subgraph, MermaidParseResult<MermaidDocument> result) {
        var position = 0;
        if (!TryParseNodeGroup(document, nodes, text, ref position, span, subgraph, result, out var current)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning, "Unsupported flowchart statement: " + text);
            return;
        }
        var inlineLabels = new MermaidFlowchartInlineLabels(text);
        while (TryParseEdgeOperator(text, ref position, inlineLabels, out var edgeOperator, out var label)) {
            if (!TryParseNodeGroup(document, nodes, text, ref position, span, subgraph, result, out var target)) {
                MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Flowchart edge operator '" + edgeOperator + "' is missing a target node.");
                return;
            }

            MermaidResourceGuard.ReserveEdges((long)current.Count * target.Count, span);
            foreach (var sourceNode in current) foreach (var targetNode in target)
                document.Edges.Add(new MermaidFlowchartEdge(sourceNode.Id, targetNode.Id, edgeOperator, span) { Label = label });
            current = target;
        }
        SkipWhitespace(text, ref position);
        if (position < text.Length) MermaidParserUtilities.Add(result, MermaidFlowchartText.AtOffset(text, span, position, text.Length - position), MermaidDiagnosticSeverity.Error, "Flowchart statement contains malformed or unsupported trailing syntax.");
    }

    private static void AddOrUpdateNode(MermaidFlowchartDocument document, Dictionary<string, MermaidFlowchartNode> nodes, MermaidFlowchartNode candidate) {
        if (!nodes.TryGetValue(candidate.Id, out var existing)) {
            nodes[candidate.Id] = candidate;
            document.Nodes.Add(candidate);
            return;
        }

        if (candidate.Text != null) {
            existing.Text = candidate.Text;
            existing.LabelSpan = candidate.LabelSpan;
        }
        if (candidate.Shape != MermaidFlowchartNodeShape.Default) existing.Shape = candidate.Shape;
        if (candidate.SubgraphId != null) existing.SubgraphId = candidate.SubgraphId;
        if (candidate.Href != null) existing.Href = candidate.Href;
        if (candidate.Tooltip != null) existing.Tooltip = candidate.Tooltip;
        foreach (var property in candidate.Properties) existing.Properties[property.Key] = property.Value;
        foreach (var className in candidate.Classes) existing.Classes.Add(className);
        foreach (var style in candidate.Styles) existing.Styles[style.Key] = style.Value;
    }

    private static bool TryParseNode(string text, ref int position, MermaidSourceSpan statementSpan, out MermaidFlowchartNode node) {
        node = null!;
        SkipWhitespace(text, ref position);
        var start = position;
        while (position < text.Length && IsNodeIdCharacter(text[position])) position++;
        if (position == start) return false;

        var id = text.Substring(start, position - start);
        var nodeSpan = MermaidFlowchartText.AtOffset(text, statementSpan, start, id.Length);
        node = new MermaidFlowchartNode(id, nodeSpan);
        SkipWhitespace(text, ref position);

        if (position >= text.Length) return true;
        if (TryParseNodeShape(text, ref position, out var shape, out var label)) {
            node.Shape = shape;
            node.Text = label;
        }

        SkipWhitespace(text, ref position);
        return true;
    }

    private static bool TryParseSubgraphStart(MermaidFlowchartDocument document, string text, MermaidSourceSpan span, Stack<MermaidFlowchartSubgraph> subgraphs) {
        if (!text.StartsWith("subgraph ", StringComparison.OrdinalIgnoreCase)) return false;
        var value = text.Substring("subgraph ".Length).Trim();
        if (value.Length == 0) return false;
        MermaidResourceGuard.CheckDepth(subgraphs.Count + 1, span);

        var id = value;
        var title = value;
        var bracketStart = value.IndexOf('[');
        var bracketEnd = value.LastIndexOf(']');
        if (bracketStart > 0 && bracketEnd > bracketStart) {
            id = value.Substring(0, bracketStart).Trim();
            title = Unquote(value.Substring(bracketStart + 1, bracketEnd - bracketStart - 1).Trim());
        } else {
            title = Unquote(value);
            id = Slug(title);
        }

        if (id.Length == 0) id = "subgraph-" + (document.Subgraphs.Count + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (title.Length == 0) title = id;
        var subgraph = new MermaidFlowchartSubgraph(id, title, span) {
            ParentSubgraphId = subgraphs.Count == 0 ? null : subgraphs.Peek().Id
        };
        document.Subgraphs.Add(subgraph);
        subgraphs.Push(subgraph);
        return true;
    }

    private static bool IsSubgraphEnd(string text, Stack<MermaidFlowchartSubgraph> subgraphs, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        if (!string.Equals(text, "end", StringComparison.OrdinalIgnoreCase)) return false;
        if (subgraphs.Count > 0) subgraphs.Pop();
        else MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Flowchart 'end' has no open subgraph.");
        return true;
    }

    private static bool TryParseClassAssignment(MermaidFlowchartDocument document, Dictionary<string, MermaidFlowchartNode> nodes, string text, MermaidSourceSpan span) {
        if (!text.StartsWith("class ", StringComparison.Ordinal)) return false;
        var body = text.Substring("class ".Length).Trim();
        var split = body.IndexOfAny(new[] { ' ', '\t' });
        if (split <= 0) return false;
        var nodeIds = SplitCsv(body.Substring(0, split));
        var classNames = SplitCsv(body.Substring(split + 1));
        foreach (var nodeId in nodeIds) {
            var node = GetOrAddNode(document, nodes, nodeId, span);
            foreach (var className in classNames) node.Classes.Add(className);
        }

        return true;
    }

    private static bool TryParseClickAssignment(MermaidFlowchartDocument document, Dictionary<string, MermaidFlowchartNode> nodes, string text, MermaidSourceSpan span) {
        if (!text.StartsWith("click ", StringComparison.Ordinal)) return false;
        var body = text.Substring("click ".Length).Trim();
        var split = body.IndexOfAny(new[] { ' ', '\t' });
        if (split <= 0) return false;
        var nodeId = body.Substring(0, split);
        var node = GetOrAddNode(document, nodes, nodeId, span);
        var tokens = SplitQuoted(body.Substring(split + 1));
        for (var index = 0; index < tokens.Count; index++) {
            var token = tokens[index];
            if (string.Equals(token, "href", StringComparison.OrdinalIgnoreCase) && index + 1 < tokens.Count) {
                node.Href = tokens[index + 1];
                index++;
                continue;
            }

            if (node.Href == null && LooksLikeUrl(token)) {
                node.Href = token;
                continue;
            }

            if (node.Tooltip == null && token.Length > 0 && !string.Equals(token, "_blank", StringComparison.OrdinalIgnoreCase) && !string.Equals(token, "_self", StringComparison.OrdinalIgnoreCase)) node.Tooltip = token;
        }

        return true;
    }

    private static bool TryParseNodeShape(string text, ref int position, out MermaidFlowchartNodeShape shape, out string? label) {
        shape = MermaidFlowchartNodeShape.Default;
        label = null;
        var start = position;
        if (position >= text.Length) return false;

        if (StartsWith(text, position, "(((")) return ReadDelimited(text, ref position, "(((", ")))", MermaidFlowchartNodeShape.DoubleCircle, out shape, out label);
        if (StartsWith(text, position, "((")) return ReadDelimited(text, ref position, "((", "))", MermaidFlowchartNodeShape.Circle, out shape, out label);
        if (StartsWith(text, position, "([")) return ReadDelimited(text, ref position, "([", "])", MermaidFlowchartNodeShape.Stadium, out shape, out label);
        if (StartsWith(text, position, "[[")) return ReadDelimited(text, ref position, "[[", "]]", MermaidFlowchartNodeShape.Subroutine, out shape, out label);
        if (StartsWith(text, position, "[(")) return ReadDelimited(text, ref position, "[(", ")]", MermaidFlowchartNodeShape.Cylinder, out shape, out label);
        if (StartsWith(text, position, "{{")) return ReadDelimited(text, ref position, "{{", "}}", MermaidFlowchartNodeShape.Hexagon, out shape, out label);
        if (StartsWith(text, position, "[/")) return ReadBracketSlashShape(text, ref position, '/', out shape, out label);
        if (StartsWith(text, position, "[\\")) return ReadBracketSlashShape(text, ref position, '\\', out shape, out label);
        if (text[position] == '[') return ReadDelimited(text, ref position, "[", "]", MermaidFlowchartNodeShape.Rectangle, out shape, out label);
        if (text[position] == '(') return ReadDelimited(text, ref position, "(", ")", MermaidFlowchartNodeShape.Rounded, out shape, out label);
        if (text[position] == '{') return ReadDelimited(text, ref position, "{", "}", MermaidFlowchartNodeShape.Rhombus, out shape, out label);
        if (text[position] == '>') return ReadDelimited(text, ref position, ">", "]", MermaidFlowchartNodeShape.Asymmetric, out shape, out label);

        position = start;
        return false;
    }

    private static bool ReadBracketSlashShape(string text, ref int position, char openingSlash, out MermaidFlowchartNodeShape shape, out string? label) {
        var contentStart = position + 2;
        char quote = '\0';
        for (var index = contentStart; index < text.Length - 1; index++) {
            if (MermaidFlowchartText.ReadQuotedCharacter(text, ref index, ref quote)) continue;
            if (text[index + 1] != ']') continue;
            if (text[index] != '/' && text[index] != '\\') continue;

            label = Unquote(text.Substring(contentStart, index - contentStart));
            shape = openingSlash == '/'
                ? text[index] == '/' ? MermaidFlowchartNodeShape.Parallelogram : MermaidFlowchartNodeShape.Trapezoid
                : text[index] == '\\' ? MermaidFlowchartNodeShape.ParallelogramAlt : MermaidFlowchartNodeShape.TrapezoidAlt;
            position = index + 2;
            return true;
        }

        shape = MermaidFlowchartNodeShape.Default;
        label = null;
        return false;
    }

    private static bool ReadDelimited(string text, ref int position, string open, string close, MermaidFlowchartNodeShape nodeShape, out MermaidFlowchartNodeShape shape, out string? label) {
        var contentStart = position + open.Length;
        var closeIndex = FindLabelDelimiter(text, contentStart, close);
        if (closeIndex < 0) {
            shape = MermaidFlowchartNodeShape.Default;
            label = null;
            return false;
        }

        label = Unquote(text.Substring(contentStart, closeIndex - contentStart));
        shape = nodeShape;
        position = closeIndex + close.Length;
        return true;
    }

    private static int FindLabelDelimiter(string text, int start, string delimiter) {
        char quote = '\0';
        for (var index = start; index < text.Length; index++) {
            if (MermaidFlowchartText.ReadQuotedCharacter(text, ref index, ref quote)) continue;
            if (StartsWith(text, index, delimiter)) return index;
        }
        return -1;
    }

    private static bool TryParseEdgeOperator(string text, ref int position, MermaidFlowchartInlineLabels inlineLabels, out string edgeOperator, out string? label) {
        edgeOperator = string.Empty;
        label = null;
        SkipWhitespace(text, ref position);
        var start = position;
        if (start >= text.Length) return false;

        if (TryParsePipeLabelOperator(text, ref position, out edgeOperator, out label)) return true;
        if (TryParseInlineLabelOperator(text, ref position, inlineLabels, out edgeOperator, out label)) return true;

        position = MermaidFlowchartOperators.ReadEnd(text, start);
        if (position == start) return false;

        edgeOperator = text.Substring(start, position - start);
        if (LooksLikeOperator(edgeOperator)) return true;
        position = start;
        return false;
    }

    private static bool TryParsePipeLabelOperator(string text, ref int position, out string edgeOperator, out string? label) {
        edgeOperator = string.Empty;
        label = null;
        var start = position;
        var end = MermaidFlowchartOperators.ReadEnd(text, start);
        if (end == start) return false;
        var firstPipe = end;
        SkipWhitespace(text, ref firstPipe);
        if (firstPipe >= text.Length || text[firstPipe] != '|') return false;
        var secondPipe = FindLabelDelimiter(text, firstPipe + 1, "|");
        if (secondPipe < 0) return false;

        edgeOperator = text.Substring(start, end - start);
        label = Unquote(text.Substring(firstPipe + 1, secondPipe - firstPipe - 1).Trim());
        position = secondPipe + 1;
        return true;
    }

    private static bool TryParseInlineLabelOperator(string text, ref int position, MermaidFlowchartInlineLabels inlineLabels, out string edgeOperator, out string? label) {
        edgeOperator = string.Empty;
        label = null;
        var start = position;
        if (!inlineLabels.TryRead(start, out var labelStart, out var labelEnd, out var end)) return false;
        edgeOperator = text.Substring(start, end - start).Trim();
        label = Unquote(text.Substring(labelStart, labelEnd - labelStart).Trim());
        position = end;
        return true;
    }

    private static bool LooksLikeOperator(string text) {
        for (var index = 0; index < text.Length; index++) {
            if (!IsOperatorCharacter(text[index])) return false;
        }

        return text.IndexOf('-') >= 0 ||
               text.IndexOf('=') >= 0 ||
               text.IndexOf('.') >= 0;
    }

    private static bool IsNodeIdCharacter(char ch) =>
        !char.IsWhiteSpace(ch) &&
        ch != '[' &&
        ch != '(' &&
        ch != '{' &&
        ch != '>' &&
        ch != '<' &&
        ch != '-' &&
        ch != '=' &&
        ch != '.' &&
        ch != ':' && ch != '@' && ch != '&';

    private static bool IsOperatorCharacter(char ch) => ch == '<' || ch == '>' || ch == '-' || ch == '.' || ch == '=' || ch == 'o' || ch == 'x';

    private static bool StartsWith(string text, int position, string value) =>
        position >= 0 &&
        position + value.Length <= text.Length &&
        string.CompareOrdinal(text, position, value, 0, value.Length) == 0;

    private static bool IsComment(string text) => text.StartsWith("%%", StringComparison.Ordinal);

    private static void SkipWhitespace(string text, ref int position) {
        while (position < text.Length && char.IsWhiteSpace(text[position])) position++;
    }

    private static int LeadingWhitespace(string text) {
        var count = 0;
        while (count < text.Length && char.IsWhiteSpace(text[count])) count++;
        return count;
    }

    private static MermaidFlowchartNode GetOrAddNode(MermaidFlowchartDocument document, Dictionary<string, MermaidFlowchartNode> nodes, string id, MermaidSourceSpan span) {
        if (nodes.TryGetValue(id, out var existing)) return existing;
        var node = new MermaidFlowchartNode(id, span);
        nodes[id] = node;
        document.Nodes.Add(node);
        return node;
    }

    private static List<string> SplitCsv(string text) {
        var values = new List<string>();
        foreach (var value in text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)) {
            var trimmed = value.Trim();
            if (trimmed.Length > 0) values.Add(trimmed);
        }

        return values;
    }

    private static List<string> SplitQuoted(string text) {
        var values = new List<string>();
        var current = new System.Text.StringBuilder();
        var quote = '\0';
        for (var index = 0; index < text.Length; index++) {
            var ch = text[index];
            if (quote != '\0') {
                if (ch == quote) {
                    values.Add(current.ToString());
                    current.Clear();
                    quote = '\0';
                } else {
                    current.Append(ch);
                }

                continue;
            }

            if (ch == '"' || ch == '\'') {
                if (current.Length > 0) {
                    values.Add(current.ToString());
                    current.Clear();
                }

                quote = ch;
                continue;
            }

            if (char.IsWhiteSpace(ch)) {
                if (current.Length > 0) {
                    values.Add(current.ToString());
                    current.Clear();
                }

                continue;
            }

            current.Append(ch);
        }

        if (current.Length > 0) values.Add(current.ToString());
        return values;
    }

    private static string Unquote(string value) {
        if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"') return value.Substring(1, value.Length - 2);
        return value;
    }

    private static string Slug(string value) {
        var builder = new System.Text.StringBuilder();
        foreach (var ch in value) {
            if (char.IsLetterOrDigit(ch)) builder.Append(char.ToLowerInvariant(ch));
            else if (builder.Length > 0 && builder[builder.Length - 1] != '-') builder.Append('-');
        }

        return builder.ToString().Trim('-');
    }

    private static bool LooksLikeUrl(string value) =>
        value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("#", StringComparison.Ordinal);

    private static void AddNodeToSubgraph(MermaidFlowchartSubgraph subgraph, string nodeId) => AddUnique(subgraph.NodeIds, nodeId);

    private static void AddUnique(List<string> values, string value) {
        if (string.IsNullOrWhiteSpace(value)) return;
        for (var index = 0; index < values.Count; index++) {
            if (string.Equals(values[index], value, StringComparison.Ordinal)) return;
        }

        values.Add(value);
    }
}

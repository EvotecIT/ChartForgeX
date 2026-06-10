using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

internal static class MermaidFlowchartParser {
    private static readonly string[] LabelSuffixOperators = { "-.->", "-->", "==>", "---", "--o", "--x" };

    public static void ParseStatements(MermaidFlowchartDocument document, string[] lines, int firstBodyLine) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        if (lines == null) throw new ArgumentNullException(nameof(lines));

        var nodes = new Dictionary<string, MermaidFlowchartNode>(StringComparer.Ordinal);
        var subgraphs = new Stack<MermaidFlowchartSubgraph>();
        for (var index = firstBodyLine - 1; index < lines.Length; index++) {
            var raw = lines[index];
            var trimmed = raw.Trim();
            if (trimmed.Length == 0 || IsComment(trimmed)) continue;

            var column = LeadingWhitespace(raw) + 1;
            var span = new MermaidSourceSpan(index + 1, column, trimmed.Length);
            document.Statements.Add(new MermaidRawStatement(trimmed, span));
            if (TryParseSubgraphStart(document, trimmed, span, subgraphs)) continue;
            if (IsSubgraphEnd(trimmed, subgraphs)) continue;
            if (TryParseClassDefinition(document, trimmed, span)) continue;
            if (TryParseClassAssignment(document, nodes, trimmed, span)) continue;
            if (TryParseStyleAssignment(document, nodes, trimmed, span)) continue;
            if (TryParseClickAssignment(document, nodes, trimmed, span)) continue;
            if (TryParseLinkStyle(document, trimmed, span)) continue;
            ParseStatement(document, nodes, trimmed, span, subgraphs.Count == 0 ? null : subgraphs.Peek());
        }

        ApplyClassDefinitions(document);
        ApplyLinkStyles(document);
    }

    private static void ParseStatement(MermaidFlowchartDocument document, Dictionary<string, MermaidFlowchartNode> nodes, string text, MermaidSourceSpan span, MermaidFlowchartSubgraph? subgraph) {
        var position = 0;
        if (!TryParseNode(text, ref position, span, out var current)) return;
        if (subgraph != null && current.SubgraphId == null) current.SubgraphId = subgraph.Id;
        AddOrUpdateNode(document, nodes, current);
        if (subgraph != null) AddNodeToSubgraph(subgraph, current.Id);

        while (TryParseEdgeOperator(text, ref position, out var edgeOperator, out var label)) {
            if (!TryParseNode(text, ref position, span, out var target)) break;
            if (subgraph != null && target.SubgraphId == null) target.SubgraphId = subgraph.Id;
            AddOrUpdateNode(document, nodes, target);
            if (subgraph != null) AddNodeToSubgraph(subgraph, target.Id);

            var edge = new MermaidFlowchartEdge(current.Id, target.Id, edgeOperator, span) { Label = label };
            document.Edges.Add(edge);
            current = target;
        }
    }

    private static void AddOrUpdateNode(MermaidFlowchartDocument document, Dictionary<string, MermaidFlowchartNode> nodes, MermaidFlowchartNode candidate) {
        if (!nodes.TryGetValue(candidate.Id, out var existing)) {
            nodes[candidate.Id] = candidate;
            document.Nodes.Add(candidate);
            return;
        }

        if (candidate.Text != null) existing.Text = candidate.Text;
        if (candidate.Shape != MermaidFlowchartNodeShape.Default) existing.Shape = candidate.Shape;
        if (candidate.SubgraphId != null) existing.SubgraphId = candidate.SubgraphId;
        if (candidate.Href != null) existing.Href = candidate.Href;
        if (candidate.Tooltip != null) existing.Tooltip = candidate.Tooltip;
        foreach (var className in candidate.Classes) AddUnique(existing.Classes, className);
        foreach (var style in candidate.Styles) existing.Styles[style.Key] = style.Value;
    }

    private static bool TryParseNode(string text, ref int position, MermaidSourceSpan statementSpan, out MermaidFlowchartNode node) {
        node = null!;
        SkipWhitespace(text, ref position);
        var start = position;
        while (position < text.Length && IsNodeIdCharacter(text[position])) position++;
        if (position == start) return false;

        var id = text.Substring(start, position - start);
        var nodeSpan = new MermaidSourceSpan(statementSpan.Line, statementSpan.Column + start, id.Length);
        node = new MermaidFlowchartNode(id, nodeSpan);
        SkipWhitespace(text, ref position);

        if (position >= text.Length) return true;
        if (TryParseNodeShape(text, ref position, out var shape, out var label)) {
            node.Shape = shape;
            node.Text = label;
        }

        SkipWhitespace(text, ref position);
        ParseClassSuffix(text, ref position, node.Classes);
        return true;
    }

    private static bool TryParseSubgraphStart(MermaidFlowchartDocument document, string text, MermaidSourceSpan span, Stack<MermaidFlowchartSubgraph> subgraphs) {
        if (!text.StartsWith("subgraph ", StringComparison.OrdinalIgnoreCase)) return false;
        var value = text.Substring("subgraph ".Length).Trim();
        if (value.Length == 0) return false;

        var id = value;
        var title = value;
        var bracketStart = value.IndexOf('[');
        var bracketEnd = value.LastIndexOf(']');
        if (bracketStart > 0 && bracketEnd > bracketStart) {
            id = value.Substring(0, bracketStart).Trim();
            title = value.Substring(bracketStart + 1, bracketEnd - bracketStart - 1).Trim();
        } else {
            title = Unquote(value);
            id = Slug(title);
        }

        if (id.Length == 0) id = "subgraph-" + (document.Subgraphs.Count + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (title.Length == 0) title = id;
        var subgraph = new MermaidFlowchartSubgraph(id, title, span);
        document.Subgraphs.Add(subgraph);
        subgraphs.Push(subgraph);
        return true;
    }

    private static bool IsSubgraphEnd(string text, Stack<MermaidFlowchartSubgraph> subgraphs) {
        if (!string.Equals(text, "end", StringComparison.OrdinalIgnoreCase)) return false;
        if (subgraphs.Count > 0) subgraphs.Pop();
        return true;
    }

    private static bool TryParseClassDefinition(MermaidFlowchartDocument document, string text, MermaidSourceSpan span) {
        if (!text.StartsWith("classDef ", StringComparison.Ordinal)) return false;
        var body = text.Substring("classDef ".Length).Trim();
        var split = body.IndexOfAny(new[] { ' ', '\t' });
        if (split <= 0) return false;
        var classDefinition = new MermaidFlowchartClassDefinition(body.Substring(0, split), span);
        AddStyles(classDefinition.Styles, body.Substring(split + 1));
        document.ClassDefinitions.Add(classDefinition);
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
            foreach (var className in classNames) AddUnique(node.Classes, className);
        }

        return true;
    }

    private static bool TryParseStyleAssignment(MermaidFlowchartDocument document, Dictionary<string, MermaidFlowchartNode> nodes, string text, MermaidSourceSpan span) {
        if (!text.StartsWith("style ", StringComparison.Ordinal)) return false;
        var body = text.Substring("style ".Length).Trim();
        var split = body.IndexOfAny(new[] { ' ', '\t' });
        if (split <= 0) return false;
        var nodeId = body.Substring(0, split);
        var node = GetOrAddNode(document, nodes, nodeId, span);
        AddStyles(node.Styles, body.Substring(split + 1));
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

    private static bool TryParseLinkStyle(MermaidFlowchartDocument document, string text, MermaidSourceSpan span) {
        if (!text.StartsWith("linkStyle ", StringComparison.Ordinal)) return false;
        var body = text.Substring("linkStyle ".Length).Trim();
        var split = body.IndexOfAny(new[] { ' ', '\t' });
        if (split <= 0) return false;
        var style = new MermaidFlowchartLinkStyle(body.Substring(0, split), span);
        AddStyles(style.Styles, body.Substring(split + 1));
        document.LinkStyles.Add(style);
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
        for (var index = contentStart; index < text.Length - 1; index++) {
            if (text[index + 1] != ']') continue;
            if (text[index] != '/' && text[index] != '\\') continue;

            label = text.Substring(contentStart, index - contentStart);
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
        var closeIndex = text.IndexOf(close, contentStart, StringComparison.Ordinal);
        if (closeIndex < 0) {
            shape = MermaidFlowchartNodeShape.Default;
            label = null;
            return false;
        }

        label = text.Substring(contentStart, closeIndex - contentStart);
        shape = nodeShape;
        position = closeIndex + close.Length;
        return true;
    }

    private static bool TryParseEdgeOperator(string text, ref int position, out string edgeOperator, out string? label) {
        edgeOperator = string.Empty;
        label = null;
        SkipWhitespace(text, ref position);
        var start = position;
        if (start >= text.Length) return false;

        if (TryParsePipeLabelOperator(text, ref position, out edgeOperator, out label)) return true;
        if (TryParseInlineLabelOperator(text, ref position, out edgeOperator, out label)) return true;

        while (position < text.Length && IsOperatorCharacter(text[position])) position++;
        if (position == start) return false;

        edgeOperator = text.Substring(start, position - start);
        return LooksLikeOperator(edgeOperator);
    }

    private static bool TryParsePipeLabelOperator(string text, ref int position, out string edgeOperator, out string? label) {
        edgeOperator = string.Empty;
        label = null;
        var start = position;
        var firstPipe = text.IndexOf('|', start);
        if (firstPipe < 0) return false;

        var before = text.Substring(start, firstPipe - start).Trim();
        if (before.Length == 0 || !LooksLikeOperator(before)) return false;
        var secondPipe = text.IndexOf('|', firstPipe + 1);
        if (secondPipe < 0) return false;

        edgeOperator = before;
        label = text.Substring(firstPipe + 1, secondPipe - firstPipe - 1).Trim();
        position = secondPipe + 1;
        return true;
    }

    private static bool TryParseInlineLabelOperator(string text, ref int position, out string edgeOperator, out string? label) {
        edgeOperator = string.Empty;
        label = null;
        var start = position;
        if (!StartsWith(text, start, "--") && !StartsWith(text, start, "==")) return false;
        if (start + 2 < text.Length && IsOperatorCharacter(text[start + 2])) return false;

        foreach (var suffix in LabelSuffixOperators) {
            var suffixIndex = text.IndexOf(suffix, start + 2, StringComparison.Ordinal);
            if (suffixIndex < 0) continue;

            var candidateLabel = text.Substring(start + 2, suffixIndex - start - 2).Trim();
            if (candidateLabel.Length == 0) continue;

            edgeOperator = text.Substring(start, suffixIndex + suffix.Length - start).Trim();
            label = candidateLabel;
            position = suffixIndex + suffix.Length;
            return true;
        }

        return false;
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
        ch != ':';

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

    private static void ParseClassSuffix(string text, ref int position, List<string> classes) {
        while (StartsWith(text, position, ":::")) {
            position += 3;
            var start = position;
            while (position < text.Length && !char.IsWhiteSpace(text[position]) && !IsOperatorCharacter(text[position])) position++;
            foreach (var className in SplitCsv(text.Substring(start, position - start))) AddUnique(classes, className);
            SkipWhitespace(text, ref position);
        }
    }

    private static void ApplyClassDefinitions(MermaidFlowchartDocument document) {
        foreach (var node in document.Nodes) {
            foreach (var className in node.Classes) {
                var classDefinition = document.ClassDefinitions.Find(item => string.Equals(item.Name, className, StringComparison.Ordinal));
                if (classDefinition == null) continue;
                foreach (var style in classDefinition.Styles) {
                    if (!node.Styles.ContainsKey(style.Key)) node.Styles[style.Key] = style.Value;
                }
            }
        }
    }

    private static void ApplyLinkStyles(MermaidFlowchartDocument document) {
        foreach (var linkStyle in document.LinkStyles) {
            if (string.Equals(linkStyle.Selector, "default", StringComparison.OrdinalIgnoreCase)) {
                foreach (var edge in document.Edges) AddStyles(edge.Styles, linkStyle.Styles);
                continue;
            }

            foreach (var selector in SplitCsv(linkStyle.Selector)) {
                if (!int.TryParse(selector, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var edgeIndex)) continue;
                if (edgeIndex < 0 || edgeIndex >= document.Edges.Count) continue;
                AddStyles(document.Edges[edgeIndex].Styles, linkStyle.Styles);
            }
        }
    }

    private static void AddStyles(Dictionary<string, string> styles, string text) {
        foreach (var item in SplitCssDeclarations(text)) {
            var split = item.IndexOf(':');
            if (split <= 0) continue;
            styles[item.Substring(0, split).Trim()] = item.Substring(split + 1).Trim().TrimEnd(';');
        }
    }

    private static void AddStyles(Dictionary<string, string> styles, Dictionary<string, string> source) {
        foreach (var item in source) styles[item.Key] = item.Value;
    }

    private static List<string> SplitCssDeclarations(string text) {
        var normalized = text.Replace(';', ',');
        return SplitCsv(normalized);
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
        if (value.Length >= 2 && ((value[0] == '"' && value[value.Length - 1] == '"') || (value[0] == '\'' && value[value.Length - 1] == '\''))) return value.Substring(1, value.Length - 2);
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

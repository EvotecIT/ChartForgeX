using System;
using System.Collections.Generic;
using System.Text;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidAgentflowParser {
    private static bool Endpoint(MermaidAgentflowDocument document, Dictionary<string, MermaidAgentflowNode> nodes,
        List<Attachment> attachments, string[] lines, ref int line, string text, ref int position, MermaidSourceSpan span,
        string? parent, bool global, MermaidParseResult<MermaidDocument> result, out MermaidAgentflowNode? node) {
        White(text, ref position); var start = position;
        while (position < text.Length && !char.IsWhiteSpace(text[position]) && "[({@;|".IndexOf(text[position]) < 0
            && !IsOperator(text, position)) position++;
        var id = text.Substring(start, position - start);
        node = null;
        if (id.Length == 0) { Error("Agentflow statements require a node identity."); return false; }
        if (!nodes.TryGetValue(id, out node)) {
            node = new MermaidAgentflowNode(id, span); nodes.Add(id, node); document.Nodes.Add(node);
            MermaidResourceGuard.ReserveElements(1, span);
        }
        if (global) { node.IsGlobal = true; node.ParentFlowId = null; }
        else if (!node.IsGlobal && node.ParentFlowId == null) node.ParentFlowId = parent;
        White(text, ref position);
        if (position < text.Length && text[position] == '[') {
            var beginning = ++position; var quote = '\0';
            while (position < text.Length) {
                var ch = text[position];
                if (quote != '\0') { if (ch == '\\') position++; else if (ch == quote) quote = '\0'; }
                else if (ch is '"' or '\'') quote = ch;
                else if (ch == ']') break;
                position++;
            }
            if (position >= text.Length) { Error("Agentflow labels must close with ']'."); return false; }
            node.Label = MermaidParserUtilities.Unquote(text.Substring(beginning, position - beginning).Trim());
            node.LabelSpan = span; position++;
        }
        White(text, ref position);
        if (position + 1 < text.Length && text[position] == '@' && text[position + 1] == '{') {
            position += 2;
            var depth = 1; var quote = '\0'; var scalarIndent = -1; var value = new StringBuilder();
            var current = text;
            while (true) {
                MermaidResourceGuard.Checkpoint();
                var indentation = MermaidParserUtilities.LeadingWhitespace(current);
                var literalLine = scalarIndent >= 0 && (string.IsNullOrWhiteSpace(current) || indentation > scalarIndent);
                if (scalarIndent >= 0 && !literalLine) scalarIndent = -1;
                if (literalLine) { value.Append(current.Substring(position)); position = current.Length; }
                else if (quote == '\0' && System.Text.RegularExpressions.Regex.IsMatch(current, @":\s*[|>]\s*$", System.Text.RegularExpressions.RegexOptions.CultureInvariant)) scalarIndent = indentation;
                while (position < current.Length) {
                    var ch = current[position++];
                    if (quote != '\0') {
                        value.Append(ch);
                        if (ch == '\\' && quote == '"' && position < current.Length) value.Append(current[position++]);
                        else if (ch == quote) {
                            if (quote == '\'' && position < current.Length && current[position] == '\'') value.Append(current[position++]);
                            else quote = '\0';
                        }
                    } else if (ch is '"' or '\'') { quote = ch; value.Append(ch); }
                    else if (ch == '{') { MermaidResourceGuard.CheckDepth(++depth, span); value.Append(ch); }
                    else if (ch == '}') { depth--; if (depth > 0) value.Append(ch); }
                    else value.Append(ch);
                    if (depth == 0) break;
                }
                if (depth == 0) {
                    try { attachments.Add(new Attachment(id, MermaidParser.ReadMetadata(value.ToString(), span), span)); }
                    catch (ArgumentException exception) { Error(exception.Message); }
                    if (line != span.Line && current.Substring(position).Trim().Length > 0) Error("A multiline Agentflow metadata block must finish its statement on the closing line.");
                    if (line != span.Line) position = text.Length;
                    break;
                }
                if (line == lines.Length) { Error("Agentflow metadata must close with '}'."); return false; }
                value.Append('\n'); current = lines[line++]; position = 0;
            }
        }
        return true;
        void Error(string message) => MermaidParserUtilities.Add(result, new MermaidSourceSpan(span.Line, span.Column + start, Math.Max(1, span.Length - start)),
            MermaidDiagnosticSeverity.Error, message, MermaidDiagnosticCodes.InvalidStatement);
    }
    private static bool IsOperator(string text, int index) => index + 2 < text.Length &&
        (text.Substring(index, 3) == "-->" || text.Substring(index, 3) == "-.-" || text.Substring(index, 3) == "--x");
    private static bool Operator(string text, ref int position, ref string id, out MermaidAgentflowEdgeKind kind, out string? label) {
        kind = MermaidAgentflowEdgeKind.Sequence; label = null; White(text, ref position);
        var at = text.IndexOf('@', position);
        if (at > position && text.Substring(position, at - position).IndexOfAny(new[] { ' ', '\t' }) < 0 && IsOperator(text, at + 1)) {
            id = text.Substring(position, at - position); position = at + 1;
        }
        if (!IsOperator(text, position)) {
            if (position + 2 >= text.Length || text.Substring(position, 2) != "--") return false;
            var ending = text.IndexOf("-->", position + 2, StringComparison.Ordinal);
            if (ending < 0) return false;
            label = MermaidParserUtilities.Unquote(text.Substring(position + 2, ending - position - 2).Trim());
            position = ending;
        }
        var operation = text.Substring(position, 3); position += 3;
        kind = operation == "-.-" ? MermaidAgentflowEdgeKind.Reference : operation == "--x" ? MermaidAgentflowEdgeKind.Failure : MermaidAgentflowEdgeKind.Sequence;
        White(text, ref position);
        if (position < text.Length && text[position] == '|') {
            var end = text.IndexOf('|', position + 1);
            if (end < 0) return false;
            label = MermaidParserUtilities.Unquote(text.Substring(position + 1, end - position - 1)); position = end + 1;
        }
        return true;
    }
}

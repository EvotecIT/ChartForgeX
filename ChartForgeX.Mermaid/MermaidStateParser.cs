using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

internal static class MermaidStateParser {
    public static void ParseStatements(MermaidStateDocument document, string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        var states = new Dictionary<string, MermaidStateNode>(StringComparer.Ordinal);
        var composites = new Stack<string>();
        var specialIndex = 0;
        for (var line = Math.Max(1, startLine); line <= lines.Length; line++) {
            var raw = lines[line - 1];
            var trimmed = MermaidParserUtilities.StripInlineComment(raw.Trim());
            if (MermaidParserUtilities.IsSkippable(trimmed)) continue;
            var span = new MermaidSourceSpan(line, MermaidParserUtilities.LeadingWhitespace(raw) + 1, trimmed.Length);
            if (trimmed == "}") {
                if (composites.Count > 0) composites.Pop();
                continue;
            }

            if (trimmed.StartsWith("direction ", StringComparison.Ordinal)) {
                document.Direction = trimmed.Substring(10).Trim();
                continue;
            }

            if (trimmed.StartsWith("note ", StringComparison.Ordinal) || trimmed.StartsWith("classDef ", StringComparison.Ordinal) || trimmed.StartsWith("class ", StringComparison.Ordinal) || trimmed == "--") {
                document.Statements.Add(new MermaidStateStatement(trimmed, span));
                continue;
            }

            if (TryParseTransition(trimmed, span, ref specialIndex, out var transition)) {
                document.Transitions.Add(transition);
                EnsureState(document, states, transition.SourceId, span, composites.Count == 0 ? null : composites.Peek());
                EnsureState(document, states, transition.TargetId, span, composites.Count == 0 ? null : composites.Peek());
                continue;
            }

            if (trimmed.StartsWith("state ", StringComparison.Ordinal)) {
                ParseStateDeclaration(document, states, trimmed, span, composites);
                continue;
            }

            var colon = trimmed.IndexOf(':');
            if (colon > 0) {
                var id = trimmed.Substring(0, colon).Trim();
                var label = MermaidParserUtilities.Unquote(trimmed.Substring(colon + 1).Trim());
                EnsureState(document, states, id, span, composites.Count == 0 ? null : composites.Peek()).Label = label;
                continue;
            }

            EnsureState(document, states, trimmed, span, composites.Count == 0 ? null : composites.Peek());
        }

        if (document.States.Count == 0 && document.Transitions.Count == 0) MermaidParserUtilities.Add(result, document.HeaderSpan, MermaidDiagnosticSeverity.Error, "Mermaid state diagrams require at least one state or transition.");
    }

    private static void ParseStateDeclaration(MermaidStateDocument document, Dictionary<string, MermaidStateNode> states, string text, MermaidSourceSpan span, Stack<string> composites) {
        var body = text.Substring(6).Trim();
        var opensComposite = body.EndsWith("{", StringComparison.Ordinal);
        if (opensComposite) body = body.Substring(0, body.Length - 1).Trim();
        string id;
        string? label = null;
        string? kind = null;
        var marker = body.IndexOf("<<", StringComparison.Ordinal);
        if (marker >= 0 && body.EndsWith(">>", StringComparison.Ordinal)) {
            kind = body.Substring(marker + 2, body.Length - marker - 4).Trim();
            body = body.Substring(0, marker).Trim();
        }

        var asIndex = body.IndexOf(" as ", StringComparison.Ordinal);
        if (asIndex > 0) {
            var first = body.Substring(0, asIndex).Trim();
            var second = body.Substring(asIndex + 4).Trim();
            if (first.StartsWith("\"", StringComparison.Ordinal)) {
                label = MermaidParserUtilities.Unquote(first);
                id = second;
            } else {
                id = first;
                label = MermaidParserUtilities.Unquote(second);
            }
        } else {
            id = body;
        }

        var state = EnsureState(document, states, id, span, composites.Count == 0 ? null : composites.Peek());
        if (!string.IsNullOrWhiteSpace(label)) state.Label = label;
        if (!string.IsNullOrWhiteSpace(kind)) state.Kind = kind;
        if (opensComposite) composites.Push(state.Id);
    }

    private static bool TryParseTransition(string text, MermaidSourceSpan span, ref int specialIndex, out MermaidStateTransition transition) {
        transition = null!;
        var index = text.IndexOf("-->", StringComparison.Ordinal);
        if (index <= 0) return false;
        var left = NormalizeSpecial(text.Substring(0, index).Trim(), true, ref specialIndex);
        var right = text.Substring(index + 3).Trim();
        var label = SplitLabel(ref right);
        var target = NormalizeSpecial(right, false, ref specialIndex);
        transition = new MermaidStateTransition(left, target, label, span);
        return true;
    }

    private static string NormalizeSpecial(string value, bool source, ref int specialIndex) {
        value = MermaidParserUtilities.Unquote(value);
        if (value == "[*]") return source ? "mermaid-start" : "mermaid-end-" + (++specialIndex).ToString(System.Globalization.CultureInfo.InvariantCulture);
        var styleIndex = value.IndexOf(":::", StringComparison.Ordinal);
        return styleIndex > 0 ? value.Substring(0, styleIndex).Trim() : value;
    }

    private static string? SplitLabel(ref string text) {
        var colon = text.IndexOf(':');
        if (colon < 0) return null;
        var label = text.Substring(colon + 1).Trim();
        text = text.Substring(0, colon).Trim();
        return label.Length == 0 ? null : label;
    }

    private static MermaidStateNode EnsureState(MermaidStateDocument document, Dictionary<string, MermaidStateNode> states, string id, MermaidSourceSpan span, string? parentId) {
        id = MermaidParserUtilities.Unquote(id);
        if (states.TryGetValue(id, out var existing)) {
            if (existing.ParentId == null) existing.ParentId = parentId;
            return existing;
        }

        var node = new MermaidStateNode(id, span) { ParentId = parentId };
        if (id.StartsWith("mermaid-start", StringComparison.Ordinal)) node.Kind = "start";
        if (id.StartsWith("mermaid-end", StringComparison.Ordinal)) node.Kind = "end";
        states.Add(id, node);
        document.States.Add(node);
        return node;
    }
}

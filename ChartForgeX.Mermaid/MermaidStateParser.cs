using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ChartForgeX.Mermaid;

internal static class MermaidStateParser {
    private static readonly Regex Note = new(@"^note\s+(?<side>left|right)\s+of\s+(?<id>[^\s:]+)\s*(?::\s*(?<text>.*))?$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    public static void ParseStatements(MermaidStateDocument document, string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        var states = new Dictionary<string, MermaidStateNode>(StringComparer.Ordinal);
        var composites = new Stack<(string Id, MermaidSourceSpan Span)>();
        var specialIndex = 0;
        var originalLineStarts = MermaidParserUtilities.OriginalLineStarts(document.SourceText);
        for (var line = Math.Max(1, startLine); line <= lines.Length; line++) {
            MermaidResourceGuard.Checkpoint();
            var raw = lines[line - 1];
            var trimmed = MermaidParserUtilities.StripInlineComment(raw.Trim());
            if (MermaidParserUtilities.IsSkippable(trimmed)) continue;
            var span = new MermaidSourceSpan(line, MermaidParserUtilities.LeadingWhitespace(raw) + 1, trimmed.Length);
            if (trimmed == "}") {
                if (composites.Count > 0) composites.Pop();
                else MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "State closing brace has no open composite.", MermaidDiagnosticCodes.InvalidStatement);
                continue;
            }

            if (MermaidParserUtilities.TryReadDirection(trimmed, span, result, out var direction, ignoreCase: true)) {
                if (composites.Count == 0) document.Direction = direction;
                else states[composites.Peek().Id].Direction = direction;
                continue;
            }

            if (MermaidParserUtilities.StartsStatement(trimmed, "note", ignoreCase: true)) {
                if (MermaidParserUtilities.IsMultilineStateNote(trimmed)) line = ReadNote(document, lines, line, span, originalLineStarts, result);
                else {
                    var note = Note.Match(trimmed);
                    if (note.Success && note.Groups["text"].Success) AddNote(document, note, note.Groups["text"].Value, span);
                    else {
                        document.Statements.Add(new MermaidStateStatement(trimmed, span));
                        MermaidParserUtilities.RetainUnsupported(document, trimmed, span, result, "state note syntax");
                    }
                }
                continue;
            }

            if (MermaidParserUtilities.IsStateNoteTerminator(trimmed)) {
                MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "State note terminator has no open note.", MermaidDiagnosticCodes.InvalidStatement);
                continue;
            }

            if (MermaidParserUtilities.StartsStatement(trimmed, "classDef") || MermaidParserUtilities.StartsStatement(trimmed, "class") ||
                MermaidParserUtilities.StartsStatement(trimmed, "style") || trimmed == "--") {
                document.Statements.Add(new MermaidStateStatement(trimmed, span));
                MermaidParserUtilities.RetainUnsupported(document, trimmed, span, result, "state style or concurrency boundary");
                continue;
            }

            if (TryParseTransition(trimmed, span, ref specialIndex, out var transition)) {
                document.Transitions.Add(transition);
                EnsureState(document, states, transition.SourceId, span, composites.Count == 0 ? null : composites.Peek().Id);
                EnsureState(document, states, transition.TargetId, span, composites.Count == 0 ? null : composites.Peek().Id);
                continue;
            }

            if (trimmed.StartsWith("state ", StringComparison.Ordinal)) {
                ParseStateDeclaration(document, states, trimmed, span, composites, result);
                continue;
            }

            var colon = trimmed.IndexOf(':');
            if (colon > 0) {
                var id = trimmed.Substring(0, colon).Trim();
                var label = MermaidParserUtilities.Unquote(trimmed.Substring(colon + 1).Trim());
                EnsureState(document, states, id, span, composites.Count == 0 ? null : composites.Peek().Id).Label = label;
                continue;
            }

            EnsureState(document, states, trimmed, span, composites.Count == 0 ? null : composites.Peek().Id);
        }

        foreach (var composite in composites) MermaidParserUtilities.Add(result, composite.Span, MermaidDiagnosticSeverity.Error,
            "Composite state definitions must close with '}'.", MermaidDiagnosticCodes.InvalidStatement);
        foreach (var note in document.Notes) if (note.NodeId != null && !states.ContainsKey(note.NodeId))
            MermaidParserUtilities.Add(result, note.Span, MermaidDiagnosticSeverity.Error, "State notes must reference a declared state or transition endpoint.", MermaidDiagnosticCodes.InvalidStatement);
        if (document.States.Count == 0 && document.Transitions.Count == 0) MermaidParserUtilities.Add(result, document.HeaderSpan, MermaidDiagnosticSeverity.Error, "Mermaid state diagrams require at least one state or transition.");
    }

    private static int ReadNote(MermaidStateDocument document, string[] lines, int line, MermaidSourceSpan firstSpan,
        IReadOnlyList<int> originalLineStarts, MermaidParseResult<MermaidDocument> result) {
        var lastLine = line;
        var closed = false;
        while (lastLine < lines.Length) {
            MermaidResourceGuard.Checkpoint();
            lastLine++;
            var raw = lines[lastLine - 1];
            if (MermaidParserUtilities.IsStateNoteTerminator(MermaidParserUtilities.StripInlineComment(raw.Trim()))) { closed = true; break; }
        }
        var span = new MermaidSourceSpan(line, firstSpan.Column,
            originalLineStarts[lastLine - 1] + lines[lastLine - 1].Length - originalLineStarts[line - 1] - firstSpan.Column + 1);
        var retained = document.SourceText.Substring(originalLineStarts[line - 1] + firstSpan.Column - 1, span.Length)
            .Replace("\r\n", "\n").Replace('\r', '\n');
        if (closed) {
            var note = Note.Match(lines[line - 1].Trim());
            if (note.Success) {
                var body = new List<string>();
                for (var current = line + 1; current < lastLine; current++) body.Add(lines[current - 1].Trim());
                AddNote(document, note, string.Join("\n", body), span);
            } else {
                document.Statements.Add(new MermaidStateStatement(retained, span));
                MermaidParserUtilities.RetainUnsupported(document, retained, span, result, "state note syntax");
            }
        }
        else {
            document.Statements.Add(new MermaidStateStatement(retained, span));
            document.RawStatements.Add(new MermaidRawStatement(retained, span));
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Multiline state notes must close with 'end note'.", MermaidDiagnosticCodes.InvalidStatement);
        }
        return lastLine;
    }

    private static void AddNote(MermaidStateDocument document, Match note, string text, MermaidSourceSpan span) =>
        document.Notes.Add(new MermaidDiagramNote(MermaidNotationText.Note(text), MermaidParserUtilities.Unquote(note.Groups["id"].Value),
            string.Equals(note.Groups["side"].Value, "left", StringComparison.OrdinalIgnoreCase), span));

    private static void ParseStateDeclaration(MermaidStateDocument document, Dictionary<string, MermaidStateNode> states, string text, MermaidSourceSpan span, Stack<(string Id, MermaidSourceSpan Span)> composites, MermaidParseResult<MermaidDocument> result) {
        var body = text.Substring(6).Trim();
        var opensComposite = body.EndsWith("{", StringComparison.Ordinal);
        if (opensComposite) body = body.Substring(0, body.Length - 1).Trim();
        string id;
        string? label = null;
        string? kind = null;
        var marker = body.IndexOf("<<", StringComparison.Ordinal);
        if (marker >= 0 && body.EndsWith(">>", StringComparison.Ordinal)) {
            kind = body.Substring(marker + 2, body.Length - marker - 4).Trim();
            if (!string.Equals(kind, "choice", StringComparison.OrdinalIgnoreCase) && !string.Equals(kind, "fork", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(kind, "join", StringComparison.OrdinalIgnoreCase))
                MermaidParserUtilities.RetainUnsupported(document, text, span, result, "state stereotype");
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

        var state = EnsureState(document, states, id, span, composites.Count == 0 ? null : composites.Peek().Id);
        if (!string.IsNullOrWhiteSpace(label)) state.Label = label;
        if (!string.IsNullOrWhiteSpace(kind)) state.Kind = kind;
        if (opensComposite) {
            MermaidResourceGuard.CheckDepth(composites.Count + 1, span);
            state.IsComposite = true;
            composites.Push((state.Id, span));
        }
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
        if (value == "[*]") return (source ? "mermaid-start-" : "mermaid-end-") + (++specialIndex).ToString(System.Globalization.CultureInfo.InvariantCulture);
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

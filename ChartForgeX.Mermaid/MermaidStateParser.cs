using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidStateParser {
    private static readonly Regex Note = new(@"^note\s+(?<side>left|right)\s+of\s+(?<id>[^\s:]+)\s*(?::\s*(?<text>.*))?$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    public static void ParseStatements(MermaidStateDocument document, string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        var states = new Dictionary<string, MermaidStateNode>(StringComparer.Ordinal);
        var composites = new Stack<StateScope>();
        var pseudoStates = new Dictionary<string, string>(StringComparer.Ordinal);
        var specialIndex = 0;
        var assignments = new List<StateStyleAssignment>();
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
                else {
                    var scope = composites.Peek();
                    scope.Region.Direction = direction;
                    if (scope.Region.Index == 0) states[scope.Id].Direction = direction;
                }
                continue;
            }

            if (MermaidParserUtilities.StartsStatement(trimmed, "note", ignoreCase: true)) {
                if (TryFloatingNote(document, trimmed, span, result)) continue;
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

            if (trimmed == "--") {
                if (composites.Count == 0) MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error,
                    "Concurrent regions require a containing composite state.", MermaidDiagnosticCodes.InvalidStatement);
                else {
                    var scope = composites.Peek();
                    scope.Region = CreateRegion(document, scope.Id, scope.Region.Index + 1, span);
                }
                continue;
            }
            if (trimmed.Length > 2 && trimmed.Trim('-').Length == 0) {
                MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error,
                    "Concurrent regions use exactly two hyphens.", MermaidDiagnosticCodes.InvalidStatement);
                continue;
            }

            if (TryReadStyle(document, trimmed, span, assignments, result)) continue;

            if (TryParseTransition(trimmed, span, composites.Count == 0 ? "root" : composites.Peek().Region.Id, pseudoStates,
                ref specialIndex, assignments, result, out var transition)) {
                if (transition != null) {
                    document.Transitions.Add(transition);
                    EnsureState(document, states, transition.SourceId, span, composites.Count == 0 ? null : composites.Peek().Id, composites.Count == 0 ? null : composites.Peek().Region.Id);
                    EnsureState(document, states, transition.TargetId, span, composites.Count == 0 ? null : composites.Peek().Id, composites.Count == 0 ? null : composites.Peek().Region.Id);
                }
                continue;
            }

            if (trimmed.StartsWith("state ", StringComparison.Ordinal)) {
                ParseStateDeclaration(document, states, trimmed, span, composites, assignments, result);
                continue;
            }

            var label = SplitLabel(ref trimmed);
            if (!ReadInlineClasses(ref trimmed, span, result, out var classes)) continue;
            if (trimmed.Length == 0) {
                MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "State declarations require an identity.", MermaidDiagnosticCodes.InvalidStatement);
                continue;
            }
            var state = EnsureState(document, states, trimmed, span, composites.Count == 0 ? null : composites.Peek().Id, composites.Count == 0 ? null : composites.Peek().Region.Id);
            if (label != null) state.Label = MermaidParserUtilities.Unquote(label);
            AddInlineAssignment(assignments, state.Id, classes, span);
        }

        ApplyStyles(document, states, assignments, result);
        foreach (var composite in composites) MermaidParserUtilities.Add(result, composite.Span, MermaidDiagnosticSeverity.Error,
            "Composite state definitions must close with '}'.", MermaidDiagnosticCodes.InvalidStatement);
        foreach (var note in document.Notes) if (note.NodeId != null && !states.ContainsKey(note.NodeId))
            MermaidParserUtilities.Add(result, note.Span, MermaidDiagnosticSeverity.Error, "State notes must reference a declared state or transition endpoint.", MermaidDiagnosticCodes.InvalidStatement);
        ValidateNoteIdentities(document, states, result);
        if (document.States.Count == 0 && document.Transitions.Count == 0 && document.Notes.Count == 0 && !result.HasErrors) MermaidParserUtilities.Add(result, document.HeaderSpan, MermaidDiagnosticSeverity.Error, "Mermaid state diagrams require at least one state, transition or note.");
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

    private static void ParseStateDeclaration(MermaidStateDocument document, Dictionary<string, MermaidStateNode> states, string text, MermaidSourceSpan span, Stack<StateScope> composites, List<StateStyleAssignment> assignments, MermaidParseResult<MermaidDocument> result) {
        var body = text.Substring(6).Trim();
        var opensComposite = body.EndsWith("{", StringComparison.Ordinal);
        if (opensComposite) body = body.Substring(0, body.Length - 1).Trim();
        if (!ReadInlineClasses(ref body, span, result, out var classes)) return;
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

        if (string.IsNullOrWhiteSpace(id)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "State declarations require an identity.", MermaidDiagnosticCodes.InvalidStatement);
            return;
        }
        var state = EnsureState(document, states, id, span, composites.Count == 0 ? null : composites.Peek().Id, composites.Count == 0 ? null : composites.Peek().Region.Id);
        AddInlineAssignment(assignments, state.Id, classes, span);
        if (!string.IsNullOrWhiteSpace(label)) state.Label = label;
        if (!string.IsNullOrWhiteSpace(kind)) state.Kind = kind;
        if (opensComposite) {
            MermaidResourceGuard.CheckDepth(composites.Count + 1, span);
            state.IsComposite = true;
            composites.Push(new StateScope(state.Id, span, OpenRegion(document, state.Id, span)));
        }
    }

    private static bool TryParseTransition(string text, MermaidSourceSpan span, string scopeId, Dictionary<string, string> pseudoStates,
        ref int specialIndex, List<StateStyleAssignment> assignments, MermaidParseResult<MermaidDocument> result, out MermaidStateTransition? transition) {
        transition = null;
        var index = text.IndexOf("-->", StringComparison.Ordinal);
        if (index <= 0) return false;
        var left = text.Substring(0, index).Trim();
        var right = text.Substring(index + 3).Trim();
        var label = SplitLabel(ref right);
        if (!ReadInlineClasses(ref left, span, result, out var leftClasses)
            || !ReadInlineClasses(ref right, span, result, out var rightClasses)) return true;
        if (left.Length == 0 || right.Length == 0) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "State transitions require two state identities.", MermaidDiagnosticCodes.InvalidStatement);
            return true;
        }
        var source = NormalizeSpecial(left, true, scopeId, pseudoStates, ref specialIndex);
        var target = NormalizeSpecial(right, false, scopeId, pseudoStates, ref specialIndex);
        transition = new MermaidStateTransition(source, target, label, span);
        AddInlineAssignment(assignments, source, leftClasses, span);
        AddInlineAssignment(assignments, target, rightClasses, span);
        return true;
    }

    private static string NormalizeSpecial(string value, bool source, string scopeId, Dictionary<string, string> pseudoStates, ref int specialIndex) {
        value = MermaidParserUtilities.Unquote(value);
        if (value == "[*]") {
            var key = scopeId + (source ? ":start" : ":end");
            if (!pseudoStates.TryGetValue(key, out var id)) {
                id = (source ? "mermaid-start-" : "mermaid-end-") + (++specialIndex).ToString(System.Globalization.CultureInfo.InvariantCulture);
                pseudoStates.Add(key, id);
            }
            return id;
        }
        return value;
    }

    private static string? SplitLabel(ref string text) {
        var colon = -1;
        var quote = '\0';
        for (var index = 0; index < text.Length; index++) {
            var character = text[index];
            if (quote != '\0') { if (character == quote) quote = '\0'; continue; }
            if (character == '"' || character == '`') { quote = character; continue; }
            if (character != ':') continue;
            if (index + 2 < text.Length && text[index + 1] == ':' && text[index + 2] == ':') { index += 2; continue; }
            colon = index; break;
        }
        if (colon < 0) return null;
        var label = text.Substring(colon + 1).Trim();
        text = text.Substring(0, colon).Trim();
        return label.Length == 0 ? null : label;
    }

    private static MermaidStateNode EnsureState(MermaidStateDocument document, Dictionary<string, MermaidStateNode> states, string id, MermaidSourceSpan span, string? parentId, string? regionId) {
        id = MermaidParserUtilities.Unquote(id);
        if (states.TryGetValue(id, out var existing)) {
            if (existing.ParentId == null) { existing.ParentId = parentId; existing.RegionId = regionId; }
            return existing;
        }

        var node = new MermaidStateNode(id, span) { ParentId = parentId, RegionId = regionId };
        if (id.StartsWith("mermaid-start", StringComparison.Ordinal)) node.Kind = "start";
        if (id.StartsWith("mermaid-end", StringComparison.Ordinal)) node.Kind = "end";
        states.Add(id, node);
        document.States.Add(node);
        return node;
    }
}

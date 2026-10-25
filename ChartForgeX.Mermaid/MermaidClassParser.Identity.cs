using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidClassParser {
    private static readonly Regex Connector = new(@"\G(?:<\||[<*o])?(?:--|\.\.)(?:\|>|[>*o])?", RegexOptions.CultureInvariant);

    /// <summary>Reads one quoted identity or finite generic type, leaving aliases, members and connectors untouched.</summary>
    private static bool ReadIdentity(string text, ref int position, MermaidSourceSpan span,
        MermaidParseResult<MermaidDocument> result, out string identity) {
        SkipSpace(text, ref position);
        var start = position;
        identity = string.Empty;
        if (position < text.Length && (text[position] == '`' || text[position] == '"')) {
            var quote = text[position++];
            while (position < text.Length) {
                if ((position & 255) == 0) MermaidResourceGuard.Checkpoint();
                if (text[position] == quote && !MermaidFlowchartText.IsEscaped(text, position)) {
                    position++;
                    identity = text.Substring(start, position - start);
                    if (CleanEndpoint(identity).Length > 0) return true;
                    break;
                }
                position++;
            }
            InvalidClass(result, span, "Class quoted identities require a closing quote and nonempty text.");
            return false;
        }
        if (!ReadType(text, ref position, span)) {
            InvalidClass(result, span, "Class identities require a name and balanced nonempty generic arguments.");
            return false;
        }
        identity = text.Substring(start, position - start);
        return true;
    }

    // A tilde after a type name opens its argument list; a tilde after a completed argument closes it.
    // Commas separate arguments only within that list. No expressions, quoted arguments or wildcard grammar.
    private static bool ReadType(string text, ref int position, MermaidSourceSpan span) {
        var start = position;
        while (position < text.Length) {
            if ((position & 255) == 0) MermaidResourceGuard.Checkpoint();
            var ch = text[position];
            if (char.IsWhiteSpace(ch) || "~,:{}[]<>|*()\"`".IndexOf(ch) >= 0 ||
                position + 1 < text.Length && (ch == '-' && text[position + 1] == '-' || ch == '.' && text[position + 1] == '.')) break;
            position++;
        }
        if (position == start) return false;
        if (position >= text.Length || text[position] != '~') return true;
        if (!MermaidNotationText.TryGenericArguments(text, ref position, out var nestingDepth)) return false;
        MermaidResourceGuard.CheckDepth(nestingDepth, span);
        return true;
    }

    private static bool TryReadRelationship(string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result,
        bool lollipop, out MermaidClassRelationship? relationship) {
        relationship = null;
        if (lollipop ? text.IndexOf("()--", StringComparison.Ordinal) < 0 && text.IndexOf("--()", StringComparison.Ordinal) < 0
            : text.IndexOf("--", StringComparison.Ordinal) < 0 && text.IndexOf("..", StringComparison.Ordinal) < 0) return false;
        var position = 0;
        if (!ReadIdentity(text, ref position, span, result, out var source)) return true;
        SkipSpace(text, ref position);
        string? sourceMultiplicity = null;
        if (position < text.Length && text[position] == '"') {
            if (!ReadIdentity(text, ref position, span, result, out var multiplicity)) return true;
            sourceMultiplicity = CleanEndpoint(multiplicity);
            SkipSpace(text, ref position);
        }
        string connector;
        if (lollipop) {
            if (text.IndexOf("()--", position, StringComparison.Ordinal) == position) connector = "()--";
            else if (text.IndexOf("--()", position, StringComparison.Ordinal) == position) connector = "--()";
            else return false;
        } else {
            var match = Connector.Match(text, position);
            if (!match.Success) return false;
            connector = match.Value;
        }
        position += connector.Length;
        SkipSpace(text, ref position);
        if (!ReadIdentity(text, ref position, span, result, out var target)) return true;
        SkipSpace(text, ref position);
        string? targetMultiplicity = null;
        if (target[0] == '"' && position < text.Length && text[position] != ':') {
            targetMultiplicity = CleanEndpoint(target);
            if (!ReadIdentity(text, ref position, span, result, out target)) return true;
            SkipSpace(text, ref position);
        }
        string? label = null;
        if (position < text.Length) {
            if (text[position] != ':') {
                InvalidClass(result, span, "Class relationships require two identities and an optional label after ':'.");
                return true;
            }
            label = text.Substring(position + 1).Trim();
        }
        relationship = new MermaidClassRelationship(CleanEndpoint(source), CleanEndpoint(target), connector, label, span) {
            SourceMultiplicity = sourceMultiplicity, TargetMultiplicity = targetMultiplicity,
            SourceIdentity = source, TargetIdentity = target
        };
        return true;
    }

    private static bool ReadAlias(string text, out string label, out string remainder) {
        label = remainder = string.Empty;
        var quote = '\0';
        for (var index = 1; index < text.Length; index++) {
            if (quote != '\0') {
                if (text[index] == quote && !MermaidFlowchartText.IsEscaped(text, index)) quote = '\0';
            } else if (text[index] == '"' || text[index] == '`') quote = text[index];
            else if (text[index] == ']') {
                var inner = text.Substring(1, index - 1).Trim();
                if (inner.Length == 0) return false;
                label = MermaidParserUtilities.Unquote(inner);
                remainder = text.Substring(index + 1).Trim();
                return label.Length > 0;
            }
        }
        return false;
    }

    private static bool TryNote(MermaidClassDocument document, Dictionary<string, MermaidClassNode> classes,
        string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result, out MermaidDiagramNote? note) {
        note = null;
        var position = 4;
        SkipSpace(text, ref position);
        string? id = null;
        string? reference = null;
        if (text.IndexOf("for", position, StringComparison.Ordinal) == position && position + 3 < text.Length && char.IsWhiteSpace(text[position + 3])) {
            position += 3;
            if (!ReadIdentity(text, ref position, span, result, out var identity)) return false;
            id = CleanEndpoint(identity);
            reference = identity;
            SkipSpace(text, ref position);
        }
        if (position >= text.Length || text[position] != '"' || text[text.Length - 1] != '"') return false;
        note = new MermaidDiagramNote(MermaidNotationText.Note(text.Substring(position)), id, false, span);
        if (reference != null) {
            classes.TryGetValue(id!, out var owner);
            RetainGenericReference(document, owner, id!, reference, span, result);
        }
        return true;
    }

    private static int MemberSeparator(string text) {
        var quote = '\0';
        for (var index = 0; index < text.Length; index++) {
            if (quote != '\0') {
                if (text[index] == quote && !MermaidFlowchartText.IsEscaped(text, index)) quote = '\0';
            } else if (text[index] == '`' || text[index] == '"') quote = text[index];
            else if (text[index] == ':') return index;
        }
        return -1;
    }

    private static void SkipSpace(string text, ref int position) {
        while (position < text.Length && char.IsWhiteSpace(text[position])) position++;
    }

    private static void InvalidClass(MermaidParseResult<MermaidDocument> result, MermaidSourceSpan span, string message) =>
        MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, message, MermaidDiagnosticCodes.InvalidStatement);

    private static void DiagnoseClassCaptions(MermaidClassDocument document, MermaidParseResult<MermaidDocument> result) {
        foreach (var node in document.Classes) {
            var captionSpan = document.CaptionSpans.TryGetValue(node.Id, out var replaced) ? replaced : node.Span;
            var caption = !document.CaptionSpans.ContainsKey(node.Id) && document.GenericDeclarations.TryGetValue(node.Id, out var generic)
                ? generic : node.Label ?? node.Id;
            MermaidInlineCaption.Diagnose(caption, captionSpan, result, "Class");
            foreach (var member in node.Members) MermaidInlineCaption.Diagnose(member.Text, member.Span, result, "Class");
        }
        foreach (var group in document.Namespaces) MermaidInlineCaption.Diagnose(group.Label,
            document.NamespaceCaptionSpans.TryGetValue(group.Id, out var replacement) ? replacement : group.Span, result, "Class");
        foreach (var edge in document.Relationships) if (edge.Label != null) MermaidInlineCaption.Diagnose(edge.Label, edge.Span, result, "Class");
    }
}

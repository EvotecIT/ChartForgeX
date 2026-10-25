using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidZenUmlParser {
    private static bool IsParticipantDeclaration(string text) => text.StartsWith("participant ", StringComparison.Ordinal) ||
        text.StartsWith("@", StringComparison.Ordinal) && !text.StartsWith("@Return", StringComparison.OrdinalIgnoreCase) && !text.StartsWith("@Reply", StringComparison.OrdinalIgnoreCase);

    private sealed partial class ParserState {
        private MermaidZenUmlParticipant Participant(string id, MermaidSourceSpan span, bool inferred) {
            foreach (var existing in _document.Participants) if (existing.Id == id) return existing;
            if (!IsIdentity(id)) throw Syntax("Invalid ZenUML participant identity.", span);
            MermaidResourceGuard.ReserveElements(1, span);
            var participant = new MermaidZenUmlParticipant(id, span) { IsImplicit = inferred };
            _document.Participants.Add(participant); return participant;
        }

        private void ReadParticipant(Token token, MermaidZenUmlParticipantGroup? group) {
            var text = token.Text; var kind = SequenceArtifactParticipantKind.Participant;
            var attributes = new Dictionary<string, string>(StringComparer.Ordinal);
            if (text.StartsWith("participant ", StringComparison.Ordinal)) text = text.Substring(12).Trim();
            else if (text.StartsWith("@", StringComparison.Ordinal)) {
                var space = text.IndexOf(' ');
                if (space < 0) throw Syntax("ZenUML participant annotation requires an identity.", token.Span);
                var annotation = text.Substring(1, space - 1);
                if (!Enum.TryParse(annotation, false, out kind) || !Enum.IsDefined(typeof(SequenceArtifactParticipantKind), kind)) {
                    kind = SequenceArtifactParticipantKind.Participant; attributes["annotation"] = annotation;
                    Unsupported(token.Text, token.Span, "participant annotation " + annotation);
                }
                text = text.Substring(space + 1).Trim();
            }
            if (text.StartsWith("<<", StringComparison.Ordinal)) {
                var end = text.IndexOf(">>", StringComparison.Ordinal);
                if (end < 2) throw Syntax("Unclosed ZenUML participant stereotype.", token.Span);
                attributes["stereotype"] = text.Substring(2, end - 2);
                text = text.Substring(end + 2).Trim();
            }
            if (text.StartsWith("[", StringComparison.Ordinal)) {
                var end = text.IndexOf(']');
                if (end <= 1) throw Syntax("Invalid ZenUML participant emoji shortcode.", token.Span);
                attributes["emoji"] = text.Substring(0, end + 1);
                text = text.Substring(end + 1).Trim();
                MermaidZenUmlPresentation.Symbol(attributes["emoji"],out var known);
                if(!known)Unsupported(token.Text,token.Span,"unresolved participant emoji shortcode");
            }
            var parts = Words(text, token.Span);
            if (parts.Count == 0) throw Syntax("ZenUML participant requires an identity.", token.Span);
            // An initial Unicode emoji is presentation only when followed by a name.
            var first = 0;
            if (parts.Count > 1 && !char.IsLetterOrDigit(parts[0][0]) && parts[0][0] != '_' && parts[0][0] != '$') {
                attributes["emoji"] = parts[0]; first++;
            }
            var id = parts[first++]; var participant = Participant(id, token.Span, false);
            participant.IsImplicit = false; participant.Kind = kind;
            while (first < parts.Count) {
                var value = parts[first++];
                if (value == "as") {
                    if (first >= parts.Count) throw Syntax("ZenUML participant alias is missing.", token.Span);
                    participant.Alias = MermaidParserUtilities.Unquote(parts[first++]);
                } else if (value.StartsWith("#", StringComparison.Ordinal)) {
                    if (!IsHexColor(value)) throw Syntax("ZenUML participant color must be a hex color.", token.Span);
                    attributes["color"] = value;
                } else if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var width) && width > 0) {
                    attributes["width"] = value;
                } else throw Syntax("Unsupported ZenUML participant declaration token: " + value, token.Span);
            }
            foreach (var attribute in attributes) participant.Attributes[attribute.Key] = attribute.Value;
            if (group == null) return;
            foreach (var previous in _document.ParticipantGroups)
                if (previous.ParticipantIds.Contains(id)) throw Syntax("ZenUML participant belongs to more than one group.", token.Span);
            if (group.ParticipantIds.Contains(id)) throw Syntax("Duplicate ZenUML participant in a group.", token.Span);
            if (group.ParticipantIds.Count > 0) {
                var last = _document.Participants.FindIndex(item => item.Id == group.ParticipantIds[group.ParticipantIds.Count - 1]);
                var current = _document.Participants.FindIndex(item => item.Id == id);
                if (current != last + 1) throw Syntax("ZenUML grouped participants must be contiguous in first-reference order.", token.Span);
            }
            group.ParticipantIds.Add(id);
        }
    }

    private static List<string> Words(string text, MermaidSourceSpan span) {
        var words = new List<string>(); var start = -1; var quote = '\0'; var escaped = false;
        for (var index = 0; index < text.Length; index++) {
            MermaidResourceGuard.Checkpoint(); var ch = text[index];
            if (start < 0) { if (char.IsWhiteSpace(ch)) continue; start = index; }
            if (quote != '\0') {
                if (escaped) escaped = false; else if (ch == '\\') escaped = true; else if (ch == quote) quote = '\0';
            } else if (ch is '"' or '\'' or '`') quote = ch;
            else if (char.IsWhiteSpace(ch)) { words.Add(text.Substring(start, index - start)); start = -1; }
        }
        if (quote != '\0') throw Syntax("Unclosed ZenUML participant alias.", span);
        if (start >= 0) words.Add(text.Substring(start));
        return words;
    }
    private static bool IsHexColor(string value) {
        if (value.Length != 4 && value.Length != 7 && value.Length != 9) return false;
        for (var index = 1; index < value.Length; index++) if (!Uri.IsHexDigit(value[index])) return false;
        return true;
    }
}

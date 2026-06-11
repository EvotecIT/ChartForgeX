using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

internal static class MermaidSequenceParser {
    private static readonly string[] MessageOperators = {
        "<<-->>", "<<->>", "-->>", "->>", "--)", "-)", "--x", "-x", "-->", "->", "--"
    };

    public static void ParseStatements(MermaidSequenceDocument document, string[] lines, int firstBodyLine, MermaidParseResult<MermaidDocument> result) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        if (lines == null) throw new ArgumentNullException(nameof(lines));
        if (result == null) throw new ArgumentNullException(nameof(result));

        var participants = new Dictionary<string, MermaidSequenceParticipant>(StringComparer.Ordinal);
        var blockStack = new Stack<MermaidSequenceBlockKind>();
        for (var index = firstBodyLine - 1; index < lines.Length; index++) {
            var raw = lines[index];
            var fragments = SplitStatements(raw);
            foreach (var fragment in fragments) {
                var trimmed = fragment.Text.Trim();
                if (trimmed.Length == 0 || IsComment(trimmed)) continue;

                var span = new MermaidSourceSpan(index + 1, LeadingWhitespace(raw) + fragment.Column, trimmed.Length);
                document.Statements.Add(new MermaidRawStatement(trimmed, span));

                if (TryParseAutonumber(document, trimmed, span)) continue;
                if (TryParseParticipant(document, participants, trimmed, span)) continue;
                if (TryParseActivation(document, participants, trimmed, span)) continue;
                if (TryParseNote(document, participants, trimmed, span)) continue;
                if (TryParseLink(document, participants, trimmed, span)) continue;
                if (TryParseBlock(document, blockStack, trimmed, span)) continue;
                if (TryParseMessage(document, participants, trimmed, span)) continue;
                MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Unrecognized sequence diagram statement was not rendered: " + trimmed);
            }
        }
    }

    private static bool TryParseAutonumber(MermaidSequenceDocument document, string text, MermaidSourceSpan span) {
        if (!StartsCommand(text, "autonumber")) return false;
        var tail = text.Length == "autonumber".Length ? string.Empty : text.Substring("autonumber".Length).Trim();
        var tokens = SplitWhitespace(tail);
        document.Autonumber = new MermaidSequenceAutonumber(span) {
            Start = tokens.Count > 0 ? tokens[0] : null,
            Increment = tokens.Count > 1 ? tokens[1] : null
        };
        return true;
    }

    private static bool TryParseParticipant(MermaidSequenceDocument document, Dictionary<string, MermaidSequenceParticipant> participants, string text, MermaidSourceSpan span) {
        var command = FirstToken(text);
        var kind = ParseParticipantKind(command);
        if (!kind.HasValue) return false;

        var body = text.Substring(command.Length).Trim();
        if (body.Length == 0) return false;

        var id = ReadIdentifier(body, out var consumed);
        if (id.Length == 0) return false;

        var rest = body.Substring(consumed).Trim();
        var participant = GetOrAddParticipant(document, participants, id, span);
        participant.Kind = kind.Value;
        participant.IsImplicit = false;

        if (rest.StartsWith("{", StringComparison.Ordinal)) {
            var config = ReadJsonLike(rest, out var configLength);
            participant.Configuration = config;
            var inlineAlias = ReadJsonStringValue(config, "alias");
            if (!string.IsNullOrWhiteSpace(inlineAlias)) participant.Alias = inlineAlias;
            var type = ReadJsonStringValue(config, "type");
            if (type != null && !string.IsNullOrWhiteSpace(type)) participant.Kind = ParseParticipantKind(type) ?? participant.Kind;
            rest = rest.Substring(configLength).Trim();
        }

        var alias = ReadExternalAlias(rest);
        if (!string.IsNullOrWhiteSpace(alias)) participant.Alias = alias;
        return true;
    }

    private static bool TryParseActivation(MermaidSequenceDocument document, Dictionary<string, MermaidSequenceParticipant> participants, string text, MermaidSourceSpan span) {
        var active = StartsCommand(text, "activate");
        var inactive = StartsCommand(text, "deactivate");
        if (!active && !inactive) return false;

        var command = active ? "activate" : "deactivate";
        var participantId = text.Substring(command.Length).Trim();
        if (participantId.Length == 0) return false;

        GetOrAddParticipant(document, participants, participantId, span);
        document.Activations.Add(new MermaidSequenceActivation(participantId, active, span));
        return true;
    }

    private static bool TryParseNote(MermaidSequenceDocument document, Dictionary<string, MermaidSequenceParticipant> participants, string text, MermaidSourceSpan span) {
        if (!StartsCommand(text, "Note")) return false;
        var body = text.Substring("Note".Length).Trim();
        var colon = body.IndexOf(':');
        if (colon <= 0) return false;

        var target = body.Substring(0, colon).Trim();
        var noteText = body.Substring(colon + 1).Trim();
        var placement = string.Empty;
        var ids = string.Empty;
        if (StartsCommand(target, "right of")) {
            placement = "right of";
            ids = target.Substring("right of".Length).Trim();
        } else if (StartsCommand(target, "left of")) {
            placement = "left of";
            ids = target.Substring("left of".Length).Trim();
        } else if (StartsCommand(target, "over")) {
            placement = "over";
            ids = target.Substring("over".Length).Trim();
        }

        if (placement.Length == 0 || ids.Length == 0) return false;
        var note = new MermaidSequenceNote(placement, span) { Text = noteText };
        foreach (var id in SplitCsv(ids)) {
            note.ParticipantIds.Add(id);
            GetOrAddParticipant(document, participants, id, span);
        }

        document.Notes.Add(note);
        return true;
    }

    private static bool TryParseLink(MermaidSequenceDocument document, Dictionary<string, MermaidSequenceParticipant> participants, string text, MermaidSourceSpan span) {
        var advanced = StartsCommand(text, "links");
        var simple = StartsCommand(text, "link");
        if (!advanced && !simple) return false;

        var command = advanced ? "links" : "link";
        var body = text.Substring(command.Length).Trim();
        var colon = body.IndexOf(':');
        if (colon <= 0) return false;

        var participantId = body.Substring(0, colon).Trim();
        var value = body.Substring(colon + 1).Trim();
        if (participantId.Length == 0 || value.Length == 0) return false;

        GetOrAddParticipant(document, participants, participantId, span);
        var link = new MermaidSequenceLink(participantId, span);
        if (advanced) {
            link.RawJson = value;
        } else {
            var at = value.IndexOf('@');
            if (at >= 0) {
                link.Label = value.Substring(0, at).Trim();
                link.Url = value.Substring(at + 1).Trim();
            } else {
                link.Label = value;
            }
        }

        document.Links.Add(link);
        return true;
    }

    private static bool TryParseBlock(MermaidSequenceDocument document, Stack<MermaidSequenceBlockKind> blockStack, string text, MermaidSourceSpan span) {
        if (string.Equals(text, "end", StringComparison.OrdinalIgnoreCase)) {
            var depth = Math.Max(0, blockStack.Count - 1);
            if (blockStack.Count > 0) blockStack.Pop();
            document.Blocks.Add(new MermaidSequenceBlock(MermaidSequenceBlockKind.End, span) { Depth = depth });
            return true;
        }

        var kind = TryGetBlockKind(text, out var command);
        if (!kind.HasValue) return false;

        var body = text.Length == command.Length ? string.Empty : text.Substring(command.Length).Trim();
        var branch = kind == MermaidSequenceBlockKind.Else || kind == MermaidSequenceBlockKind.And || kind == MermaidSequenceBlockKind.Option;
        var block = new MermaidSequenceBlock(kind.Value, span) {
            Text = body.Length == 0 ? null : body,
            Depth = branch ? Math.Max(0, blockStack.Count - 1) : blockStack.Count
        };

        document.Blocks.Add(block);
        if (!branch) blockStack.Push(kind.Value);
        return true;
    }

    private static bool TryParseMessage(MermaidSequenceDocument document, Dictionary<string, MermaidSequenceParticipant> participants, string text, MermaidSourceSpan span) {
        if (!TryFindOperator(text, out var operatorIndex, out var messageOperator)) return false;
        var sourceId = text.Substring(0, operatorIndex).Trim();
        var afterOperator = text.Substring(operatorIndex + messageOperator.Length).Trim();
        if (sourceId.Length == 0 || afterOperator.Length == 0) return false;

        var activates = false;
        var deactivates = false;
        if (afterOperator[0] == '+' || afterOperator[0] == '-') {
            activates = afterOperator[0] == '+';
            deactivates = afterOperator[0] == '-';
            messageOperator += afterOperator[0];
            afterOperator = afterOperator.Substring(1).TrimStart();
        }

        var central = false;
        if (afterOperator.StartsWith("()", StringComparison.Ordinal)) {
            central = true;
            afterOperator = afterOperator.Substring(2).TrimStart();
        }

        var colon = afterOperator.IndexOf(':');
        var targetId = colon >= 0 ? afterOperator.Substring(0, colon).Trim() : afterOperator.Trim();
        var messageText = colon >= 0 ? afterOperator.Substring(colon + 1).Trim() : null;
        if (targetId.EndsWith("()", StringComparison.Ordinal)) {
            central = true;
            targetId = targetId.Substring(0, targetId.Length - 2).TrimEnd();
        }

        if (targetId.Length == 0) return false;
        GetOrAddParticipant(document, participants, sourceId, span);
        if (!central) GetOrAddParticipant(document, participants, targetId, span);

        document.Messages.Add(new MermaidSequenceMessage(sourceId, targetId, messageOperator, span) {
            Text = string.IsNullOrWhiteSpace(messageText) ? null : messageText,
            IsCentralConnection = central,
            ActivatesTarget = activates,
            Deactivates = deactivates
        });
        return true;
    }

    private static MermaidSequenceParticipant GetOrAddParticipant(MermaidSequenceDocument document, Dictionary<string, MermaidSequenceParticipant> participants, string id, MermaidSourceSpan span) {
        if (participants.TryGetValue(id, out var participant)) return participant;
        participant = new MermaidSequenceParticipant(id, span) { IsImplicit = true };
        participants[id] = participant;
        document.Participants.Add(participant);
        return participant;
    }

    private static bool TryFindOperator(string text, out int operatorIndex, out string messageOperator) {
        operatorIndex = -1;
        messageOperator = string.Empty;
        foreach (var candidate in MessageOperators) {
            var index = text.IndexOf(candidate, StringComparison.Ordinal);
            if (index <= 0) continue;
            if (operatorIndex >= 0 && (index > operatorIndex || (index == operatorIndex && candidate.Length <= messageOperator.Length))) continue;
            operatorIndex = index;
            messageOperator = candidate;
        }

        return operatorIndex >= 0;
    }

    private static MermaidSequenceBlockKind? TryGetBlockKind(string text, out string command) {
        command = string.Empty;
        foreach (var item in new[] {
            Tuple.Create("critical", MermaidSequenceBlockKind.Critical),
            Tuple.Create("option", MermaidSequenceBlockKind.Option),
            Tuple.Create("break", MermaidSequenceBlockKind.Break),
            Tuple.Create("loop", MermaidSequenceBlockKind.Loop),
            Tuple.Create("alt", MermaidSequenceBlockKind.Alt),
            Tuple.Create("else", MermaidSequenceBlockKind.Else),
            Tuple.Create("opt", MermaidSequenceBlockKind.Opt),
            Tuple.Create("par", MermaidSequenceBlockKind.Par),
            Tuple.Create("and", MermaidSequenceBlockKind.And),
            Tuple.Create("rect", MermaidSequenceBlockKind.Rect)
        }) {
            if (!StartsCommand(text, item.Item1)) continue;
            command = item.Item1;
            return item.Item2;
        }

        return null;
    }

    private static MermaidSequenceParticipantKind? ParseParticipantKind(string value) {
        switch (Normalize(value)) {
            case "participant":
                return MermaidSequenceParticipantKind.Participant;
            case "actor":
                return MermaidSequenceParticipantKind.Actor;
            case "boundary":
                return MermaidSequenceParticipantKind.Boundary;
            case "control":
                return MermaidSequenceParticipantKind.Control;
            case "entity":
                return MermaidSequenceParticipantKind.Entity;
            case "database":
                return MermaidSequenceParticipantKind.Database;
            case "collections":
                return MermaidSequenceParticipantKind.Collections;
            case "queue":
                return MermaidSequenceParticipantKind.Queue;
            default:
                return null;
        }
    }

    private static List<StatementFragment> SplitStatements(string line) {
        var fragments = new List<StatementFragment>();
        var start = 0;
        var quote = '\0';
        for (var index = 0; index < line.Length; index++) {
            var value = line[index];
            if (quote != '\0') {
                if (value == quote) quote = '\0';
                continue;
            }

            if (value == '"' || value == '\'') {
                quote = value;
                continue;
            }

            if (value != ';') continue;
            fragments.Add(new StatementFragment(line.Substring(start, index - start), start + 1));
            start = index + 1;
        }

        fragments.Add(new StatementFragment(line.Substring(start), start + 1));
        return fragments;
    }

    private static string ReadIdentifier(string text, out int consumed) {
        consumed = 0;
        while (consumed < text.Length && !char.IsWhiteSpace(text[consumed]) && text[consumed] != '{') consumed++;
        return text.Substring(0, consumed);
    }

    private static string ReadJsonLike(string text, out int length) {
        var depth = 0;
        var quote = '\0';
        for (var index = 0; index < text.Length; index++) {
            var value = text[index];
            if (quote != '\0') {
                if (value == quote) quote = '\0';
                continue;
            }

            if (value == '"' || value == '\'') {
                quote = value;
                continue;
            }

            if (value == '{') depth++;
            else if (value == '}') {
                depth--;
                if (depth == 0) {
                    length = index + 1;
                    return text.Substring(0, length);
                }
            }
        }

        length = text.Length;
        return text;
    }

    private static string? ReadJsonStringValue(string text, string key) {
        foreach (var quote in new[] { '"', '\'' }) {
            var needle = quote + key + quote;
            var index = text.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
            if (index < 0) continue;
            var colon = text.IndexOf(':', index + needle.Length);
            if (colon < 0) continue;
            var valueStart = colon + 1;
            while (valueStart < text.Length && char.IsWhiteSpace(text[valueStart])) valueStart++;
            if (valueStart >= text.Length || text[valueStart] != quote) continue;
            var valueEnd = text.IndexOf(quote, valueStart + 1);
            if (valueEnd > valueStart) return text.Substring(valueStart + 1, valueEnd - valueStart - 1);
        }

        return null;
    }

    private static string? ReadExternalAlias(string text) {
        if (text.Length == 0) return null;
        if (text.StartsWith("as ", StringComparison.OrdinalIgnoreCase)) return Unquote(text.Substring(3).Trim());
        var index = text.IndexOf(" as ", StringComparison.OrdinalIgnoreCase);
        if (index < 0) return null;
        return Unquote(text.Substring(index + 4).Trim());
    }

    private static List<string> SplitWhitespace(string text) {
        var values = new List<string>();
        foreach (var token in text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)) values.Add(token);
        return values;
    }

    private static IEnumerable<string> SplitCsv(string text) {
        foreach (var value in text.Split(',')) {
            var trimmed = value.Trim();
            if (trimmed.Length > 0) yield return trimmed;
        }
    }

    private static string FirstToken(string text) {
        var trimmed = text.Trim();
        var split = trimmed.IndexOfAny(new[] { ' ', '\t' });
        return split < 0 ? trimmed : trimmed.Substring(0, split);
    }

    private static bool StartsCommand(string text, string command) =>
        text.Equals(command, StringComparison.OrdinalIgnoreCase) ||
        (text.StartsWith(command, StringComparison.OrdinalIgnoreCase) && text.Length > command.Length && char.IsWhiteSpace(text[command.Length]));

    private static bool IsComment(string text) => text.StartsWith("%%", StringComparison.Ordinal);

    private static string Normalize(string value) {
        var builder = new System.Text.StringBuilder();
        foreach (var ch in value) {
            if (char.IsLetterOrDigit(ch)) builder.Append(char.ToLowerInvariant(ch));
        }

        return builder.ToString();
    }

    private static string Unquote(string value) {
        if (value.Length >= 2 && ((value[0] == '"' && value[value.Length - 1] == '"') || (value[0] == '\'' && value[value.Length - 1] == '\''))) return value.Substring(1, value.Length - 2);
        return value;
    }

    private static int LeadingWhitespace(string value) {
        var index = 0;
        while (index < value.Length && char.IsWhiteSpace(value[index])) index++;
        return index;
    }

    private readonly struct StatementFragment {
        public StatementFragment(string text, int column) {
            Text = text;
            Column = column;
        }

        public string Text { get; }

        public int Column { get; }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Primitives;
using ChartForgeX.SvgRaster;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidSequenceParser {
    private static bool TryParseTitle(MermaidSequenceDocument document, string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        if (!StartsCommand(text, "title") && !text.StartsWith("title:", StringComparison.OrdinalIgnoreCase)) return false;
        var title = text.Substring(5).Trim().TrimStart(':').Trim();
        if (title.Length == 0) Invalid(result, span, "Sequence title must contain text.");
        else document.Title = Unquote(title);
        return true;
    }

    private static bool TryParseLifetime(MermaidSequenceDocument document, Dictionary<string, MermaidSequenceParticipant> participants,
        string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        var creates = StartsCommand(text, "create");
        if (!creates && !StartsCommand(text, "destroy")) return false;
        var body = text.Substring(creates ? 6 : 7).Trim();
        string id;
        if (creates) {
            var kind = FirstToken(body);
            if ((!kind.Equals("participant", StringComparison.OrdinalIgnoreCase) && !kind.Equals("actor", StringComparison.OrdinalIgnoreCase))
                || !TryParseParticipant(document, participants, body, span, result)) {
                Invalid(result, span, "Sequence create requires 'participant' or 'actor' and an identifier.");
                return true;
            }
            id = ReadIdentifier(body.Substring(kind.Length).TrimStart(), out _);
            if (id.Length == 0) return true; // The declaration owner already reported the missing identifier.
        } else {
            id = ReadIdentifier(body, out var consumed);
            if (id.Length == 0 || consumed != body.Length) {
                Invalid(result, span, "Sequence destroy requires exactly one participant identifier.");
                return true;
            }
            GetOrAddParticipant(document, participants, id, span);
        }
        document.Lifetimes.Add(new MermaidSequenceLifetime(id, creates, span) { MessageIndex = document.Messages.Count });
        return true;
    }

    private static bool TryParseParticipantBox(MermaidSequenceDocument document, ref MermaidSequenceParticipantBox? open,
        Stack<MermaidSequenceBlock> blocks, string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        if (string.Equals(text, "end", StringComparison.OrdinalIgnoreCase) && open != null && blocks.Count == 0) {
            open.EndSpan = span;
            if (open.ParticipantIds.Count == 0) Invalid(result, open.Span, "Sequence participant box requires at least one participant declaration.");
            open = null;
            return true;
        }
        if (!StartsCommand(text, "box")) return false;
        MermaidResourceGuard.CheckDepth(1, span);
        if (open != null || blocks.Count != 0) {
            Invalid(result, span, "Sequence participant boxes cannot nest or start inside an interaction block.");
            return true;
        }
        var body = text.Substring(3).Trim();
        var token = FirstToken(body);
        var length = token.Length;
        var opening = body.IndexOf('(');
        if (opening >= 0 && (token.StartsWith("rgb", StringComparison.OrdinalIgnoreCase) || token.StartsWith("hsl", StringComparison.OrdinalIgnoreCase))) {
            var close = body.IndexOf(')', opening + 1);
            if (close >= 0) { token = body.Substring(0, close + 1); length = token.Length; }
        }
        var box = new MermaidSequenceParticipantBox(span);
        if (token.Equals("transparent", StringComparison.OrdinalIgnoreCase) || SvgRasterColor.TryParse(token, out _)) {
            box.Color = token;
            box.Label = Unquote(body.Substring(length).Trim());
        } else if (token.StartsWith("rgb(", StringComparison.OrdinalIgnoreCase) || token.StartsWith("rgba(", StringComparison.OrdinalIgnoreCase)
            || token.StartsWith("hsl(", StringComparison.OrdinalIgnoreCase) || token.StartsWith("hsla(", StringComparison.OrdinalIgnoreCase)) {
            box.Color = token;
            box.Label = Unquote(body.Substring(length).Trim());
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning,
                "Sequence box color is retained without native application: " + token, MermaidDiagnosticCodes.UnsupportedStatement);
        } else box.Label = Unquote(body);
        document.ParticipantBoxes.Add(box);
        open = box;
        return true;
    }

    private static void ValidateLifetimes(MermaidSequenceDocument document, MermaidParseResult<MermaidDocument> result) {
        var births = new Dictionary<string, MermaidSequenceLifetime>(StringComparer.Ordinal);
        var deaths = new Dictionary<string, MermaidSequenceLifetime>(StringComparer.Ordinal);
        foreach (var lifetime in document.Lifetimes) {
            var target = lifetime.Creates ? births : deaths;
            if (target.ContainsKey(lifetime.ParticipantId)) {
                Invalid(result, lifetime.Span, "Sequence participant '" + lifetime.ParticipantId + "' cannot be " + (lifetime.Creates ? "created" : "destroyed") + " more than once.");
                continue;
            }
            target.Add(lifetime.ParticipantId, lifetime);
            if (lifetime.MessageIndex >= document.Messages.Count) {
                Invalid(result, lifetime.Span, "Sequence " + (lifetime.Creates ? "create" : "destroy") + " requires an associated following message.");
                continue;
            }
            var message = document.Messages[lifetime.MessageIndex];
            if (lifetime.Creates ? message.TargetId != lifetime.ParticipantId || message.SourceId == lifetime.ParticipantId
                : message.SourceId != lifetime.ParticipantId && message.TargetId != lifetime.ParticipantId)
                Invalid(result, lifetime.Span, lifetime.Creates ? "Only the recipient of the following message can be created." : "The following message must involve the participant being destroyed.");
        }
        foreach (var death in deaths.Values) {
            if (births.TryGetValue(death.ParticipantId, out var birth) && death.MessageIndex <= birth.MessageIndex)
                Invalid(result, death.Span, "Sequence destruction must follow the participant's creating message.");
        }
        for (var index = 0; index < document.Messages.Count; index++) {
            var message = document.Messages[index];
            ValidateUse(message.SourceId, index, message.Span, false);
            if (message.SourceId != message.TargetId) ValidateUse(message.TargetId, index, message.Span, false);
        }
        foreach (var note in document.Notes) foreach (var id in note.ParticipantIds) ValidateUse(id, MessagesBefore(note.Span), note.Span, true);
        foreach (var activation in document.Activations) ValidateUse(activation.ParticipantId, MessagesBefore(activation.Span), activation.Span, true);
        void ValidateUse(string id, int index, MermaidSourceSpan span, bool boundary) {
            if (births.TryGetValue(id, out var birth) && (index < birth.MessageIndex || boundary && index == birth.MessageIndex))
                Invalid(result, span, "Sequence participant '" + id + "' is used before its creating message.");
            if (deaths.TryGetValue(id, out var death) && index > death.MessageIndex)
                Invalid(result, span, "Sequence participant '" + id + "' is used after its destroying message.");
        }
        int MessagesBefore(MermaidSourceSpan span) {
            var low = 0;
            var high = document.Messages.Count;
            while (low < high) {
                var middle = low + (high - low) / 2;
                var message = document.Messages[middle];
                if (message.Span.Line < span.Line || message.Span.Line == span.Line && message.Span.Column < span.Column) low = middle + 1;
                else high = middle;
            }
            return low;
        }
    }

    private static void ValidateAutonumber(MermaidSequenceDocument document, MermaidParseResult<MermaidDocument> result) {
        if (document.AutonumberStatements.Count == 0) return;
        if (!MermaidSequenceNumbering.TryResolve(document, out _, out var invalidSpan))
            Invalid(result, invalidSpan, "Sequence autonumber exceeds the supported decimal range across its messages.");
    }

    private static void ValidateParticipantBoxes(MermaidSequenceDocument document, MermaidParseResult<MermaidDocument> result) {
        var grouped = new HashSet<string>(StringComparer.Ordinal);
        foreach (var box in document.ParticipantBoxes) {
            var first = -1;
            for (var i = 0; i < document.Participants.Count; i++) {
                if (box.ParticipantIds.Contains(document.Participants[i].Id)) { first = i; break; }
            }
            for (var i = 0; i < box.ParticipantIds.Count; i++) {
                var id = box.ParticipantIds[i];
                if (!grouped.Add(id)) Invalid(result, box.Span, "Sequence participant '" + id + "' cannot belong to more than one box.");
                if (first < 0 || first + i >= document.Participants.Count || document.Participants[first + i].Id != id) {
                    Invalid(result, box.Span, "Sequence participant boxes require a contiguous range in participant declaration order.");
                    break;
                }
            }
        }
    }

    private static void Invalid(MermaidParseResult<MermaidDocument> result, MermaidSourceSpan span, string message) =>
        MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, message, MermaidDiagnosticCodes.InvalidStatement);
}

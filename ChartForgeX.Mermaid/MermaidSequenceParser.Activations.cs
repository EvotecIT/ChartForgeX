using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidSequenceParser {
    private static void ValidateActivations(MermaidSequenceDocument document, MermaidParseResult<MermaidDocument> result) {
        var depth = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var participant in document.Participants) depth[participant.Id] = 0;
        var events = new List<MermaidAstNode>();
        events.AddRange(document.Messages);
        events.AddRange(document.Activations);
        events.Sort((left, right) => left.Span.Line != right.Span.Line ? left.Span.Line.CompareTo(right.Span.Line) : left.Span.Column.CompareTo(right.Span.Column));
        var deaths = new Dictionary<int, List<string>>();
        foreach (var lifetime in document.Lifetimes) {
            if (lifetime.Creates) continue;
            if (!deaths.TryGetValue(lifetime.MessageIndex, out var ids)) deaths.Add(lifetime.MessageIndex, ids = new List<string>());
            ids.Add(lifetime.ParticipantId);
        }
        var messageIndex = 0;
        foreach (var item in events) {
            MermaidResourceGuard.Checkpoint();
            if (item is MermaidSequenceActivation activation) Change(activation.ParticipantId, activation.Active, activation.Span);
            else if (item is MermaidSequenceMessage message) {
                if (message.ActivatesTarget) Change(message.TargetId, true, message.Span);
                if (message.Deactivates) Change(message.SourceId, false, message.Span);
                if (deaths.TryGetValue(messageIndex, out var ids)) foreach (var id in ids) depth[id] = 0;
                messageIndex++;
            }
        }
        // Open activations deliberately continue to the diagram end, as Mermaid does.
        void Change(string id, bool active, MermaidSourceSpan span) {
            depth.TryGetValue(id, out var current);
            if (active) depth[id] = current + 1;
            else if (current > 0) depth[id] = current - 1;
            else Invalid(result, span, "Sequence deactivation has no matching activation for participant '" + id + "'.");
        }
    }
}

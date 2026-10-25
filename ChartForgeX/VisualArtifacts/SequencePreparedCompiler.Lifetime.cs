using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualArtifacts;

internal static partial class SequencePreparedCompiler {
    private static void ParticipantGroups(VisualSceneBuilder builder, SequenceArtifact model, SequencePreparedLayout layout,
        VisualArtifactInterchangeEnvelope semantics, VisualRenderContext context) {
        var colors = context.Theme.Resolve(context.ThemeMode);var padding=layout.Presentation.Layout?.FragmentLabelPadding;
        foreach (var item in layout.ParticipantGroups) {
            var source = model.ParticipantGroups[item.Index];
            using (Group(builder, item, "sequence-participant-group", source.Label)) {
                builder.Rect(item.Bounds, source.Fill, colors.Border, context.Theme.AxisStrokeWidth, role: "sequence-participant-group-frame");
                var label = new ChartRect(item.Bounds.X + (padding??8), item.Bounds.Y + (padding??6), item.Bounds.Width - (padding.HasValue?padding.Value*2:16), Math.Max(0, item.Y - (padding.HasValue?padding.Value*2:10)));
                // Group colors can be opaque in either palette. A theme surface behind the caption
                // keeps it legible without changing the authored group fill.
                if (item.Text.Length > 0) {
                    builder.Rect(padding.HasValue?new ChartRect(item.Bounds.X,item.Bounds.Y,item.Bounds.Width,item.Y):new ChartRect(item.Bounds.X + 4, item.Bounds.Y + 2, item.Bounds.Width - 8, item.Y - 4), colors.Surface);
                    Text(builder, item.Text, label, layout, SequenceTextRole.Fragment, colors.Foreground, center: true);
                }
            }
            Region(builder, item, "sequence-participant-group", source.Label);
            var group = semantics.Groups[item.Index];
            group.X = item.Bounds.X; group.Y = item.Bounds.Y; group.Width = item.Bounds.Width; group.Height = item.Bounds.Height;
        }
    }

    private static void ValidateParticipantLifetimesAndGroups(SequenceArtifact model) {
        var occupied = new HashSet<int>();
        var groupIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var group in model.ParticipantGroups) {
            if (!groupIds.Add(group.Id)) throw new InvalidOperationException("Duplicate sequence participant group: " + group.Id);
            if (group.FirstParticipantIndex < 0 || group.LastParticipantIndex < group.FirstParticipantIndex || group.LastParticipantIndex >= model.Participants.Count)
                throw new InvalidOperationException("Sequence participant group requires a valid inclusive participant range.");
            for (var index = group.FirstParticipantIndex; index <= group.LastParticipantIndex; index++)
                if (!occupied.Add(index)) throw new InvalidOperationException("Sequence participant groups must not overlap.");
        }
        var participants = new Dictionary<string, SequenceArtifactParticipant>(StringComparer.Ordinal);
        foreach (var participant in model.Participants) {
            participants.Add(participant.Id, participant);
            Check(participant.CreatedAtMessageIndex); Check(participant.DestroyedAtMessageIndex);
            if (participant.CreatedAtMessageIndex.HasValue && participant.DestroyedAtMessageIndex.HasValue
                && participant.DestroyedAtMessageIndex <= participant.CreatedAtMessageIndex)
                throw new InvalidOperationException("Sequence destruction must follow the creating message.");
            if (participant.CreatedAtMessageIndex.HasValue) {
                var creating = model.Messages[participant.CreatedAtMessageIndex.Value];
                if (creating.TargetId != participant.Id || creating.SourceId == participant.Id)
                    throw new InvalidOperationException("Only a sequence message recipient can be created.");
            }
            if (participant.DestroyedAtMessageIndex.HasValue) {
                var destroying = model.Messages[participant.DestroyedAtMessageIndex.Value];
                if (destroying.SourceId != participant.Id && destroying.TargetId != participant.Id)
                    throw new InvalidOperationException("A destroying sequence message must involve its participant.");
            }
        }
        for (var index = 0; index < model.Messages.Count; index++) {
            var message = model.Messages[index];
            RequireLifetime(message.SourceId, index, false); RequireLifetime(message.TargetId, index, false);
        }
        foreach (var note in model.Notes) foreach (var id in note.ParticipantIds) RequireLifetime(id, Math.Max(0, note.StepIndex), true);
        foreach (var activation in model.Activations) {
            RequireLifetime(activation.ParticipantId, Math.Max(0, activation.StepIndex), true);
            if (activation.Active && model.Layout?.MirrorParticipants == true && participants.TryGetValue(activation.ParticipantId, out var participant)
                && participant.DestroyedAtMessageIndex == Math.Max(0, activation.StepIndex))
                throw new NotSupportedException("A mirrored terminal participant cannot start an activation at the destroying message.");
        }
        void RequireLifetime(string id, int index, bool boundary) {
            if (!participants.TryGetValue(id, out var participant)) return; // The shared sequence validator reports unknown ids.
            if (participant.CreatedAtMessageIndex > index || boundary && participant.CreatedAtMessageIndex == index || participant.DestroyedAtMessageIndex < index)
                throw new InvalidOperationException("Sequence reference is outside its participant's lifetime: " + id);
        }
        void Check(int? index) {
            if (index.HasValue && (index.Value < 0 || index.Value >= model.Messages.Count))
                throw new InvalidOperationException("Sequence lifetime indices must refer to existing messages.");
        }
    }
}

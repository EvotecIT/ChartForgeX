using System;
using System.Globalization;
using ChartForgeX.Primitives;
using ChartForgeX.SvgRaster;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

public static partial class MermaidSequenceRendering {
    private static void AddParticipantLifetimesAndBoxes(MermaidSequenceDocument document, SequenceArtifact sequence) {
        foreach (var lifetime in document.Lifetimes) {
            var participant = FindParticipant(sequence, lifetime.ParticipantId)!;
            if (lifetime.Creates) participant.CreatedAtMessageIndex = lifetime.MessageIndex;
            else participant.DestroyedAtMessageIndex = lifetime.MessageIndex;
            var prefix = lifetime.Creates ? "mermaid.create." : "mermaid.destroy.";
            participant.Metadata[prefix + "source.line"] = lifetime.Span.Line.ToString(CultureInfo.InvariantCulture);
            participant.Metadata[prefix + "source.column"] = lifetime.Span.Column.ToString(CultureInfo.InvariantCulture);
        }
        for (var i = 0; i < document.ParticipantBoxes.Count; i++) {
            var box = document.ParticipantBoxes[i];
            if (box.ParticipantIds.Count == 0) continue;
            var first = document.Participants.FindIndex(participant => participant.Id == box.ParticipantIds[0]);
            ChartColor? fill = null;
            if (string.Equals(box.Color, "transparent", StringComparison.OrdinalIgnoreCase)) fill = ChartColor.Transparent;
            else if (SvgRasterColor.TryParse(box.Color, out var color)) fill = color;
            sequence.AddParticipantGroup("participant-box-" + (i + 1).ToString(CultureInfo.InvariantCulture), MermaidLabelText.DisplayCaption(box.Label ?? string.Empty),
                first, first + box.ParticipantIds.Count - 1, fill);
            var group = sequence.ParticipantGroups[sequence.ParticipantGroups.Count - 1];
            group.Metadata["mermaid.source.line"] = box.Span.Line.ToString(CultureInfo.InvariantCulture);
            group.Metadata["mermaid.source.column"] = box.Span.Column.ToString(CultureInfo.InvariantCulture);
            if (box.Color != null) group.Metadata["mermaid.color"] = box.Color;
            if (box.Label != null && group.Label != box.Label) group.Metadata["mermaid.label"] = box.Label ?? string.Empty;
        }
    }
}

using System;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualArtifacts;

internal sealed partial class SequencePreparedLayout {
    private double MeasureParticipantGroups(SequenceArtifact model, VisualArtifactInterchangeEnvelope semantics,
        VisualSceneBuilder builder, double laneWidth, double left, double top, double size) {
        double height = 0;
        for (var index = 0; index < model.ParticipantGroups.Count; index++) {
            var group = model.ParticipantGroups[index];
            double x = left + group.FirstParticipantIndex * laneWidth + 2;
            double width = (group.LastParticipantIndex - group.FirstParticipantIndex + 1) * laneWidth - 4;
            var text = Wrap(group.Label, width - 16, builder, size);
            double header = text.Length == 0 ? 8 : builder.MeasureText(text, size).Height + 16;
            height = Math.Max(height, header);
            ParticipantGroups.Add(new Item { Id = semantics.Groups[index].Id, Index = index, Text = text,
                Bounds = new ChartRect(x, top, width, 0), Y = header });
        }
        return height;
    }
}

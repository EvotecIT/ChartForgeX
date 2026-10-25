using System;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualArtifacts;

internal sealed partial class SequencePreparedLayout {
    private double MeasureParticipantGroups(SequenceArtifact model, VisualArtifactInterchangeEnvelope semantics,
        VisualSceneBuilder builder, double laneWidth, double left, double top, double size) {
        double height = 0;var padding=Presentation.Layout?.FragmentLabelPadding;
        for (var index = 0; index < model.ParticipantGroups.Count; index++) {
            var group = model.ParticipantGroups[index];
            var visible = Presentation.VisibleParticipantIndices;
            var members = visible.Select((sourceIndex, column) => new { sourceIndex, column })
                .Where(item => item.sourceIndex >= group.FirstParticipantIndex && item.sourceIndex <= group.LastParticipantIndex).ToArray();
            if (members.Length == 0) continue;
            double x = left + members[0].column * laneWidth + 2;
            double width = (members[members.Length - 1].column - members[0].column + 1) * laneWidth - 4;
            var text = Presentation.Wrap(group.Label, width - (padding.HasValue?padding.Value*2:16), builder, SequenceTextRole.Fragment, size);
            double header = text.Length == 0 ? 8 : Presentation.Measure(text, builder, SequenceTextRole.Fragment, size).Height + (padding.HasValue?padding.Value*2:16);
            height = Math.Max(height, header);
            ParticipantGroups.Add(new Item { Id = semantics.Groups[index].Id, Index = index, Text = text,
                Bounds = new ChartRect(x, top, width, 0), Y = header });
        }
        return height;
    }
}

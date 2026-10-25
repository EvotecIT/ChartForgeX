using System.Collections.Generic;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;

namespace ChartForgeX.VisualArtifacts;

internal static partial class SequencePreparedCompiler {
    private static void PaintParticipantFooters(VisualSceneBuilder builder, SequenceArtifact model, SequencePreparedLayout layout,
        VisualThemeColors colors, VisualTheme theme, SequenceArtifactPaint paint) {
        foreach (var item in layout.ParticipantFooters) {
            var source = model.Participants[item.Index];
            // No second DOM ID or portable participant is allocated for the same source entity.
            using (builder.PushGroup(null, "sequence-participant-footer", new Dictionary<string,string> {
                ["data-source-id"] = item.Id, ["data-label"] = source.Label }))
                Participant(builder, source, item, layout, colors, theme, paint);
            Region(builder, item, "sequence-participant-footer", source.Label);
        }
    }
}

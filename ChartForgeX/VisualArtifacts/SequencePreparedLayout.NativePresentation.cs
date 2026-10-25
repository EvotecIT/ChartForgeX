using System;
using System.Linq;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualArtifacts;

internal sealed partial class SequencePreparedLayout {
    internal static bool IsInstant(SequenceArtifactBlockKind kind) => kind is SequenceArtifactBlockKind.Reference or SequenceArtifactBlockKind.Divider;
    internal static string ParticipantCaption(SequenceArtifactParticipant participant) =>
        string.IsNullOrEmpty(participant.Symbol) ? participant.Label : participant.Symbol + " " + participant.Label;
    internal static double MinimumParticipantLane(SequenceArtifact model, double ordinary) =>
        model.Participants.Any(item => item.MinimumWidth.HasValue)
            ? Math.Max(ordinary, model.Participants.Max(item => (item.MinimumWidth ?? 0) + 12)) : ordinary;
    internal static double ReferenceTabWidth(VisualSceneBuilder builder,double size,double width,SequencePreparedPresentation? presentation=null) {
        var padding=presentation?.Layout?.FragmentLabelPadding;
        var tab=Math.Max(40,(presentation?.Measure("ref",builder,SequenceTextRole.Fragment,size)??builder.MeasureText("ref",size)).Width+(padding.HasValue?padding.Value*2:16));
        if(tab+(padding.HasValue?padding.Value*2+16:32)>width)throw new NotSupportedException("A reference span requires a wider native allocation for its measured label.");
        return tab;
    }
}

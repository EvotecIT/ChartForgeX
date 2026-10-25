using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
namespace ChartForgeX.VisualArtifacts;
internal static partial class SequencePreparedCompiler {
    private static void BandedParticipant(VisualSceneBuilder builder,SequenceArtifactParticipant participant,SequencePreparedLayout.Item item,SequencePreparedLayout layout,VisualThemeColors colors,VisualTheme theme,SequenceArtifactPaint paint) {
        var plan=item.BandAllocation!;var b=item.Bounds;var fill=participant.Fill??paint.ParticipantFill??colors.Surface;var border=participant.Border??paint.ParticipantBorder??(participant.Kind==SequenceArtifactParticipantKind.Participant?colors.Border:colors.Accent);var stroke=theme.AxisStrokeWidth;
        using(builder.PushGroup(null,"sequence-participant-glyph",new Dictionary<string,string>{{"data-source-id",item.Id}})) {
            using var shadow = Surface(builder, participant.SurfaceShadow, plan.Glyph, stroke);
            using var notation = plan.Box && participant.Kind != SequenceArtifactParticipantKind.Participant
                ? builder.PushGroup(null, "sequence-notation", new Dictionary<string,string> { ["data-kind"] = participant.Kind.ToString() }) : null;
            if(plan.Box) {
                if(participant.Kind==SequenceArtifactParticipantKind.Collections){for(var i=2;i>=0;i--)builder.Rect(new ChartRect(b.X+i*4,b.Y+6-i*3,b.Width-8,b.Height-6),fill,border,stroke,2);}
                else {builder.Rect(b,fill,border,stroke,theme.BarRadius,"sequence-participant-box");if(participant.Kind==SequenceArtifactParticipantKind.Queue)for(var i=0;i<3;i++)builder.Line(b.X+5+i*2,b.Y+4,b.X+5+i*2,b.Bottom-4,border,stroke);}
            } else using(builder.PushTranslation(0,plan.GlyphShift))ParticipantShape(builder,participant.Kind,b,b.X+b.Width/2,b.Y+21,fill,border,stroke,theme);
        }
        var ink=ParticipantInk(participant,paint,colors,plan.Box);
        if(plan.Stereotype is ChartRect stereo)Text(builder,plan.StereotypeText,stereo,layout,SequenceTextRole.Participant,ink,center:true,ratio:.85,nativeRole:"sequence-participant-stereotype");
        Text(builder,item.Text,plan.Label,layout,SequenceTextRole.Participant,ink,center:true,nativeRole:"sequence-participant-label");
        builder.AddRegion(new VisualSemanticRegion(item.Id,item.IsFooter?"sequence-participant-footer-glyph":"sequence-participant-glyph",plan.Glyph,participant.Kind.ToString()));
        builder.AddRegion(new VisualSemanticRegion(item.Id,item.IsFooter?"sequence-participant-footer-label":"sequence-participant-label",plan.Label,participant.Label));
    }
}

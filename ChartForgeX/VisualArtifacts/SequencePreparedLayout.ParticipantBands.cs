using System;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
namespace ChartForgeX.VisualArtifacts;
internal sealed class SequenceParticipantBandAllocation {
    internal ChartRect Glyph,Label;
    internal ChartRect? Stereotype;
    internal string StereotypeText=string.Empty;
    internal double GlyphShift;
    internal bool Box;
}
internal sealed partial class SequencePreparedLayout {
    private void CaptureParticipantBands(SequenceArtifact model,VisualSceneBuilder builder) {
        var bands=Presentation.Layout?.ParticipantBands;if(bands==null)return;
        foreach(var item in Participants.Concat(ParticipantFooters)) {
            var source=model.Participants[item.Index];var b=item.Bounds;var sides=Presentation.WrapSides;
            var textHeight=Presentation.Measure(item.Text,builder,SequenceTextRole.Participant,FontSize).Height;
            var stereotype=string.IsNullOrEmpty(source.Stereotype)?string.Empty:Presentation.Wrap("«"+source.Stereotype+"»",b.Width-sides*2,builder,SequenceTextRole.Participant,FontSize,.85);
            var stereotypeHeight=stereotype.Length==0?0:Presentation.Measure(stereotype,builder,SequenceTextRole.Participant,FontSize,.85).Height;
            var labelHeight=textHeight+(stereotypeHeight==0?0:stereotypeHeight+4);var box=source.Kind is SequenceArtifactParticipantKind.Participant or SequenceArtifactParticipantKind.Queue or SequenceArtifactParticipantKind.Collections;
            var labelTop=box?b.Y+(b.Height-labelHeight)/2:item.IsFooter?b.Y+bands.LabelLifelineGap+bands.GlyphHeight+bands.GlyphLabelGap:b.Bottom-bands.LabelLifelineGap-labelHeight;
            var allocation=new SequenceParticipantBandAllocation{Box=box,Label=new ChartRect(b.X+sides,labelTop+(stereotypeHeight==0?0:stereotypeHeight+4),b.Width-sides*2,textHeight),StereotypeText=stereotype};
            if(stereotypeHeight>0)allocation.Stereotype=new ChartRect(b.X+sides,labelTop,b.Width-sides*2,stereotypeHeight);
            if(box)allocation.Glyph=b;
            else {
                var native=ParticipantGlyphFootprint(source.Kind);if(native.Height>bands.GlyphHeight)throw new NotSupportedException("The authored participant glyph band is smaller than this native glyph.");
                var bottom=item.IsFooter?b.Y+bands.LabelLifelineGap+bands.GlyphHeight:labelTop-bands.GlyphLabelGap;
                allocation.GlyphShift=bottom-(b.Y+21+native.Bottom);
                allocation.Glyph=new ChartRect(b.X+b.Width/2+native.X,bottom-native.Height,native.Width,native.Height);
            }
            VisualDiagramPrimitives.RequireInside(allocation.Glyph,b,item.Id);VisualDiagramPrimitives.RequireInside(allocation.Label,b,item.Id);if(allocation.Stereotype is ChartRect plate)VisualDiagramPrimitives.RequireInside(plate,b,item.Id);item.BandAllocation=allocation;
        }
    }
    // Exact incumbent native glyph extrema relative to its shared x / y=b.Y+21 origin, not copied source SVG.
    internal static ChartRect ParticipantGlyphFootprint(SequenceArtifactParticipantKind kind)=>kind switch {
        SequenceArtifactParticipantKind.Actor=>new ChartRect(-12,-17,24,37),
        SequenceArtifactParticipantKind.Boundary=>new ChartRect(-20,-12,37,28),
        SequenceArtifactParticipantKind.Control=>new ChartRect(-13,-16,26,31),
        SequenceArtifactParticipantKind.Entity=>new ChartRect(-16,-13,32,30),
        SequenceArtifactParticipantKind.Database=>new ChartRect(-17,-14,34,33),
        SequenceArtifactParticipantKind.Collections=>new ChartRect(-18,-19,38,32),
        SequenceArtifactParticipantKind.Queue=>new ChartRect(-22,-10,44,27),
        _=>throw new NotSupportedException("This participant kind does not have a separate native glyph band.")};
}

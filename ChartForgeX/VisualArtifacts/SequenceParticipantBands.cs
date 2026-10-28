using System;
namespace ChartForgeX.VisualArtifacts;
/// <summary>Immutable native glyph/label/lifeline bands for one measured participant row.</summary>
/// <remarks>Null on the layout retains ordinary native anatomy. Glyphs keep their existing native shapes and must fit the selected band; labels use the shared Participant typography. Mirrored icon labels grow downward from the footer datum.</remarks>
public sealed class SequenceParticipantBands {
    /// <summary>Creates a positive glyph band with nonnegative glyph-label and label-lifeline clearances, in logical units.</summary>
    public SequenceParticipantBands(double glyphHeight=44,double glyphLabelGap=6,double labelLifelineGap=6) {
        GlyphHeight=Check(glyphHeight,nameof(glyphHeight),true);GlyphLabelGap=Check(glyphLabelGap,nameof(glyphLabelGap),false);LabelLifelineGap=Check(labelLifelineGap,nameof(labelLifelineGap),false);
    }
    /// <summary>Gets the reserved glyph band height.</summary>
    public double GlyphHeight {get;}
    /// <summary>Gets the gap between glyph and measured label.</summary>
    public double GlyphLabelGap {get;}
    /// <summary>Gets the gap separating the label/glyph stack from the lifeline datum.</summary>
    public double LabelLifelineGap {get;}
    internal double StackHeight(double labelHeight)=>GlyphHeight+GlyphLabelGap+labelHeight+LabelLifelineGap;
    private static double Check(double value,string name,bool positive){if(double.IsNaN(value)||double.IsInfinity(value)||(positive?value<=0:value<0))throw new ArgumentOutOfRangeException(name);return value;}
}

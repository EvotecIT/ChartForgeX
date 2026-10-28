using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
namespace ChartForgeX.VisualArtifacts;
internal static partial class SequencePreparedCompiler {
    // The incumbent native reference tab and authored fragment tabs share this exact generic contour.
    private static ChartPath SequenceTabPath(ChartRect b)=>new(new[]{ChartPathCommand.MoveTo(b.X,b.Y),ChartPathCommand.LineTo(b.Right,b.Y),ChartPathCommand.LineTo(b.Right,b.Bottom-7),ChartPathCommand.LineTo(b.Right-8,b.Bottom),ChartPathCommand.LineTo(b.X,b.Bottom)});
    private static void FragmentHeader(VisualSceneBuilder builder,SequencePreparedLayout.Item item,SequencePreparedLayout layout,ChartRect tab,VisualThemeColors colors,SequenceArtifactPaint paint,double stroke) {
        var pad=layout.Presentation.Layout?.FragmentLabelPadding??6;
        using (Surface(builder, paint.FragmentTabShadow, tab, stroke)) {
            builder.Path(SequenceTabPath(tab), paint.FragmentFill ?? colors.Surface,
                paint.FragmentBorder ?? colors.Border, stroke, "sequence-fragment-tab", close: true);
        }
        Text(builder,item.FragmentKindText,new ChartRect(tab.X+pad,tab.Y+pad,tab.Width-pad*2,tab.Height-pad*2),layout,SequenceTextRole.Fragment,paint.FragmentText??colors.Foreground,center:true,nativeRole:"sequence-fragment-kind");
        builder.AddRegion(new VisualSemanticRegion(item.Id,"sequence-fragment-tab",tab,item.FragmentKindText));
        if(item.FragmentTitleBounds is ChartRect title&&item.Text.Length>0){Text(builder,item.Text,title,layout,SequenceTextRole.Fragment,paint.FragmentTitleText??paint.FragmentText??colors.Foreground,center:true,nativeRole:"sequence-fragment-title");builder.AddRegion(new VisualSemanticRegion(item.Id,"sequence-fragment-title",title,item.Text));}
    }
}

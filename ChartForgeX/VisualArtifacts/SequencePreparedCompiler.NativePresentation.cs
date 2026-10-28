using System;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;

namespace ChartForgeX.VisualArtifacts;

internal static partial class SequencePreparedCompiler {
    private static ChartColor ParticipantInk(SequenceArtifactParticipant participant, SequenceArtifactPaint paint,
        VisualThemeColors colors, bool enclosedCaption) =>
        paint.ParticipantText ?? (enclosedCaption && (participant.Fill ?? paint.ParticipantFill) is ChartColor fill
            ? ChartColorMath.AccessibleTextOnBackground(ChartColorMath.CompositeOverOpaque(fill,colors.Background)) : colors.Foreground);
    private static void InstantFragment(VisualSceneBuilder builder,SequenceArtifactBlock block,SequencePreparedLayout.Item item,
        SequencePreparedLayout layout,VisualThemeColors colors,VisualTheme theme,SequenceArtifactPaint paint) {
        var size=layout.FontSize;var padding=layout.Presentation.Layout?.FragmentLabelPadding;
        var b=item.Bounds; var border=paint.FragmentBorder??colors.Border; var fill=block.BackgroundColor??paint.FragmentFill??colors.Surface;
        using (Group(builder,item,block.Kind==SequenceArtifactBlockKind.Reference?"sequence-reference":"sequence-divider",block.Text)) {
            if(block.Kind==SequenceArtifactBlockKind.Reference) {
                builder.Rect(b,fill,border,theme.AxisStrokeWidth,role:"sequence-reference-frame");
                var tab=SequencePreparedLayout.ReferenceTabWidth(builder,size,b.Width,layout.Presentation);
                var tabHeight=layout.Presentation.Measure("ref",builder,SequenceTextRole.Fragment,size).Height+(padding.HasValue?padding.Value*2:10);
                var tabBounds = new ChartRect(b.X, b.Y, tab, tabHeight);
                using (Surface(builder, paint.FragmentTabShadow, tabBounds, theme.AxisStrokeWidth)) {
                    builder.Path(SequenceTabPath(tabBounds), fill, border,
                        theme.AxisStrokeWidth, "sequence-reference-tab", close: true);
                }
                Text(builder,"ref",new ChartRect(b.X+(padding??6),b.Y+(padding??5),tab-(padding.HasValue?padding.Value*2:12),tabHeight-(padding.HasValue?padding.Value*2:10)),layout,SequenceTextRole.Fragment,paint.FragmentText??colors.Foreground);
                Text(builder,item.Text,new ChartRect(b.X+tab+(padding??8),b.Y+(padding??10),Math.Max(1,b.Width-tab-(padding.HasValue?padding.Value*2:16)),b.Height-(padding.HasValue?padding.Value*2:20)),layout,SequenceTextRole.Fragment,paint.FragmentText??colors.Foreground,center:true);
            } else {
                var label=layout.Presentation.Measure(item.Text,builder,SequenceTextRole.Fragment,size); var width=Math.Min(b.Width,Math.Max(24,label.Width+(padding.HasValue?padding.Value*2:20)));
                var left=b.X+(b.Width-width)/2; var y=b.Y+b.Height/2;
                builder.Line(b.X,y,left,y,border,theme.AxisStrokeWidth,"sequence-divider-line");
                builder.Line(left+width,y,b.Right,y,border,theme.AxisStrokeWidth,"sequence-divider-line");
                builder.Rect(new ChartRect(left,b.Y,width,b.Height),fill,border,theme.AxisStrokeWidth,2,"sequence-divider-label-surface");
                Text(builder,item.Text,new ChartRect(left+(padding??8),b.Y+(padding??10),width-(padding.HasValue?padding.Value*2:16),b.Height-(padding.HasValue?padding.Value*2:20)),layout,SequenceTextRole.Fragment,paint.FragmentText??colors.Foreground,center:true);
            }
        }
    }
}

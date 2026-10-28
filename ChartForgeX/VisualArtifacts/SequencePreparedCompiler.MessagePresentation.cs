using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualArtifacts;

internal static partial class SequencePreparedCompiler {
    private static void PaintNumberBadge(VisualSceneBuilder builder,SequencePreparedLayout layout,SequencePreparedLayout.Item item,SequenceArtifactPaint paint,VisualThemeColors colors) {
        var badge=item.NumberBadgeBounds!.Value;var text=item.NumberTextBounds!.Value;
        var fill=paint.NumberFill??paint.MessageLine??colors.Accent;
        var ink=paint.NumberText??ChartColorMath.AccessibleTextOnBackground(ChartColorMath.CompositeOverOpaque(fill,colors.Surface));
        var style=layout.Presentation.BadgeStyle(item.NumberText,ink);style.Alignment=TextAlignment.Center;
        builder.Ellipse(badge.X+badge.Width/2,badge.Y+badge.Height/2,badge.Width/2,badge.Height/2,fill,role:"sequence-number-badge");
        builder.Text(item.NumberText,text.X+text.Width/2,text.Y+builder.TextAscent(style)-VisualSceneTextFace.BaselineOffset(style),style,role:"sequence-number");
        builder.AddRegion(new VisualSemanticRegion(item.Id,"sequence-number-badge",badge,item.NumberText));
        builder.AddRegion(new VisualSemanticRegion(item.Id,"sequence-number",text,item.NumberText));
    }
}

using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
namespace ChartForgeX.VisualArtifacts;
internal static partial class SequencePreparedCompiler {
    private static void DrawLifeline(VisualSceneBuilder builder,SequencePreparedLayout.Item item,double x,double bottom,SequenceArtifactPaint paint,VisualThemeColors colors,double stroke) {
        var style=paint.LifelineStroke;
        if(style==null){builder.Line(x,item.Bounds.Bottom,x,bottom,paint.Lifeline??colors.Border,stroke,"sequence-lifeline",dash:new[]{6d,5d});return;}
        if(style.Color==null&&paint.Lifeline.HasValue)style=new ChartStrokeStyle(paint.Lifeline,style.Width,style.Opacity,style.Dash);
        var binding=VisualReferenceStroke.Paint(style,colors.Border,SvgColorRole.Axis,out var color);var width=style.Width??stroke;
        using(builder.PushGroup(null,"sequence-lifeline-source",new Dictionary<string,string>{{"data-source-id",item.Id}}))
            builder.Line(x,item.Bounds.Bottom,x,bottom,color,width,"sequence-lifeline",dash:style.ResolveDash(new[]{6d,5d}),paint:binding);
        var extent=VisualReferenceStroke.PaintedWidth(style,colors.Border,stroke);
        builder.AddRegion(new VisualSemanticRegion(item.Id,"sequence-lifeline",new ChartRect(x-extent/2,item.Bounds.Bottom,extent,System.Math.Max(0,bottom-item.Bounds.Bottom)),item.Id));
    }
}

using System;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualArtifacts;

internal sealed partial class SequencePreparedLayout {
    private void PlaceMessagePresentation(SequenceArtifact model, VisualSceneBuilder builder, ChartRect plot) {
        foreach (var item in Messages) {
            if (item.NumberBadgeRadius <= 0) continue;
            var message = model.Messages[item.Index];var self=message.SourceId==message.TargetId;
            var reverse = message.Metadata.TryGetValue("mermaid.operator",out var notation) && SequenceHalfArrowNotation.TryRead(notation,out _,out _,out var reversed) && reversed;
            var target = reverse && !self;var origin=target?item.X2:item.X1;var toward=target?item.X1:item.X2;
            var direction=self||toward>=origin?1:-1;var radius=item.NumberBadgeRadius;var clearance=Presentation.BadgeClearance;
            var center=origin+direction*(radius+clearance);var boundary=origin+direction*(radius*2+clearance*2);
            if(target)item.X2=boundary;else item.X1=boundary;
            if(self)item.PresentedLoopX=Math.Max(item.X1,item.X2)+56;
            var badge=new ChartRect(center-radius,item.Y-radius,radius*2,radius*2);
            var style=Presentation.BadgeStyle(item.NumberText,ChartColor.Black);var metrics=builder.MeasureText(item.NumberText,style);
            item.NumberBadgeBounds=badge;item.NumberTextBounds=new ChartRect(center-metrics.Width/2,item.Y-metrics.Height/2+VisualSceneTextFace.BaselineOffset(style),metrics.Width,metrics.Height);
            if(!self && Math.Abs(item.X2-item.X1)<Presentation.WrapSides*2+item.LabelBounds.Width)
                throw new NotSupportedException("The mapped native message span cannot contain its badge and complete caption.");
            VisualDiagramPrimitives.RequireInside(badge,plot,item.Id);
        }
    }
    private double AlignedMessageLeft(Item item, double ordinary, bool self) {
        var alignment=Presentation.Layout?.MessageAlignment;if(!alignment.HasValue)return ordinary;
        var left=self?ordinary:Math.Min(item.X1,item.X2)+Presentation.WrapSides;
        var right=self?Math.Max(item.Bounds.Right,left+item.LabelBounds.Width):Math.Max(item.X1,item.X2)-Presentation.WrapSides;
        if(right-left+1e-8<item.LabelBounds.Width)throw new NotSupportedException("The mapped native message allocation cannot contain its aligned complete caption.");
        return alignment.Value==TextAlignment.Left?left:alignment.Value==TextAlignment.Right?right-item.LabelBounds.Width:(left+right-item.LabelBounds.Width)/2;
    }
    private void CaptureMessageFootprint(Item item) {
        var left=Math.Min(item.Bounds.Left,item.LabelBounds.Left);var right=Math.Max(item.Bounds.Right,item.LabelBounds.Right);
        var top=Math.Min(item.Bounds.Top,item.LabelBounds.Top);var bottom=Math.Max(item.Bounds.Bottom,item.LabelBounds.Bottom);
        if(item.PresentedLoopX.HasValue)right=Math.Max(right,item.PresentedLoopX.Value);
        if(item.NumberBadgeBounds is ChartRect badge){left=Math.Min(left,badge.Left);right=Math.Max(right,badge.Right);top=Math.Min(top,badge.Top);bottom=Math.Max(bottom,badge.Bottom);}
        item.Bounds=new ChartRect(left,top,right-left,bottom-top);
    }
}

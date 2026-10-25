using System;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualArtifacts;

internal sealed partial class SequencePreparedLayout {
    private void CaptureSelfRoutes(ChartRect plot) {
        foreach (var item in Messages) {
            if (item.SelfRoute == null) continue;
            var route=item.SelfRoute.Capture(item.X1,item.X2,item.Y,item.PresentedLoopX);item.CapturedRoute=route;
            if (item.SelfRoute.Routing.HasValue) {
                var b=item.Bounds;var p=route.Bounds;
                var left=Math.Min(b.Left,p.Left);var top=Math.Min(b.Top,p.Top);var right=Math.Max(b.Right,p.Right);var bottom=Math.Max(b.Bottom,p.Bottom);
                item.Bounds=new ChartRect(left,top,right-left,bottom-top);
                VisualDiagramPrimitives.RequireInside(p,plot,item.Id);
            }
        }
    }
}

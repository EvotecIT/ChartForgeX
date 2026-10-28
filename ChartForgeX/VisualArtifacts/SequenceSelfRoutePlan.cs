using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualArtifacts;

/// <summary>One captured self-route profile. ChartPath alone owns cubic extrema, samples and actual native path geometry.</summary>
internal sealed class SequenceSelfRoutePlan {
    // Normalized vertical extrema are reusable linear scaling facts, measured once by the incumbent path owner.
    private static readonly ChartRect UnitCurveBounds = new ChartPath(new[] { ChartPathCommand.MoveTo(0,0), ChartPathCommand.CubicTo(60,-.5,60,1.5,0,1) }).Bounds();
    internal SequenceSelfMessageRouting? Routing { get; }
    internal bool IsCurved => Routing == SequenceSelfMessageRouting.Curved;
    internal double Drop { get; }
    internal double Reach { get; }
    internal double MinimumY => IsCurved ? UnitCurveBounds.Top * Drop : 0;
    internal double MaximumY => IsCurved ? UnitCurveBounds.Bottom * Drop : Drop;
    private SequenceSelfRoutePlan(SequenceSelfMessageRouting? routing,double drop,double reach) {Routing=routing;Drop=drop;Reach=reach;}
    internal static double ReturnDrop(SequenceLayoutPolicy? policy,double? badgeDrop) {
        var routing=policy?.SelfMessageRouting;var ordinary=routing==SequenceSelfMessageRouting.Curved?20:routing==SequenceSelfMessageRouting.Orthogonal?25:24;
        return Math.Max(ordinary,badgeDrop??ordinary);
    }
    internal static SequenceSelfRoutePlan Create(SequenceLayoutPolicy? policy,double headerWidth,double captionWidth,double? badgeDrop) {
        var routing=policy?.SelfMessageRouting;var drop=ReturnDrop(policy,badgeDrop);
        return new SequenceSelfRoutePlan(routing,drop,routing==SequenceSelfMessageRouting.Curved?60:routing==SequenceSelfMessageRouting.Orthogonal?Math.Max(headerWidth/2,captionWidth/2):56);
    }
    internal CapturedSequenceRoute Capture(double startX,double endX,double y,double? legacyLoopX) {
        var start=new ChartPoint(startX,y);var end=new ChartPoint(endX,y+Drop);
        ChartPath path;ChartPoint[] terminals;ChartPoint center;
        if(IsCurved) {
            var first=new ChartPoint(startX+60,y-Drop/2);var second=new ChartPoint(endX+60,y+Drop*1.5);
            path=new ChartPath(new[]{ChartPathCommand.MoveTo(start.X,start.Y),ChartPathCommand.CubicTo(first.X,first.Y,second.X,second.Y,end.X,end.Y)});
            terminals=new[]{start,first,second,end};center=ChartPath.CubicPoint(start,first,second,end,.5);
        } else {
            var reach=Routing.HasValue?Math.Max(startX,endX)+Reach:legacyLoopX??Math.Max(startX,endX)+56;
            terminals=new[]{start,new ChartPoint(reach,y),new ChartPoint(reach,end.Y),end};
            path=new ChartPath(terminals.Select((point,index)=>index==0?ChartPathCommand.MoveTo(point.X,point.Y):ChartPathCommand.LineTo(point.X,point.Y)).ToArray());
            center=new ChartPoint(reach,(y+end.Y)/2);
        }
        var bounds=path.Bounds();var observed=path.Flatten(maximumPoints:VisualArtifactInterchangeValidation.MaximumWaypointsPerEdge);
        return new CapturedSequenceRoute(path,bounds,terminals,observed,center,IsCurved);
    }
}

internal sealed class CapturedSequenceRoute {
    internal CapturedSequenceRoute(ChartPath path,ChartRect bounds,ChartPoint[] terminals,IReadOnlyList<ChartPoint> observed,ChartPoint center,bool curved) {
        Path=path;Bounds=bounds;Terminals=terminals;Observed=observed;Center=center;Curved=curved;
    }
    internal ChartPath Path { get; }
    internal ChartRect Bounds { get; }
    internal ChartPoint[] Terminals { get; }
    internal IReadOnlyList<ChartPoint> Observed { get; }
    internal ChartPoint Center { get; }
    internal bool Curved { get; }
}

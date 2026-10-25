using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Topology;

/// <summary>Produces one bounded numeric path for native paint, tangent, hit, label and portable consumers.</summary>
internal static partial class TopologyInterpolationGeometry {
    internal static ChartPath Build(IReadOnlyList<ChartPoint> points,TopologyEdgeInterpolation kind, double? cornerBlendLength = null, double defaultUnits = 1) {
        TopologyModelGuards.EnumDefined(kind,nameof(kind));
        TopologyModelGuards.PositiveFinite(defaultUnits, nameof(defaultUnits));
        if(points==null)throw new ArgumentNullException(nameof(points));
        if(points.Count<2||points.Count>10000)throw new ArgumentException("Interpolation requires 2 through 10000 resolved route vertices.",nameof(points));
        foreach(var p in points)if(!Finite(p.X)||!Finite(p.Y)||Math.Abs(p.X)>1000000000||Math.Abs(p.Y)>1000000000)
            throw new ArgumentOutOfRangeException(nameof(points),"Interpolated coordinates must be finite and within one billion units.");
        for (var index = 1; index < points.Count; index++) if (points[index].BreakBefore)
            throw new NotSupportedException("Native interpolation requires one continuous resolved route; disconnected geographic segments cannot be joined implicitly.");
        points = BlendCorners(points, cornerBlendLength);
        var commands=new List<ChartPathCommand>{ChartPathCommand.MoveTo(points[0].X,points[0].Y)};
        switch(kind) {
            case TopologyEdgeInterpolation.Basis:Basis(points,commands);break;
            case TopologyEdgeInterpolation.Cardinal:Cardinal(points,commands);break;
            case TopologyEdgeInterpolation.CatmullRom:CatmullRom(points,commands);break;
            case TopologyEdgeInterpolation.MonotoneX:Monotone(points,commands,false);break;
            case TopologyEdgeInterpolation.MonotoneY:Monotone(points,commands,true);break;
            case TopologyEdgeInterpolation.Natural:Natural(points,commands);break;
            case TopologyEdgeInterpolation.Rounded:Rounded(points,commands,defaultUnits);break;
            default:
                for(var i=1;i<points.Count;i++) {
                    var a=points[i-1];var b=points[i];
                    if(kind==TopologyEdgeInterpolation.BumpX)Cubic(commands,new ChartPoint((a.X+b.X)/2,a.Y),new ChartPoint((a.X+b.X)/2,b.Y),b);
                    else if(kind==TopologyEdgeInterpolation.BumpY)Cubic(commands,new ChartPoint(a.X,(a.Y+b.Y)/2),new ChartPoint(b.X,(a.Y+b.Y)/2),b);
                    else {
                        if(kind==TopologyEdgeInterpolation.Step){Line(commands,new ChartPoint((a.X+b.X)/2,a.Y));Line(commands,new ChartPoint((a.X+b.X)/2,b.Y));continue;}
                        else if(kind==TopologyEdgeInterpolation.StepAfter)Line(commands,new ChartPoint(b.X,a.Y));
                        else if(kind==TopologyEdgeInterpolation.StepBefore)Line(commands,new ChartPoint(a.X,b.Y));
                        Line(commands,b);
                    }
                }
                if(kind==TopologyEdgeInterpolation.Step)Line(commands,points[points.Count-1]);
                break;
        }
        if(commands.Count>TopologyPathDecoration.MaximumCommands)throw new ArgumentException("An interpolated route exceeds 4096 path commands.");
        foreach(var c in commands)if(!Coordinate(c.X)||!Coordinate(c.Y)||!Coordinate(c.Control1X)||!Coordinate(c.Control1Y)||!Coordinate(c.Control2X)||!Coordinate(c.Control2Y))
            throw new ArgumentOutOfRangeException(nameof(points),"Interpolated controls must be finite and within one billion units.");
        return new ChartPath(commands);
    }
    private static bool Finite(double value)=>!double.IsNaN(value)&&!double.IsInfinity(value);
    private static bool Coordinate(double value)=>Finite(value)&&Math.Abs(value)<=1000000000;
    private static void Line(List<ChartPathCommand> commands,ChartPoint point) {
        CommandCapacity(commands); commands.Add(ChartPathCommand.LineTo(point.X,point.Y));
    }
    private static void Cubic(List<ChartPathCommand> commands,ChartPoint first,ChartPoint second,ChartPoint end) {
        CommandCapacity(commands); commands.Add(ChartPathCommand.CubicTo(first.X,first.Y,second.X,second.Y,end.X,end.Y));
    }
    private static void CommandCapacity(List<ChartPathCommand> commands) {
        if (commands.Count >= TopologyPathDecoration.MaximumCommands)
            throw new ArgumentException("An interpolated route exceeds 4096 path commands.");
    }
    private static ChartPoint Mix(ChartPoint a,ChartPoint b,double t)=>new(a.X+(b.X-a.X)*t,a.Y+(b.Y-a.Y)*t);
    private static double Distance(ChartPoint a,ChartPoint b)=>Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Y-b.Y)*(a.Y-b.Y));
}

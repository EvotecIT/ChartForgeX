using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Topology;

internal static partial class TopologyInterpolationGeometry {
    private static void Monotone(IReadOnlyList<ChartPoint> source,List<ChartPathCommand> output,bool transpose) {
        var p=new List<ChartPoint>(source.Count);
        foreach(var raw in source) {
            var point=transpose?new ChartPoint(raw.Y,raw.X):raw;
            if(p.Count==0||point.X!=p[p.Count-1].X||point.Y!=p[p.Count-1].Y)p.Add(point);
        }
        if(p.Count<2){Line(output,source[source.Count-1]);return;}
        if(p.Count==2){Line(output,source[source.Count-1]);return;}
        var slopes=new double[p.Count];
        for(var i=1;i<p.Count-1;i++) {
            var left=p[i].X-p[i-1].X;var right=p[i+1].X-p[i].X;
            var before=(p[i].Y-p[i-1].Y)/SignedZero(left,right);
            var after=(p[i+1].Y-p[i].Y)/SignedZero(right,left);
            var weighted=(before*right+after*left)/(left+right);
            var slope=(Sign(before)+Sign(after))*Math.Min(Math.Min(Math.Abs(before),Math.Abs(after)),Math.Abs(weighted)/2);
            slopes[i]=Finite(slope)?slope:0;
        }
        slopes[0]=Endpoint(p[0],p[1],slopes[1]);slopes[p.Count-1]=Endpoint(p[p.Count-2],p[p.Count-1],slopes[p.Count-2]);
        for(var i=0;i<p.Count-1;i++) {
            var a=p[i];var b=p[i+1];var third=(b.X-a.X)/3;
            var c1=new ChartPoint(a.X+third,a.Y+third*slopes[i]);var c2=new ChartPoint(b.X-third,b.Y-third*slopes[i+1]);
            Cubic(output,transpose?new ChartPoint(c1.Y,c1.X):c1,transpose?new ChartPoint(c2.Y,c2.X):c2,transpose?new ChartPoint(b.Y,b.X):b);
        }
    }
    private static int Sign(double value)=>value<0?-1:1;
    private static double SignedZero(double value,double neighbour)=>value!=0?value:neighbour<0?BitConverter.Int64BitsToDouble(long.MinValue):0d;
    private static double Endpoint(ChartPoint a,ChartPoint b,double inner)=>b.X==a.X?inner:(3*(b.Y-a.Y)/(b.X-a.X)-inner)/2;

}

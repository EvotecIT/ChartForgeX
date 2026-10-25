using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Topology;

internal static partial class TopologyInterpolationGeometry {
    private static void Basis(IReadOnlyList<ChartPoint> p,List<ChartPathCommand> output) {
        if(p.Count==2){Line(output,p[1]);return;}
        Line(output,Mix(p[0],p[1],1d/6));
        for(var i=1;i<p.Count;i++) {
            var a=p[i-1];var b=p[i];var c=p[Math.Min(i+1,p.Count-1)];
            Cubic(output,Mix(a,b,1d/3),Mix(a,b,2d/3),new ChartPoint((a.X+4*b.X+c.X)/6,(a.Y+4*b.Y+c.Y)/6));
        }
        Line(output,p[p.Count-1]);
    }
    private static void Cardinal(IReadOnlyList<ChartPoint> p,List<ChartPathCommand> output) {
        if(p.Count==2){Line(output,p[1]);return;}
        for(var i=0;i<p.Count-1;i++) {
            var a=p[i];var b=p[i+1];
            var first=i==0?a:new ChartPoint(a.X+(b.X-p[i-1].X)/6,a.Y+(b.Y-p[i-1].Y)/6);
            var second=i+2==p.Count?b:new ChartPoint(b.X-(p[i+2].X-a.X)/6,b.Y-(p[i+2].Y-a.Y)/6);
            Cubic(output,first,second,b);
        }
    }
    private static void CatmullRom(IReadOnlyList<ChartPoint> p,List<ChartPathCommand> output) {
        if(p.Count==2){Line(output,p[1]);return;}
        for(var i=0;i<p.Count-1;i++) {
            var a=p[i];var b=p[i+1];var delta=Math.Sqrt(Distance(a,b));var first=a;var second=b;
            if(i>0) {
                var before=p[i-1];var prior=Math.Sqrt(Distance(before,a));
                if(prior>1e-6&&delta>1e-6)first=new ChartPoint(a.X+delta/3*((a.X-before.X)/prior-(b.X-before.X)/(prior+delta)+(b.X-a.X)/delta),
                    a.Y+delta/3*((a.Y-before.Y)/prior-(b.Y-before.Y)/(prior+delta)+(b.Y-a.Y)/delta));
            }
            if(i+2<p.Count) {
                var after=p[i+2];var next=Math.Sqrt(Distance(b,after));
                if(next>1e-6&&delta>1e-6)second=new ChartPoint(b.X-delta/3*((b.X-a.X)/delta-(after.X-a.X)/(delta+next)+(after.X-b.X)/next),
                    b.Y-delta/3*((b.Y-a.Y)/delta-(after.Y-a.Y)/(delta+next)+(after.Y-b.Y)/next));
            }
            Cubic(output,first,second,b);
        }
    }
    private static void Natural(IReadOnlyList<ChartPoint> points,List<ChartPathCommand> output) {
        if(points.Count==2){Line(output,points[1]);return;}
        var x=SecondDerivatives(points,true);var y=SecondDerivatives(points,false);
        for(var i=0;i<points.Count-1;i++) {
            var a=points[i];var b=points[i+1];
            Cubic(output,new ChartPoint(a.X+(b.X-a.X)/3-(2*x[i]+x[i+1])/18,a.Y+(b.Y-a.Y)/3-(2*y[i]+y[i+1])/18),
                new ChartPoint(b.X-(b.X-a.X)/3-(x[i]+2*x[i+1])/18,b.Y-(b.Y-a.Y)/3-(y[i]+2*y[i+1])/18),b);
        }
    }
    // Unit-knot natural cubic equations: M(i-1)+4M(i)+M(i+1)=6(P(i+1)-2P(i)+P(i-1)).
    private static double[] SecondDerivatives(IReadOnlyList<ChartPoint> points,bool horizontal) {
        var n=points.Count;var diagonal=new double[n];var rhs=new double[n];diagonal[0]=diagonal[n-1]=1;
        double Value(int i)=>horizontal?points[i].X:points[i].Y;
        for(var i=1;i<n-1;i++){diagonal[i]=4;rhs[i]=6*(Value(i+1)-2*Value(i)+Value(i-1));}
        for(var i=2;i<n-1;i++){var multiplier=1/diagonal[i-1];diagonal[i]-=multiplier;rhs[i]-=rhs[i-1]*multiplier;}
        var result=new double[n];for(var i=n-2;i>0;i--)result[i]=(rhs[i]-result[i+1])/diagonal[i];return result;
    }
}

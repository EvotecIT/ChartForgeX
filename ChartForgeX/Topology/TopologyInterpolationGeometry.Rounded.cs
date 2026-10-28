using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Topology;

internal static partial class TopologyInterpolationGeometry {
    // The exact quadratic corner is represented as cubic controls; it is not a circular arc approximation.
    private static void Rounded(IReadOnlyList<ChartPoint> p,List<ChartPathCommand> output,double defaultUnits) {
        for(var i=1;i<p.Count-1;i++) {
            var a=p[i-1];var b=p[i];var c=p[i+1];var incoming=Distance(a,b);var outgoing=Distance(b,c);
            if(incoming<1e-5||outgoing<1e-5){Line(output,b);continue;}
            var dot=((b.X-a.X)*(c.X-b.X)+(b.Y-a.Y)*(c.Y-b.Y))/(incoming*outgoing);
            var angle=Math.Acos(Math.Max(-1,Math.Min(1,dot)));
            if(angle<1e-5||Math.Abs(Math.PI-angle)<1e-5){Line(output,b);continue;}
            var cut=Math.Min(5*defaultUnits/Math.Sin(angle/2),Math.Min(incoming,outgoing)/2);
            var first=Mix(b,a,cut/incoming);var last=Mix(b,c,cut/outgoing);Line(output,first);
            Cubic(output,Mix(first,b,2d/3),Mix(last,b,2d/3),last);
        }
        Line(output,p[p.Count-1]);
    }
}

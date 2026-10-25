using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Topology;

/// <summary>Shares incumbent glyph surface parts with explicit glyph-slot measurement without changing native primitive kinds.</summary>
internal static class TopologyGlyphSurfaceGeometry {
    internal enum Kind {Rectangle,Ellipse,Path}
    internal readonly struct Part {
        internal Part(Kind kind,double x,double y,double width,double height,double radius=0,ChartPath? path=null) {
            Type=kind;X=x;Y=y;Width=width;Height=height;Radius=radius;Path=path;
        }
        internal Kind Type {get;}
        internal double X {get;}internal double Y {get;}internal double Width {get;}internal double Height {get;}internal double Radius {get;}
        internal ChartPath? Path {get;}
    }
    internal static IReadOnlyList<Part> Parts(TopologyNode node,TopologyIconShape shape,TopologyRenderOptions options,double x,double y,double scale,double corner,ChartPoint? canonicalAnchor=null) {
        if(shape==TopologyIconShape.Cloud)return new[]{new Part(Kind.Ellipse,-5,0,7,7),new Part(Kind.Ellipse,4,-2,8,8)};
        if(shape==TopologyIconShape.Database||(node.IconShape is null or TopologyIconShape.Auto)&&node.Kind==TopologyNodeKind.Database) {
            var parts=new List<Part>{new(Kind.Ellipse,0,-7,10,4)};
            foreach(var contour in ChartMapPathParser.ParseSubpaths(TopologyInfrastructureGlyphs.DatabaseBodyPath(canonicalAnchor?.X??x,canonicalAnchor?.Y??y),2)) {
                var commands=contour.Points.Select((p,i)=>{
                    var point=new ChartPoint(x+(p.X-(canonicalAnchor?.X??x))*scale,y+(p.Y-(canonicalAnchor?.Y??y))*scale);
                    return i==0?ChartPathCommand.MoveTo(point.X,point.Y):ChartPathCommand.LineTo(point.X,point.Y);
                }).ToArray();
                parts.Add(new Part(Kind.Path,0,0,0,0,path:new ChartPath(commands)));
            }
            return parts;
        }
        var mode=TopologyRenderPrimitives.EffectiveNodeDisplayMode(node,options);
        var size=mode==TopologyNodeDisplayMode.Pill?18:mode==TopologyNodeDisplayMode.Icon?26:mode==TopologyNodeDisplayMode.Tile?24:22;
        return new[]{new Part(Kind.Rectangle,0,0,size,size,corner)};
    }
    internal static ChartRect Bounds(IReadOnlyList<Part> parts,double x,double y,double scale,double stroke=0) {
        var left=double.PositiveInfinity;var top=double.PositiveInfinity;var right=double.NegativeInfinity;var bottom=double.NegativeInfinity;
        foreach(var part in parts) {
            var b=part.Type==Kind.Path?part.Path!.Bounds():part.Type==Kind.Ellipse
                ?new ChartRect(x+(part.X-part.Width)*scale,y+(part.Y-part.Height)*scale,part.Width*2*scale,part.Height*2*scale)
                :new ChartRect(x-part.Width*scale/2,y-part.Height*scale/2,part.Width*scale,part.Height*scale);
            var half=stroke*scale/2;left=Math.Min(left,b.Left-half);top=Math.Min(top,b.Top-half);right=Math.Max(right,b.Right+half);bottom=Math.Max(bottom,b.Bottom+half);
        }
        return double.IsInfinity(left)?new ChartRect(x,y,0,0):new ChartRect(left,top,right-left,bottom-top);
    }
}

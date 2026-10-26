using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private void ValidateSurfacePaintHost() {
        if(!_options.EnableHtmlInteractions||!_options.EnableHtmlForceGraphControls)return;
        if(_source.Nodes.Any(node=>node.SurfaceElementAppearance!=null||TopologySurfacePaintRules.HasOverrides(node.StrokeWidth,node.Opacity,node.SurfaceOpacity,node.DashPattern))
            ||_source.Groups.Any(group=>TopologySurfacePaintRules.HasOverrides(group.StrokeWidth,group.Opacity,group.SurfaceOpacity,group.DashPattern)))
            throw new NotSupportedException("Typed native surface paints cannot be rewritten by live force graph controls.");
    }
    private double SurfaceStrokeWidth(double? authored,double selectedMinimum)=>authored.HasValue
        ?Math.Max(authored.Value,selectedMinimum):selectedMinimum>0?selectedMinimum:_context.Theme.AxisStrokeWidth;
    private double[]? SurfaceDash(IReadOnlyList<double> values)=>values.Count==0?null:values.Select(value=>value*_scale).ToArray();
    private void SurfaceRectangle(ChartRect bounds,ChartColor? fill,ChartColor? stroke,double width,double radius,string role,
        double[]? dash,VisualScenePaintBinding paint,double opacity) {
        if(dash==null)_builder.Rect(bounds,fill,stroke,width,radius,role,paint:paint,opacity:opacity);
        else _builder.Path(ChartPathBuilder.RoundedRectangle(bounds,radius),fill,stroke,width,role,close:true,dash:dash,paint:paint,opacity:opacity);
    }
    private void SurfaceEllipse(double x,double y,double rx,double ry,ChartColor? fill,ChartColor? stroke,double width,string role,
        double[]? dash,VisualScenePaintBinding paint,double opacity) {
        if(dash==null)_builder.Ellipse(x,y,rx,ry,fill,stroke,width,role,paint:paint,opacity:opacity);
        else _builder.Path(ChartPathBuilder.Ellipse(x,y,rx,ry),fill,stroke,width,role,close:true,dash:dash,paint:paint,opacity:opacity);
    }

    private ChartRect IncludeSurfaceStrokeBounds(ChartRect content) {
        if(_options.IncludeGroups)foreach(var group in _chart.Groups)if(group.StrokeWidth.HasValue)Include(new ChartRect(group.X,group.Y,group.Width,group.Height),
            SurfaceStrokeWidth(group.StrokeWidth,_options.SelectedGroupIds.Contains(group.Id)?2.4:0)/2);
        foreach(var node in _chart.Nodes)if(node.StrokeWidth.HasValue) {
            var mode=TopologyRenderPrimitives.EffectiveNodeDisplayMode(node,_options);
            if(mode==TopologyNodeDisplayMode.Hidden||mode==TopologyNodeDisplayMode.Artwork&&TopologyRenderPrimitives.ResolveRenderableNodeArtwork(node,_options)!=null&&!HasArtworkSurfacePolicy(node)
                ||mode==TopologyNodeDisplayMode.Dot&&node.Shape!=TopologyNodeShape.DoubleCircle)continue;
            if(node.Shape.HasValue)foreach(var part in TopologyNodeShapeGeometry.SceneParts(node)) {if(part.Ink)Include(part.Path.Bounds(),NodeSurfaceStrokeWidth(node,part.ContourPaint)/2);}
            else Include(new ChartRect(node.X,node.Y,node.Width,node.Height),NodeSurfaceStrokeWidth(node)/2);
        }
        return content;
        void Include(ChartRect bounds,double half) {
            var left=Math.Min(content.Left,bounds.Left-half);var top=Math.Min(content.Top,bounds.Top-half);
            var right=Math.Max(content.Right,bounds.Right+half);var bottom=Math.Max(content.Bottom,bounds.Bottom+half);
            content=new ChartRect(left,top,right-left,bottom-top);
        }
    }
}

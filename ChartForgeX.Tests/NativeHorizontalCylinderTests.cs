using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeHorizontalCylinderTests {
    [Theory]
    [InlineData(TopologyEdgePort.Top)] [InlineData(TopologyEdgePort.Right)]
    [InlineData(TopologyEdgePort.Bottom)] [InlineData(TopologyEdgePort.Left)]
    public void QueueSurfaceCaptionsAndOrdinaryNamedAttachmentsUseTheSameNativeContour(TopologyEdgePort port) {
        var node=new TopologyNode {Id="queue",Label="Queue",X=20.5,Y=30.25,Width=180,Height=90,
            Shape=TopologyNodeShape.HorizontalCylinder,PreserveDisplayModeSize=true,ShowStatusBadge=false,FitLabelToContent=true};
        var parts=TopologyNodeShapeGeometry.SceneParts(node);Assert.Equal(2,parts.Count);Assert.True(parts[0].Fill);Assert.True(parts[0].Close);Assert.False(parts[1].Fill);
        var boundary=parts[0].Path.Flatten(.1);var endpoint=TopologyNodeShapeGeometry.BoundaryPoint(node,node.X+node.Width/2,node.Y+node.Height/2,port,0);
        Assert.InRange(Distance(endpoint,boundary),0,.0001);
        var route=new List<ChartPoint>{new(node.X+node.Width/2,node.Y+node.Height/2),new(node.X+node.Width+100,node.Y+node.Height/2)};
        TopologyNodeShapeGeometry.AttachRoute(node,route,0,1,0,port,.5);Assert.InRange(Distance(route[0],boundary),0,.002);
        var serialized=ChartMapPathParser.ParseSubpaths(TopologyNodeShapeGeometry.Path(node),.1);Assert.Equal(2,serialized.Count);Assert.InRange(Distance(endpoint,serialized[0].Points),0,.002);
        var caption=TopologyNodeShapeGeometry.CaptionBounds(node,30);Assert.True(caption.Left>node.X+14&&caption.Right<node.X+node.Width-14);Assert.InRange(caption.Height,0,node.Height-12);
        var chart=TopologyChart.Create().WithTitle("").WithViewport(360,220,12).WithLegend(null);chart.Nodes.Add(node);
        var prepared=chart.Prepare(new TopologyRenderOptions {IncludeLegend=false,IncludeStatusBadges=false,IncludeTitle=false});
        Assert.Equal(2,prepared.Visual.Scene.Nodes.OfType<VisualScenePath>().Count(path=>path.Role=="topology-node-surface"));
        Assert.Contains("data-node-shape=\"HorizontalCylinder\"",prepared.ToSvg(),StringComparison.Ordinal);Assert.NotEmpty(prepared.ToPng());
        var facts=prepared.ToInterchangeEnvelope();Assert.Equal(TopologyNodeShape.HorizontalCylinder,Assert.Single(facts.Nodes).Topology!.Shape);
        Assert.Equal(facts.ToJson(),VisualArtifactInterchangeEnvelope.FromJson(facts.ToJson()).ToJson());
    }
    private static double Distance(ChartPoint point,IReadOnlyList<ChartPoint> contour)=>Enumerable.Range(0,contour.Count).Min(i=>Segment(point,contour[i],contour[(i+1)%contour.Count]));
    private static double Segment(ChartPoint point,ChartPoint a,ChartPoint b){var x=b.X-a.X;var y=b.Y-a.Y;var length=x*x+y*y;var t=length==0?0:Math.Max(0,Math.Min(1,((point.X-a.X)*x+(point.Y-a.Y)*y)/length));return Math.Sqrt(Math.Pow(point.X-a.X-t*x,2)+Math.Pow(point.Y-a.Y-t*y,2));}
}

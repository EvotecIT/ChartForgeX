using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeTopologyCatalogSurfaceTests {
    public static IEnumerable<object[]> Shapes() {
        foreach(var name in new[]{"DataStore","Folder","Bucket","Console","Browser","Person","TextBlock","NotchedRectangle",
            "LinedRectangle","FilledCircle","FramedCircle","Hourglass","BraceLeft","BraceRight","Braces","LightningBolt",
            "Document","HalfRoundedRectangle","LinedCylinder","CurvedTrapezoid","DividedRectangle","Triangle","WindowPane",
            "NotchedPentagon","TriangleDown","SlopedRectangle","StackedDocument","StackedRectangle","BowTieRectangle",
            "CrossedCircle","TaggedDocument","TaggedRectangle","WavyRectangle","CollapsedContainer","ScallopedBurst"}) yield return new object[]{name};
    }

    [Theory]
    [MemberData(nameof(Shapes))]
    public void NativeCatalogSharesExteriorContoursNamedPortsCaptionCellsAndPortableIdentity(string name) {
        Assert.True(Enum.TryParse<TopologyNodeShape>(name,out var shape));
        var node=new TopologyNode {Id="surface",Label="Caption",X=20.5,Y=30.25,Width=180,Height=110,Shape=shape,
            PreserveDisplayModeSize=true,ShowStatusBadge=false,FitLabelToContent=false};
        var parts=TopologyNodeShapeGeometry.SceneParts(node);
        var contourCount=name is "StackedDocument" or "StackedRectangle"?3:name=="Person"?2:1;
        var contours=parts.Take(contourCount).Select(part=>(part.Path.Flatten(1),part.Close)).ToArray();
        foreach(var port in new[]{TopologyEdgePort.Top,TopologyEdgePort.Right,TopologyEdgePort.Bottom,TopologyEdgePort.Left}) {
            var point=TopologyNodeShapeGeometry.BoundaryPoint(node,node.X+node.Width/2,node.Y+node.Height/2,port,0);
            Assert.InRange(Distance(point,contours),0,.002);
            var route=new List<ChartPoint>{new(node.X+node.Width/2,node.Y+node.Height/2),new(node.X+node.Width+100,node.Y+node.Height/2)};
            TopologyNodeShapeGeometry.AttachRoute(node,route,0,1,0,port,.5);
            Assert.InRange(Distance(route[0],contours),0,.002);
        }
        var caption=TopologyNodeShapeGeometry.CaptionBounds(node,14);
        Assert.True(caption.Width>0&&caption.Height>0);
        Assert.True(caption.Left>=node.X&&caption.Top>=node.Y&&caption.Right<=node.X+node.Width&&caption.Bottom<=node.Y+node.Height);
        var chart=TopologyChart.Create().WithTitle("").WithViewport(380,230,12).WithLegend(null);chart.Nodes.Add(node);
        var prepared=chart.Prepare(new TopologyRenderOptions{IncludeLegend=false,IncludeTitle=false,IncludeStatusBadges=false});
        var svg=prepared.ToSvg();Assert.Contains("data-node-shape=\""+name+"\"",svg,StringComparison.Ordinal);Assert.NotEmpty(prepared.ToPng());
        if(name=="TextBlock")Assert.DoesNotContain(prepared.Visual.Scene.Nodes.OfType<VisualScenePath>(),part=>part.Role=="topology-node-surface");
        var facts=prepared.ToInterchangeEnvelope();Assert.Equal(shape,Assert.Single(facts.Nodes).Topology!.Shape);
        Assert.Equal(2,facts.Version);
        Assert.Equal(facts.ToJson(),VisualArtifactInterchangeEnvelope.FromUtf8Json(facts.ToUtf8Json()).ToJson());
        Assert.Equal(shape,Assert.Single(facts.ToTopologyChart().Nodes).Shape);
        Assert.Equal(shape,TopologyLayoutEngine.Clone(chart).Nodes[0].Shape);
        MermaidFlowchartCatalogCausalTests.Capture(chart.ToVisualArtifact(),"native-catalog-"+name);
    }

    [Theory]
    [InlineData("DataStore")][InlineData("TextBlock")][InlineData("BraceLeft")][InlineData("BraceRight")][InlineData("Braces")]
    public void OpenInkAndTextKeepExplicitLogicalAttachmentBoxes(string name) {
        var shape=(TopologyNodeShape)Enum.Parse(typeof(TopologyNodeShape),name);
        var node=new TopologyNode {Id="open",Label="Text",X=20,Y=30,Width=180,Height=110,Shape=shape,ShowStatusBadge=false};
        var point=TopologyNodeShapeGeometry.BoundaryPoint(node,400,85,TopologyEdgePort.Right,0);
        Assert.Equal(200,point.X,6);Assert.Equal(85,point.Y,6);
        var top=TopologyNodeShapeGeometry.BoundaryPoint(node,110,-100,TopologyEdgePort.Top,0);
        Assert.Equal(110,top.X,6);Assert.Equal(30,top.Y,6);
    }

    [Fact]
    public void ScallopedBangUsesCurvesWhileExistingMindmapBurstRetainsItsAngularSurface() {
        var node=new TopologyNode{Id="burst",X=0,Y=0,Width=180,Height=110,Shape=TopologyNodeShape.Burst};
        var before=TopologyNodeShapeGeometry.SceneParts(node);Assert.Equal(24,Assert.Single(before).Path.Commands.Count);
        Assert.All(before[0].Path.Commands.Skip(1),command=>Assert.Equal(ChartPathCommandKind.LineTo,command.Kind));
        node.Shape=(TopologyNodeShape)Enum.Parse(typeof(TopologyNodeShape),"ScallopedBurst");
        var scalloped=Assert.Single(TopologyNodeShapeGeometry.SceneParts(node));
        Assert.Contains(scalloped.Path.Commands,command=>command.Kind==ChartPathCommandKind.CubicTo);
        Assert.True(scalloped.Path.Bounds().Width>0&&scalloped.Path.Bounds().Height>0);
    }

    private static double Distance(ChartPoint point,IEnumerable<(List<ChartPoint> Points,bool Closed)> contours)=>contours.Min(contour=>
        Enumerable.Range(0,contour.Closed?contour.Points.Count:contour.Points.Count-1).Min(index=>Segment(point,contour.Points[index],contour.Points[(index+1)%contour.Points.Count])));
    private static double Segment(ChartPoint point,ChartPoint start,ChartPoint end) {
        var dx=end.X-start.X;var dy=end.Y-start.Y;var length=dx*dx+dy*dy;
        var t=length==0?0:Math.Max(0,Math.Min(1,((point.X-start.X)*dx+(point.Y-start.Y)*dy)/length));
        var x=point.X-start.X-t*dx;var y=point.Y-start.Y-t*dy;return Math.Sqrt(x*x+y*y);
    }
}

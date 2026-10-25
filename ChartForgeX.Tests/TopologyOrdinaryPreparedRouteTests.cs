using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyOrdinaryPreparedRouteTests {
    [Theory] [InlineData(false)] [InlineData(true)]
    public void MateriallyFittedOrdinaryPaintAndSharedTailsRetainTheirActualSampledEndpoints(bool rounded) {
        var chart=Source();var options=chart.ResolveRenderOptions(null);options.EdgeCornerStyle=rounded?TopologyEdgeCornerStyle.Rounded:TopologyEdgeCornerStyle.Sharp;
        var before=chart.Prepare(options);Capture("ordinary-"+rounded+"-direct",before);var saved=before.ToInterchangeEnvelope();
        var restored=VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson()).ToTopologyChart();var after=restored.Prepare();Capture("ordinary-"+rounded+"-restored",after);
        Assert.All(saved.Edges,edge=>{Assert.Null(edge.Topology!.Interpolation);Assert.NotNull(edge.Topology.PreparedEdgePlan!.Geometry);Assert.Null(edge.Topology.PreparedEdgePlan.Geometry!.Interpolation);});
        Assert.Equal(saved.Edges.Select(Endpoints),after.ToInterchangeEnvelope().Edges.Select(Endpoints));
        Assert.Equal(TopologyStaticPaintObservation.Paint(before.ToSvg()),TopologyStaticPaintObservation.Paint(after.ToSvg()));Assert.Equal(before.ToPng(),after.ToPng());
        Assert.All(chart.Edges,edge=>Assert.Null(edge.Interpolation));
    }
    [Theory] [InlineData("position")] [InlineData("waypoint")] [InlineData("policy")] [InlineData("context")]
    public void ActualInputAndContextEditsDiscardNumericGeometryAndUseTheSameCurrentRouteOwner(string change) {
        var before=Source().Prepare();var saved=before.ToInterchangeEnvelope();Capture("edit-"+change+"-before",before);
        var chart=VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson()).ToTopologyChart();var initial=chart.Edges[0].PreparedEdgePlan!;Assert.NotNull(initial.Geometry);Assert.NotNull(initial.Basis);
        if(change=="position")chart.Nodes[0].X+=13;
        else if(change=="waypoint")chart.Edges[0].Waypoints.Add(new ChartPoint(chart.Nodes[0].X+30,chart.Nodes[0].Y+30));
        else if(change=="policy")chart.Edges[0].Routing=TopologyEdgeRouting.Straight;
        var ordinary=TopologyLayoutEngine.Clone(chart);foreach(var edge in ordinary.Edges)if(edge.PreparedEdgePlan!=null)edge.PreparedEdgePlan=edge.PreparedEdgePlan.WithoutGeometry();
        if(change=="context") {
            var options=chart.ResolveRenderOptions(null);var request=VisualExportRequest.ForTopology(chart,options).Context;
            var context=new VisualRenderContext(request.Layout,request.Theme,request.ThemeMode,new VisualFrame(showLegend:false,transparentBackground:true),request.Font);
            var actual=chart.Prepare(context,options);var expected=ordinary.Prepare(context,options);Capture("edit-context-actual",actual);Capture("edit-context-expected",expected);
            Assert.Equal(expected.ToPng(),actual.ToPng());Assert.Equal(TopologyStaticPaintObservation.Paint(expected.ToSvg()),TopologyStaticPaintObservation.Paint(actual.ToSvg()));
        } else {
            var actual=chart.Prepare();var expected=ordinary.Prepare();Capture("edit-"+change+"-actual",actual);Capture("edit-"+change+"-expected",expected);
            Assert.Null(actual.ToInterchangeEnvelope().Edges[0].Topology!.PreparedEdgePlan?.Geometry);
            Assert.Equal(expected.ToPng(),actual.ToPng());Assert.Equal(TopologyStaticPaintObservation.Paint(expected.ToSvg()),TopologyStaticPaintObservation.Paint(actual.ToSvg()));
        }
        Assert.NotNull(initial.Geometry);Assert.NotEqual(1,initial.DefaultUnits);
    }
    [Fact]
    public void OrdinaryManualUnitAndAuthoredWaypointsDoNotAcquireANumericCapture() {
        var chart=TopologyChart.Create().WithTitle("").WithViewport(760,520,24).WithLegend(null).WithLayout(TopologyLayoutMode.Manual)
            .AddNode("from","From",80,120,width:100,height:60).AddNode("to","To",400,120,width:100,height:60)
            .AddEdge("route","from","to",routing:TopologyEdgeRouting.Orthogonal);
        foreach(var node in chart.Nodes){node.Shape=TopologyNodeShape.Rectangle;node.PreserveDisplayModeSize=true;node.ShowStatusBadge=false;}
        chart.WithRenderOptions(new TopologyRenderOptions{IncludeTitle=false,IncludeLegend=false,FitContentToViewport=false});
        var ordinary=chart.Prepare();Capture("raw-unit-control",ordinary);Assert.Null(ordinary.ToInterchangeEnvelope().Edges[0].Topology!.PreparedEdgePlan);Assert.Equal(1,ordinary.ToInterchangeEnvelope().Version);
        var authored=new ChartPoint(300,100);chart.Edges[0].Waypoints.Add(authored);var waypoints=chart.Prepare();Capture("authored-waypoint-control",waypoints);
        Assert.Null(waypoints.ToInterchangeEnvelope().Edges[0].Topology!.PreparedEdgePlan?.Geometry);Assert.Equal(authored,Assert.Single(chart.Edges[0].Waypoints));
    }
    [Fact]
    public void OrdinaryCapturedFullPaintCannotContradictItsBodyAndSharedOwnerTail() {
        var saved=Source().Prepare().ToInterchangeEnvelope();var edge=saved.Edges[0];var original=edge.Topology!.PreparedEdgePlan!.Geometry!;
        foreach(var sibling in saved.Edges.Skip(1))if(sibling.Topology!.PreparedEdgePlan is {} other)sibling.Topology.PreparedEdgePlan=other.WithoutGeometry();
        var start=original.Input[0];var end=original.Input[original.Input.Count-1];var middle=new ChartPoint((start.X+end.X)/2,(start.Y+end.Y)/2);
        var body=ChartPathBuilder.FromPoints(new[]{start,middle});var tail=ChartPathBuilder.FromPoints(new[]{middle,end});
        var geometry=new TopologyPreparedEdgeGeometry(null,null,original.Input,TopologyInterpolationGeometry.JoinBodyAndTail(body,tail),body,tail,edge.Order,edge.Order);
        edge.Topology.PreparedEdgePlan=TopologyPreparedEdgePlan.Restore(edge.Topology.PreparedEdgePlan.DefaultUnits,geometry,false,true);
        var json=saved.ToJson();var raw=System.Text.Json.Nodes.JsonNode.Parse(json)!;
        raw["edges"]![0]!["topology"]!["preparedEdgePlan"]!["geometry"]!["fullPath"]!["commands"]![1]![1]=middle.X+1;
        var malformed=raw.ToJsonString();
        if(Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is {Length:>0} output){Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"ordinary-route185-coherent-valid.json"),json);File.WriteAllText(Path.Combine(output,"ordinary-route185-contradictory-full.json"),malformed);}
        Assert.ThrowsAny<ArgumentException>(()=>VisualArtifactInterchangeEnvelope.FromJson(malformed));
    }
    private static TopologyChart Source() {
        var chart=TopologyChart.Create().WithId("ordinary-captured-routes").WithTitle("").WithViewport(960,640,24).WithLegend(null).WithLayout(TopologyLayoutMode.Layered)
            .WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family));
        for(var index=0;index<3;index++)chart.AddAutoNode("source_"+index,"Source "+index,width:160,height:70);
        chart.AddAutoNode("target","Target",width:160,height:70);foreach(var node in chart.Nodes){node.Shape=TopologyNodeShape.Rectangle;node.PreserveDisplayModeSize=true;node.ShowStatusBadge=false;}
        for(var index=0;index<3;index++)chart.AddEdge("route_"+index,"source_"+index,"target","Relationship "+index,routing:TopologyEdgeRouting.ObstacleAvoidingOrthogonal);
        chart.WithRenderOptions(new TopologyRenderOptions{IncludeTitle=false,IncludeLegend=false,FitContentToViewport=true,ShareIncomingTrunks=true,EdgeCornerRadius=11});return chart;
    }
    private static (double X1,double Y1,double X2,double Y2) Endpoints(VisualArtifactInterchangeEdge edge){var a=edge.ResolvedRoute[0];var b=edge.ResolvedRoute[edge.ResolvedRoute.Count-1];return(a.X,a.Y,b.X,b.Y);}
    private static void Capture(string name,PreparedTopology value)=>Capture(name,value.Visual);
    private static void Capture(string name,PreparedVisual value){if(Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not{Length:>0} output)return;Directory.CreateDirectory(output);var stem=Path.Combine(output,"ordinary-route185-"+name);File.WriteAllText(stem+".svg",value.ToSvg());File.WriteAllBytes(stem+".png",value.ToPng());File.WriteAllText(stem+".json",value.ToArtifact().ToInterchangeJson());}
}

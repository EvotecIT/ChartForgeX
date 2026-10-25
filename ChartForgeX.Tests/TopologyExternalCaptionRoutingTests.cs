using System.Text.Json;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyExternalCaptionRoutingTests {
    [Theory] [InlineData(false)] [InlineData(true)]
    public void ObstacleRoutingAndDiagnosticsReserveTheActualCaptionAboveOrBelowItsBody(bool top) {
        var chart=Source(top);var direct=chart.Prepare();var envelope=direct.ToInterchangeEnvelope();var json=envelope.ToJson();Capture(top?"caption-top-route":"caption-bottom-route",direct);
        var observation=VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart();var node=observation.Nodes.Single(item=>item.Id=="captioned");
        var caption=TopologyExternalCaption.Prepare(node,observation.ResolveRenderOptions(null))!;var points=envelope.Edges[0].ResolvedRoute;var collisions=new List<int>();
        for(var index=1;index<points.Count;index++)if(CrossesInterior(caption.Bounds,new ChartPoint(points[index-1].X,points[index-1].Y),new ChartPoint(points[index].X,points[index].Y)))collisions.Add(index-1);
        Write("caption-"+top+".route-observation.json",JsonSerializer.Serialize(new{caption=caption.Bounds,collisions,points=points.Select(point=>new{point.X,point.Y})}));
        Assert.NotEmpty(points);Assert.Empty(collisions);Assert.Equal(TopologyEdgeRouting.ObstacleAvoidingOrthogonal,chart.Edges[0].Routing);
        Assert.Equal(direct.ToPng(),observation.ToPng());Assert.Equal(top?TopologyCaptionPlacement.Top:TopologyCaptionPlacement.Bottom,node.CaptionPlacement);
        var report=TopologyLayoutDiagnostics.Analyze(chart);var diagnosticNode=report.Nodes.Single(item=>item.Id=="captioned");var reported=diagnosticNode.CaptionBounds;Assert.NotNull(reported);
        Assert.True(top?reported!.Value.Bottom<=diagnosticNode.Bounds.Top-8+.001:reported!.Value.Top>=diagnosticNode.Bounds.Bottom+8-.001);
    }

    [Fact]
    public void AuthoredWaypointsKeepTheirOwnershipEvenWhenTheyCrossAnEnabledCaption() {
        var chart=Source(true);var edge=chart.Edges[0];edge.Waypoints.AddRange(new[]{new ChartPoint(230,180),new ChartPoint(380,180)});var authored=edge.Waypoints.ToArray();
        var direct=chart.Prepare();var envelope=direct.ToInterchangeEnvelope();Capture("caption-authored-waypoints",direct);var saved=envelope.Edges[0];
        Assert.Equal(authored,edge.Waypoints);Assert.Equal(2,saved.Topology!.Waypoints.Count);
        foreach(var point in saved.Topology.Waypoints)Assert.Contains(saved.ResolvedRoute,row=>row.X==point.X&&row.Y==point.Y);
        Assert.Equal(direct.ToPng(),VisualArtifactInterchangeEnvelope.FromJson(envelope.ToJson()).ToTopologyChart().ToPng());
    }

    internal static TopologyChart Source(bool top) {
        var chart=TopologyChart.Create().WithId("external-caption-obstacle").WithTitle("").WithViewport(640,400,24).WithLayout(TopologyLayoutMode.Manual)
            .WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family)).AddNode("source","Start",40,150,width:80,height:60)
            .AddNode("captioned","Review request\nAudit record",250,top?220:80,width:100,height:60).AddNode("target","Finish",480,150,width:80,height:60)
            .AddEdge("request","source","target",routing:TopologyEdgeRouting.ObstacleAvoidingOrthogonal);
        foreach(var node in chart.Nodes){node.Shape=TopologyNodeShape.Rectangle;node.PreserveDisplayModeSize=true;node.ShowStatusBadge=false;node.ShowLabel=false;}
        var captioned=chart.Nodes[1];captioned.DisplayMode=TopologyNodeDisplayMode.Icon;captioned.IconShape=TopologyIconShape.Router;captioned.ShowLabel=true;
        captioned.CaptionPlacement=top?TopologyCaptionPlacement.Top:TopologyCaptionPlacement.Bottom;captioned.CaptionGap=8;captioned.CaptionSurface=TopologyCaptionSurface.Plain;captioned.MaximumTextWidth=160;
        captioned.CaptionTextStyle=new TextStyleOverride{FontSize=20,FontWeight="400"};chart.WithRenderOptions(new TopologyRenderOptions{IncludeLegend=false,IncludeIconLabels=true,FitContentToViewport=false,MaxNodeLabelLines=4,WrapNodeLabels=true,ReadableDenseLayout=true});return chart;
    }
    private static bool CrossesInterior(ChartRect box,ChartPoint a,ChartPoint b){const double inset=.001;var left=box.Left+inset;var right=box.Right-inset;var top=box.Top+inset;var bottom=box.Bottom-inset;if(right<=left||bottom<=top)return false;var first=0d;var last=1d;var dx=b.X-a.X;var dy=b.Y-a.Y;return Clip(-dx,a.X-left)&&Clip(dx,right-a.X)&&Clip(-dy,a.Y-top)&&Clip(dy,bottom-a.Y)&&last>=first;bool Clip(double direction,double distance){if(direction==0)return distance>=0;var value=distance/direction;if(direction<0){if(value>last)return false;if(value>first)first=value;}else{if(value<first)return false;if(value<last)last=value;}return true;}}
    private static void Capture(string name,PreparedTopology prepared)=>TopologyExternalCaptionPolicyTests.Capture(name,prepared.ToSvg(),prepared.ToPng(),prepared.ToInterchangeEnvelope().ToJson());
    private static void Write(string name,string text){if(Environment.GetEnvironmentVariable("CFX_FLOWCHART_ASSET_CAPTURE_DIRECTORY") is not {Length:>0} output)return;Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,name),text);}
}

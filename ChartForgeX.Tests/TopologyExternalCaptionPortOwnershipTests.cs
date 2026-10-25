using System.Text.Json;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyExternalCaptionPortOwnershipTests {
    [Theory] [InlineData(false,false)] [InlineData(false,true)] [InlineData(true,false)] [InlineData(true,true)]
    public void ReadableDenseNamedTopOrBottomCaptionCollisionsRejectWithoutReassigningTheirIdentity(bool top,bool target) {
        var chart=Source(top,target,top?TopologyEdgePort.Top:TopologyEdgePort.Bottom,true); var edge=chart.Edges[0];
        var ordinary=chart.ResolveRenderOptions(null).Clone();ordinary.ReadableDenseLayout=false;var control=chart.Prepare(ordinary);Capture("named-control-"+top+"-"+target,control);
        Exception? error=null;try{_ = chart.Prepare();}catch(Exception actual){error=actual;}
        Observe("named-occupied-"+top+"-"+target,error);Assert.IsType<InvalidOperationException>(error);
        Assert.Equal("owned",target?edge.TargetPortId:edge.SourcePortId);Assert.Equal(top?TopologyEdgePort.Top:TopologyEdgePort.Bottom,chart.Nodes[0].Ports[0].Side);
        var saved=control.ToInterchangeEnvelope();Assert.Equal("owned",target?saved.Edges[0].TargetPortId:saved.Edges[0].SourcePortId);
    }

    [Theory] [InlineData(false)] [InlineData(true)]
    public void AnAdmittedNamedSidePortKeepsItsActualOffsetAndPortableCoordinates(bool target) {
        var chart=Source(true,target,TopologyEdgePort.Right,true);var before=chart.Prepare();var saved=before.ToInterchangeEnvelope();Capture("named-side-"+target,before);
        var after=saved.ToTopologyChart().Prepare();Capture("named-side-restored-"+target,after);
        var node=saved.Nodes.Single(item=>item.Id=="captioned");var route=saved.Edges[0].ResolvedRoute;var end=target?route[route.Count-1]:route[0];
        Assert.Equal(node.X!.Value+node.Width!.Value+7,end.X,6);Assert.Equal(node.Y!.Value+node.Height!.Value*.35,end.Y,6);
        Assert.Equal("owned",target?saved.Edges[0].TargetPortId:saved.Edges[0].SourcePortId);Assert.Equal(before.ToPng(),after.ToPng());
    }

    [Theory] [InlineData(false)] [InlineData(true)]
    public void NewCaptionPolicyCannotSilentlySwitchAnExplicitOccupiedSide(bool top) {
        var chart=Source(top,false,top?TopologyEdgePort.Top:TopologyEdgePort.Bottom,false);Exception? error=null;
        try{_ = chart.Prepare();}catch(Exception actual){error=actual;}Observe("explicit-occupied-"+top,error);
        Assert.IsType<TopologyValidationException>(error);Assert.Equal(top?TopologyEdgePort.Top:TopologyEdgePort.Bottom,chart.Edges[0].SourcePort);
    }

    private static TopologyChart Source(bool top,bool target,TopologyEdgePort side,bool named) {
        var chart=TopologyExternalCaptionPolicyTests.Source(TopologyNodeDisplayMode.Icon);var node=chart.Nodes[0];node.Id="captioned";
        node.CaptionPlacement=top?TopologyCaptionPlacement.Top:TopologyCaptionPlacement.Bottom;node.CaptionGap=8;
        node.CaptionTextStyle=new TextStyleOverride{FontSize=14};node.Ports.Add(new TopologyNodePort{Id="owned",Side=side,Offset=.35});
        chart.AddNode("other","Other",430,40,width:90,height:60);chart.Nodes[1].Shape=TopologyNodeShape.Rectangle;chart.Nodes[1].ShowStatusBadge=false;
        chart.AddEdge("flow",target?"other":"captioned",target?"captioned":"other",routing:TopologyEdgeRouting.ObstacleAvoidingOrthogonal);
        var edge=chart.Edges[0];if(target){if(named)edge.TargetPortId="owned";else edge.TargetPort=side;}else{if(named)edge.SourcePortId="owned";else edge.SourcePort=side;}
        chart.DefaultRenderOptions!.ReadableDenseLayout=true;return chart;
    }
    private static void Capture(string name,PreparedTopology value)=>TopologyExternalCaptionPolicyTests.Capture("ports-"+name,value.ToSvg(),value.ToPng(),value.ToInterchangeEnvelope().ToJson());
    private static void Observe(string name,Exception? error){if(Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not {Length:>0} output)return;
        Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"external-caption-port-"+name+".observed.json"),JsonSerializer.Serialize(new{error=error?.GetType().FullName,error?.Message}));}
}

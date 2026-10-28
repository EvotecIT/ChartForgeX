using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyProjectedSourceCompletionTests {
    [Theory] [InlineData(false)] [InlineData(true)]
    public void OrdinaryAndCollidingFlowIdsCompleteAfterActualRoutesWithoutChangingPreparedPaint(bool collision) {
        var step=collision?"shared":"start";var flow=FlowArtifact.Create("projected-flow-completion").AddLane("shared","Lane")
            .AddStep(step,"Step",laneId:"shared").AddStep("target","Target",laneId:"shared").AddConnector(step,"target");
        var direct=flow.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(640,400),24)));
        var svg=direct.ToSvg();var png=direct.ToPng();Capture("projected-flow-"+collision+"-before-json",svg,png,"");
        var envelope=direct.ToArtifact().ToInterchangeEnvelope();var json=envelope.ToJson();Capture("projected-flow-"+collision+"-after-json",direct.ToSvg(),direct.ToPng(),json);
        var node=envelope.Nodes.Single(item=>item.Label=="Step");Assert.Equal("shared",envelope.Groups.Single().Id);
        Assert.Equal(node.Id,envelope.Edges.Single().SourceId);Assert.Equal("shared",node.GroupId);Assert.True(envelope.Edges.Single().ResolvedRoute.Count>=2);
        if(collision){Assert.NotEqual(step,node.Id);Assert.Equal(step,node.Extensions["chartforgex.sourceId"]);}else Assert.False(node.Extensions.ContainsKey("chartforgex.sourceId"));
        Assert.Equal(svg,direct.ToSvg());Assert.Equal(png,direct.ToPng());Assert.Equal(json,VisualArtifactInterchangeEnvelope.FromJson(json).ToJson());
    }

    [Fact]
    public void PendingPortableCompletionKeepsItsNativeSnapshotDetachedAndRetainsCollisionPrecedence() {
        var chart=TopologyChart.Create().WithId("projected-completion").WithTitle("").WithViewport(640,400,24).WithLayout(TopologyLayoutMode.Manual)
            .AddGroup("shared","Group",20,20,500,250).AddNode("shared","Node",60,80,groupId:"shared");
        chart.Nodes[0].Metadata["chartforgex.sourceId"]="caller-owned";
        chart.WithRenderOptions(new TopologyRenderOptions{IncludeTitle=false,IncludeLegend=false,FitContentToViewport=false});var prepared=chart.Prepare();
        var svg=prepared.ToSvg();var png=prepared.ToPng();var first=prepared.ToInterchangeEnvelope();var json=first.ToJson();Capture("projected-source-precedence",svg,png,json);
        var node=Assert.Single(first.Nodes);Assert.Equal("shared",node.Extensions["chartforgex.sourceId"]);Assert.Contains("caller-owned",node.Extensions.Values);
        node.Extensions["chartforgex.sourceId"]="changed caller";Assert.Equal(json,prepared.ToInterchangeEnvelope().ToJson());Assert.Equal(svg,prepared.ToSvg());Assert.Equal(png,prepared.ToPng());
        first.Edges.Add(new VisualArtifactInterchangeEdge{Id="invalid",SourceId="missing",TargetId=node.Id});Assert.Throws<ArgumentException>(()=>first.ToJson());
    }

    private static void Capture(string name,string svg,byte[] png,string json) {
        if(Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not {Length:>0} output)return;
        Directory.CreateDirectory(output);var path=Path.Combine(output,name);File.WriteAllText(path+".svg",svg);File.WriteAllBytes(path+".png",png);if(json.Length>0)File.WriteAllText(path+".json",json);
    }
}

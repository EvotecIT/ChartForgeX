using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class TopologySwimlaneAutomaticOrderingTests {
    [Theory]
    [InlineData(TopologyLayoutDirection.LeftToRight,false)] [InlineData(TopologyLayoutDirection.RightToLeft,false)]
    [InlineData(TopologyLayoutDirection.TopToBottom,false)] [InlineData(TopologyLayoutDirection.BottomToTop,false)]
    [InlineData(TopologyLayoutDirection.LeftToRight,true)] [InlineData(TopologyLayoutDirection.RightToLeft,true)]
    [InlineData(TopologyLayoutDirection.TopToBottom,true)] [InlineData(TopologyLayoutDirection.BottomToTop,true)]
    public void NativeOrderingChangesOnlyTemporaryOuterPlacementAndComposesWithLocalRanks(TopologyLayoutDirection direction,bool local) {
        var chart=Chart(direction,local);var sourceGroups=chart.Groups.Select(group=>group.Id).ToArray();var sourceNodes=chart.Nodes.Select(node=>node.Id).ToArray();
        var options=new TopologyRenderOptions {IncludeTitle=false,IncludeLegend=false,SwimlaneAutomaticLaneOrdering=true,SwimlaneIgnoreCrossLaneRankConstraints=true,LayeredNodeSpacing=70,LayeredRankSpacing=130};
        var prepared=chart.Prepare(options);MermaidNestedSwimlaneTests.Capture(prepared,"swimlane-auto-order-native-"+direction+"-"+local);
        var saved=prepared.ToInterchangeEnvelope();var token=direction is TopologyLayoutDirection.LeftToRight or TopologyLayoutDirection.RightToLeft?"LR":"TB";
        var order=MermaidSwimlaneOrderingCausalTests.Order(saved,token);Assert.True(MermaidSwimlaneOrderingCausalTests.Cost(order)<12);
        Assert.Equal(sourceGroups,chart.Groups.Select(group=>group.Id));Assert.Equal(sourceNodes,chart.Nodes.Select(node=>node.Id));Assert.Equal(6,saved.Edges.Count);
        MermaidNestedSwimlaneTests.AssertContainment(saved);var svg=prepared.ToSvg();var json=saved.ToJson();options.SwimlaneAutomaticLaneOrdering=false;chart.Groups[0].Label="Changed";
        Assert.Equal(svg,prepared.ToSvg());Assert.Equal(json,prepared.ToInterchangeEnvelope().ToJson());
        var restored=VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart();Assert.Equal(TopologyLayoutMode.Manual,restored.LayoutMode);
        Assert.Equal(saved.Groups.Select(group=>(group.Id,group.X,group.Y)),restored.Prepare().ToInterchangeEnvelope().Groups.Select(group=>(group.Id,group.X,group.Y)));
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void NullFalseAndNoConnectionControlsKeepTheActualIncumbentScene(bool noEdges) {
        var chart=Chart(TopologyLayoutDirection.LeftToRight,false);if(noEdges)chart.Edges.Clear();
        var original=chart.Prepare(new TopologyRenderOptions {IncludeTitle=false,IncludeLegend=false});
        var disabled=chart.Prepare(new TopologyRenderOptions {IncludeTitle=false,IncludeLegend=false,SwimlaneAutomaticLaneOrdering=false});
        Assert.Equal(original.ToSvg(),disabled.ToSvg());Assert.Equal(original.ToPng(),disabled.ToPng());Assert.Equal(original.ToInterchangeEnvelope().ToJson(),disabled.ToInterchangeEnvelope().ToJson());
        if(noEdges){var active=chart.Prepare(new TopologyRenderOptions {IncludeTitle=false,IncludeLegend=false,SwimlaneAutomaticLaneOrdering=true});Assert.Equal(original.ToSvg(),active.ToSvg());Assert.Equal(original.ToPng(),active.ToPng());}
    }
    [Fact]
    public void RepeatedBoundedSearchIsDeterministicAndNeverWorsensItsStatedObjective() {
        var chart=Chart(TopologyLayoutDirection.LeftToRight,true);var options=new TopologyRenderOptions {IncludeTitle=false,IncludeLegend=false,SwimlaneAutomaticLaneOrdering=true};
        var first=chart.Prepare(options);var second=chart.Prepare(options);Assert.Equal(first.ToSvg(),second.ToSvg());Assert.Equal(first.ToPng(),second.ToPng());
        Assert.True(MermaidSwimlaneOrderingCausalTests.Cost(MermaidSwimlaneOrderingCausalTests.Order(first.ToInterchangeEnvelope(),"LR"))<=12);
    }
    private static TopologyChart Chart(TopologyLayoutDirection direction,bool local) {
        var chart=TopologyChart.Create().WithTitle("").WithViewport(1100,900,24).WithLayout(TopologyLayoutMode.Swimlane,direction);
        foreach(var id in new[]{"A","B","C","D"})chart.AddAutoGroup(id,id);
        if(local){chart.AddAutoGroup("nested","Nested");chart.Groups.Last().ParentGroupId="A";chart.Groups.Last().LayoutDirection=direction==TopologyLayoutDirection.TopToBottom?TopologyLayoutDirection.LeftToRight:TopologyLayoutDirection.TopToBottom;}
        foreach(var id in new[]{"a","b","c","d"})chart.AddAutoNode(id,id,groupId:local&&id=="a"?"nested":id.ToUpperInvariant(),width:120,height:56);
        foreach(var node in chart.Nodes){node.Metadata["layer"]="0";node.DisplayMode=TopologyNodeDisplayMode.Card;node.PreserveDisplayModeSize=true;}
        for(var index=0;index<3;index++)chart.AddEdge("ad-"+index,"a","d").AddEdge("bc-"+index,"b","c");return chart;
    }
}

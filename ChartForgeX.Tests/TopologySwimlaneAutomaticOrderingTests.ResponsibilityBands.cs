using ChartForgeX.Rendering;
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
    public void WeightedLocalResponsibilitiesKeepTheirBandsWithRootMinimaUngroupedSiblingsAndBothOutputPolicies(TopologyLayoutDirection direction,bool fit) {
        var chart=Chart(direction,true).WithViewport(2400,2400,24);chart.Nodes.Single(node=>node.Id=="d").Metadata["layer"]="3";
        chart.AddAutoNode("free","External",width:120,height:56).AddEdge("free-b","free","b");chart.Nodes.Last().Metadata["layer"]="1";
        var groups=chart.Groups.Select(group=>group.Id).ToArray();var nodes=chart.Nodes.Select(node=>node.Id).ToArray();
        var options=new TopologyRenderOptions{IncludeTitle=false,IncludeLegend=false,FitContentToViewport=fit,SwimlaneAutomaticLaneOrdering=true,
            SwimlaneIgnoreCrossLaneRankConstraints=true,LayeredNodeSpacing=70,LayeredRankSpacing=130};
        var prepared=fit?chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(800,700),24)),options)
            :chart.Prepare(options).Visual;
        var saved=prepared.ToArtifact().ToInterchangeEnvelope();MermaidNestedSwimlaneTests.Capture(prepared.ToArtifact(),"weighted-local-bands-"+direction+"-"+fit);
        var horizontal=direction is TopologyLayoutDirection.LeftToRight or TopologyLayoutDirection.RightToLeft;
        var roots=saved.Groups.Where(group=>group.Topology!.ParentGroupId==null).OrderBy(group=>horizontal?group.Y:group.X).ToArray();
        Assert.True(MermaidSwimlaneOrderingCausalTests.Cost(roots.Select(group=>group.Id).ToArray())<12);
        for(var i=1;i<roots.Length;i++)Assert.True(horizontal?roots[i-1].Y!.Value+roots[i-1].Height!.Value<=roots[i].Y!.Value:roots[i-1].X!.Value+roots[i-1].Width!.Value<=roots[i].X!.Value);
        var first=roots.Single(group=>group.Id=="A");var later=roots.Single(group=>group.Id=="D");
        var firstMain=horizontal?first.X!.Value:first.Y!.Value;var laterMain=horizontal?later.X!.Value:later.Y!.Value;
        Assert.True(direction is TopologyLayoutDirection.RightToLeft or TopologyLayoutDirection.BottomToTop?laterMain<firstMain:laterMain>firstMain);
        Assert.Null(saved.Nodes.Single(node=>node.Id=="free").GroupId);Assert.Equal(7,saved.Edges.Count);MermaidNestedSwimlaneTests.AssertContainment(saved);
        Assert.Equal(groups,chart.Groups.Select(group=>group.Id));Assert.Equal(nodes,chart.Nodes.Select(node=>node.Id));Assert.Equal("3",chart.Nodes.Single(node=>node.Id=="d").Metadata["layer"]);
    }

    [Theory] [InlineData(false)] [InlineData(true)]
    public void LocalNoConnectionAndNonSwimlanePoliciesRetainExactOrdinaryBytes(bool noConnections) {
        var chart=Chart(TopologyLayoutDirection.LeftToRight,true);if(noConnections)chart.Edges.Clear();else chart.LayoutMode=TopologyLayoutMode.Layered;
        var ordinary=chart.Prepare(new TopologyRenderOptions{IncludeTitle=false,IncludeLegend=false});
        var selected=chart.Prepare(new TopologyRenderOptions{IncludeTitle=false,IncludeLegend=false,SwimlaneAutomaticLaneOrdering=true});
        MermaidNestedSwimlaneTests.Capture(ordinary,"weighted-local-band-control-"+noConnections+"-ordinary");MermaidNestedSwimlaneTests.Capture(selected,"weighted-local-band-control-"+noConnections+"-selected");
        Assert.Equal(ordinary.ToSvg(),selected.ToSvg());Assert.Equal(ordinary.ToPng(),selected.ToPng());Assert.Equal(ordinary.ToInterchangeEnvelope().ToJson(),selected.ToInterchangeEnvelope().ToJson());
    }
}

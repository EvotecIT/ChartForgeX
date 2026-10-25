using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyRankCrossingOptimizationTests {
    [Theory]
    [InlineData(TopologyLayoutDirection.LeftToRight,TopologyLayoutMode.Layered)] [InlineData(TopologyLayoutDirection.RightToLeft,TopologyLayoutMode.Layered)]
    [InlineData(TopologyLayoutDirection.TopToBottom,TopologyLayoutMode.Layered)] [InlineData(TopologyLayoutDirection.BottomToTop,TopologyLayoutMode.Layered)]
    [InlineData(TopologyLayoutDirection.LeftToRight,TopologyLayoutMode.Swimlane)] [InlineData(TopologyLayoutDirection.RightToLeft,TopologyLayoutMode.Swimlane)]
    [InlineData(TopologyLayoutDirection.TopToBottom,TopologyLayoutMode.Swimlane)] [InlineData(TopologyLayoutDirection.BottomToTop,TopologyLayoutMode.Swimlane)]
    public void NativeOptInMakesARealLegalRankMoveAndLowersTheObservableFixtureObjective(TopologyLayoutDirection direction,TopologyLayoutMode mode) {
        var chart=TopologyRankOptimizationCausalTests.Chart(direction,mode);var baseline=chart.Prepare(new TopologyRenderOptions {IncludeTitle=false,IncludeLegend=false});
        var options=new TopologyRenderOptions {IncludeTitle=false,IncludeLegend=false,LayeredOptimizeRanksByCrossings=true};var prepared=chart.Prepare(options);
        MermaidNestedSwimlaneTests.Capture(prepared,"rank-optimized-native-"+mode+"-"+direction);var saved=prepared.ToInterchangeEnvelope();
        Assert.Equal(1,TopologyRankOptimizationCausalTests.Crossings(baseline.ToInterchangeEnvelope()));Assert.Equal(0,TopologyRankOptimizationCausalTests.Crossings(saved));
        Assert.Equal(new[]{"A","B","C","D","E"},chart.Nodes.Select(node=>node.Id));Assert.Equal(new[]{"ad","bc"},saved.Edges.Select(edge=>edge.Id));
        Assert.Equal(new[]{"0","0","1","1","2"},chart.Nodes.Select(node=>node.Metadata["layer"]));
        var horizontal=direction is TopologyLayoutDirection.LeftToRight or TopologyLayoutDirection.RightToLeft;var sign=direction is TopologyLayoutDirection.RightToLeft or TopologyLayoutDirection.BottomToTop?-1:1;
        var source=saved.Nodes.Single(node=>node.Id=="A");var target=saved.Nodes.Single(node=>node.Id=="D");Assert.True(((horizontal?target.X!.Value:target.Y!.Value)-(horizontal?source.X!.Value:source.Y!.Value))*sign>0);
        var svg=prepared.ToSvg();var json=saved.ToJson();options.LayeredOptimizeRanksByCrossings=false;chart.Nodes[0].Metadata["layer"]="100";
        Assert.Equal(svg,prepared.ToSvg());Assert.Equal(json,prepared.ToInterchangeEnvelope().ToJson());
        var restored=VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart();Assert.Equal(TopologyLayoutMode.Manual,restored.LayoutMode);Assert.Equal(saved.Nodes.Select(node=>(node.Id,node.X,node.Y)),restored.Prepare().ToInterchangeEnvelope().Nodes.Select(node=>(node.Id,node.X,node.Y)));
    }
    [Theory]
    [InlineData(TopologyLayoutMode.Layered)] [InlineData(TopologyLayoutMode.Swimlane)]
    public void NullAndFalseKeepExactIncumbentSvgPngAndPortableBytes(TopologyLayoutMode mode) {
        var chart=TopologyRankOptimizationCausalTests.Chart(TopologyLayoutDirection.LeftToRight,mode);
        var original=chart.Prepare(new TopologyRenderOptions {IncludeTitle=false,IncludeLegend=false});var disabled=chart.Prepare(new TopologyRenderOptions {IncludeTitle=false,IncludeLegend=false,LayeredOptimizeRanksByCrossings=false});
        Assert.Equal(original.ToSvg(),disabled.ToSvg());Assert.Equal(original.ToPng(),disabled.ToPng());Assert.Equal(original.ToInterchangeEnvelope().ToJson(),disabled.ToInterchangeEnvelope().ToJson());
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void RetainedMinimumSpansBackwardEdgesAndStrongComponentsRemainAuthoritative(bool reverse) {
        var chart=TopologyRankOptimizationCausalTests.Chart(TopologyLayoutDirection.LeftToRight,TopologyLayoutMode.Swimlane);
        chart.Edges[0].MinimumRankSpan=3;chart.Edges[0].Direction=reverse?VisualLinkDirection.Backward:VisualLinkDirection.Forward;
        chart.AddEdge("cycle-one","C","E").AddEdge("cycle-two","E","C");
        var prepared=chart.Prepare(new TopologyRenderOptions {IncludeTitle=false,IncludeLegend=false,LayeredOptimizeRanksByCrossings=true,SwimlaneIgnoreCrossLaneRankConstraints=true});
        var saved=prepared.ToInterchangeEnvelope();Assert.Equal(4,saved.Edges.Count);Assert.Equal(3,saved.Edges.Single(edge=>edge.Id=="ad").Topology!.MinimumRankSpan);
        var c=saved.Nodes.Single(node=>node.Id=="C");var e=saved.Nodes.Single(node=>node.Id=="E");Assert.Equal(c.X,e.X);
        var a=saved.Nodes.Single(node=>node.Id=="A");var d=saved.Nodes.Single(node=>node.Id=="D");Assert.True(reverse?a.X>d.X:d.X>a.X);Assert.Equal(reverse?VisualLinkDirection.Backward:VisualLinkDirection.Forward,chart.Edges[0].Direction);
    }
    [Fact]
    public void EffectiveLocalCompositionReusesTheSameBoundedRankOwnerAndFilteredConstraints() {
        var chart=TopologyRankOptimizationCausalTests.Chart(TopologyLayoutDirection.LeftToRight,TopologyLayoutMode.Swimlane).AddAutoGroup("root","Root").AddAutoGroup("local","Local").AddAutoGroup("other","Other");
        chart.Groups[1].ParentGroupId="root";chart.Groups[1].LayoutDirection=TopologyLayoutDirection.TopToBottom;
        foreach(var node in chart.Nodes)node.GroupId="local";chart.AddAutoNode("outside","Outside",groupId:"other",width:120,height:56).AddEdge("handoff","D","outside");
        var options=new TopologyRenderOptions {IncludeTitle=false,IncludeLegend=false,LayeredOptimizeRanksByCrossings=true,SwimlaneIgnoreCrossLaneRankConstraints=true,SwimlaneAutomaticLaneOrdering=true};
        var saved=chart.Prepare(options).ToInterchangeEnvelope();Assert.Equal(0,TopologyRankOptimizationCausalTests.Crossings(saved));MermaidNestedSwimlaneTests.AssertContainment(saved);Assert.Contains(saved.Edges,edge=>edge.Id=="handoff");
    }
    [Theory]
    [InlineData(TopologyLayoutMode.Manual)] [InlineData(TopologyLayoutMode.MindMap)]
    public void ActivePolicyRejectsLayoutsThatCannotRunItsAutomaticRankOwner(TopologyLayoutMode mode) {
        var chart=TopologyRankOptimizationCausalTests.Chart(TopologyLayoutDirection.LeftToRight,mode);Assert.Throws<NotSupportedException>(()=>chart.Prepare(new TopologyRenderOptions {LayeredOptimizeRanksByCrossings=true}));
    }
    [Fact]
    public void RankDomainOverflowIsDiagnosedInsteadOfWrappingARequiredSuccessorRank() {
        var chart=TopologyRankOptimizationCausalTests.Chart(TopologyLayoutDirection.LeftToRight,TopologyLayoutMode.Layered);chart.Nodes[0].Metadata["layer"]=int.MaxValue.ToString();
        Assert.Throws<NotSupportedException>(()=>chart.Prepare(new TopologyRenderOptions {LayeredOptimizeRanksByCrossings=true}));
    }
}

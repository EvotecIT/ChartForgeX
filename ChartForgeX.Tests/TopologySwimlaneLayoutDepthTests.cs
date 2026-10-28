using ChartForgeX.Primitives;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologySwimlaneLayoutDepthTests {
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ExplicitRankMinimaOuterResponsibilitiesFreeActorsAndRealEdgesSurviveFilteredRanking(bool local) {
        var chart=TopologySwimlaneLayoutDepthCausalTests.Chart(TopologyLayoutDirection.LeftToRight,true);
        chart.AddAutoGroup("other","Other").AddAutoGroup("other-child","Other child");chart.Groups.Last().ParentGroupId="other";
        chart.AddAutoNode("d","Deliver",groupId:"other-child",width:120,height:56).AddAutoNode("free","External",width:120,height:56);
        chart.Nodes.Single(node=>node.Id=="d").Metadata["layer"]="7";chart.Nodes.Last().Metadata["layer"]="0";
        chart.AddEdge("within","a","c").AddEdge("cross","c","d").AddEdge("external","free","d");
        chart.Edges[2].MinimumRankSpan=3;
        if(local)chart.Groups.Single(group=>group.Id=="work").LayoutDirection=TopologyLayoutDirection.TopToBottom;
        var options=new TopologyRenderOptions {IncludeTitle=false,IncludeLegend=false,SwimlaneIgnoreCrossLaneRankConstraints=true,LayeredNodeSpacing=70,LayeredRankSpacing=130};
        var prepared=chart.Prepare(options);MermaidNestedSwimlaneTests.Capture(prepared,"swimlane-filtered-explicit-ranks-"+local);
        var saved=prepared.ToInterchangeEnvelope();MermaidNestedSwimlaneTests.AssertContainment(saved);
        Assert.Equal(new[]{"within","cross","external"},saved.Edges.Select(edge=>edge.Id));
        var restored=saved.ToTopologyChart();Assert.True(int.Parse(restored.Nodes.Single(node=>node.Id=="d").Metadata["layer"])>=7);
        Assert.Equal(3,saved.Edges.Single(edge=>edge.Id=="external").Topology!.MinimumRankSpan);
        var svg=prepared.ToSvg();var json=saved.ToJson();options.SwimlaneIgnoreCrossLaneRankConstraints=false;options.LayeredNodeSpacing=0;
        chart.Groups[0].LayoutDirection=TopologyLayoutDirection.BottomToTop;Assert.Equal(svg,prepared.ToSvg());Assert.Equal(json,prepared.ToInterchangeEnvelope().ToJson());
        Assert.Equal(TopologyLayoutMode.Manual,restored.LayoutMode);Assert.Equal(saved.Nodes.Select(node=>(node.Id,node.X,node.Y)),restored.Prepare().ToInterchangeEnvelope().Nodes.Select(node=>(node.Id,node.X,node.Y)));
    }

    [Theory]
    [InlineData(TopologyLayoutDirection.LeftToRight)] [InlineData(TopologyLayoutDirection.RightToLeft)]
    [InlineData(TopologyLayoutDirection.TopToBottom)] [InlineData(TopologyLayoutDirection.BottomToTop)]
    public void NullFalseAndSameInheritedDirectionsRetainExactIncumbentSvgPngAndPortableBytes(TopologyLayoutDirection global) {
        var baseline=TopologySwimlaneLayoutDepthCausalTests.Chart(global,true);var expected=baseline.Prepare(new TopologyRenderOptions {IncludeTitle=false,IncludeLegend=false});
        var chart=TopologySwimlaneLayoutDepthCausalTests.Chart(global,true);chart.Groups.Single(group=>group.Id=="work").LayoutDirection=global;
        var prepared=chart.Prepare(new TopologyRenderOptions {IncludeTitle=false,IncludeLegend=false,SwimlaneIgnoreCrossLaneRankConstraints=false});
        // Authored direction facts participate in deterministic IDs; compare all flat numeric paint and exact pixels separately from those facts.
        MermaidNestedSwimlaneTests.Capture(expected,"swimlane-same-direction-"+global+"-ordinary");MermaidNestedSwimlaneTests.Capture(prepared,"swimlane-same-direction-"+global+"-authored");
        Assert.Equal(TopologyStaticPaintObservation.Paint(expected.ToSvg()),TopologyStaticPaintObservation.Paint(prepared.ToSvg()));Assert.Equal(expected.ToPng(),prepared.ToPng());
        Assert.Null(expected.ToInterchangeEnvelope().Groups.Single(group=>group.Id=="work").Topology!.LayoutDirection);
        Assert.Equal(global,prepared.ToInterchangeEnvelope().Groups.Single(group=>group.Id=="work").Topology!.LayoutDirection);
        Assert.Equal(expected.ToInterchangeEnvelope().Nodes.Select(node=>(node.Id,node.X,node.Y,node.Width,node.Height)),prepared.ToInterchangeEnvelope().Nodes.Select(node=>(node.Id,node.X,node.Y,node.Width,node.Height)));
        var noPolicy=TopologySwimlaneLayoutDepthCausalTests.Chart(global,true).Prepare(new TopologyRenderOptions {IncludeTitle=false,IncludeLegend=false});
        var falsePolicy=TopologySwimlaneLayoutDepthCausalTests.Chart(global,true).Prepare(new TopologyRenderOptions {IncludeTitle=false,IncludeLegend=false,SwimlaneIgnoreCrossLaneRankConstraints=false});
        Assert.Equal(noPolicy.ToInterchangeEnvelope().ToJson(),falsePolicy.ToInterchangeEnvelope().ToJson());
    }

    [Theory]
    [InlineData(TopologyLayoutDirection.LeftToRight)] [InlineData(TopologyLayoutDirection.RightToLeft)]
    [InlineData(TopologyLayoutDirection.TopToBottom)] [InlineData(TopologyLayoutDirection.BottomToTop)]
    public void LocalCompositionKeepsCrossBoundaryNamedPortsAuthoredWaypointsAndMembership(TopologyLayoutDirection local) {
        var chart=TopologySwimlaneLayoutDepthCausalTests.Chart(TopologyLayoutDirection.LeftToRight,true);
        chart.Groups.Single(group=>group.Id=="work").LayoutDirection=local;chart.AddAutoGroup("peer","Partner").AddAutoNode("d","Deliver",groupId:"peer",width:120,height:56);
        chart.Nodes.Single(node=>node.Id=="a").Ports.Add(new TopologyNodePort {Id="out",Side=TopologyEdgePort.Right,Offset=.25});
        chart.Nodes.Last().Ports.Add(new TopologyNodePort {Id="in",Side=TopologyEdgePort.Left,Offset=.75});
        chart.AddEdge("handoff","a","d");chart.Edges[0].SourcePortId="out";chart.Edges[0].TargetPortId="in";
        chart.AddEdge("authored","b","d");chart.Edges[1].Waypoints.Add(new ChartPoint(700,700));
        var prepared=chart.Prepare(new TopologyRenderOptions {IncludeTitle=false,IncludeLegend=false,SwimlaneIgnoreCrossLaneRankConstraints=true,LayeredRankSpacing=150,LayeredNodeSpacing=80});
        MermaidNestedSwimlaneTests.Capture(prepared,"swimlane-local-cross-boundary-"+local);var saved=prepared.ToInterchangeEnvelope();MermaidNestedSwimlaneTests.AssertContainment(saved);
        var edge=saved.Edges.Single(item=>item.Id=="handoff");Assert.Equal("a",edge.SourceId);Assert.Equal("d",edge.TargetId);
        Assert.Equal("out",edge.SourcePortId);Assert.Equal("in",edge.TargetPortId);
        Assert.Single(saved.Nodes.Single(node=>node.Id=="a").Ports);Assert.Single(saved.Edges.Single(item=>item.Id=="authored").Topology!.Waypoints);
        Assert.Equal(new ChartPoint(700,700),chart.Edges[1].Waypoints[0]);Assert.Equal("work",saved.Nodes.Single(node=>node.Id=="a").GroupId);
        var restored=VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson()).ToTopologyChart();Assert.Equal("out",restored.Edges.Single(item=>item.Id=="handoff").SourcePortId);
    }

    [Fact]
    public void TrueFilterUsesOutermostResponsibilityRetainsIntraHierarchyEdgesAndTerminatesCycles() {
        var chart=TopologySwimlaneLayoutDepthCausalTests.Chart(TopologyLayoutDirection.LeftToRight,true).AddAutoGroup("sibling","Sibling");
        chart.Groups.Last().ParentGroupId="parent";chart.Nodes.Single(node=>node.Id=="c").GroupId="sibling";
        chart.AddEdge("one","a","c").AddEdge("cycle","c","a");chart.Edges[0].MinimumRankSpan=3;
        var saved=chart.Prepare(new TopologyRenderOptions {IncludeTitle=false,IncludeLegend=false,SwimlaneIgnoreCrossLaneRankConstraints=true}).ToInterchangeEnvelope();
        Assert.Equal(saved.Nodes.Single(node=>node.Id=="a").X,saved.Nodes.Single(node=>node.Id=="c").X);Assert.Equal(2,saved.Edges.Count);
        MermaidNestedSwimlaneTests.AssertContainment(saved);
    }
    [Fact]
    public void EffectiveLocalDirectionRejectsPartiallyPositionedSwimlanesInsteadOfPretendingToApplyIt() {
        var chart=TopologySwimlaneLayoutDepthCausalTests.Chart(TopologyLayoutDirection.LeftToRight,true);
        chart.Groups[0].LayoutDirection=TopologyLayoutDirection.TopToBottom;chart.Nodes[0].X=100;chart.Nodes[0].Y=100;
        Assert.Throws<NotSupportedException>(()=>chart.Prepare());
    }

}

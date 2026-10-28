using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Before/control phase uses only incumbent public APIs; capture real output before consequential assertions.</summary>
public sealed class TopologySwimlaneLayoutDepthCausalTests {
    [Theory]
    [InlineData(TopologyLayoutDirection.LeftToRight,false)] [InlineData(TopologyLayoutDirection.RightToLeft,false)]
    [InlineData(TopologyLayoutDirection.TopToBottom,false)] [InlineData(TopologyLayoutDirection.BottomToTop,false)]
    [InlineData(TopologyLayoutDirection.LeftToRight,true)] [InlineData(TopologyLayoutDirection.RightToLeft,true)]
    [InlineData(TopologyLayoutDirection.TopToBottom,true)] [InlineData(TopologyLayoutDirection.BottomToTop,true)]
    public void ExistingSpacingOptionsReachBothNativeSwimlaneOwners(TopologyLayoutDirection direction,bool nested) {
        var chart=Chart(direction,nested);var prepared=chart.Prepare(new TopologyRenderOptions {
            IncludeTitle=false,IncludeLegend=false,LayeredNodeSpacing=70,LayeredRankSpacing=130 });
        MermaidNestedSwimlaneTests.Capture(prepared,"swimlane-spacing-"+direction+"-"+nested);var saved=prepared.ToInterchangeEnvelope();
        var a=saved.Nodes.Single(node=>node.Id=="a");var b=saved.Nodes.Single(node=>node.Id=="b");var c=saved.Nodes.Single(node=>node.Id=="c");
        var horizontal=direction is TopologyLayoutDirection.LeftToRight or TopologyLayoutDirection.RightToLeft;
        Assert.True(horizontal?b.Y!.Value-a.Y!.Value-a.Height!.Value>=69.999:b.X!.Value-a.X!.Value-a.Width!.Value>=69.999,"Authored node gap must reach the actual same-rank cards.");
        var reverse=direction is TopologyLayoutDirection.RightToLeft or TopologyLayoutDirection.BottomToTop;
        var gap=horizontal?(reverse?a.X!.Value-c.X!.Value-c.Width!.Value:c.X!.Value-a.X!.Value-a.Width!.Value)
            :(reverse?a.Y!.Value-c.Y!.Value-c.Height!.Value:c.Y!.Value-a.Y!.Value-a.Height!.Value);
        Assert.True(gap>=129.999,"Authored rank gap must reach actual forward/reversed geometry.");MermaidNestedSwimlaneTests.AssertContainment(saved);
    }

    [Theory]
    [InlineData(TopologyLayoutDirection.LeftToRight)] [InlineData(TopologyLayoutDirection.RightToLeft)]
    [InlineData(TopologyLayoutDirection.TopToBottom)] [InlineData(TopologyLayoutDirection.BottomToTop)]
    public void ExistingLocalGroupDirectionChangesActualCardPlacement(TopologyLayoutDirection local) {
        var chart=Chart(TopologyLayoutDirection.LeftToRight,true);chart.Groups.Single(group=>group.Id=="work").LayoutDirection=local;
        var prepared=chart.Prepare(new TopologyRenderOptions {IncludeTitle=false,IncludeLegend=false});
        MermaidNestedSwimlaneTests.Capture(prepared,"swimlane-local-direction-"+local);var saved=prepared.ToInterchangeEnvelope();
        var a=saved.Nodes.Single(node=>node.Id=="a");var c=saved.Nodes.Single(node=>node.Id=="c");
        var dx=c.X!.Value-a.X!.Value;var dy=c.Y!.Value-a.Y!.Value;
        if(local==TopologyLayoutDirection.LeftToRight)Assert.True(dx>0);else if(local==TopologyLayoutDirection.RightToLeft)Assert.True(dx<0);
        else if(local==TopologyLayoutDirection.TopToBottom)Assert.True(dy>0);else Assert.True(dy<0);
        MermaidNestedSwimlaneTests.AssertContainment(saved);
    }

    [Fact]
    public void SourceCrossLaneRankSettingReachesNativeRankingWithoutDeletingTheHandoff() {
        const string source="---\nconfig:\n  swimlane:\n    ignoreCrossLaneEdges: true\n    optimizeRanksByCrossings: false\n    automaticLaneOrdering: false\n    lineHops: false\n---\nswimlane-beta LR\nsubgraph One[First]\n A[Accept] --> B[Build]\nend\nsubgraph Two[Second]\n C[Check]\nend\nB --> C";
        var result=MermaidRenderer.Render(source);Assert.False(result.HasErrors);var artifact=result.Artifact!;
        MermaidNestedSwimlaneTests.Capture(artifact,"swimlane-source-filtered-rank-before");Assert.Empty(result.Diagnostics);
        var saved=artifact.ToInterchangeEnvelope();var a=saved.Nodes.Single(node=>node.Id=="A");var c=saved.Nodes.Single(node=>node.Id=="C");
        Assert.Equal(a.X,c.X);Assert.Contains(saved.Edges,edge=>edge.SourceId=="B"&&edge.TargetId=="C");
    }

    [Theory]
    [InlineData(false,390)] [InlineData(true,390)] [InlineData(false,1100)] [InlineData(true,1100)]
    public void IncumbentDefaultSceneIsCapturedWithoutNewPolicies(bool dark,int width) {
        var source=MermaidNestedSwimlaneTests.Fixture();var result=MermaidRenderer.Render(source,MermaidNestedSwimlaneTests.Options(dark,width));
        Assert.Empty(result.Diagnostics);MermaidNestedSwimlaneTests.Capture(result.Artifact!,"swimlane-incumbent-default-"+width+"-"+dark);
        MermaidNestedSwimlaneTests.AssertContainment(result.Artifact!.ToInterchangeEnvelope());
    }

    internal static TopologyChart Chart(TopologyLayoutDirection direction,bool nested) {
        var chart=TopologyChart.Create().WithTitle("").WithViewport(1600,1200,24).WithLayout(TopologyLayoutMode.Swimlane,direction).AddAutoGroup("work","Work");
        if(nested){chart.AddAutoGroup("parent","Delivery");chart.Groups[0].ParentGroupId="parent";}
        chart.AddAutoNode("a","Accept",groupId:"work",width:120,height:56).AddAutoNode("b","Browse",groupId:"work",width:120,height:56)
            .AddAutoNode("c","Complete",groupId:"work",width:120,height:56);
        foreach(var node in chart.Nodes){node.DisplayMode=TopologyNodeDisplayMode.Card;node.PreserveDisplayModeSize=true;node.Metadata["layer"]=node.Id=="c"?"1":"0";}
        return chart;
    }
}

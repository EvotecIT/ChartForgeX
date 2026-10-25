using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Incumbent public APIs demonstrate actual feasible rank slack before new production policy is applied.</summary>
public sealed class TopologyRankOptimizationCausalTests {
    [Theory]
    [InlineData(TopologyLayoutDirection.LeftToRight,false)] [InlineData(TopologyLayoutDirection.TopToBottom,false)]
    [InlineData(TopologyLayoutDirection.LeftToRight,true)] [InlineData(TopologyLayoutDirection.TopToBottom,true)]
    public void ManuallyVerifiableIncumbentAndLegalSlackControlsHaveDifferentActualCentrelineObjectives(TopologyLayoutDirection direction,bool move) {
        var chart=Chart(direction,TopologyLayoutMode.Layered);if(move)chart.Nodes.Single(node=>node.Id=="D").Metadata["layer"]="2";
        var prepared=chart.Prepare(new TopologyRenderOptions {IncludeTitle=false,IncludeLegend=false});
        MermaidNestedSwimlaneTests.Capture(prepared,"rank-slack-manual-"+direction+"-"+move);
        Assert.Equal(move?0:1,Crossings(prepared.ToInterchangeEnvelope()));Assert.Equal(2,prepared.ToInterchangeEnvelope().Edges.Count);
    }

    [Theory]
    [InlineData("LR")] [InlineData("RL")] [InlineData("TB")] [InlineData("BT")]
    public void ExplicitSourcePolicyActuallyImprovesALegalPublicNativeRankFixture(string direction) {
        var parsed=MermaidRenderer.Render(Source(direction));Assert.False(parsed.HasErrors);
        var chart=Assert.IsType<TopologyChart>(parsed.Artifact!.Model);SetMinima(chart);var prepared=chart.Prepare();
        MermaidNestedSwimlaneTests.Capture(prepared,"rank-slack-source-opt-in-"+direction);
        Assert.Equal(0,Crossings(prepared.ToInterchangeEnvelope()));Assert.Empty(parsed.Diagnostics);
    }
    internal static string Source(string direction)=>"---\nconfig:\n  swimlane:\n    optimizeRanksByCrossings: true\n    automaticLaneOrdering: false\n    lineHops: false\n---\nswimlane-beta "+direction+"\nA[Accept] --> D[Deliver]\nB[Build] --> C[Check]\nE[Existing rank anchor]";
    internal static TopologyChart Chart(TopologyLayoutDirection direction,TopologyLayoutMode mode) {
        var chart=TopologyChart.Create().WithTitle("").WithViewport(1100,900,24).WithLayout(mode,direction);
        foreach(var id in new[]{"A","B","C","D","E"})chart.AddAutoNode(id,id,width:120,height:56);
        chart.AddEdge("ad","A","D").AddEdge("bc","B","C");SetMinima(chart);return chart;
    }
    internal static void SetMinima(TopologyChart chart) {
        foreach(var node in chart.Nodes){node.Metadata["layer"]=node.Id is "A" or "B"?"0":node.Id=="E"?"2":"1";node.DisplayMode=TopologyNodeDisplayMode.Card;node.PreserveDisplayModeSize=true;}
        // The fixture's stable source order is part of its rank objective, even after source parsing creates D before B.
        chart.Nodes.Sort((a,b)=>Array.IndexOf(new[]{"A","B","C","D","E"},a.Id).CompareTo(Array.IndexOf(new[]{"A","B","C","D","E"},b.Id)));
    }
    internal static int Crossings(VisualArtifactInterchangeEnvelope saved) {
        var a=saved.Nodes.Single(node=>node.Id=="A");var b=saved.Nodes.Single(node=>node.Id=="D");var c=saved.Nodes.Single(node=>node.Id=="B");var d=saved.Nodes.Single(node=>node.Id=="C");
        var ax=a.X!.Value+a.Width!.Value/2;var ay=a.Y!.Value+a.Height!.Value/2;var bx=b.X!.Value+b.Width!.Value/2;var by=b.Y!.Value+b.Height!.Value/2;
        var cx=c.X!.Value+c.Width!.Value/2;var cy=c.Y!.Value+c.Height!.Value/2;var dx=d.X!.Value+d.Width!.Value/2;var dy=d.Y!.Value+d.Height!.Value/2;
        var denominator=(bx-ax)*(dy-cy)-(by-ay)*(dx-cx);if(Math.Abs(denominator)<1e-12)return 0;
        var t=((cx-ax)*(dy-cy)-(cy-ay)*(dx-cx))/denominator;var u=((cx-ax)*(by-ay)-(cy-ay)*(bx-ax))/denominator;
        return t>1e-9&&t<1-1e-9&&u>1e-9&&u<1-1e-9?1:0;
    }
}

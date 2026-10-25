using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Incumbent-public source API proof, applied before automatic ordering production code.</summary>
public sealed class MermaidSwimlaneOrderingCausalTests {
    [Theory]
    [InlineData("LR")] [InlineData("RL")] [InlineData("TB")] [InlineData("BT")]
    public void ExplicitSourceOrderingLowersTheActualWeightedResponsibilityDistance(string direction) {
        var source=Source(direction,true);var result=MermaidRenderer.Render(source);Assert.False(result.HasErrors);
        var artifact=result.Artifact!;MermaidNestedSwimlaneTests.Capture(artifact,"swimlane-ordering-causal-"+direction);
        var order=Order(artifact.ToInterchangeEnvelope(),direction);Assert.True(Cost(order)<12,"A-D three times plus B-C three times has source cost12 and a legal arranged cost6.");
        Assert.Empty(result.Diagnostics);Assert.Equal(new[]{"a","b","c","d"},artifact.ToInterchangeEnvelope().Nodes.Select(node=>node.Id));
    }
    [Theory]
    [InlineData("LR")] [InlineData("RL")] [InlineData("TB")] [InlineData("BT")]
    public void DisabledSourceOrderingKeepsSourceResponsibilityOrder(string direction) {
        var result=MermaidRenderer.Render(Source(direction,false));Assert.Empty(result.Diagnostics);
        MermaidNestedSwimlaneTests.Capture(result.Artifact!,"swimlane-ordering-disabled-"+direction);
        Assert.Equal(new[]{"A","B","C","D"},Order(result.Artifact!.ToInterchangeEnvelope(),direction));
    }
    internal static string Source(string direction,bool enabled)=>"---\nconfig:\n  swimlane:\n    automaticLaneOrdering: "+(enabled?"true":"false")+"\n    optimizeRanksByCrossings: false\n    lineHops: false\n---\nswimlane-beta "+direction+"\nsubgraph A[Alpha]\n a[Accept]\nend\nsubgraph B[Bravo]\n b[Build]\nend\nsubgraph C[Charlie]\n c[Check]\nend\nsubgraph D[Delta]\n d[Deliver]\nend\na --> d\na --> d\na --> d\nb --> c\nb --> c\nb --> c";
    internal static string[] Order(VisualArtifactInterchangeEnvelope saved,string direction)=>saved.Groups.Where(group=>group.Topology!.ParentGroupId==null)
        .OrderBy(group=>direction is "LR" or "RL"?group.Y:group.X).Select(group=>group.Id).ToArray();
    internal static int Cost(string[] order)=>3*Math.Abs(Array.IndexOf(order,"A")-Array.IndexOf(order,"D"))+3*Math.Abs(Array.IndexOf(order,"B")-Array.IndexOf(order,"C"));
}

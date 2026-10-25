using ChartForgeX.Mermaid;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidFlowchartMembershipCausalTests {
    [Theory]
    [InlineData("flowchart", "subgraph Outer[Outer]\nsubgraph Inner[Inner]\nN[Node]\nend\nX --> N\nend", "Inner")]
    [InlineData("swimlane-beta", "subgraph Outer[Outer]\nsubgraph Inner[Inner]\nN[Node]\nend\nX --> N\nend", "Inner")]
    [InlineData("flowchart", "subgraph First[First]\nN[Node]\nend\nsubgraph Second[Second]\nX --> N\nend", "First")]
    [InlineData("swimlane-beta", "subgraph First[First]\nN[Node]\nend\nsubgraph Second[Second]\nX --> N\nend", "First")]
    [InlineData("flowchart", "subgraph Outer[Outer]\nX --> N\nsubgraph Inner[Inner]\nN[Node]\nend\nend", "Inner")]
    [InlineData("swimlane-beta", "subgraph Outer[Outer]\nX --> N\nsubgraph Inner[Inner]\nN[Node]\nend\nend", "Inner")]
    public void FirstClosedPrimarySubgraphOwnsTheNodeDespiteLaterAncestorOrSiblingReferences(string family, string body, string expectedParent) {
        var source = family + " LR\n" + body; var parsed = new MermaidParser().ParseFlowchart(source); Assert.Empty(parsed.Diagnostics);
        var document = parsed.Document!; Assert.Equal(source, document.SourceText);
        var node = document.Nodes.Single(node => node.Id == "N");
        Assert.Equal(expectedParent, node.SubgraphId);
        Assert.Equal(expectedParent, document.ToTopologyChart().Nodes.Single(item => item.Id == "N").GroupId);
    }
}

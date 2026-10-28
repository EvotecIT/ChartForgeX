using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidRepeatedSubgraphCausalTests {
    internal static string Source(string family, string scenario) => family + " LR\n" + (scenario switch {
        "siblings" => "subgraph G[First]\ndirection LR\nA[Alpha]\nend\nsubgraph G[Later]\ndirection BT\nB[Beta]\nend",
        "self" => "subgraph G[Outer]\ndirection LR\nA[Alpha]\nsubgraph G[Inner]\ndirection BT\nB[Beta]\nend\nC[Gamma]\nend",
        "wrapper" => "subgraph G[First]\ndirection LR\nA[Alpha]\nend\nsubgraph Outer[Wrapper]\nsubgraph G[Later]\ndirection BT\nB[Beta]\nend\nC[Gamma]\nend",
        "unique" => "subgraph G[First]\ndirection LR\nA[Alpha]\nend\nsubgraph H[Other]\nB[Beta]\nend",
        _ => throw new ArgumentOutOfRangeException(nameof(scenario))
    });

    [Theory]
    [InlineData("flowchart", "siblings", "First", 2, MermaidFlowchartDirection.LeftToRight, null, 1)]
    [InlineData("swimlane-beta", "siblings", "First", 2, MermaidFlowchartDirection.LeftToRight, null, 1)]
    [InlineData("flowchart", "self", "Inner", 5, MermaidFlowchartDirection.BottomToTop, null, 1)]
    [InlineData("swimlane-beta", "self", "Inner", 5, MermaidFlowchartDirection.BottomToTop, null, 1)]
    [InlineData("flowchart", "wrapper", "First", 2, MermaidFlowchartDirection.LeftToRight, "Outer", 2)]
    [InlineData("swimlane-beta", "wrapper", "First", 2, MermaidFlowchartDirection.LeftToRight, "Outer", 2)]
    [InlineData("flowchart", "unique", "First", 2, MermaidFlowchartDirection.LeftToRight, null, 2)]
    [InlineData("swimlane-beta", "unique", "First", 2, MermaidFlowchartDirection.LeftToRight, null, 2)]
    public void FirstCompletedPrimaryDeclarationOwnsOneNativeGroupAndLaterOccurrencesMergeMembers(string family, string scenario, string title, int line, MermaidFlowchartDirection direction, string? parent, int groupCount) {
        var source = Source(family, scenario); var parsed = new MermaidParser().ParseFlowchart(source); var document = parsed.Document;
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); var path = Path.Combine(output, "repeated-subgraph-causal-" + family + "-" + scenario);
            File.WriteAllText(path + ".source.mmd", source);
            File.WriteAllText(path + ".parse.json", JsonSerializer.Serialize(new { Diagnostics = parsed.Diagnostics.Select(diagnostic => new { diagnostic.Code, diagnostic.Message, diagnostic.Span.Line }),
                Groups = document?.Subgraphs.Select(group => new { group.Id, group.Title, group.Direction, group.ParentSubgraphId, group.Span.Line, group.NodeIds }) }));
            if (!parsed.HasErrors && document != null) {
                var artifact = document.ToTopologyChart().ToVisualArtifact(); File.WriteAllText(path + ".svg", artifact.ToSvg());
                File.WriteAllBytes(path + ".native.png", artifact.ToPng()); File.WriteAllText(path + ".native.json", artifact.ToInterchangeJson());
            }
        }
        Assert.False(parsed.HasErrors, string.Join("; ", parsed.Diagnostics.Select(diagnostic => diagnostic.Message))); Assert.NotNull(document);
        Assert.Equal(source, document.SourceText); Assert.Equal(groupCount, document.Subgraphs.Count);
        var group = Assert.Single(document.Subgraphs, item => item.Id == "G"); Assert.Equal(title, group.Title); Assert.Equal(line, group.Span.Line);
        Assert.Equal(direction, group.Direction); Assert.Equal(parent, group.ParentSubgraphId);
        Assert.Equal("G", document.Nodes.Single(node => node.Id == "A").SubgraphId);
        Assert.Equal(scenario == "unique" ? "H" : "G", document.Nodes.Single(node => node.Id == "B").SubgraphId);
        if (scenario is "self" or "wrapper") Assert.Equal(scenario == "wrapper" ? "Outer" : "G", document.Nodes.Single(node => node.Id == "C").SubgraphId);
        var native = document.ToTopologyChart(); Assert.Equal(groupCount, native.Groups.Count); Assert.Equal(title, native.Groups.Single(item => item.Id == "G").Label);
        Assert.Equal(parent, native.Groups.Single(item => item.Id == "G").ParentGroupId); Assert.DoesNotContain(native.Nodes, node => node.Id == "G");
        Assert.Equal(source.Split('\n').Length - 1, document.Statements.Count);
    }
}

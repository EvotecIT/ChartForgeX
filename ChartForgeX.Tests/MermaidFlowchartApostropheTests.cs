using System.Text.Json;
using ChartForgeX.Mermaid;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidFlowchartApostropheTests {
    [Fact]
    public void VersionedFixturePreservesLiteralNodeAndEdgeLabels() {
        var path = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures", "flowchart-apostrophe-labels.mmd");
        var parsed = new MermaidParser().ParseFlowchart(File.ReadAllText(path));
        Assert.Empty(parsed.Diagnostics);
        using var expected = JsonDocument.Parse(File.ReadAllText(Path.ChangeExtension(path, ".expected.json")));
        var document = parsed.Document!;
        Assert.Equal(expected.RootElement.GetProperty("nodes").EnumerateArray().Select(node => node.GetString()), document.Nodes.Select(node => node.Id));
        foreach (var label in expected.RootElement.GetProperty("nodeLabels").EnumerateObject())
            Assert.Equal(label.Value.GetString(), Assert.Single(document.Nodes, node => node.Id == label.Name).Text ?? label.Name);
        Assert.Equal(expected.RootElement.GetProperty("edges").EnumerateArray().Select(edge => (edge[0].GetString()!, edge[1].GetString()!)),
            document.Edges.Select(edge => (edge.SourceId, edge.TargetId)));
        Assert.Equal(expected.RootElement.GetProperty("edgeLabels").EnumerateArray().Select(label => label.GetString()),
            document.Edges.Select(edge => edge.Label ?? ""));
    }

    [Theory]
    [InlineData("-- 'til complete -->", "'til complete")]
    [InlineData("o-- 'til complete --o", "'til complete")]
    [InlineData("x== 'til complete ==x", "'til complete")]
    [InlineData("<-. 'til complete .->", "'til complete")]
    [InlineData("-- 'go' -->", "'go'")]
    [InlineData("-- don't stop -->", "don't stop")]
    [InlineData("-- \"x --> y; 'til ready\" -->", "x --> y; 'til ready")]
    [InlineData("-->|'til complete|", "'til complete")]
    public void ApostrophesRemainLiteralEdgeCaptionText(string op, string caption) {
        var parsed = new MermaidParser().ParseFlowchart("flowchart LR\nA " + op + " B");
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(new[] { "A", "B" }, parsed.Document!.Nodes.Select(node => node.Id));
        var edge = Assert.Single(parsed.Document.Edges);
        Assert.Equal(("A", "B", caption), (edge.SourceId, edge.TargetId, edge.Label));
        Assert.Equal(caption, Assert.Single(parsed.Document.ToTopologyChart().Edges).Label);
    }

    [Theory]
    [InlineData("['til complete]")]
    [InlineData("(('til complete))")]
    [InlineData("{{'til complete}}")]
    [InlineData("[/'til complete/]")]
    [InlineData("[\\'til complete\\]")]
    public void LeadingApostropheDoesNotHideTheNodeClosingDelimiter(string shape) {
        var parsed = new MermaidParser().ParseFlowchart("flowchart LR\nA" + shape + " --> B");
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(new[] { "A", "B" }, parsed.Document!.Nodes.Select(node => node.Id));
        Assert.Equal("'til complete", parsed.Document.Nodes[0].Text);
        var edge = Assert.Single(parsed.Document.Edges);
        Assert.Equal(("A", "B"), (edge.SourceId, edge.TargetId));
    }

    [Fact]
    public void ApostrophesDoNotConsumeFollowingCompactStatementsOrComments() {
        var source = "flowchart LR\nA -- 'til complete --> B; C['hello] --> D %% retained comment";
        var parsed = new MermaidParser().ParseFlowchart(source);
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(new[] { "A", "B", "C", "D" }, parsed.Document!.Nodes.Select(node => node.Id));
        Assert.Equal(new[] { ("A", "B", "'til complete"), ("C", "D", "") },
            parsed.Document.Edges.Select(edge => (edge.SourceId, edge.TargetId, edge.Label ?? "")));
        Assert.Equal("'hello", parsed.Document.Nodes[2].Text);
    }

    [Fact]
    public void MetadataQuotedBracesAndSemicolonsKeepTheirExistingStatementBoundary() {
        var parsed = new MermaidParser().ParseFlowchart("flowchart LR\nA@{label: \"'til; complete }\", shape: rect} --> B; B --> C");
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(new[] { "A", "B", "C" }, parsed.Document!.Nodes.Select(node => node.Id));
        Assert.Equal("'til; complete }", parsed.Document.Nodes[0].Text);
        Assert.Equal(new[] { ("A", "B"), ("B", "C") }, parsed.Document.Edges.Select(edge => (edge.SourceId, edge.TargetId)));
    }
}

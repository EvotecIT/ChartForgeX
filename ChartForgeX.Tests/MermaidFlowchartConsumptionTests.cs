using ChartForgeX.Mermaid;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidFlowchartConsumptionTests {
    [Theory]
    [InlineData("flowchart LR\nA --> B; B --> C")]
    [InlineData("  flowchart LR; A --> B; B --> C")]
    public void CompactStatementsPreserveAllRelationshipsAndSourceLocations(string source) {
        var result = new MermaidParser().ParseFlowchart(source);
        Assert.False(result.HasErrors);
        Assert.Empty(result.Diagnostics);
        var graph = result.Document!;
        Assert.Equal(MermaidFlowchartDirection.LeftToRight, graph.Direction);
        Assert.Equal(new[] { "A", "B", "C" }, graph.Nodes.Select(node => node.Id));
        Assert.Equal(new[] { ("A", "B"), ("B", "C") }, graph.Edges.Select(edge => (edge.SourceId, edge.TargetId)));
        Assert.Equal("B --> C", graph.Statements.Last().Text);
        Assert.Equal(source.LastIndexOf("B --> C", StringComparison.Ordinal) - source.LastIndexOf('\n') , graph.Statements.Last().Span.Column);
    }

    [Fact]
    public void SemicolonsInsideNodeAndEdgeLabelsArePreserved() {
        var result = new MermaidParser().ParseFlowchart("flowchart LR; A[\"First; second\"] -->|one; two| B; B --> C %% comment");
        Assert.Empty(result.Diagnostics);
        Assert.Equal("First; second", result.Document!.Nodes[0].Text);
        Assert.Equal("one; two", result.Document.Edges[0].Label);
        Assert.Equal(2, result.Document.Edges.Count);
    }

    [Fact]
    public void QuotedClosingDelimitersAndAsymmetricLabelsDoNotConsumeFollowingStatements() {
        var result = new MermaidParser().ParseFlowchart("flowchart LR; A[\"Square ]; label\"] --> B>First; second]; B --> C");
        Assert.Empty(result.Diagnostics);
        Assert.Equal("Square ]; label", result.Document!.Nodes[0].Text);
        Assert.Equal("First; second", result.Document.Nodes[1].Text);
        Assert.Equal(2, result.Document.Edges.Count);
    }

    [Theory]
    [InlineData("/", "/", "Text /] label", MermaidFlowchartNodeShape.Parallelogram)]
    [InlineData("\\", "\\", "Text \\] label", MermaidFlowchartNodeShape.ParallelogramAlt)]
    [InlineData("/", "\\", "Text \\] label", MermaidFlowchartNodeShape.Trapezoid)]
    [InlineData("\\", "/", "Text /] label", MermaidFlowchartNodeShape.TrapezoidAlt)]
    public void QuotedSlashShapeDelimitersRemainLabelContent(string open, string close, string label, MermaidFlowchartNodeShape shape) {
        var source = "flowchart LR; A[" + open + "\"" + label + "\"" + close + "] --> B; B --> C";
        var result = new MermaidParser().ParseFlowchart(source);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(label, result.Document!.Nodes[0].Text);
        Assert.Equal(shape, result.Document.Nodes[0].Shape);
        Assert.Equal(2, result.Document.Edges.Count);
    }

    [Theory]
    [InlineData("flowchart LR\nA[Broken --> B")]
    [InlineData("flowchart LR\nsubgraph G\nA --> B")]
    [InlineData("flowchart LR\nend")]
    [InlineData("flowchart LR\nA -->")]
    [InlineData("flowchart LR\nA >")]
    public void MalformedStatementsProduceLocatedErrors(string source) {
        var result = new MermaidParser().ParseFlowchart(source);
        Assert.True(result.HasErrors);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Severity == MermaidDiagnosticSeverity.Error && diagnostic.Span.Line >= 2 && diagnostic.Span.Column > 0);
    }
}

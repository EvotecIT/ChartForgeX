using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidNotationBoundaryTests {
    [Theory]
    [InlineData("erDiagram", "A ||--o{ B : contains", "A {", "int id PK")]
    [InlineData("erDiagram", "A ||--o{ B : contains", "subgraph Area", "A ||--o{ B : contains")]
    [InlineData("stateDiagram-v2", "A --> B", "state A {", "B --> C")]
    public void UnclosedBoundariesPointToTheirOpeningEvenAfterAnEarlierReference(string header, string prior, string opening, string body) {
        var source = "---\r\nconfig:\r\n  theme: dark\r\n---\r\n" + header + "\r\n" + prior + "\r\n  " + opening + "\r\n" + body;
        var rendered = MermaidRenderer.Render(source);
        Assert.Null(rendered.Artifact);
        var error = Assert.Single(rendered.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidStatement);
        Assert.Equal(7, error.Span.Line);
        Assert.Equal(3, error.Span.Column);
        Assert.Equal(opening.Length, error.Span.Length);
    }

    [Theory]
    [InlineData("erDiagram\n  subgraph Outer\n    subgraph Inner\n      A {\nint id PK", "2,3,4")]
    [InlineData("stateDiagram-v2\n  state Outer {\n    state Inner {\nA --> B", "2,3")]
    public void EachUnclosedNestedBoundaryRetainsItsOwnOpeningLocation(string source, string lines) {
        var rendered = MermaidRenderer.Render(source);
        Assert.Null(rendered.Artifact);
        Assert.Equal(lines, string.Join(",", rendered.Diagnostics.Where(item => item.Code == MermaidDiagnosticCodes.InvalidStatement)
            .Select(item => item.Span.Line).OrderBy(line => line)));
    }

    [Theory]
    [InlineData("LR", TopologyLayoutDirection.LeftToRight)]
    [InlineData("RL", TopologyLayoutDirection.RightToLeft)]
    [InlineData("TB", TopologyLayoutDirection.TopToBottom)]
    [InlineData("BT", TopologyLayoutDirection.BottomToTop)]
    public void DiagramDirectionsReachTheExistingTopologyOwnerWithoutCreatingNodes(string direction, TopologyLayoutDirection expected) {
        foreach (var body in new[] { "classDiagram\nA --> B", "stateDiagram-v2\nA --> B", "erDiagram\nA ||--o{ B : contains" }) {
            var newline = body.IndexOf('\n');
            var source = body.Insert(newline + 1, "direction " + direction + "\n");
            var rendered = MermaidRenderer.Render(source);
            Assert.Empty(rendered.Diagnostics);
            var chart = Assert.IsType<TopologyChart>(rendered.Artifact!.Model);
            Assert.Equal(expected, chart.LayoutDirection);
            Assert.Equal(new[] { "A", "B" }, chart.Nodes.Select(node => node.Id));
            Assert.Single(chart.Edges);
            Assert.NotEmpty(rendered.Artifact.ToSvg());
            Assert.NotEmpty(rendered.Artifact.ToPng());
        }
    }

    [Theory]
    [InlineData("classDiagram\nA --> B", "click A href \"https://example.invalid\" \"Open\"")]
    [InlineData("classDiagram\nA --> B", "note for A \"Keep: A --> Phantom\"")]
    [InlineData("classDiagram\nA --> B", "callback A \"callbackName\"")]
    [InlineData("erDiagram\nA ||--o{ B : contains", "style A fill:#ffeeaa")]
    [InlineData("erDiagram\nA ||--o{ B : contains", "class A marked")]
    [InlineData("erDiagram\nA ||--o{ B : contains", "classDef marked fill:#ffeeaa")]
    public void RetainedNotationReportsItsLimitWithoutInventingGraphFacts(string body, string statement) {
        var rendered = MermaidRenderer.Render(body + "\n  " + statement);
        Assert.False(rendered.HasErrors);
        var chart = Assert.IsType<TopologyChart>(rendered.Artifact!.Model);
        Assert.Equal(new[] { "A", "B" }, chart.Nodes.Select(node => node.Id));
        Assert.Single(chart.Edges);
        var retained = Assert.Single(rendered.Document!.RawStatements);
        Assert.Equal(statement, retained.Text);
        var warning = Assert.Single(rendered.Diagnostics, item => item.Code == MermaidDiagnosticCodes.UnsupportedStatement);
        Assert.Equal(3, warning.Span.Line);
        Assert.Equal(3, warning.Span.Column);
        Assert.Equal(statement.Length, warning.Span.Length);
        Assert.DoesNotContain("<script", rendered.Artifact.ToSvg(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MultilineStateNotesKeepTheirWholeSourceWithoutParsingTheirBodyAsGraphSyntax() {
        const string declaration = "note right of A\r\n  This note: A --> Phantom\nend note";
        var source = "stateDiagram-v2\nA --> B\n" + declaration + "\nB --> C";
        var rendered = MermaidRenderer.Render(source);
        Assert.False(rendered.HasErrors);
        var document = Assert.IsType<MermaidStateDocument>(rendered.Document);
        Assert.Equal(new[] { "A", "B", "C" }, document.States.Select(state => state.Id));
        Assert.Equal(2, document.Transitions.Count);
        var statement = Assert.Single(document.RawStatements);
        Assert.Equal(declaration.Replace("\r\n", "\n"), statement.Text);
        Assert.Equal(declaration, document.SourceText.Substring(source.IndexOf("note right", StringComparison.Ordinal), statement.Span.Length));
        Assert.Equal(6, document.States.Single(state => state.Id == "C").Span.Line);
        Assert.Contains(rendered.Diagnostics, item => item.Code == MermaidDiagnosticCodes.UnsupportedStatement && item.Span.Line == 3);
    }

    [Fact]
    public void ErGroupsAndLocalDirectionsRemainExplicitApproximationWithOnlyRealEntities() {
        const string source = "erDiagram\ndirection LR\nsubgraph Domain\ndirection BT\nA ||--o{ B : contains\nend\nB ||--o{ C : follows";
        var rendered = MermaidRenderer.Render(source);
        Assert.False(rendered.HasErrors);
        var chart = Assert.IsType<TopologyChart>(rendered.Artifact!.Model);
        Assert.Equal(TopologyLayoutDirection.LeftToRight, chart.LayoutDirection);
        Assert.Equal(new[] { "A", "B", "C" }, chart.Nodes.Select(node => node.Id));
        Assert.Equal(2, chart.Edges.Count);
        Assert.Contains(rendered.Diagnostics, item => item.Code == MermaidDiagnosticCodes.UnsupportedStatement && item.Span.Line == 3);
        Assert.Contains(rendered.Diagnostics, item => item.Code == MermaidDiagnosticCodes.UnsupportedStatement && item.Span.Line == 4);
    }

    [Theory]
    [InlineData("classDiagram\ndirection sideways\nA --> B")]
    [InlineData("stateDiagram-v2\ndirection sideways\nA --> B")]
    [InlineData("erDiagram\ndirection sideways\nA ||--o{ B : contains")]
    [InlineData("stateDiagram-v2\nA --> B\nnote right of A\nUnterminated")]
    [InlineData("stateDiagram-v2\nA --> B\n}")]
    [InlineData("stateDiagram-v2\nstate Outer {\nA --> B")]
    [InlineData("erDiagram\nA {\nstring id PK")]
    [InlineData("erDiagram\nA ||--o{ B : contains\n}")]
    [InlineData("stateDiagram-v2\nA --> B\nend note")]
    [InlineData("erDiagram\nsubgraph Domain\nA ||--o{ B : contains")]
    public void InvalidDirectionsAndUnclosedNotationPreventArtifacts(string source) {
        var rendered = MermaidRenderer.Render(source);
        Assert.True(rendered.HasErrors);
        Assert.Null(rendered.Artifact);
        Assert.Contains(rendered.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidStatement && item.Span.Line > 0);
    }

    [Fact]
    public void ScopedStateDirectionsRetainTheirLimitWithoutChangingTheGlobalDirection() {
        var rendered = MermaidRenderer.Render("stateDiagram-v2\ndirection LR\nstate Outer {\ndirection TB\nA --> B\n}");
        Assert.False(rendered.HasErrors);
        Assert.Equal("LR", Assert.IsType<MermaidStateDocument>(rendered.Document).Direction);
        Assert.Equal(TopologyLayoutDirection.LeftToRight, Assert.IsType<TopologyChart>(rendered.Artifact!.Model).LayoutDirection);
        Assert.Contains(rendered.Diagnostics, item => item.Code == MermaidDiagnosticCodes.UnsupportedStatement && item.Span.Line == 4);
    }

    [Fact]
    public void QuotedAndPrefixedIdentifiersRemainOrdinaryEntities() {
        var parsed = new MermaidParser().ParseEntityRelationship("erDiagram\n\"direction LR\"\nstyleguide\nnoteworthy\n\"class\"");
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(new[] { "direction LR", "styleguide", "noteworthy", "class" }, parsed.Document!.Entities.Select(entity => entity.Id));
    }

    [Fact]
    public void LiteralStateNoteContentDoesNotBecomeSharedAccessibilityMetadata() {
        const string declaration = "note right of A\n  accTitle: Literal title\n  accDescr { Literal description }\n  A --> Phantom\nend note %% finished";
        var parsed = new MermaidParser().ParseState("stateDiagram-v2\nA --> B\n" + declaration);
        Assert.False(parsed.HasErrors);
        Assert.Null(parsed.Document!.Accessibility.Name);
        Assert.Null(parsed.Document.Accessibility.Description);
        Assert.Equal(new[] { "A", "B" }, parsed.Document.States.Select(state => state.Id));
        Assert.Single(parsed.Document.Transitions);
        Assert.Equal(declaration, Assert.Single(parsed.Document.RawStatements).Text);
    }
}

using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidNotationCompatibilityTests {
    [Theory]
    [InlineData("Remember this")]
    [InlineData("Remember: A --> Phantom")]
    public void FloatingStateNotesRetainOneLineAndDoNotHideFollowingStatements(string text) {
        var note = "note \"" + text + "\" as N";
        var rendered = MermaidRenderer.Render("stateDiagram-v2\nA --> B\n" + note + "\naccTitle: Real title\nB --> C");
        Assert.False(rendered.HasErrors);
        Assert.NotNull(rendered.Artifact);
        var document = Assert.IsType<MermaidStateDocument>(rendered.Document);
        Assert.Equal(new[] { "A", "B", "C" }, document.States.Select(state => state.Id));
        Assert.Equal(2, document.Transitions.Count);
        Assert.Equal("Real title", document.Accessibility.Name);
        Assert.Equal(note, Assert.Single(document.RawStatements).Text);
        Assert.Equal(MermaidDiagnosticCodes.UnsupportedStatement, Assert.Single(rendered.Diagnostics).Code);
    }

    [Theory]
    [InlineData("lr", TopologyLayoutDirection.LeftToRight)]
    [InlineData("rl", TopologyLayoutDirection.RightToLeft)]
    [InlineData("tb", TopologyLayoutDirection.TopToBottom)]
    [InlineData("bt", TopologyLayoutDirection.BottomToTop)]
    public void StateDirectionsPreserveCaseInsensitiveParsingAndDirectConstruction(string direction, TopologyLayoutDirection expected) {
        var rendered = MermaidRenderer.Render("stateDiagram-v2\ndirection " + direction + "\nA --> B");
        Assert.Empty(rendered.Diagnostics);
        var document = Assert.IsType<MermaidStateDocument>(rendered.Document);
        Assert.Equal(direction.ToUpperInvariant(), document.Direction);
        Assert.Equal(expected, Assert.IsType<TopologyChart>(rendered.Artifact!.Model).LayoutDirection);
        document.Direction = direction;
        Assert.Equal(expected, document.ToTopologyChart().LayoutDirection);
        var er = MermaidRenderer.Render("erDiagram\ndirection " + direction + "\nA ||--o{ B : contains");
        Assert.Empty(er.Diagnostics);
        Assert.Equal(expected, Assert.IsType<TopologyChart>(er.Artifact!.Model).LayoutDirection);
    }

    [Theory]
    [InlineData("direction")]
    [InlineData("note")]
    [InlineData("class")]
    [InlineData("classDef")]
    [InlineData("style")]
    public void BareStateKeywordsRemainStateIdentifiersAtEndOfSource(string id) {
        var parsed = new MermaidParser().ParseState("stateDiagram-v2\nA --> B\n" + id);
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(new[] { "A", "B", id }, parsed.Document!.States.Select(state => state.Id));
        Assert.Empty(parsed.Document.RawStatements);
    }

    [Theory]
    [InlineData("direction")]
    [InlineData("class")]
    [InlineData("classDef")]
    [InlineData("style")]
    [InlineData("subgraph")]
    public void BareErKeywordsRemainEntityIdentifiersAtEndOfSource(string id) {
        var parsed = new MermaidParser().ParseEntityRelationship("erDiagram\nA ||--o{ B : contains\n" + id);
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(new[] { "A", "B", id }, parsed.Document!.Entities.Select(entity => entity.Id));
        Assert.Empty(parsed.Document.RawStatements);
    }
}

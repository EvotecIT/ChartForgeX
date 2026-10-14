using ChartForgeX.Markup;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidSequenceBoundaryTests {
    [Theory]
    [InlineData("sequenceDiagram\nA->>B: request\n  loop retry\nB->>A: response", 3, 3, 10)]
    [InlineData("sequenceDiagram\n  A->>B: request;    end", 2, 22, 3)]
    [InlineData("sequenceDiagram\nloop retry\nA->>B: request\n  else fallback\nB->>A: response\nend", 4, 3, 13)]
    [InlineData("sequenceDiagram\nalt main\nA->>B: request\n  and fallback\nB->>A: response\nend", 4, 3, 12)]
    [InlineData("sequenceDiagram\npar main\nA->>B: request\n  option fallback\nB->>A: response\nend", 4, 3, 15)]
    [InlineData("sequenceDiagram\nA->>B: request\n  else fallback", 3, 3, 13)]
    [InlineData("sequenceDiagram\nalt main\nloop retry\nA->>B: request\n  else fallback\nend\nend", 5, 3, 13)]
    public void InvalidBlockStructureReportsItsLocationAndPreventsDirectAndFenceArtifacts(string source, int line, int column, int length) {
        var parsed = new MermaidParser().ParseSequence(source);
        Assert.True(parsed.HasErrors);
        var error = Assert.Single(parsed.Diagnostics);
        Assert.Equal(MermaidDiagnosticCodes.InvalidStatement, error.Code);
        Assert.Equal(MermaidDiagnosticSeverity.Error, error.Severity);
        Assert.Equal((line, column, length), (error.Span.Line, error.Span.Column, error.Span.Length));

        var rendered = MermaidRenderer.Render(source);
        Assert.True(rendered.HasErrors);
        Assert.Null(rendered.Artifact);
        Assert.Equal(error.Message, Assert.Single(rendered.Diagnostics).Message);

        var fenced = new MermaidVisualMarkupParser().Parse("Intro\n\n```mermaid\n" + source + "\n```");
        Assert.True(fenced.HasErrors);
        Assert.Empty(fenced.Artifacts);
        var fenceError = Assert.Single(fenced.Diagnostics);
        Assert.Equal(MermaidDiagnosticCodes.InvalidStatement, fenceError.Code);
        Assert.Equal(MarkupDiagnosticSeverity.Error, fenceError.Severity);
        Assert.Equal(line + 3, fenceError.Line);
    }

    [Fact]
    public void EachUnclosedNestedBlockPointsToItsOpeningAfterFrontmatterAndEarlierMessages() {
        const string source = "---\r\nconfig:\r\n  theme: dark\r\n---\r\nsequenceDiagram\r\nA->>B: request\r\n  loop retry\r\n    par first\r\nB->>A: response";
        var rendered = MermaidRenderer.Render(source);
        Assert.True(rendered.HasErrors);
        Assert.Null(rendered.Artifact);
        Assert.All(rendered.Diagnostics, error => Assert.Equal(MermaidDiagnosticCodes.InvalidStatement, error.Code));
        Assert.Equal(new[] { (7, 3, 10), (8, 5, 9) }, rendered.Diagnostics
            .OrderBy(error => error.Span.Line).Select(error => (error.Span.Line, error.Span.Column, error.Span.Length)));
    }

    [Fact]
    public void ValidNestedBranchesPreserveTheirOwningBlocksAndMessageRanges() {
        const string source = "sequenceDiagram\n" +
            "alt preferred\n" +
            "loop retry\nA->>B: request\nend\n" +
            "else fallback\n" +
            "par first\nB->>A: first\nand second\nA->>B: second\nend\n" +
            "critical guarded\nB->>A: guarded\noption recover\nA->>B: recovery\nend\nend";
        var rendered = MermaidRenderer.Render(source);
        Assert.Empty(rendered.Diagnostics);
        var sequence = Assert.IsType<SequenceArtifact>(rendered.Artifact!.Model);
        Assert.Equal(new[] { "request", "first", "second", "guarded", "recovery" }, sequence.Messages.Select(message => message.Text));
        Assert.Equal(4, sequence.Blocks.Count);
        var outer = Assert.Single(sequence.Blocks, block => block.Kind == SequenceArtifactBlockKind.Alt);
        Assert.Equal((0, 4, 0), (outer.StartStepIndex, outer.EndStepIndex, outer.Depth));
        Assert.Contains(sequence.Branches, branch => branch.Kind == "Else" && branch.ParentKind == SequenceArtifactBlockKind.Alt
            && branch.StartStepIndex == 1 && branch.EndStepIndex == 4 && branch.Depth == 0);
        Assert.Contains(sequence.Branches, branch => branch.Kind == "And" && branch.ParentKind == SequenceArtifactBlockKind.Par
            && branch.StartStepIndex == 2 && branch.EndStepIndex == 2 && branch.Depth == 1);
        Assert.Contains(sequence.Branches, branch => branch.Kind == "Option" && branch.ParentKind == SequenceArtifactBlockKind.Critical
            && branch.StartStepIndex == 4 && branch.EndStepIndex == 4 && branch.Depth == 1);

        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.Empty(fenced.Diagnostics);
        var fenceSequence = Assert.IsType<SequenceArtifact>(Assert.Single(fenced.Artifacts).Model);
        Assert.Equal(sequence.Branches.Select(branch => (branch.Kind, branch.ParentKind, branch.StartStepIndex, branch.EndStepIndex)),
            fenceSequence.Branches.Select(branch => (branch.Kind, branch.ParentKind, branch.StartStepIndex, branch.EndStepIndex)));
    }
}

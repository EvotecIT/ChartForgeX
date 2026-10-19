using ChartForgeX.Markup;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidMalformedConformanceTests {
    [Theory]
    [InlineData("flowchart-missing-target.mmd", 3, 1, 5)]
    [InlineData("flowchart-unclosed-subgraph.mmd", 2, 1, 16)]
    [InlineData("sequence-orphan-branch.mmd", 3, 1, 13)]
    public void CurrentReferenceRejectedSourcesKeepLocatedErrorsWithoutDirectOrFenceArtifacts(string fixture, int line, int column, int length) {
        var path = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "invalid", fixture);
        var source = File.ReadAllText(path);
        var parsed = new MermaidParser().Parse(source);
        Assert.True(parsed.HasErrors);
        Assert.NotNull(parsed.Document);
        var error = Assert.Single(parsed.Diagnostics);
        Assert.Equal(MermaidDiagnosticSeverity.Error, error.Severity);
        Assert.Equal((line, column, length), (error.Span.Line, error.Span.Column, error.Span.Length));

        var rendered = MermaidRenderer.Render(source);
        Assert.True(rendered.HasErrors);
        Assert.Null(rendered.Artifact);
        var renderError = Assert.Single(rendered.Diagnostics);
        Assert.Equal(error.Span, renderError.Span);
        Assert.Equal(error.Message, renderError.Message);

        var fenced = new MermaidVisualMarkupParser().Parse("Intro\n\n```mermaid\n" + source + "\n```");
        Assert.True(fenced.HasErrors);
        Assert.Empty(fenced.Artifacts);
        var fenceError = Assert.Single(fenced.Diagnostics);
        Assert.Equal(MarkupDiagnosticSeverity.Error, fenceError.Severity);
        Assert.Equal(line + 3, fenceError.Line);
        Assert.Equal(error.Message, fenceError.Message);
    }
}

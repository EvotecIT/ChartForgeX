using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidValidationFidelityTests {
    [Fact]
    public void KanbanColumnsUseTheirRelativeIndentAndRetainTasksInNativeOutput() {
        const string source = "kanban\n  todo[To do]\n    first[First task]\n  done[Done]\n    second[Second task]";
        var parsed = new MermaidParser().ParseKanban(source);
        Assert.False(parsed.HasErrors);
        Assert.Equal(new[] { "todo", "done" }, parsed.Document!.Columns.Select(column => column.Id));
        Assert.Equal(new[] { "first", "second" }, parsed.Document.Columns.SelectMany(column => column.Tasks).Select(task => task.Id));
        var output = MermaidRenderer.Render(source);
        Assert.NotNull(output.Artifact);
        var svg = output.Artifact.ToSvg();
        Assert.Contains("To do", svg);
        Assert.Contains("Done", svg);
        Assert.Contains("First task", svg);
        Assert.Contains("Second task", svg);
        Assert.True(output.Artifact.ToPng().Length > 64);
    }

    [Theory]
    [InlineData("autonumber nope")]
    [InlineData("autonumber 1 nope")]
    [InlineData("autonumber 1 1 trailing")]
    [InlineData("autonumber 79228162514264337593543950335 1")]
    public void InvalidAutonumberStopsDirectAndFenceArtifactsBeforeExport(string declaration) {
        var source = "sequenceDiagram\n  " + declaration + "\nA->>B: First\nB->>A: Second";
        var parsed = new MermaidParser().ParseSequence(source);
        Assert.True(parsed.HasErrors);
        var error = Assert.Single(parsed.Diagnostics);
        Assert.Equal(MermaidDiagnosticCodes.InvalidStatement, error.Code);
        Assert.Equal((2, 3, declaration.Length), (error.Span.Line, error.Span.Column, error.Span.Length));
        var direct = MermaidRenderer.Render(source);
        Assert.True(direct.HasErrors);
        Assert.Null(direct.Artifact);
        var fenced = new MermaidVisualMarkupParser().Parse("Intro\n\n```mermaid\n" + source + "\n```");
        Assert.True(fenced.HasErrors);
        Assert.Empty(fenced.Artifacts);
        Assert.Equal(5, Assert.Single(fenced.Diagnostics).Line);
    }

    [Theory]
    [InlineData("risk", "catastrophic")]
    [InlineData("risk", "")]
    [InlineData("verifymethod", "magic")]
    [InlineData("verifymethod", "")]
    public void RequirementEnumerationsAreValidatedAtTheirSourceField(string field, string value) {
        var source = "requirementDiagram\nrequirement req {\n  " + field + ": " + value + "\n}";
        var parsed = new MermaidParser().ParseRequirement(source);
        Assert.True(parsed.HasErrors);
        var error = Assert.Single(parsed.Diagnostics);
        Assert.Equal(MermaidDiagnosticCodes.InvalidStatement, error.Code);
        Assert.Equal((3, 3), (error.Span.Line, error.Span.Column));
        Assert.Null(MermaidRenderer.Render(source).Artifact);
        Assert.Empty(new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```").Artifacts);
    }

    [Theory]
    [InlineData("style req fill:#ff0000,stroke:#000000")]
    [InlineData("classDef urgent fill:#ff0000")]
    public void RetainedRequirementStylesDiscloseTheirUnchangedNativeAppearance(string style) {
        const string source = "requirementDiagram\nrequirement req {\n  id: 1\n  risk: low\n  verifymethod: test\n}";
        var plain = MermaidRenderer.Render(source);
        var styled = MermaidRenderer.Render(source + "\n" + style);
        Assert.False(styled.HasErrors);
        Assert.Contains(styled.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.UnsupportedStatement);
        Assert.Equal(plain.Artifact!.ToSvg(), styled.Artifact!.ToSvg());
        Assert.Equal(plain.Artifact!.ToPng(), styled.Artifact!.ToPng());
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n" + style + "\n```");
        Assert.Contains(fenced.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.UnsupportedStatement);
    }
}

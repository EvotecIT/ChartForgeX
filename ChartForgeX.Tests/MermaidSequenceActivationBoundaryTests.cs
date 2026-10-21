using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidSequenceActivationBoundaryTests {
    [Theory]
    [InlineData("  deactivate B", 2, 3, 12)]
    [InlineData("A->>-B: Stop", 2, 1, 12)]
    [InlineData("activate B; deactivate B; deactivate B", 2, 27, 12)]
    [InlineData("A->>+B: Begin\nB-->>-A: End\nB-->>-A: Again", 4, 1, 14)]
    public void UnmatchedStandaloneAndInlineDeactivationAreLocatedBeforeDirectAndFenceArtifacts(string body, int line, int column, int length) {
        var source = "sequenceDiagram\n" + body;
        var parsed = new MermaidParser().ParseSequence(source);
        Assert.True(parsed.HasErrors);
        var diagnostic = Assert.Single(parsed.Diagnostics);
        Assert.Equal(MermaidDiagnosticCodes.InvalidStatement, diagnostic.Code);
        Assert.Equal((line, column, length), (diagnostic.Span.Line, diagnostic.Span.Column, diagnostic.Span.Length));
        var direct = MermaidRenderer.Render(source);
        Assert.True(direct.HasErrors);
        Assert.Null(direct.Artifact);
        var fenced = new MermaidVisualMarkupParser().Parse("Intro\n\n```mermaid\n" + source + "\n```");
        Assert.True(fenced.HasErrors);
        Assert.Empty(fenced.Artifacts);
        Assert.Equal(line + 3, Assert.Single(fenced.Diagnostics).Line);
    }

    [Theory]
    [InlineData("activate B\nA->>B: Open activation")]
    [InlineData("activate B; activate B; A->>B: Nested; deactivate B; deactivate B")]
    [InlineData("A->>+B: Begin; B-->>-A: End")]
    [InlineData("create participant B; A->>+B: Create; destroy B; B-->>A: Final")]
    public void ValidOpenNestedInlineAndDestroyedActivationsKeepNativeOutput(string body) {
        var source = "sequenceDiagram\n" + body;
        var parsed = new MermaidParser().ParseSequence(source);
        Assert.Empty(parsed.Diagnostics);
        var direct = MermaidRenderer.Render(source);
        Assert.Empty(direct.Diagnostics);
        Assert.True(direct.Artifact!.ToSvg().Contains("sequence-activation", StringComparison.Ordinal));
        Assert.True(direct.Artifact!.ToPng().Length > 64);
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.Empty(fenced.Diagnostics);
        Assert.True(Assert.Single(fenced.Artifacts).ToPng().Length > 64);
    }
}

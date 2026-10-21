using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Use-case relationship kinds require concrete actors or use cases at both ends.</summary>
public sealed class MermaidUseCaseEndpointTests {
    private const string Prefix = "usecase-beta\nactor User\nactor Admin\nsystemBoundary work[Work]\nA(Do work)\nend\nsystemBoundary other[Other]\nB(Other task)\nend\n";

    [Theory]
    [InlineData("work ..> : include A")]
    [InlineData("A ..> : include work")]
    [InlineData("work ..> : include other")]
    [InlineData("work ..> : extend A")]
    [InlineData("A ..> : extend work")]
    [InlineData("work ..> : extend other")]
    [InlineData("work --|> A")]
    [InlineData("A --|> work")]
    [InlineData("work --|> other")]
    [InlineData("User ..> : include A")]
    [InlineData("A ..> : include User")]
    [InlineData("User ..> : extend A")]
    [InlineData("A ..> : extend User")]
    [InlineData("User --|> A")]
    [InlineData("A --|> User")]
    public void InvalidEndpointKindsProduceLocatedDirectAndFenceDiagnostics(string relation) {
        var source = Prefix + "  " + relation;
        var direct = MermaidRenderer.Render(source);
        Assert.True(direct.HasErrors);
        Assert.Null(direct.Artifact);
        var diagnostic = Assert.Single(direct.Diagnostics);
        Assert.Equal(MermaidDiagnosticCodes.InvalidStatement, diagnostic.Code);
        Assert.Equal((10, 3, relation.Length), (diagnostic.Span.Line, diagnostic.Span.Column, diagnostic.Span.Length));
        var fenced = new MermaidVisualMarkupParser().Parse("Intro\n```mermaid\n" + source + "\n```");
        Assert.Empty(fenced.Artifacts);
        var located = Assert.Single(fenced.Diagnostics);
        Assert.Equal(diagnostic.Code, located.Code);
        Assert.Equal(diagnostic.Message, located.Message);
        Assert.Equal((12, 3), (located.Line, located.Column));
    }

    [Theory]
    [InlineData("A ..> : include B", "A", "B", "<<include>>")]
    [InlineData("A ..> : extend B", "A", "B", "<<extend>>")]
    [InlineData("User --|> Admin", "User", "Admin", "")]
    [InlineData("A --|> B", "A", "B", "")]
    public void ValidKindsRetainNativeRelationshipsAcrossDirectAndFenceExports(string relation, string from, string to, string label) {
        var source = Prefix + "  " + relation;
        var parsed = new MermaidParser().Parse(source);
        Assert.Empty(parsed.Diagnostics);
        var document = Assert.IsType<MermaidUseCaseDocument>(parsed.Document);
        var authored = Assert.Single(document.Edges);
        if (relation.Contains("--|>", StringComparison.Ordinal)) Assert.Equal("--|>", authored.Operator);
        var direct = MermaidRenderer.Render(source);
        Assert.Empty(direct.Diagnostics);
        var artifact = Assert.IsType<VisualArtifact>(direct.Artifact);
        var edge = Assert.Single(artifact.ToInterchangeEnvelope().Edges);
        Assert.Equal((from, to, label), (edge.SourceId, edge.TargetId, edge.Label));
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.Empty(fenced.Diagnostics);
        var fenceArtifact = Assert.Single(fenced.Artifacts);
        Assert.Equal(artifact.ToSvg(), fenceArtifact.ToSvg());
        Assert.Equal(artifact.ToPng(), fenceArtifact.ToPng());
    }
}

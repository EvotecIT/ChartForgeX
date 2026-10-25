using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidEventModelingMetadataBoundaryTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FullRawDocumentRemainsOnTheSourceOwnerWhilePortableSourceMetadataRespectsItsExistingTextBound(bool longComments) {
        var comments = string.Concat(Enumerable.Repeat("%% " + new string('x', 1024) + "\n", longComments ? 70 : 1));
        var source = "eventmodeling\n" + comments + "tf 1 ui Screen\ntf 2 evt Saved";
        var result = MermaidRenderer.Render(source); Assert.False(result.HasErrors);
        Assert.Equal(source, Assert.IsType<MermaidEventModelingDocument>(result.Document).SourceText);
        var artifact = Assert.IsType<VisualArtifact>(result.Artifact);
        var portable = artifact.ToInterchangeEnvelope();
        if (longComments) Assert.False(portable.Extensions.ContainsKey("mermaid.source"));
        else Assert.Equal(source, portable.Extensions["mermaid.source"]);
        Assert.NotEmpty(artifact.ToSvg()); Assert.NotEmpty(artifact.ToPng());
    }

    [Fact]
    public void OversizedLiteralFactsRejectAtTheCapturedResourceBoundaryBeforeReturningAnArtifact() {
        var body = string.Concat(Enumerable.Repeat(new string('x', 600) + "\n", 120));
        var source = "eventmodeling\ntf 1 ui Screen [[Payload]]\ndata Payload `text`{" + body + "}";
        var result = MermaidRenderer.Render(source);
        Assert.True(result.HasErrors); Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.ResourceLimitExceeded
            && diagnostic.Span.Line >= 2 && diagnostic.Span.Column > 0);
    }
}

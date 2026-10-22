using ChartForgeX.Markup;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using System.Xml.Linq;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidHeaderRecognitionTests {
    [Fact]
    public void UpstreamRecognitionFixturesRetainSourceAndNativeArtifacts() {
        var directory = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "recognition");
        var fixtures = Directory.GetFiles(directory, "*.mmd");
        Assert.NotEmpty(fixtures);
        foreach (var path in fixtures) {
            var source = File.ReadAllText(path);
            var result = MermaidRenderer.Render(source);
            Assert.False(result.HasErrors, path);
            Assert.True(result.Document is MermaidAgentflowDocument or MermaidRailroadDocument);
            Assert.NotNull(result.Artifact);
            Assert.Equal(source, result.Document!.SourceText);
            Assert.False(result.Document.IsDiagnosticOnly);
        }
    }

    [Fact]
    public void ZenUmlHeaderSelectsTypedNativeRendering() {
        const string source = "%% retained comment\n  zenuml\n  A.run()";
        var result = MermaidRenderer.Render(source);
        Assert.False(result.HasErrors);
        var document = Assert.IsType<MermaidZenUmlDocument>(result.Document);
        Assert.Equal(source, document.SourceText);
        Assert.False(document.IsDiagnosticOnly);
        Assert.NotNull(result.Artifact);
        Assert.Equal(2, document.HeaderSpan.Line);
        Assert.Equal(3, document.HeaderSpan.Column);
    }

    [Fact]
    public void ZenUmlBodyErrorsAreLocatedInsteadOfRecognitionOnlyWarnings() {
        var result = MermaidRenderer.Render("%% heading\n  zenuml\n  return orphan");
        Assert.True(result.HasErrors);
        Assert.Null(result.Artifact);
        Assert.IsType<MermaidZenUmlDocument>(result.Document);
        var diagnostic = Assert.Single(result.Diagnostics, item => item.Severity == MermaidDiagnosticSeverity.Error);
        Assert.Equal(3, diagnostic.Span.Line);
        Assert.Equal(MermaidDiagnosticCodes.InvalidStatement, diagnostic.Code);
    }

    [Fact]
    public void MarkdownPreservesLocatedZenUmlGrammarErrors() {
        var result = new MermaidVisualMarkupParser().Parse("Text\n\n```mermaid\nzenuml\nreturn orphan\n```");
        Assert.True(result.HasErrors);
        Assert.Empty(result.Artifacts);
        var diagnostic = Assert.Single(result.Diagnostics, item => item.Severity == MarkupDiagnosticSeverity.Error);
        Assert.Equal(5, diagnostic.Line);
        Assert.Equal(MermaidDiagnosticCodes.InvalidStatement, diagnostic.Code);
    }

    [Fact]
    public void ElkHeaderPreservesFlowchartSemanticsAndReportsItsLayoutFallback() {
        var result = MermaidRenderer.Render("flowchart-elk LR\nA --> B");
        Assert.False(result.HasErrors);
        Assert.NotNull(result.Artifact);
        var document = Assert.IsType<MermaidFlowchartDocument>(result.Document);
        Assert.False(document.IsDiagnosticOnly);
        Assert.Equal(MermaidFlowchartDirection.LeftToRight, document.Direction);
        Assert.Equal(new[] { "A", "B" }, document.Nodes.Select(node => node.Id));
        Assert.Equal("A", Assert.Single(document.Edges).SourceId);
        Assert.Equal("B", document.Edges[0].TargetId);
        Assert.Contains(result.Diagnostics, item => item.Code == "CFXM003" && item.Severity == MermaidDiagnosticSeverity.Warning && item.Message.Contains("ELK", StringComparison.Ordinal));
        var svg = XDocument.Parse(result.Artifact!.ToSvg());
        Assert.Single(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "topology-edge");
        Assert.Contains(svg.Descendants(), element => element.Name.LocalName == "text" && element.Value == "A");
        Assert.Contains(svg.Descendants(), element => element.Name.LocalName == "text" && element.Value == "B");
    }

    [Fact]
    public void ElkHeaderRejectsMalformedFlowchartSyntax() {
        var result = MermaidRenderer.Render("flowchart-elk LR\nA -->");
        Assert.True(result.HasErrors);
        Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, item => item.Severity == MermaidDiagnosticSeverity.Error && item.Span.Line == 2);
    }

    [Theory]
    [InlineData("flowchart-elk")]
    [InlineData("flowchart_elk")]
    [InlineData("FLOWCHARTELK")]
    public void EveryAcceptedElkSpellingReportsTheLayoutOnItsHeaderToken(string header) {
        var result = MermaidRenderer.Render("  " + header + " LR; A --> B");
        Assert.False(result.HasErrors);
        Assert.NotNull(result.Artifact);
        var warning = Assert.Single(result.Diagnostics, item => item.Code == "CFXM003");
        Assert.Equal(1, warning.Span.Line);
        Assert.Equal(3, warning.Span.Column);
        Assert.Equal(header.Length, warning.Span.Length);
    }

    [Fact]
    public void UnknownHeadersRemainLocatedErrors() {
        var result = MermaidRenderer.Render("%% heading\n  unsupported-family\nbody");
        Assert.True(result.HasErrors);
        Assert.Null(result.Document);
        Assert.Null(result.Artifact);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("CFXM001", diagnostic.Code);
        Assert.Equal(2, diagnostic.Span.Line);
        Assert.Equal(3, diagnostic.Span.Column);
    }
}

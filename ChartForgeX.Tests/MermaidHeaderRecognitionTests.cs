using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using System.Xml.Linq;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidHeaderRecognitionTests {
    [Fact]
    public void UpstreamRecognitionFixturesRetainTheirSourceWithoutArtifacts() {
        var directory = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "recognition");
        var fixtures = Directory.GetFiles(directory, "*.mmd");
        Assert.NotEmpty(fixtures);
        foreach (var path in fixtures) {
            var source = File.ReadAllText(path);
            var result = MermaidRenderer.Render(source);
            Assert.False(result.HasErrors, path);
            Assert.Null(result.Artifact);
            Assert.True(result.Document!.IsDiagnosticOnly);
            Assert.Equal(source, result.Document.SourceText);
            Assert.NotEmpty(result.Document.RawStatements);
            Assert.Equal("CFXM002", Assert.Single(result.Diagnostics).Code);
        }
    }

    [Theory]
    [InlineData("agentflow-beta", "Agentflow")]
    [InlineData("railroad-beta", "Railroad")]
    [InlineData("railroad-ebnf-beta", "Railroad")]
    [InlineData("railroad-abnf-beta", "Railroad")]
    [InlineData("railroad-peg-beta", "Railroad")]
    [InlineData("zenuml", "ZenUml")]
    public void RecognizedFamiliesRetainSourceAndReportMissingRendering(string header, string kind) {
        var source = "%% retained comment\n  " + header + "\n  retained body";
        var result = MermaidRenderer.Render(source);
        Assert.False(result.HasErrors);
        Assert.Null(result.Artifact);
        var document = Assert.IsType<MermaidDocument>(result.Document);
        Assert.Equal(kind, document.Kind.ToString());
        Assert.Equal(source, document.SourceText);
        Assert.Equal("retained body", Assert.Single(document.RawStatements).Text);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.True(document.IsDiagnosticOnly);
        Assert.Equal("CFXM002", diagnostic.Code);
        Assert.Equal(MermaidDiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal(2, diagnostic.Span.Line);
        Assert.Equal(3, diagnostic.Span.Column);
        Assert.Equal(header.Length, diagnostic.Span.Length);
    }

    [Theory]
    [InlineData("agentflow-beta")]
    [InlineData("railroad-ebnf-beta")]
    public void MarkdownPreservesRecognizedFamilyWarningsWithoutConversionErrors(string header) {
        var result = new MermaidVisualMarkupParser().Parse("Text\n\n```mermaid\n" + header + "\nbody\n```");
        Assert.False(result.HasErrors);
        Assert.Empty(result.Artifacts);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(4, diagnostic.Line);
        Assert.Equal("CFXM002", diagnostic.Code);
        Assert.Equal(MermaidDiagnosticSeverity.Warning, diagnostic.Severity);
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

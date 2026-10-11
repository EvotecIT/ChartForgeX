using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidConfigurationStreamBoundaryTests {
    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void OversizedClosedDirectivesDoNotHideTheRealHeaderOrFollowingSource(string newline) {
        var source = "%%{init: {\n\"fontFamily\":\"" + new string('x', 65536) + "\"\n}}%%\n"
            + "%%{init: {\"theme\":\"dark\"}}%%\nflowchart LR\nA --> B";
        source = source.Replace("\n", newline);
        var result = MermaidRenderer.Render(source);
        Assert.True(result.HasErrors); Assert.Null(result.Artifact);
        var document = Assert.IsType<MermaidFlowchartDocument>(result.Document);
        Assert.Equal(source, document.SourceText);
        Assert.Equal(5, document.HeaderSpan.Line);
        var error = Assert.Single(result.Diagnostics);
        Assert.Equal(MermaidDiagnosticCodes.InvalidConfiguration, error.Code);
        Assert.Equal(1, error.Span.Line);
        Assert.Equal(source.IndexOf(newline + "%%{init:", StringComparison.Ordinal), error.Span.Length);
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.True(fenced.HasErrors); Assert.Empty(fenced.Artifacts);
        Assert.DoesNotContain(fenced.Diagnostics, item => item.Code == MermaidDiagnosticCodes.UnknownDiagram);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExhaustedAggregateBudgetStillConsumesLaterMultilineDeclarations(bool frontmatterOverBudget) {
        var prefix = frontmatterOverBudget
            ? "---\nconfig:\n  other: " + new string('x', 65536) + "\n---\n"
            : "%%{init: {'other':'" + new string('x', 65500) + "'}}%%\n";
        var source = prefix + "%%{init: {\n\"theme\":\"dark\"\n}}%%\nflowchart LR\nA --> B";
        var result = MermaidRenderer.Render(source);
        Assert.True(result.HasErrors); Assert.Null(result.Artifact);
        var document = Assert.IsType<MermaidFlowchartDocument>(result.Document);
        Assert.Equal(source, document.SourceText);
        Assert.Equal(frontmatterOverBudget ? 8 : 5, document.HeaderSpan.Line);
        Assert.Single(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidConfiguration);
        Assert.DoesNotContain(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.UnknownDiagram);
    }

    [Fact]
    public void ForbiddenLiteralYamlRangesAreRejectedInPlainScalars() {
        foreach (var literal in new[] { "\u0000", "\u001f", "\u007f", "\u0080", "\u0084", "\u0086", "\u009f", "\ud800", "\udfff", "\ufffe", "\uffff" }) {
            var source = Source("plain", "Arial" + literal + "serif");
            var result = MermaidRenderer.Render(source);
            Assert.True(result.HasErrors, "Plain scalar accepted forbidden literal U+" + ((int)literal[0]).ToString("X4"));
            Assert.Null(result.Artifact);
            var error = Assert.Single(result.Diagnostics);
            Assert.Equal(MermaidDiagnosticCodes.InvalidConfiguration, error.Code);
            Assert.Equal(3, error.Span.Line);
        }
    }

    [Theory]
    [InlineData("single")]
    [InlineData("double")]
    [InlineData("flow")]
    public void QuotedScalarsPreserveNonC0CharactersRequiredForJsonCompatibility(string syntax) {
        foreach (var literal in new[] { "\u007e", "\u007f", "\u0080", "\u0084", "\u0085", "\u0086", "\u009f", "\u00a0", "\ud7ff", "\ue000", "\ufffd", "\ud83d\ude80" }) {
            var value = "Arial" + literal + "serif";
            var result = new MermaidParser().ParseFlowchart(Source(syntax, value));
            Assert.Empty(result.Diagnostics);
            Assert.Equal(value, result.Document!.Configuration.FontFamily);
        }
    }

    [Fact]
    public void PlainScalarsPreservePrintableUnicodeBoundariesAndSupplementaryCharacters() {
        foreach (var literal in new[] { "\u007e", "\u0085", "\u00a0", "\ud7ff", "\ue000", "\ufffd", "\ud83d\ude80" }) {
            var value = "Arial" + literal + "serif";
            var result = new MermaidParser().ParseFlowchart(Source("plain", value));
            Assert.Empty(result.Diagnostics);
            Assert.Equal(value, result.Document!.Configuration.FontFamily);
        }
    }

    private static string Source(string syntax, string value) => "---\n" + (syntax == "flow"
        ? "config: {\"fontFamily\":\"" + value + "\"}"
        : "config:\n  fontFamily: " + (syntax == "single" ? "'" + value + "'" : syntax == "double" ? "\"" + value + "\"" : value))
        + "\n---\nflowchart LR\nA --> B";
}

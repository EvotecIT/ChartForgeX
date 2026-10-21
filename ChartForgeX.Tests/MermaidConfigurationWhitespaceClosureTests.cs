using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidConfigurationWhitespaceClosureTests {
    [Theory]
    [InlineData("\u000bArial")]
    [InlineData("Arial\u000b")]
    [InlineData("\u000cArial")]
    [InlineData("Arial\u000c")]
    public void ScalarEdgeControlsCannotDisappearDuringYamlNormalization(string value) {
        var result = MermaidRenderer.Render(Yaml("fontFamily: " + value));
        Assert.True(result.HasErrors); Assert.Null(result.Artifact);
        var error = Assert.Single(result.Diagnostics);
        Assert.Equal(MermaidDiagnosticCodes.InvalidConfiguration, error.Code);
        Assert.Equal(3, error.Span.Line);
    }

    [Theory]
    [InlineData("\u00a0Arial\u00a0")]
    [InlineData("\u0085Arial\u0085")]
    [InlineData("Arial:\u00a0serif")]
    [InlineData("Arial:\u0085serif")]
    [InlineData("Arial\u00a0#serif")]
    [InlineData("Arial\u0085#serif")]
    public void NonSeparatingUnicodeRemainsPartOfPlainYamlScalars(string value) {
        var result = new MermaidParser().ParseFlowchart(Yaml("fontFamily: " + value));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(value, result.Document!.Configuration.FontFamily);
    }

    [Theory]
    [InlineData("O'Reilly")]
    [InlineData("Arial\"serif")]
    public void InteriorPlainQuotesDoNotHideTrailingYamlComments(string value) {
        var result = new MermaidParser().ParseFlowchart(Yaml("fontFamily: " + value + " # local fallback"));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(value, result.Document!.Configuration.FontFamily);
    }

    [Theory]
    [InlineData("\u000b")]
    [InlineData("\u000c")]
    [InlineData("\u00a0")]
    public void JsonConfigurationRejectsNonJsonWhitespaceOutsideStrings(string whitespace) {
        var payload = "{\"fontFamily\":" + whitespace + "\"Arial\"}";
        foreach (var source in new[] { "%%{init: " + payload + "}%%\nflowchart LR\nA --> B", "---\nconfig: " + payload + "\n---\nflowchart LR\nA --> B" }) {
            var result = MermaidRenderer.Render(source);
            Assert.True(result.HasErrors); Assert.Null(result.Artifact);
            Assert.Single(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidConfiguration);
            Assert.IsType<MermaidFlowchartDocument>(result.Document);
        }
    }

    [Fact]
    public void SharedJsonReaderUsesOnlyJsonWhitespaceWithoutRestrictingStringCharacters() {
        foreach (var whitespace in new[] { " ", "\t", "\n", "\r" })
            Assert.Equal("Arial", GeoJsonValue.Parse(whitespace + "{\"font\":" + whitespace + "\"Arial\"}" + whitespace).AsObject("font")["font"].AsString("font"));
        foreach (var character in new[] { "\u000b", "\u000c", "\u0085", "\u00a0" })
            Assert.Throws<ArgumentException>(() => GeoJsonValue.Parse("[1," + character + "2]"));
        Assert.Equal("Arial\u00a0serif", GeoJsonValue.Parse("\"Arial\u00a0serif\"").AsString("font"));
    }

    [Fact]
    public void LargePhysicalRootFlowPayloadRetainsBudgetFailureAndRealDiagramSource() {
        var source = "---\nconfig: {\"other\":\"" + new string('x', 1_000_000) + "\"}\n---\nflowchart LR\nA --> B";
        var result = MermaidInnerBoundaryTestPolicy.Parse(source);
        var error = Assert.Single(result.Diagnostics);
        Assert.Equal(MermaidDiagnosticCodes.InvalidConfiguration, error.Code);
        Assert.Contains("65536", error.Message);
        Assert.Equal(source, result.Document!.SourceText);
        Assert.Equal(4, result.Document.HeaderSpan.Line);
    }

    private static string Yaml(string setting) => "---\nconfig:\n  " + setting + "\n---\nflowchart LR\nA --> B";
}

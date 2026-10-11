using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidConfigurationSyntaxTests {
    [Theory]
    [InlineData("1e9999")]
    [InlineData("-1e9999")]
    [InlineData("01")]
    [InlineData("-01")]
    public void JsonNumberLimitsAreConsistentAcrossConfigurationEntryPoints(string value) {
        Assert.Throws<ArgumentException>(() => GeoJsonValue.Parse("{\"unknown\":" + value + "}"));
        foreach (var declaration in new[] {
            "%%{init: {'unknown': " + value + "}}%%\n",
            "%%{initialize: {\"flowchart\":{\"unknown\":" + value + "}}}%%\n",
            "---\nconfig: {\"unknown\":" + value + "}\n---\n"
        }) {
            var rendered = MermaidRenderer.Render(declaration + "flowchart LR\nA --> B");
            Assert.True(rendered.HasErrors);
            Assert.Null(rendered.Artifact);
            Assert.Contains(rendered.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.InvalidConfiguration);
        }
    }

    [Theory]
    [InlineData("0", 0.0)]
    [InlineData("-0", 0.0)]
    [InlineData("0.5", 0.5)]
    [InlineData("1e-9999", 0.0)]
    [InlineData("1e308", 1e308)]
    public void ValidJsonNumbersKeepTheExistingSharedReaderContract(string value, double expected) {
        Assert.Equal(expected, GeoJsonValue.Parse(value).AsNumber("number"));
        var parsed = new MermaidParser().Parse("%%{init: {'unknown': " + value + "}}%%\nflowchart LR\nA --> B");
        Assert.False(parsed.HasErrors);
        Assert.False(Assert.Single(parsed.Document!.Configuration.Settings).IsString);
    }

    [Theory]
    [InlineData("'dark'junk'")]
    [InlineData("'Arial's'")]
    [InlineData("'a'b'c'")]
    public void SingleQuotedYamlRejectsUnescapedInteriorQuotes(string value) {
        var rendered = MermaidRenderer.Render("---\nconfig:\n  fontFamily: " + value + "\n---\nflowchart LR\nA --> B");
        Assert.True(rendered.HasErrors);
        Assert.Null(rendered.Artifact);
        Assert.Contains(rendered.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.InvalidConfiguration);
    }

    [Theory]
    [InlineData("'Arial''s, serif'", "Arial's, serif")]
    [InlineData("'''Arial'''", "'Arial'")]
    [InlineData("'a''''b'", "a''b")]
    [InlineData("'Arial # serif' # comment", "Arial # serif")]
    [InlineData("'Arial\\serif'", "Arial\\serif")]
    public void SingleQuotedYamlKeepsDoubledQuotesCommentsAndLiteralBackslashes(string value, string expected) {
        var rendered = MermaidRenderer.Render("---\nconfig:\n  fontFamily: " + value + "\n---\nflowchart LR\nA --> B");
        Assert.Empty(rendered.Diagnostics);
        Assert.Equal(expected, rendered.Document!.Configuration.FontFamily);
        Assert.NotNull(rendered.Artifact);
    }
}

using System.Globalization;
using ChartForgeX.Mermaid;
using ChartForgeX.Markup.Mermaid;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidConfigurationScalarProvenanceTests {
    [Theory]
    [InlineData("]dark")]
    [InlineData("}dark")]
    [InlineData(",dark")]
    [InlineData("@dark")]
    [InlineData("`dark")]
    [InlineData("%dark")]
    [InlineData("- dark")]
    [InlineData("? dark")]
    [InlineData(":dark: ")]
    public void ForbiddenPlainScalarIndicatorsProduceLocatedErrors(string value) {
        var result = MermaidRenderer.Render("---\nconfig:\n  theme: " + value + "\n---\nflowchart LR\nA --> B");
        Assert.True(result.HasErrors);
        Assert.Null(result.Artifact);
        var error = Assert.Single(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidConfiguration);
        Assert.Equal(3, error.Span.Line);
    }

    [Theory]
    [InlineData("-dark")]
    [InlineData("?dark")]
    [InlineData(":dark")]
    public void NonSeparatingPlainIndicatorsAndQuotedReservedCharactersRemainStrings(string value) {
        var parsed = new MermaidParser().Parse("---\nconfig:\n  other: " + value + "\n  fontFamily: ']},@` Arial'\n---\nflowchart LR\nA --> B");
        Assert.False(parsed.HasErrors);
        Assert.Equal(value, parsed.Document!.Configuration.Settings.Single(item => item.Path == "other").Value);
        Assert.Equal("]},@` Arial", parsed.Document.Configuration.FontFamily);
    }

    [Theory]
    [InlineData("1e-20")]
    [InlineData("0.12345678901234567")]
    [InlineData("-0.000000000000000000000000000001")]
    public void JsonNumbersRetainTheirAuthoredFiniteSpellingAcrossBothConfigurationRoutes(string number) {
        foreach (var source in new[] {
            "%%{init: {'other':" + number + ", 'nested':{'other':" + number + "}}}%%\nflowchart LR\nA --> B",
            "---\nconfig: {\"other\":" + number + ", \"nested\":{\"other\":" + number + "}}\n---\nflowchart LR\nA --> B"
        }) {
            var parsed = new MermaidParser().Parse(source);
            Assert.False(parsed.HasErrors);
            var settings = parsed.Document!.Configuration.Settings.Where(item => item.Path == "other" || item.Path == "nested.other").ToArray();
            Assert.Equal(2, settings.Length);
            foreach (var setting in settings) {
                Assert.False(setting.IsString);
                Assert.Equal(number, setting.Value);
                Assert.Equal(double.Parse(number, CultureInfo.InvariantCulture), double.Parse(setting.Value!, CultureInfo.InvariantCulture));
            }
        }
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void MultilineRootFlowPropertiesKeepTheirOriginalSpansAndMarkdownOffsets(string newline) {
        var source = string.Join(newline, new[] {
            "---", "config: { 'other': 'a\"b',", "", "  # skipped comment",
            "  'fontFamily': null,", "  'flowchart': {", "    'look': 'classic'", "  }", "}",
            "title: Location proof", "---", "flowchart LR", "A --> B"
        });
        var rendered = MermaidRenderer.Render(source);
        Assert.True(rendered.HasErrors);
        Assert.Null(rendered.Artifact);
        var setting = rendered.Document!.Configuration.Settings.Single(item => item.Path == "fontFamily");
        Assert.Equal(5, setting.Span.Line);
        Assert.Equal(3, setting.Span.Column);
        Assert.Equal("'fontFamily': null".Length, setting.Span.Length);
        Assert.Equal(setting.Span, Assert.Single(rendered.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidConfiguration).Span);
        var nested = rendered.Document.Configuration.Settings.Single(item => item.Path == "flowchart.look");
        Assert.Equal(7, nested.Span.Line);
        Assert.Equal(5, nested.Span.Column);
        Assert.Equal("'look': 'classic'".Length, nested.Span.Length);
        var fenced = new MermaidVisualMarkupParser().Parse("Intro\n\n```mermaid\n" + source + "\n```");
        Assert.Equal(8, Assert.Single(fenced.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidConfiguration).Line);
    }

    [Fact]
    public void SameLineFlowPropertiesKeepTheirOwnColumnsAfterQuoteNormalization() {
        const string source = "---\nconfig: {'other':'a\"b', 'fontFamily':null}\n---\nflowchart LR\nA --> B";
        var result = MermaidRenderer.Render(source);
        Assert.True(result.HasErrors);
        var setting = result.Document!.Configuration.Settings.Single(item => item.Path == "fontFamily");
        Assert.Equal(2, setting.Span.Line);
        Assert.Equal(source.Split('\n')[1].IndexOf("'fontFamily'", StringComparison.Ordinal) + 1, setting.Span.Column);
        Assert.Equal("'fontFamily':null".Length, setting.Span.Length);
    }
}

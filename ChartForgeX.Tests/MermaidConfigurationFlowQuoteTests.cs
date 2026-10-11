using ChartForgeX.Mermaid;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidConfigurationFlowQuoteTests {
    [Theory]
    [InlineData("'Arial''s, serif'", "Arial's, serif")]
    [InlineData("'Arial\\tserif'", "Arial\\tserif")]
    [InlineData("'Arial\\serif'", "Arial\\serif")]
    [InlineData("'Arial''s # serif'", "Arial's # serif")]
    [InlineData("'Arial\\'", "Arial\\")]
    [InlineData("'Arial\tserif'", "Arial\tserif")]
    public void RootFlowSingleQuotesUseYamlEscapingAndPreservePropertySpans(string literal, string expected) {
        var declaration = "'fontFamily': " + literal;
        var source = "---\nconfig: {'other':'a\"b',\n  " + declaration + ",\n  'theme':'dark'\n} # trailing } comment\ntitle: Frontmatter stays separate\n---\nflowchart LR\nA --> B";
        var parsed = new MermaidParser().Parse(source);
        Assert.False(parsed.HasErrors);
        Assert.Equal(expected, parsed.Document!.Configuration.FontFamily);
        Assert.Equal("dark", parsed.Document.Configuration.Theme);
        var setting = parsed.Document.Configuration.Settings.Single(item => item.Path == "fontFamily");
        Assert.Equal(3, setting.Span.Line);
        Assert.Equal(3, setting.Span.Column);
        Assert.Equal(declaration.Length, setting.Span.Length);
        Assert.Equal(8, parsed.Document.HeaderSpan.Line);
        Assert.Contains("title: Frontmatter stays separate", parsed.Document.FrontMatter);
    }

    [Fact]
    public void RootFlowDoesNotAcceptLegacyBackslashEscapedApostrophes() {
        var result = MermaidRenderer.Render("---\nconfig: {'fontFamily':'Arial\\'s'} # comment\n---\nflowchart LR\nA --> B");
        Assert.True(result.HasErrors);
        Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidConfiguration);
    }

    [Theory]
    [InlineData("\"Arial\tserif\"", "Arial\tserif")]
    [InlineData("\"Arial\\\tserif\"", "Arial\tserif")]
    [InlineData("\"Arial\\\\\tserif\"", "Arial\\\tserif")]
    public void RootFlowDoubleQuotedTabsMatchBlockYamlAndPreservePropertySpans(string literal, string expected) {
        var declaration = "\"fontFamily\":" + literal;
        var parsed = new MermaidParser().Parse("---\nconfig: {\n  " + declaration + "\n}\n---\nflowchart LR\nA --> B");
        Assert.False(parsed.HasErrors);
        Assert.Equal(expected, parsed.Document!.Configuration.FontFamily);
        var setting = Assert.Single(parsed.Document.Configuration.Settings);
        Assert.Equal(new MermaidSourceSpan(3, 3, declaration.Length), setting.Span);
    }

    [Fact]
    public void LegacyDirectiveRejectsBackslashEscapedLiteralTabs() {
        var result = MermaidRenderer.Render("%%{init: {\"fontFamily\":\"Arial\\\tserif\"}}%%\nflowchart LR\nA --> B");
        Assert.True(result.HasErrors);
        Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidConfiguration);
    }

    [Fact]
    public void LegacyDirectiveSingleQuotesKeepTheirExistingJsonEscapeSemantics() {
        var parsed = new MermaidParser().Parse("%%{init: {'fontFamily':'Arial\\'s \\tserif'}}%%\nflowchart LR\nA --> B");
        Assert.False(parsed.HasErrors);
        Assert.Equal("Arial's \tserif", parsed.Document!.Configuration.FontFamily);
    }

    [Fact]
    public void FlowPropertyColumnsRemainOriginalAfterDoubledApostropheContraction() {
        const string source = "---\nconfig: {'other':'a''b', 'fontFamily':null}\n---\nflowchart LR\nA --> B";
        var result = MermaidRenderer.Render(source);
        Assert.True(result.HasErrors);
        var setting = result.Document!.Configuration.Settings.Single(item => item.Path == "fontFamily");
        Assert.Equal(2, setting.Span.Line);
        Assert.Equal(source.Split('\n')[1].IndexOf("'fontFamily'", StringComparison.Ordinal) + 1, setting.Span.Column);
        Assert.Equal("'fontFamily':null".Length, setting.Span.Length);
        Assert.Equal(setting.Span, Assert.Single(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidConfiguration).Span);
    }
}

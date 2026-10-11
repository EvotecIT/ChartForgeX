using ChartForgeX.Mermaid;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidConfigurationNullAndScalarTests {
    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData("\r")]
    public void EmptyUnsupportedYamlKeysRetainTheirValuesAndOriginalLocations(string newline) {
        var source = string.Join(newline, new[] {
            "---", "config:", "  htmlLabels:", "  unknown:", "    nested:", "  theme: dark", "  trailing:", "    # no child value", "---", "flowchart LR", "A --> B"
        });
        var rendered = MermaidRenderer.Render(source);
        Assert.False(rendered.HasErrors);
        Assert.NotNull(rendered.Artifact);
        var settings = rendered.Document!.Configuration.Settings;
        foreach (var (path, line, column) in new[] { ("htmlLabels", 3, 3), ("unknown.nested", 5, 5), ("trailing", 7, 3) }) {
            var setting = Assert.Single(settings, item => item.Path == path);
            Assert.Null(setting.Value);
            Assert.False(setting.IsString);
            Assert.Equal(line, setting.Span.Line);
            Assert.Equal(column, setting.Span.Column);
            Assert.Contains(rendered.Diagnostics, item => item.Code == MermaidDiagnosticCodes.UnsupportedConfiguration && item.Span.Line == line);
        }
        Assert.Equal(4, settings.Count);
        Assert.Equal("dark", rendered.Document.Configuration.Theme);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void RootFlowMappingCommentsDoNotConsumeTheConfigurationBudget(string indentation) {
        var comment = indentation + "# " + new string('x', 70000);
        var rendered = MermaidRenderer.Render("---\nconfig: {\n" + comment + "\n  \"theme\": \"dark\",\n" + comment + "\n  \"fontFamily\": \"Arial\"\n}\n---\nflowchart LR\nA --> B");
        Assert.Empty(rendered.Diagnostics);
        Assert.NotNull(rendered.Artifact);
        Assert.Equal("Arial", rendered.Document!.Configuration.FontFamily);
        Assert.Contains(comment, rendered.Document.FrontMatter);
    }

    [Theory]
    [InlineData("Georgia")]
    [InlineData("")]
    [InlineData("{wrong: value}")]
    public void UnsupportedFamilyFontDoesNotOverrideTheRootOrSuppressItsDiagnostic(string value) {
        var rendered = MermaidRenderer.Render("---\nconfig:\n  fontFamily: Arial\n  flowchart:\n    fontFamily: " + value + "\n---\nflowchart LR\nA --> B");
        Assert.False(rendered.HasErrors);
        Assert.NotNull(rendered.Artifact);
        Assert.Equal("Arial", rendered.Document!.Configuration.FontFamily);
        Assert.Contains(rendered.Document.Configuration.Settings, item => item.Path == "flowchart.fontFamily");
        Assert.Contains(rendered.Diagnostics, item => item.Code == MermaidDiagnosticCodes.UnsupportedConfiguration && item.Span.Line == 5);
    }

    [Theory]
    [InlineData("Arial: serif")]
    [InlineData("Arial:")]
    [InlineData("Arial:\tserif")]
    public void PlainYamlMappingSeparatorsAreLocatedConfigurationErrors(string value) {
        var rendered = MermaidRenderer.Render("---\nconfig:\n  fontFamily: " + value + "\n---\nflowchart LR\nA --> B");
        Assert.True(rendered.HasErrors);
        Assert.Null(rendered.Artifact);
        Assert.Contains(rendered.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidConfiguration && item.Span.Line == 3);
    }

    [Theory]
    [InlineData("'Arial: serif'", "Arial: serif")]
    [InlineData("\"Arial: serif\"", "Arial: serif")]
    [InlineData("https://example.test/font", "https://example.test/font")]
    [InlineData("12:30", "12:30")]
    public void QuotedMappingSeparatorsAndPlainNonSeparatorsRemainStrings(string value, string expected) {
        var parsed = new MermaidParser().Parse("---\nconfig:\n  fontFamily: " + value + "\n---\nflowchart LR\nA --> B");
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(expected, parsed.Document!.Configuration.FontFamily);
    }

    [Fact]
    public void ManyFlowCommentsCannotTruncateAnOtherwiseValidConfigurationNearTheBudget() {
        var source = "---\nconfig: {\n\"other\":\"" + new string('x', 65500) + "\"\n" +
            string.Concat(Enumerable.Repeat("# comment\n", 26)) + "}\n---\nflowchart LR\nA --> B";
        var rendered = MermaidRenderer.Render(source);
        Assert.False(rendered.HasErrors);
        Assert.NotNull(rendered.Artifact);
        Assert.Equal(new string('x', 65500), Assert.Single(rendered.Document!.Configuration.Settings).Value);
    }

    [Theory]
    [InlineData("\tArial", "Arial")]
    [InlineData("Arial # comment\t", "Arial")]
    [InlineData("'Arial\tserif'", "Arial\tserif")]
    [InlineData("\"Arial\tserif\"", "Arial\tserif")]
    [InlineData("\"Arial\\\tserif\"", "Arial\tserif")]
    [InlineData("\"Arial\\\\\tserif\"", "Arial\\\tserif")]
    [InlineData("\"Arial\\tserif\"", "Arial\tserif")]
    public void YamlTabsOutsideIndentationPreserveScalarMeaning(string declaration, string expected) {
        var parsed = new MermaidParser().Parse("---\nconfig:\n  fontFamily: " + declaration + "\n---\nflowchart LR\nA --> B");
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(expected, parsed.Document!.Configuration.FontFamily);
    }
}

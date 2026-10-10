using ChartForgeX.Mermaid;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidConfigurationRegressionTests {
    [Theory]
    [InlineData("theme")]
    [InlineData("fontFamily")]
    [InlineData("layout")]
    [InlineData("look")]
    public void SupportedStringSettingsRejectObjectsAndEmptyYamlInGlobalAndActiveScopes(string key) {
        foreach (var scoped in new[] { false, true }) {
            var objectSetting = "'" + key + "': {'value': 'dark'}";
            var payload = scoped ? "{'flowchart': {" + objectSetting + "}}" : "{" + objectSetting + "}";
            var yaml = scoped ? "  flowchart:\n    " + key + ":" : "  " + key + ":";
            foreach (var source in new[] {
                "%%{init: " + payload + "}%%\nflowchart LR\nA --> B",
                "---\nconfig:\n" + yaml + "\n---\nflowchart LR\nA --> B"
            }) {
                var rendered = MermaidRenderer.Render(source);
                Assert.True(rendered.HasErrors);
                Assert.Null(rendered.Artifact);
                Assert.Contains(rendered.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidConfiguration && item.Message.Contains(key));
                Assert.Contains(rendered.Document!.Configuration.Settings, item => item.Path == (scoped ? "flowchart." : "") + key && !item.IsString);
            }
        }
    }

    [Fact]
    public void ValidEffectiveValuesReplaceEarlierInvalidScalarDeclarations() {
        const string source = "%%{init: {'theme': {'value': 'dark'}, 'fontFamily': null, 'layout': {}, 'look': false}}%%\n" +
            "%%{initialize: {'theme': 'dark', 'fontFamily': 'Arial', 'layout': 'dagre', 'look': 'classic'}}%%\nflowchart LR\nA --> B";
        var rendered = MermaidRenderer.Render(source);
        Assert.False(rendered.HasErrors);
        Assert.NotNull(rendered.Artifact);
        Assert.Equal("dark", rendered.Document!.Configuration.Theme);
        Assert.Equal("Arial", rendered.Document.Configuration.FontFamily);
        Assert.Equal("dagre", rendered.Document.Configuration.Layout);
        Assert.Equal("classic", rendered.Document.Configuration.Look);
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\t")]
    [InlineData("\0")]
    public void LiteralControlsInsideEitherLegacyQuoteStyleAreRejected(string control) {
        foreach (var quote in new[] { "'", "\"" }) {
            var source = "%%{init: {" + quote + "fontFamily" + quote + ": " + quote + "Arial," + control + "sans-serif" + quote + "}}%%\nflowchart LR\nA --> B";
            var rendered = MermaidRenderer.Render(source);
            Assert.True(rendered.HasErrors);
            Assert.Null(rendered.Artifact);
            Assert.Contains(rendered.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidConfiguration);
        }
    }

    [Theory]
    [InlineData("'")]
    [InlineData("\"")]
    public void EscapedControlsRemainValidConfigurationStrings(string quote) {
        var source = "%%{init: {" + quote + "fontFamily" + quote + ": " + quote + "Arial,\\n\\tsans-serif" + quote + "}}%%\nflowchart LR\nA --> B";
        var rendered = MermaidRenderer.Render(source);
        Assert.Empty(rendered.Diagnostics);
        Assert.Equal("Arial,\n\tsans-serif", rendered.Document!.Configuration.FontFamily);
        Assert.NotNull(rendered.Artifact);
    }

    [Theory]
    [InlineData("\r\n", "\r\n")]
    [InlineData("\n", "\r\n")]
    [InlineData("\r", "\n")]
    public void CompleteDirectiveSpansSelectTheOriginalDeclarationAcrossNewlineForms(string first, string second) {
        var declaration = "%%{initialize: {" + first + "  'theme': 'dark'" + second + "}}%%";
        var source = "flowchart LR\r\nA --> B\n  " + declaration + "\r\nB --> C";
        var parsed = new MermaidParser().ParseFlowchart(source);
        Assert.Empty(parsed.Diagnostics);
        var document = parsed.Document!;
        var span = Assert.Single(document.Directives).Span;
        Assert.Equal(3, span.Line);
        Assert.Equal(3, span.Column);
        Assert.Equal(declaration.Length, span.Length);
        var offset = source.IndexOf("%%{initialize", StringComparison.Ordinal);
        Assert.Equal(declaration, document.SourceText.Substring(offset, span.Length));
        Assert.Equal(6, document.Nodes.Single(node => node.Id == "C").Span.Line);
        Assert.Equal(span.Length, document.Configuration.Settings.Single().Span.Length);
    }
}

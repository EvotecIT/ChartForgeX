using ChartForgeX.Mermaid;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidConfigurationYamlPresentationTests {
    [Theory]
    [InlineData("config: |\n  #", "")]
    [InlineData("config:\n  other: >-\n    #", "")]
    [InlineData("config:\n  other:\n  - note: |2+\n      #", "")]
    [InlineData("config: |\n  ", "")]
    public void OpaqueScalarContentCountsEvenWhenItLooksLikeCommentsOrBlankLines(string prefix, string suffix) {
        var rendered = MermaidInnerBoundaryTestPolicy.Render("---\n" + prefix + new string(' ', 70000) + suffix + "\n---\nflowchart LR\nA --> B");
        Assert.True(rendered.HasErrors); Assert.Null(rendered.Artifact);
        Assert.Contains(rendered.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.InvalidConfiguration && diagnostic.Message.Contains("65536"));
    }

    [Theory]
    [InlineData("#")]
    [InlineData("")]
    public void QuotedFlowScalarBodiesShareThePhysicalConfigurationBudget(string prefix) {
        var source = "---\nconfig: {\"other\":'before\n  " + prefix + new string(' ', 70000) + "\n  after'}\n---\nflowchart LR\nA --> B";
        var rendered = MermaidInnerBoundaryTestPolicy.Render(source);
        Assert.True(rendered.HasErrors); Assert.Null(rendered.Artifact);
        Assert.Contains(rendered.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidConfiguration && item.Message.Contains("65536"));
    }

    [Theory]
    [InlineData("config:\n  other.name: |\n    #", "")]
    [InlineData("config:\n  123: |\n    ", "")]
    [InlineData("config:\n- other.name: |\n    #", "")]
    [InlineData("config:\n  other: {'message':'before\n    #", "\n    after'}")]
    [InlineData("config:\n  other: {'message':'before\n    ", "\n    after'}")]
    [InlineData("config: ['before\n  #", "\n  after']")]
    [InlineData("config: &defaults ['before\n  ", "\n  after']")]
    [InlineData("config: [!custom 'before\n  #", "\n  after']")]
    [InlineData("config: [&defaults \"before\n  ", "\n  after\"]")]
    [InlineData("config:\n  other: {note: !<tag:example.com,2000:app> 'before\n    #", "\n    after'}")]
    public void OpaqueYamlNodeKindsCannotBypassPhysicalScalarAccounting(string prefix, string suffix) {
        var result = MermaidInnerBoundaryTestPolicy.Render("---\n" + prefix + new string(' ', 70000) + suffix + "\n---\nflowchart LR\nA --> B");
        Assert.True(result.HasErrors); Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidConfiguration && item.Message.Contains("65536"));
    }

    [Theory]
    [InlineData("config: [key:'value]\n")]
    [InlineData("config: [key:\"value]\n")]
    [InlineData("config: [O'Reilly]\n")]
    [InlineData("config: [!custom O'Reilly]\n")]
    [InlineData("config: [!<tag:example.com,2000:app> 'short']\n")]
    [InlineData("config: [&defaults \"short\"]\n")]
    [InlineData("config: [Arial\"serif]\n")]
    [InlineData("config:\n  other: {note: O'Reilly}\n")]
    [InlineData("config:\n  other: {note: Arial\"serif}\n")]
    [InlineData("config: &defaults [plain\n  O'Reilly]\n")]
    [InlineData("config: [https://example.test/O'Reilly]\n")]
    public void QuotesInsideOpaquePlainScalarsDoNotConsumeFollowingMetadata(string configuration) {
        var source = "---\n" + configuration + "description: " + new string('x', 70000) + "\n---\nflowchart LR\nA --> B";
        var result = MermaidInnerBoundaryTestPolicy.Render(source);
        Assert.False(result.HasErrors); Assert.NotNull(result.Artifact);
        Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.UnsupportedConfiguration);
    }

    [Theory]
    [InlineData("\"config\"", "\"theme\"")]
    [InlineData("'config'", "'theme'")]
    [InlineData("\"confi\\u0067\"", "\"the\\u006de\"")]
    public void QuotedMappingKeysApplyTheSameSettingsAndKeepTheirPhysicalSpans(string root, string key) {
        var parsed = new MermaidParser().Parse("---\n" + root + ":\n  " + key + ": dark\n---\nflowchart LR\nA --> B");
        Assert.False(parsed.HasErrors); Assert.Equal("dark", parsed.Document!.Configuration.Theme);
        var setting = Assert.Single(parsed.Document.Configuration.Settings);
        Assert.Equal(new MermaidSourceSpan(3, 3, key.Length + 6), setting.Span);
    }

    [Theory]
    [InlineData("\"config\"")]
    [InlineData("'config'")]
    public void QuotedRootKeysShareTheConfigurationBudget(string root) {
        var rendered = MermaidInnerBoundaryTestPolicy.Render("---\n" + root + ":\n  other: " + new string('x', 70000) + "\n---\nflowchart LR\nA --> B");
        Assert.True(rendered.HasErrors); Assert.Null(rendered.Artifact);
        Assert.Contains(rendered.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidConfiguration);
    }

    [Theory]
    [InlineData("'Arial\n  serif'", "Arial serif")]
    [InlineData("'Arial  \n \tserif'", "Arial serif")]
    [InlineData("'Arial\n\n  serif'", "Arial\nserif")]
    [InlineData("'Arial\n  #serif'", "Arial #serif")]
    [InlineData("\"Arial\n  serif\"", "Arial serif")]
    [InlineData("\"Arial\\\n  serif\"", "Arialserif")]
    public void QuotedFlowScalarsFoldPhysicalLinesWithoutChangingFollowingPropertySpans(string literal, string expected) {
        foreach (var newline in new[] { "\n", "\r\n" }) {
            var source = ("---\nconfig: {fontFamily:" + literal + ",\n  theme: 'dark'\n}\n---\nflowchart LR\nA --> B").Replace("\n", newline);
            // Flow mappings require JSON-style quoted keys in the supported subset.
            source = source.Replace("fontFamily:", "\"fontFamily\":").Replace("theme:", "\"theme\":");
            var parsed = new MermaidParser().Parse(source);
            Assert.False(parsed.HasErrors); Assert.Equal(expected, parsed.Document!.Configuration.FontFamily);
            Assert.Equal("dark", parsed.Document.Configuration.Theme);
            var setting = parsed.Document.Configuration.Settings.Single(item => item.Path == "theme");
            Assert.Equal(source.Split('\n').ToList().FindIndex(line => line.Contains("\"theme\"")) + 1, setting.Span.Line);
            Assert.Equal(3, setting.Span.Column);
        }
    }

    [Theory]
    [InlineData("theme: *defaults", "theme")]
    [InlineData("fontFamily: |\n    Arial", "fontFamily")]
    public void RequiredStringSettingsRejectOpaqueValuesUnlessASupportedLaterValueReplacesThem(string declaration, string path) {
        var source = "---\nconfig:\n  " + declaration + "\n---\n";
        var invalid = MermaidRenderer.Render(source + "flowchart LR\nA --> B");
        Assert.True(invalid.HasErrors); Assert.Null(invalid.Artifact);
        Assert.Contains(invalid.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidConfiguration && item.Message.Contains(path));
        var supported = MermaidRenderer.Render(source + "%%{init: {'" + path + "':'" + (path == "theme" ? "dark" : "Arial") + "'}}%%\nflowchart LR\nA --> B");
        Assert.False(supported.HasErrors); Assert.NotNull(supported.Artifact);
    }
}

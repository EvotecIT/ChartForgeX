using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidConfigurationBoundaryTests {
    [Theory]
    [InlineData(65536, false)]
    [InlineData(65537, true)]
    public void YamlConfigurationBudgetHasTheDocumentedBoundary(int length, bool hasErrors) {
        const string prefix = "config:\n  other: ";
        var rendered = MermaidRenderer.Render("---\n" + prefix + new string('x', length - prefix.Length) + "\n---\nflowchart LR\nA --> B");
        Assert.Equal(hasErrors, rendered.HasErrors);
        Assert.Equal(hasErrors, rendered.Artifact == null);
        Assert.Equal(hasErrors ? 0 : 1, rendered.Document!.Configuration.Settings.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnrelatedFrontmatterDoesNotConsumeTheConfigurationBudget(bool withConfig) {
        var metadata = "description: " + new string('x', 70000);
        var source = "---\n" + metadata + (withConfig ? "\nconfig:\n  theme: dark" : "") + "\n---\nflowchart LR\nA --> B";
        var rendered = MermaidRenderer.Render(source);
        Assert.Empty(rendered.Diagnostics);
        Assert.NotNull(rendered.Artifact);
        Assert.Contains(metadata, rendered.Document!.FrontMatter);
        Assert.Equal(withConfig ? "dark" : null, rendered.Document.Configuration.Theme);
    }

    [Fact]
    public void OverBudgetDirectivesDoNotRemainInTheBoundedDeclarationCollection() {
        var valid = "%%{init: {'theme':'dark'}}%%";
        var over = "%%{init: {'other':'" + new string('x', 65536) + "'}}%%";
        var rendered = MermaidRenderer.Render(valid + "\n" + over + "\n" + valid + "\nflowchart LR\nA --> B");
        Assert.True(rendered.HasErrors);
        Assert.Null(rendered.Artifact);
        Assert.Equal(valid, Assert.Single(rendered.Document!.Directives).Text);
        Assert.Contains(rendered.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidConfiguration);
    }

    [Fact]
    public void QuadrantChartScopeSelectsItsPresentationOverrides() {
        var rendered = MermaidRenderer.Render("%%{init: {'theme':'default','quadrantChart':{'theme':'dark','look':'classic','layout':'dagre'}}}%%\nquadrantChart\nA: [0.2,0.4]");
        Assert.False(rendered.HasErrors);
        Assert.Equal("dark", rendered.Document!.Configuration.Theme);
        Assert.Equal("classic", rendered.Document.Configuration.Look);
        Assert.Equal("dagre", rendered.Document.Configuration.Layout);
        Assert.DoesNotContain(rendered.Diagnostics, item => item.Message.Contains("quadrantChart.theme"));
        Assert.Contains(rendered.Diagnostics, item => item.Code == MermaidDiagnosticCodes.UnsupportedLayout);
    }

    [Theory]
    [InlineData("Dark", "#0B1120")]
    [InlineData("DEFAULT", "#FFFFFF")]
    public void ThemeDiagnosticsMatchTheNativePaletteForCasingVariants(string theme, string background) {
        var rendered = MermaidRenderer.Render("%%{init: {'theme':'" + theme + "'}}%%\nflowchart LR\nA --> B");
        Assert.Empty(rendered.Diagnostics);
        Assert.Equal(background, Assert.IsType<TopologyChart>(rendered.Artifact!.Model).Theme!.Background);
    }

    [Theory]
    [InlineData("1e9999")]
    [InlineData("-1e9999")]
    [InlineData("1e-9999")]
    [InlineData("+1.0")]
    [InlineData(".5")]
    [InlineData("1.")]
    public void PlainNumericYamlSettingsHaveOneClassificationAcrossFrameworks(string scalar) {
        var rendered = MermaidRenderer.Render("---\nconfig:\n  theme: " + scalar + "\n---\nflowchart LR\nA --> B");
        Assert.True(rendered.HasErrors);
        Assert.Null(rendered.Artifact);
        Assert.False(Assert.Single(rendered.Document!.Configuration.Settings).IsString);
        Assert.Contains(rendered.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidConfiguration);
    }

    [Theory]
    [InlineData("flowchart LR\nA --> B")]
    [InlineData("pie\n\"Success\": 8\n\"Failure\": 2")]
    [InlineData("packet-beta\n0-7: \"Header\"")]
    public async Task NativeFontSerializersKeepSourceValuesInsideTheirCssContext(string body) {
        if (!InteractiveChartBrowser.Enabled) return;
        var rendered = MermaidRenderer.Render("%%{init: {'fontFamily': 'Arial;position:fixed;background-color:rgb(1,2,3)'}}%%\n" + body);
        Assert.Empty(rendered.Diagnostics);
        await using var session = await InteractiveChartBrowser.OpenAsync(rendered.Artifact!.ToHtmlPage(), 640, 460);
        Assert.Equal("static", await session.Page.EvaluateAsync<string>("() => getComputedStyle(document.body).position"));
        Assert.NotEqual("rgb(1, 2, 3)", await session.Page.EvaluateAsync<string>("() => getComputedStyle(document.body).backgroundColor"));
        Assert.Equal(0, await session.Page.Locator("script").CountAsync());
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}

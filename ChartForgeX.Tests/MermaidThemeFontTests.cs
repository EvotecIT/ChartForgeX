using ChartForgeX.Core;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidThemeFontTests {
    [Theory]
    [InlineData("flowchart LR\nA[API] --> B[Store]")]
    [InlineData("sequenceDiagram\nAPI->>Store: Save request")]
    [InlineData("pie\n\"Saved\": 8\n\"Failed\": 2")]
    [InlineData("packet-beta\n0-7: \"Header\"\n8-15: \"Payload\"")]
    public void ThemeFontOverridesGlobalFontThroughNativeConversionAndMarkdown(string body) {
        var source = "%%{init: {'theme':'dark','fontFamily':'Arial','themeVariables':{'fontFamily':'Georgia, serif'}}}%%\n" + body;
        var rendered = MermaidRenderer.Render(source);
        Assert.Empty(rendered.Diagnostics);
        Assert.Equal("Georgia, serif", rendered.Document!.Configuration.FontFamily);
        var artifact = rendered.Artifact!;
        Assert.Equal("Georgia, serif", artifact.Metadata["mermaid.config.fontFamily"]);
        Assert.Equal("Georgia, serif", FontFamily(artifact.Model!));
        Assert.Contains("Georgia", artifact.ToSvg());
        Assert.NotEmpty(artifact.ToPng());
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.Empty(fenced.Diagnostics);
        Assert.Equal("Georgia, serif", FontFamily(Assert.Single(fenced.Artifacts).Model!));
    }

    [Theory]
    [InlineData(false, "Courier New, monospace")]
    [InlineData(true, "Georgia, serif")]
    public void VariableDeclarationsMergeInSourceOrderAndFamilyFontStillWins(bool familyOverride, string expected) {
        var source = "---\nconfig:\n  fontFamily: Arial\n  themeVariables:\n    fontFamily: Arial\n" +
            (familyOverride ? "  flowchart:\n    fontFamily: Georgia, serif\n" : string.Empty) +
            "---\n%%{init: {'themeVariables':{'fontFamily':'Courier New, monospace'}}}%%\n" +
            "%%{init: {'fontFamily':'Arial'}}%%\nflowchart LR\nA --> B";
        var rendered = MermaidRenderer.Render(source);
        Assert.Empty(rendered.Diagnostics);
        Assert.Equal(expected, rendered.Document!.Configuration.FontFamily);
        Assert.Equal(expected, Assert.IsType<TopologyChart>(rendered.Artifact!.Model).Theme!.FontFamily);
        Assert.Contains(rendered.Document.Configuration.Settings, item => item.Path == "themeVariables.fontFamily" && item.Span.Line == 5);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("false")]
    [InlineData("12")]
    [InlineData("''")]
    [InlineData("{'nested':'Georgia'}")]
    public void InvalidEffectiveVariableFontsPreventRenderingAtTheOriginalDeclaration(string value) {
        var result = MermaidRenderer.Render("%%{init: {'themeVariables':{'fontFamily':" + value + "}}}%%\nflowchart LR\nA --> B");
        Assert.True(result.HasErrors);
        Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidConfiguration && item.Span.Line == 1);
    }

    [Fact]
    public void OtherVariablesAndUnsupportedFamiliesKeepTheirDiagnostics() {
        var result = MermaidRenderer.Render("%%{init: {'themeVariables':{'fontFamily':'Georgia','fontSize':'18px','primaryColor':'#123456'}}}%%\nflowchart LR\nA --> B");
        Assert.False(result.HasErrors);
        Assert.Equal(2, result.Diagnostics.Count);
        Assert.All(result.Diagnostics, item => Assert.Equal(MermaidDiagnosticCodes.UnsupportedConfiguration, item.Code));
        var unsupported = new MermaidParser().Parse("%%{init: {'themeVariables':{'fontFamily':'Georgia'}}}%%\nagentflow-beta\nflow A");
        Assert.Contains(unsupported.Diagnostics, item => item.Code == MermaidDiagnosticCodes.UnsupportedConfiguration);
    }

    [Fact]
    public void SharedReferenceFixtureKeepsBothRequestedFontsAndNativeUsesTheVariable() {
        var root = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures");
        using var expected = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "theme-variable-font.expected.json")));
        var result = MermaidRenderer.Render(File.ReadAllText(Path.Combine(root, "theme-variable-font.mmd")));
        Assert.Empty(result.Diagnostics);
        var settings = result.Document!.Configuration;
        foreach (var setting in expected.RootElement.GetProperty("configuration").EnumerateObject())
            Assert.Equal(setting.Value.GetString(), settings.Settings.Last(item => item.Path == setting.Name).Value);
        Assert.Equal(expected.RootElement.GetProperty("configuration").GetProperty("themeVariables.fontFamily").GetString(), settings.FontFamily);
        Assert.Equal("Georgia, serif", Assert.IsType<TopologyChart>(result.Artifact!.Model).Theme!.FontFamily);
    }

    [Theory]
    [InlineData("dark", 320)]
    [InlineData("default", 960)]
    public async Task NativeVariableFontIsObservedInCompactAndWideBrowserHosts(string theme, int width) {
        if (!InteractiveChartBrowser.Enabled) return;
        var artifact = MermaidRenderer.Render("%%{init: {'theme':'" + theme + "','themeVariables':{'fontFamily':'Georgia, serif'}}}%%\nflowchart LR\nA[API] --> B[Store]").Artifact!;
        await using var session = await InteractiveChartBrowser.OpenAsync(artifact.ToHtmlPage(), width, 460);
        Assert.Equal(2, await session.Page.Locator("[data-cfx-role='topology-node']").CountAsync());
        Assert.Contains("Georgia", await session.Page.Locator("svg text").First.EvaluateAsync<string>("element => getComputedStyle(element).fontFamily"));
        Assert.Equal(0, await session.Page.Locator("script").CountAsync());
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, "theme-variable-font-" + theme + "-" + width + ".png"), FullPage = true });
            File.WriteAllBytes(Path.Combine(captures, "theme-variable-font-" + theme + "-native.png"), artifact.ToPng());
        }
    }

    private static string FontFamily(object model) => model switch {
        TopologyChart topology => topology.Theme!.FontFamily,
        SequenceArtifact sequence => sequence.Theme.Typography.Family,
        Chart chart => chart.Options.Theme.FontFamily,
        IVisualBlock block => block.Options.Theme.FontFamily,
        _ => throw new InvalidOperationException("Unexpected native family.")
    };
}

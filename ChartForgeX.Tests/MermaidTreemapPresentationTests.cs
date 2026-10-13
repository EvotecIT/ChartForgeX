using ChartForgeX.Core;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidTreemapPresentationTests {
    private const string Body = "treemap-beta\n\"Allocation\"\n  \"Alpha\": 7\n  \"Beta\": 3\n";
    private const string Hidden = "---\nconfig:\n  treemap:\n    showValues: false\n---\n";

    [Fact]
    public void SourceVisibilityUsesTheNativeOwnerAndLaterDirectivesWin() {
        var native = new MermaidParser().ParseTreemap(Body).Document!.ToChart();
        Assert.True(native.Options.Treemap.ShowValues);
        foreach (var prefix in new[] { Hidden, "%%{init: {'treemap': {'showValues': false}}}%%\n" }) {
            var result = MermaidRenderer.Render(prefix + Body);
            Assert.Empty(result.Diagnostics);
            var artifact = result.Artifact!;
            var chart = Assert.IsType<Chart>(artifact.Model);
            Assert.False(chart.Options.Treemap.ShowValues);
            Assert.Equal(new[] { "Allocation", "Alpha", "Beta" }, TreemapValueCaptionTests.Captions(artifact.ToSvg()).OrderBy(label => label));
            Assert.Equal(7, chart.Series[0].HierarchyItems[1].Value);
            Assert.Equal("node-0", chart.Series[0].HierarchyItems[1].ParentId);
            var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + prefix + Body + "\n```");
            Assert.Empty(fenced.Diagnostics);
            Assert.Equal(artifact.ToSvg(), Assert.Single(fenced.Artifacts).ToSvg());
            Assert.Equal(artifact.ToPng(), Assert.Single(fenced.Artifacts).ToPng());
        }
        var later = MermaidRenderer.Render(Hidden + "%%{init: {'treemap': {'showValues': true}}}%%\n" + Body);
        Assert.Empty(later.Diagnostics);
        Assert.True(Assert.IsType<Chart>(later.Artifact!.Model).Options.Treemap.ShowValues);
        Assert.Contains("Allocation · 10", TreemapValueCaptionTests.Captions(later.Artifact.ToSvg()));
    }

    [Theory]
    [InlineData("'false'")]
    [InlineData("1")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("\n      nested: false")]
    public void InvalidVisibilityHasOneLocatedError(string value) {
        var result = MermaidRenderer.Render(Hidden.Replace("false", value) + Body);
        Assert.True(result.HasErrors); Assert.Null(result.Artifact);
        var error = Assert.Single(result.Diagnostics);
        Assert.Equal(MermaidDiagnosticCodes.InvalidConfiguration, error.Code); Assert.Equal(4, error.Span.Line);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void ExplicitHostVisibilityWinsAndMarkdownCapturesTheOverride(bool sourceValue, bool hostValue) {
        var source = Hidden.Replace("false", sourceValue ? "true" : "false") + Body;
        var host = new MermaidTreemapRenderOptions { ShowValues = hostValue };
        var direct = new MermaidParser().ParseTreemap(source).Document!.ToVisualArtifact(host);
        Assert.Equal(hostValue, Assert.IsType<Chart>(direct.Model).Options.Treemap.ShowValues);
        var parser = new MermaidVisualMarkupParser(new MermaidRenderOptions { Treemap = host });
        host.ShowValues = !hostValue;
        var fenced = parser.Parse("```mermaid\n" + source + "\n```");
        Assert.Empty(fenced.Diagnostics);
        Assert.Equal(direct.ToSvg(), Assert.Single(fenced.Artifacts).ToSvg());
        Assert.Equal(direct.ToPng(), Assert.Single(fenced.Artifacts).ToPng());
    }

    [Fact]
    public void OtherFamiliesRetainUnsupportedTreemapSettingsAsLocatedWarnings() {
        var result = MermaidRenderer.Render(Hidden + "flowchart LR\nA --> B");
        Assert.False(result.HasErrors); Assert.NotNull(result.Artifact);
        var warning = Assert.Single(result.Diagnostics);
        Assert.Equal(MermaidDiagnosticCodes.UnsupportedConfiguration, warning.Code); Assert.Equal(4, warning.Span.Line);
    }

    [BrowserTheory]
    [InlineData(320, "default")]
    [InlineData(960, "dark")]
    public async Task LabelsRemainVisibleWhenValuesAreHiddenInStaticHosts(int width, string theme) {
        if (!InteractiveChartBrowser.Enabled) return;
        var result = MermaidRenderer.Render(Hidden.Replace("config:", "config:\n  theme: " + theme) + Body);
        Assert.Empty(result.Diagnostics);
        var artifact = result.Artifact!;
        await using var session = await InteractiveChartBrowser.OpenAsync(artifact.ToHtmlPage(), width, 620);
        Assert.Equal("Alpha", await session.Page.Locator("[data-cfx-target-id=node-1] [data-cfx-role=treemap-label]").TextContentAsync());
        Assert.Equal("7", await session.Page.Locator("[data-cfx-target-id=node-1]").GetAttributeAsync("data-cfx-value"));
        Assert.Equal(0, await session.Page.Locator("script").CountAsync());
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, "treemap-hidden-browser-" + width + ".png"), FullPage = true });
            File.WriteAllBytes(Path.Combine(captures, "treemap-hidden-native-" + theme + ".png"), artifact.ToPng());
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}

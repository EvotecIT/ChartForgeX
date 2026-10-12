using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidSankeyConfigurationBrowserTests {
    [Theory]
    [InlineData(false, 320)]
    [InlineData(true, 960)]
    public async Task ConfiguredSankeyUsesNativeLabelsInCompactAndWideHosts(bool showValues, int width) {
        if (!InteractiveChartBrowser.Enabled) return;
        var source = "%%{init: {'theme': '" + (showValues ? "dark" : "default") + "', 'sankey': {'width': 800, 'height': 480, 'nodeWidth': 18, 'nodePadding': 24, 'nodeAlignment': 'center', 'showValues': "
            + (showValues ? "true" : "false") + ", 'prefix': '$', 'suffix': ' units'}}}%%\nsankey-beta\nIntake,Review,6\nReview,Build,6\nBuild,Delivery,6\nFast,Delivery,4\nIntake,Deferred,2";
        var rendered = MermaidRenderer.Render(source);
        Assert.Empty(rendered.Diagnostics);
        var artifact = rendered.Artifact!;
        await using var session = await InteractiveChartBrowser.OpenAsync(artifact.ToHtmlPage(), width, 600);
        Assert.Equal(6, await session.Page.Locator("[data-cfx-role='sankey-node']").CountAsync());
        var labels = string.Join(" ", await session.Page.Locator("[data-cfx-role='sankey-node-label']").AllTextContentsAsync());
        if (showValues) Assert.Contains("$8 units", labels);
        else Assert.DoesNotContain("units", labels);
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        Assert.Equal(0, await session.Page.Locator("script").CountAsync());
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        var name = "sankey-configured-" + (showValues ? "values-dark-" : "names-light-") + width;
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + "-browser.png"), FullPage = true });
        File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), artifact.ToPng());
    }
}

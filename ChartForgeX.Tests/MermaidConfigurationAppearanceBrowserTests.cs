using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidConfigurationAppearanceBrowserTests {
    [Theory]
    [InlineData(320)]
    [InlineData(960)]
    public async Task UnusableScopedAppearanceUsesTheGlobalDarkThemeInStaticHosts(int width) {
        if (!InteractiveChartBrowser.Enabled) return;
        const string source = "---\nconfig:\n  theme: dark\n  look: classic\n  flowchart:\n    theme: unknown\n    look: unknown\n---\nflowchart LR\nA[Start] --> B[Done]";
        var result = MermaidRenderer.Render(source);
        Assert.False(result.HasErrors);
        var artifact = result.Artifact!;
        Assert.Equal("#0B1120", Assert.IsType<TopologyChart>(artifact.Model).Theme!.Background);
        await using var session = await InteractiveChartBrowser.OpenAsync(artifact.ToHtmlPage(), width, 460);
        Assert.Equal(2, await session.Page.Locator("[data-cfx-role=topology-node]").CountAsync());
        Assert.Contains("Start", await session.Page.Locator("body").InnerTextAsync());
        Assert.Contains("Done", await session.Page.Locator("body").InnerTextAsync());
        Assert.Equal(0, await session.Page.Locator("script").CountAsync());
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            await session.Page.ScreenshotAsync(new PageScreenshotOptions {
                Path = Path.Combine(captures, "configuration-fallback-browser-" + width + ".png"), FullPage = true
            });
            File.WriteAllBytes(Path.Combine(captures, "configuration-fallback-native.png"), artifact.ToPng());
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}

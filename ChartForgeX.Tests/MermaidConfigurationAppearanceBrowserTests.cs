using ChartForgeX.Themes;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidConfigurationAppearanceBrowserTests {
    [BrowserTheory]
    [InlineData(320)]
    [InlineData(960)]
    public async Task UnusableScopedAppearanceUsesTheGlobalDarkThemeInStaticHosts(int width) {
        if (!InteractiveChartBrowser.Enabled) return;
        const string source = "---\nconfig:\n  theme: dark\n  look: classic\n  flowchart:\n    theme: unknown\n    look: unknown\n---\nflowchart LR\nA[Start] --> B[Done]";
        var result = MermaidRenderer.Render(source);
        Assert.False(result.HasErrors);
        var artifact = result.Artifact!;
        var topology = Assert.IsType<TopologyChart>(artifact.Model);
        Assert.Equal(VisualThemeMode.Dark, topology.ThemeMode);
        var global = MermaidRenderer.Render("---\nconfig:\n  theme: dark\n  look: classic\n---\nflowchart LR\nA[Start] --> B[Done]");
        Assert.Equal(global.Artifact!.ToPng(), artifact.ToPng());
        await using var session = await InteractiveChartBrowser.OpenAsync(artifact.ToHtmlPage(), width, 460);
        Assert.Equal(2, await session.Page.Locator("[data-cfx-role=topology-node]").CountAsync());
        await BrowserProofCaptions.InstallNativeAsync(session.Page);
        var captions = await session.Page.Locator("svg").EvaluateAsync<string>("node=>window.cfxCaptionText(node)");
        Assert.Contains("Start", captions);
        Assert.Contains("Done", captions);
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

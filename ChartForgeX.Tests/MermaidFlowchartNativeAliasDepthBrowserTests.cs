using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class MermaidFlowchartNativeAliasDepthBrowserTests {
    [BrowserTheory]
    [InlineData(false, 390)]
    [InlineData(true, 390)]
    [InlineData(false, 1100)]
    [InlineData(true, 1100)]
    public async Task ExistingNativeSurfacesKeepCaptionsAndRoutesVisibleInStaticHosts(bool dark, int width) {
        if (!InteractiveChartBrowser.Enabled) return;
        var source = File.ReadAllText(Path.Combine(TestRepository.Root, "ChartForgeX.Tests", "Fixtures", "Mermaid", "flowchart-native-storage-burst.mmd"));
        if (dark) source = "%%{init: {'theme':'dark'}}%%\n" + source;
        var rendered = MermaidRenderer.Render(source);
        Assert.Empty(rendered.Diagnostics);
        var artifact = rendered.Artifact!;
        var html = artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.PreserveSize });
        await using var session = await BrowserProofFonts.OpenAsync(html, width, 720);
        Assert.Equal(4, await session.Page.Locator("[data-cfx-role=topology-node]").CountAsync());
        Assert.Equal(3, await session.Page.Locator("[data-cfx-role=topology-edge]").CountAsync());
        Assert.Equal(1, await session.Page.Locator("[data-node-shape=ScallopedBurst]").CountAsync());
        Assert.Equal(3, await session.Page.Locator("[data-node-shape=HorizontalCylinder]").CountAsync());
        Assert.Equal(0, await session.Page.Locator("script, image, foreignObject").CountAsync());
        Assert.True(await session.Page.EvaluateAsync<bool>("""
            () => [...document.querySelectorAll('[data-cfx-role="topology-node"]')].every(node => {
                const surfaces = [...node.querySelectorAll('[data-cfx-role="topology-node-surface"]')];
                const labels = [...node.querySelectorAll('[data-cfx-role="topology-node-label"]')];
                if (!surfaces.length || !labels.length) return false;
                const box = surfaces[0].getBBox();
                return labels.every(label => {
                    const text = label.getBBox();
                    return text.width > 0 && text.height > 0 && text.x >= box.x - 1
                        && text.y >= box.y - 1 && text.x + text.width <= box.x + box.width + 1
                        && text.y + text.height <= box.y + box.height + 1;
                });
            })
            """));
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output);
        var stem = Path.Combine(output, "flowchart-native-storage-burst-" + width + (dark ? "-dark" : "-light"));
        File.WriteAllText(stem + ".svg", artifact.ToSvg());
        File.WriteAllBytes(stem + ".native.png", artifact.ToPng());
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
    }
}

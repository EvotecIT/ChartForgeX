using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class MermaidAgentflowShapePolicyBrowserTests {
    [BrowserTheory]
    [InlineData(false, 390)]
    [InlineData(true, 390)]
    [InlineData(false, 1100)]
    [InlineData(true, 1100)]
    public async Task FamilyShapesKeepVisibleLabelsAndStaticContainment(bool dark, int width) {
        if (!InteractiveChartBrowser.Enabled) return;
        var source = MermaidAgentflowShapePolicyTests.Fixture();
        if (dark) source = "%%{init: {'theme':'dark'}}%%\n" + source;
        var rendered = MermaidRenderer.Render(source);
        Assert.Empty(rendered.Diagnostics);
        var artifact = rendered.Artifact!;
        var html = artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.PreserveSize });
        await using var session = await BrowserProofFonts.OpenAsync(html, width, 720);
        Assert.Equal(8, await session.Page.Locator("[data-cfx-role=topology-node]").CountAsync());
        Assert.Equal(7, await session.Page.Locator("[data-cfx-role=topology-edge]").CountAsync());
        Assert.Equal(3, await session.Page.Locator("[data-node-shape=Rounded]").CountAsync());
        Assert.Equal(0, await session.Page.Locator("script, image, foreignObject, a").CountAsync());
        Assert.True(await session.Page.EvaluateAsync<bool>("""
            () => [...document.querySelectorAll('[data-cfx-role="topology-node"]')].every(node => {
                const labels = [...node.querySelectorAll('[data-cfx-role="topology-node-label"]')];
                return labels.length > 0 && labels.every(label => {
                    const box = label.getBBox();
                    return box.width > 0 && box.height > 0 && !/\u2026/.test(label.textContent);
                });
            })
            """));
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output);
        var stem = Path.Combine(output, "agentflow-family-shapes-" + width + (dark ? "-dark" : "-light"));
        File.WriteAllText(stem + ".svg", artifact.ToSvg());
        File.WriteAllBytes(stem + ".native.png", artifact.ToPng());
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
    }
}

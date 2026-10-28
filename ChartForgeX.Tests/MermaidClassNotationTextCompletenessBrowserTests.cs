using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class MermaidClassNotationTextCompletenessBrowserTests {
    [BrowserTheory]
    [InlineData("class", false, 390)] [InlineData("class", true, 390)]
    [InlineData("class", false, 1100)] [InlineData("class", true, 1100)]
    [InlineData("er", false, 390)] [InlineData("er", true, 390)]
    [InlineData("er", false, 1100)] [InlineData("er", true, 1100)]
    public async Task EveryMultilineRelationshipAndDetailRowIsVisibleInsideItsNativeSurface(string family, bool dark, int width) {
        if (!InteractiveChartBrowser.Enabled) return;
        var result = MermaidRenderer.Render(MermaidClassNotationTextCompletenessTests.Source(family), MermaidClassErCaptionTests.Options(dark, width));
        Assert.Empty(result.Diagnostics); var artifact = result.Artifact!; var svg = artifact.ToSvg(); var png = artifact.ToPng();
        await using var session = await BrowserProofFonts.OpenAsync(artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.PreserveSize }), width, 720);
        var rows = await session.Page.Locator("svg text").AllTextContentsAsync();
        foreach (var expected in MermaidClassNotationTextCompletenessTests.ExpectedRows(family)) Assert.Contains(expected, rows);
        Assert.Equal("2", await session.Page.Locator("[data-cfx-role=topology-edge-label]").GetAttributeAsync("data-label-line-count"));
        Assert.True(await session.Page.EvaluateAsync<bool>("""
            () => {
                const contained = (outer, inner) => inner.x >= outer.x - 1 && inner.y >= outer.y - 1 && inner.x + inner.width <= outer.x + outer.width + 1 && inner.y + inner.height <= outer.y + outer.height + 1;
                if (![...document.querySelectorAll('[data-cfx-role="topology-node"]')].every(node => {
                    const surface = node.querySelector('[data-cfx-role="topology-node-surface"]')?.getBBox();
                    return surface && [...node.querySelectorAll('[data-cfx-role="topology-node-detail"] text')].every(text => contained(surface, text.getBBox()));
                })) return false;
                return [...document.querySelectorAll('[data-cfx-role="topology-edge-label"]')].every(edge => {
                    const surface = edge.querySelector('[data-cfx-role="topology-edge-label-surface"]')?.getBBox();
                    return surface && [...edge.querySelectorAll('[data-cfx-role="topology-edge-label-text"] text')].every(text => contained(surface, text.getBBox()));
                });
            }
            """));
        Assert.Equal(0, await session.Page.Locator("script, image, foreignObject").CountAsync());
        var host = session.Page.Locator("[data-cfx-html-sizing=preserve-size]"); await host.FocusAsync(); await session.Page.Keyboard.PressAsync("End");
        Assert.True(await host.EvaluateAsync<double>("element => element.scrollLeft") >= 0); InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var root = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(root)) {
            Directory.CreateDirectory(root); var path = Path.Combine(root, "notation-complete-browser-" + family + "-" + width + (dark ? "-dark" : "-light"));
            File.WriteAllText(path + ".svg", svg); File.WriteAllBytes(path + ".native.png", png);
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = path + ".browser.png", FullPage = true });
        }
    }
}

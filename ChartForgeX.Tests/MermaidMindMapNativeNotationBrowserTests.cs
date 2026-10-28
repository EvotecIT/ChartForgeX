using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class MermaidMindMapNativeNotationBrowserTests {
    [BrowserTheory]
    [InlineData(false, 390)]
    [InlineData(true, 390)]
    [InlineData(false, 1100)]
    [InlineData(true, 1100)]
    public async Task CompleteShapeCaptionsStayInsidePaintedSurfacesInBothThemesAndScrollableHosts(bool dark, int width) {
        if (!InteractiveChartBrowser.Enabled) return;
        var result = MermaidRenderer.Render(MermaidMindMapNativeNotationTests.Source, MermaidMindMapNativeNotationTests.Options(dark, width));
        Assert.False(result.HasErrors); var artifact = result.Artifact!; var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var html = artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.PreserveSize });
        await using var session = await BrowserProofFonts.OpenAsync(html, width, 720);
        Assert.Equal(7, await session.Page.Locator("[data-cfx-role=topology-node]").CountAsync());
        Assert.Equal(6, await session.Page.Locator("[data-cfx-role=topology-edge]").CountAsync());
        Assert.Equal(1, await session.Page.Locator("[data-node-shape=Burst]").CountAsync());
        Assert.Equal(1, await session.Page.Locator("[data-cfx-selected=true]").CountAsync());
        Assert.Equal(0, await session.Page.Locator("script, image, foreignObject, svg svg").CountAsync());
        Assert.True(await session.Page.EvaluateAsync<bool>("""
            () => [...document.querySelectorAll('[data-cfx-role="topology-node"]')].every(node => {
                const surface = node.querySelector('[data-cfx-role="topology-node-surface"]').getBBox();
                const labels = [...node.querySelectorAll('[data-cfx-role="topology-node-label"]')];
                if (!labels.length) return false;
                const inset = node.dataset.nodeShape === 'Burst' ? .25 : 0;
                return labels.every(label => {
                    const bounds = label.getBBox();
                    return bounds.width > 0 && bounds.height > 0
                        && bounds.x >= surface.x + surface.width * inset - 1
                        && bounds.x + bounds.width <= surface.x + surface.width * (1 - inset) + 1
                        && bounds.y >= surface.y + surface.height * inset - 1
                        && bounds.y + bounds.height <= surface.y + surface.height * (1 - inset) + 1;
                });
            })
            """));
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        var host = session.Page.Locator("[data-cfx-html-sizing=preserve-size]"); await host.FocusAsync(); await session.Page.Keyboard.PressAsync("End");
        Assert.True(await host.EvaluateAsync<double>("element => { element.scrollLeft = element.scrollWidth; return element.scrollLeft; }") >= 0);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output); var path = Path.Combine(output, "mindmap-notation-browser-" + width + (dark ? "-dark" : "-light"));
        File.WriteAllText(path + ".svg", svg); File.WriteAllBytes(path + ".native.png", png);
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = path + ".browser.png", FullPage = true });
    }
}

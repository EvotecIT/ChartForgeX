using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class MermaidStateFloatingNoteBrowserTests {
    [BrowserTheory]
    [InlineData(false, 390)]
    [InlineData(true, 390)]
    [InlineData(false, 1100)]
    [InlineData(true, 1100)]
    public async Task NamedNoteTextStaysInsideMeasuredPanelsInBothThemesAndScrollableHosts(bool dark, int width) {
        if (!InteractiveChartBrowser.Enabled) return;
        var rendered = MermaidRenderer.Render(MermaidStateFloatingNoteTests.Source, MermaidStateFloatingNoteTests.Options(dark, width));
        Assert.Empty(rendered.Diagnostics); var artifact = rendered.Artifact!;
        var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var html = artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.PreserveSize });
        await using var session = await BrowserProofFonts.OpenAsync(html, width, 720);
        Assert.Equal(4, await session.Page.Locator("[data-cfx-role=topology-annotation]").CountAsync());
        Assert.Equal(3, await session.Page.Locator("[data-cfx-role=topology-annotation][data-target-id='']").CountAsync());
        Assert.Equal(1, await session.Page.Locator("[data-cfx-role=topology-annotation-leader]").CountAsync());
        Assert.Equal(1, await session.Page.Locator("[data-cfx-role=topology-compartment-separator]").CountAsync());
        Assert.Equal(0, await session.Page.Locator("script, image, foreignObject, svg svg").CountAsync());
        foreach (var id in new[] { "Overview", "RegionMemo", "FollowUp" })
            Assert.Equal(1, await session.Page.Locator("[data-cfx-role=topology-annotation][data-annotation-id='" + id + "']").CountAsync());
        Assert.True(await session.Page.EvaluateAsync<bool>("""
            () => [...document.querySelectorAll('[data-cfx-role="topology-annotation"]')].every(note => {
                const surface = note.querySelector('[data-cfx-role="topology-annotation-surface"]').getBBox();
                return [...note.querySelectorAll('text')].every(text => {
                    const bounds = text.getBBox();
                    return bounds.x >= surface.x - 1 && bounds.x + bounds.width <= surface.x + surface.width + 1
                        && bounds.y >= surface.y - 1 && bounds.y + bounds.height <= surface.y + surface.height + 1;
                });
            })
            """));
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        var host = session.Page.Locator("[data-cfx-html-sizing=preserve-size]"); await host.FocusAsync(); await session.Page.Keyboard.PressAsync("End");
        Assert.True(await host.EvaluateAsync<double>("element => { element.scrollLeft = element.scrollWidth; return element.scrollLeft; }") >= 0);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output); var path = Path.Combine(output, "state-floating-browser-" + width + (dark ? "-dark" : "-light"));
            File.WriteAllText(path + ".svg", svg); File.WriteAllBytes(path + ".native.png", png);
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = path + ".browser.png", FullPage = true });
        }
    }
}

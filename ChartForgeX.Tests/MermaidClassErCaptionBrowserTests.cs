using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class MermaidClassErCaptionBrowserTests {
    [BrowserTheory]
    [InlineData("class", false, 390)]
    [InlineData("class", true, 390)]
    [InlineData("class", false, 1100)]
    [InlineData("class", true, 1100)]
    [InlineData("er", false, 390)]
    [InlineData("er", true, 390)]
    [InlineData("er", false, 1100)]
    [InlineData("er", true, 1100)]
    public async Task SafeRichCaptionsAndShellsRemainContainedInKeyboardScrollableNativeHosts(string family, bool dark, int width) {
        if (!InteractiveChartBrowser.Enabled) return;
        var result = MermaidRenderer.Render(MermaidClassErCaptionTests.Source(family), MermaidClassErCaptionTests.Options(dark, width));
        Assert.False(result.HasErrors); var artifact = result.Artifact!;
        var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var html = artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.PreserveSize });
        await using var session = await BrowserProofFonts.OpenAsync(html, width, 720);
        Assert.Equal(0, await session.Page.Locator("script, image, foreignObject").CountAsync());
        Assert.Equal(family == "class" ? 2 : 3, await session.Page.Locator("[data-cfx-role=topology-node]").CountAsync());
        Assert.Equal(family == "class" ? 1 : 2, await session.Page.Locator("[data-cfx-role=topology-group-surface]").CountAsync());
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        Assert.True(await session.Page.EvaluateAsync<bool>("""
            () => [...document.querySelectorAll('[data-cfx-role="topology-node"]')].every(node => {
                const marks = [...node.querySelectorAll('[data-cfx-role="topology-node-surface"]')].map(mark => mark.getBBox());
                const texts = [...node.querySelectorAll('[data-cfx-role="topology-node-label"] text, [data-cfx-role="topology-node-label"] [data-cfx-native-text-ink]')].map(mark => mark.getBBox());
                const left = Math.min(...marks.map(r => r.x)), top = Math.min(...marks.map(r => r.y));
                const right = Math.max(...marks.map(r => r.x + r.width)), bottom = Math.max(...marks.map(r => r.y + r.height));
                return texts.length > 0 && texts.every(r => r.x >= left - 1 && r.x + r.width <= right + 1 && r.y >= top - 1 && r.y + r.height <= bottom + 1);
            })
            """));
        var host = session.Page.Locator("[data-cfx-html-sizing=preserve-size]"); await host.FocusAsync();
        await session.Page.Keyboard.PressAsync("End");
        var scroll = await host.EvaluateAsync<double>("element => { element.scrollLeft = element.scrollWidth; return element.scrollLeft; }");
        Assert.True(scroll >= 0); InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output); var path = Path.Combine(output, family + "-captions-browser-" + width + (dark ? "-dark" : "-light"));
            File.WriteAllText(path + ".svg", svg); File.WriteAllBytes(path + ".native.png", png);
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = path + ".browser.png", FullPage = true });
        }
    }
}

using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class MermaidTreeViewRowsBrowserTests {
    [BrowserTheory]
    [InlineData(false, 390)]
    [InlineData(true, 390)]
    [InlineData(false, 1100)]
    [InlineData(true, 1100)]
    public async Task NaturalRowsKeepTheirCaptionsInsideTheBrowserFrameWithNoExecutableAssets(bool dark, int width) {
        if (!InteractiveChartBrowser.Enabled) return;
        var result = MermaidRenderer.Render(MermaidTreeViewNativeRowsTests.Source, MermaidTreeViewNativeRowsTests.Options(dark, width));
        Assert.Empty(result.Diagnostics); var artifact = result.Artifact!;
        var svg = artifact.ToSvg(); var png = artifact.ToPng();
        await using var session = await BrowserProofFonts.OpenAsync(svg, width, 600);
        Assert.Equal(5, await session.Page.Locator("[data-cfx-role=treeview-row]").CountAsync());
        Assert.Equal(0, await session.Page.Locator("script, image, foreignObject").CountAsync());
        Assert.Equal(2, await session.Page.Locator("[data-cfx-role=treeview-description]").CountAsync());
        Assert.True(await session.Page.EvaluateAsync<bool>("""
            () => {
                const svg = document.querySelector('svg'), frame = svg.getBoundingClientRect();
                return [...svg.querySelectorAll('text')].every(text => {
                    const r = text.getBoundingClientRect();
                    return r.left >= frame.left - 1 && r.right <= frame.right + 1 && r.top >= frame.top - 1 && r.bottom <= frame.bottom + 1;
                });
            }
            """));
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output); var path = Path.Combine(output, "treeview-browser-" + width + "-" + (dark ? "dark" : "light"));
            File.WriteAllText(path + ".svg", svg); File.WriteAllBytes(path + ".native.png", png);
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = path + ".browser.png", FullPage = true });
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}

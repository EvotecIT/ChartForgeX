using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class MermaidCynefinNativeDepthBrowserTests {
    [BrowserTheory]
    [InlineData(390, false)]
    [InlineData(1100, false)]
    [InlineData(390, true)]
    [InlineData(1100, true)]
    public async Task CynefinAndOrdinaryBoundaryUseStaticNativePathsAtEachHostSize(int width, bool dark) {
        if (!InteractiveChartBrowser.Enabled) return;
        var source = "%%{init: {'theme':'" + (dark ? "dark" : "default") + "','cynefin':{'seed':41}}}%%\n" + MermaidCynefinNativeDepthTests.Body;
        var result = MermaidRenderer.Render(source);
        Assert.Empty(result.Diagnostics);
        var artifact = result.Artifact!;
        await using (var session = await InteractiveChartBrowser.OpenAsync(artifact.ToHtmlPage(), width, 760)) {
            Assert.Equal(8, await session.Page.Locator("[data-path-decoration-id]").CountAsync());
            Assert.Contains("Disorder", await session.Page.Locator("body").InnerTextAsync());
            Assert.Contains("Investigate context", await session.Page.Locator("body").InnerTextAsync());
            Assert.Equal(0, await session.Page.Locator("script").CountAsync());
            await Capture(session.Page, artifact, "cynefin-native-" + width + (dark ? "-dark" : "-light"));
            InteractiveChartBrowser.AssertNoConsoleErrors(session);
        }
        var ordinary = TopologyPathDecorationRecipe.Create("wide", dark ? VisualThemeMode.Dark : VisualThemeMode.Light).ToVisualArtifact();
        await using (var session = await InteractiveChartBrowser.OpenAsync(ordinary.ToHtmlPage(), width, 500)) {
            Assert.Equal(1, await session.Page.Locator("[data-path-decoration-id=\"maintenance-zone\"]").CountAsync());
            Assert.Contains("Gateway", await session.Page.Locator("body").InnerTextAsync());
            await Capture(session.Page, ordinary, "topology-boundary-" + width + (dark ? "-dark" : "-light"));
            InteractiveChartBrowser.AssertNoConsoleErrors(session);
        }
    }

    [BrowserTheory]
    [InlineData(390, false)] [InlineData(1100, false)] [InlineData(390, true)] [InlineData(1100, true)]
    public async Task SavedCynefinInkAndAuthoredFontPaintUseTheSameNativeSceneAtEachHostSize(int width, bool dark) {
        if (!InteractiveChartBrowser.Enabled) return;
        var source = "%%{init: {'theme':'" + (dark ? "dark" : "default") + "','themeVariables':{'cynefin':{'domainFontSize':22,'itemFontSize':15,'labelColor':'#653412','textColor':'#123456'}}}}%%\n" + MermaidCynefinNativeDepthTests.Body;
        var result = MermaidRenderer.Render(source, BrowserProofFonts.MermaidOptions()); Assert.Empty(result.Diagnostics);
        var saved = result.Artifact!.ToInterchangeEnvelope();
        var restored = VisualArtifactInterchangeEnvelope.FromUtf8Json(saved.ToUtf8Json()).ToTopologyChart().ToVisualArtifact();
        await using var session = await BrowserProofFonts.OpenAsync(restored.ToHtmlPage(), width, 800);
        Assert.Equal(8, await session.Page.Locator("[data-path-decoration-id]").CountAsync());
        Assert.Equal(0, await session.Page.Locator("script, image, foreignObject").CountAsync());
        Assert.True(await session.Page.EvaluateAsync<bool>("" +
            "() => { const rows=[...document.querySelectorAll('[data-cfx-role=topology-node-subtitle] text')];" +
            "return rows.length>0 && rows.every(row => {const style=getComputedStyle(row);return style.fill==='rgb(18, 52, 86)'&&Math.abs(parseFloat(style.fontSize)-14)<.01;}); }"));
        Assert.Contains("Investigate context", await session.Page.Locator("body").InnerTextAsync());
        await Capture(session.Page, restored, "cynefin-restored-fonts-" + width + (dark ? "-dark" : "-light"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    private static async Task Capture(IPage page, VisualArtifact artifact, string name) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".browser.png"), FullPage = true });
        await File.WriteAllTextAsync(Path.Combine(output, name + ".svg"), artifact.ToSvg());
        await File.WriteAllBytesAsync(Path.Combine(output, name + ".native.png"), artifact.ToPng());
    }
}

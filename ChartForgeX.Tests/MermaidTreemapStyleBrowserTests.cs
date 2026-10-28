using ChartForgeX.Primitives;
using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class MermaidTreemapStyleBrowserTests {
    [BrowserTheory]
    [InlineData(390, false)]
    [InlineData(390, true)]
    [InlineData(1100, false)]
    [InlineData(1100, true)]
    public async Task SharedClassPaintAndFormattedValuesRemainVisibleInStaticHosts(int width, bool dark) {
        if (!InteractiveChartBrowser.Enabled) return;
        var result = MermaidRenderer.Render(MermaidTreemapStyleMappingTests.GallerySource, MermaidTreemapStyleMappingTests.Options(width, dark));
        Assert.Empty(result.Diagnostics); var artifact = result.Artifact!;
        await using var session = await BrowserProofFonts.OpenAsync(artifact.ToHtmlPage(), width, 700);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        var node = page.Locator("[data-cfx-target-id='node-2']");
        Assert.Equal("1250.5", await node.GetAttributeAsync("data-cfx-value"));
        Assert.Equal("$1,250.50", await node.GetAttributeAsync("data-cfx-formatted-value"));
        var mark = node.Locator("[data-cfx-role='treemap-tile-mark']");
        Assert.Equal("rgb(29, 78, 216)", await mark.EvaluateAsync<string>("node => getComputedStyle(node).fill"));
        Assert.Equal("rgb(20, 83, 45)", await mark.EvaluateAsync<string>("node => getComputedStyle(node).stroke"));
        Assert.Contains("Shared", await node.Locator("[data-cfx-role='treemap-label']").TextContentAsync());
        Assert.Equal("zero", await page.Locator("[data-cfx-target-id='node-3']").GetAttributeAsync("data-cfx-geometry-status"));
        Assert.Equal(0, await page.Locator("script,foreignObject").CountAsync());
        Assert.True(await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory);
            var name = "treemap-style-host-" + width + "-" + (dark ? "dark" : "light");
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".png"), FullPage = true });
            File.WriteAllText(Path.Combine(directory, name + ".svg"), artifact.ToSvg());
            File.WriteAllBytes(Path.Combine(directory, name + "-native.png"), artifact.ToPng());
        }
        Assert.Empty(errors); InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
    [BrowserTheory]
    [InlineData(390, false)]
    [InlineData(390, true)]
    [InlineData(1100, false)]
    [InlineData(1100, true)]
    public async Task TranslucentClassCaptionsUseThePaintedAncestorInTheRealStaticHost(int width, bool dark) {
        if (!InteractiveChartBrowser.Enabled) return;
        var result = MermaidRenderer.Render(MermaidTreemapTranslucentClassTests.Source(dark), MermaidTreemapStyleMappingTests.Options(width, dark));
        Assert.Empty(result.Diagnostics); var artifact = result.Artifact!;
        var chart = Assert.IsType<Chart>(artifact.Model);
        var prepared = chart.Prepare(ChartForgeX.Rendering.VisualExportRequest.ForChart(chart).Context);
        var surface = HierarchyCaptionSurfaceTestFacts.FrameSurface(prepared);
        foreach (var mark in prepared.Scene.Nodes.OfType<ChartForgeX.Rendering.VisualSceneRectangle>().Where(item => item.Role is "treemap-group-mark" or "treemap-tile-mark"))
            surface = ChartForgeX.Rendering.ChartColorMath.CompositeOverOpaque(mark.Fill!.Value, surface);
        var ink = Assert.Single(prepared.Scene.Nodes.OfType<ChartForgeX.Rendering.VisualSceneText>(), item => item.Role == "treemap-label").Color;
        await using var session = await BrowserProofFonts.OpenAsync(artifact.ToHtmlPage(), width, 700);
        var caption = session.Page.Locator("[data-cfx-target-id='node-2'] [data-cfx-role='treemap-label']");
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory); var name = "treemap-alpha-class-host-" + width + "-" + dark;
            File.WriteAllText(Path.Combine(directory, name + ".svg"), artifact.ToSvg()); File.WriteAllBytes(Path.Combine(directory, name + ".native.png"), artifact.ToPng());
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".browser.png"), FullPage = true });
        }
        Assert.True(await caption.IsVisibleAsync());
        var paintedInks = await caption.Locator("text").EvaluateAllAsync<string[]>("nodes => nodes.map(node => getComputedStyle(node).fill)");
        Assert.NotEmpty(paintedInks);
        foreach (var paintedInk in paintedInks) {
            Assert.True(ChartForgeX.SvgRaster.SvgRasterColor.TryParse(paintedInk, out var actual));
            Assert.Equal(ink, actual);
            Assert.True(ChartForgeX.Rendering.ChartColorMath.ContrastRatio(actual, surface) >= 4.5);
        }
        Assert.Equal(0, await session.Page.Locator("script,foreignObject").CountAsync());
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

}

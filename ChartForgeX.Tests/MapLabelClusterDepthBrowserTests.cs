using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class MapLabelClusterDepthBrowserTests {
    [BrowserTheory]
    [InlineData(390, VisualThemeMode.Light)]
    [InlineData(390, VisualThemeMode.Dark)]
    [InlineData(800, VisualThemeMode.Light)]
    [InlineData(800, VisualThemeMode.Dark)]
    public async Task DerivedCountSelectionExposesContributorsWhileRawClusteredSourcesStayNonTargetable(int width, VisualThemeMode mode) {
        if (!Enabled) return;
        var chart = MapNavigationDepthRecipe.Create(width == 390 ? "compact" : "wide", mode);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(Options), width + 24, 900);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        await page.EvaluateAsync("() => { window.mapDepth = ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart')); }");
        var cluster = page.Locator("[data-cfx-role='dotted-map-cluster-source']"); Assert.Equal(1, await cluster.CountAsync());
        Assert.Equal("button", await cluster.GetAttributeAsync("role"));
        Assert.Equal(5, await page.Locator("[data-cfx-role='dotted-map-point-source']").CountAsync());
        Assert.Equal(3, await page.Locator("[data-cfx-role='dotted-map-point-source'][data-cfx-clustered='true']").CountAsync());
        Assert.Equal(0, await page.Locator("[data-cfx-role='dotted-map-point-source'][data-cfx-visible='false'][role='button'],[data-cfx-role='dotted-map-point-source'][data-cfx-visible='false'][tabindex]").CountAsync());
        var identity = await cluster.GetAttributeAsync("data-cfx-derived-identity");
        await FocusWithKeyboardAsync(page, cluster); await page.Keyboard.PressAsync("Space");
        var target = Assert.Single((await page.EvaluateAsync<JsonElement>("() => mapDepth.getSelection()")).EnumerateArray());
        Assert.Equal(new[] { 1, 2, 3 }, target.GetProperty("sourcePoints").EnumerateArray().Select(value => value.GetInt32()));
        Assert.Equal(new[] { "intake", "processing", "dispatch" }, target.GetProperty("observationIds").EnumerateArray().Select(value => value.GetString()));
        Assert.False(target.TryGetProperty("observationId", out _));
        await page.SetViewportSizeAsync(width == 390 ? 824 : 414, 900);
        Assert.Equal(identity, await cluster.GetAttributeAsync("data-cfx-derived-identity"));
        Assert.True(await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth + 1"));
        await page.SetViewportSizeAsync(width + 24, 900);
        var svgDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export='svg']").ClickAsync());
        var svg = XDocument.Parse(await File.ReadAllTextAsync(await svgDownload.PathAsync() ?? throw new InvalidOperationException("No map SVG download.")));
        Assert.DoesNotContain(svg.Descendants(), node => node.Name.LocalName == "script");
        Assert.Equal(5, svg.Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "dotted-map-point-source"));
        var aggregate = Assert.Single(svg.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "dotted-map-cluster-source");
        Assert.Equal("1,2,3", (string?)aggregate.Attribute("data-cfx-source-points")); Assert.Null(aggregate.Attribute("data-cfx-observation-id"));
        var pngDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export='png']").ClickAsync());
        var image = RasterImageDecoder.Decode(await File.ReadAllBytesAsync(await pngDownload.PathAsync() ?? throw new InvalidOperationException("No map PNG download.")));
        Assert.Equal(width, image.Width); Assert.Equal(600, image.Height);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            if (!Path.IsPathRooted(directory)) throw new InvalidOperationException("Captures require an absolute task path.");
            Directory.CreateDirectory(directory); var stem = Path.Combine(directory, "map-navigation-" + width + "-" + mode);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".cluster.browser.png", FullPage = true });
            await svgDownload.SaveAsAsync(stem + ".export.svg"); await pngDownload.SaveAsAsync(stem + ".export.png");
            await File.WriteAllTextAsync(stem + ".selection.json", target.GetRawText());
        }
        chart.Series[1].WithMapClustering(null);
        await page.EvaluateAsync("html => mapDepth.update(html)", chart.ToInteractiveHtmlFragmentWithoutAssets(Options));
        Assert.Equal(0, await page.Locator("[data-cfx-role='dotted-map-cluster-source']").CountAsync());
        var raw = page.Locator("[data-cfx-role='dotted-map-point-source'][data-cfx-observation-id='intake']");
        Assert.Equal("intake", await raw.GetAttributeAsync("data-cfx-feature-id"));
        await FocusWithKeyboardAsync(page, raw); await page.Keyboard.PressAsync("Space");
        Assert.Contains((await page.EvaluateAsync<JsonElement>("() => mapDepth.getSelection()")).EnumerateArray(), item => item.TryGetProperty("observationId", out var id) && id.GetString() == "intake");
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }
    private static void Options(HtmlChartInteractionOptions options) {
        options.Interaction.Enable(ChartInteractionFeatures.Export); options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit; options.Tooltip.PinOnSelect = false;
    }
}

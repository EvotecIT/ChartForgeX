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
public sealed class AntimeridianMapViewportBrowserTests {
    [BrowserTheory]
    [InlineData("compact", VisualThemeMode.Light)]
    [InlineData("wide", VisualThemeMode.Dark)]
    [InlineData("compact-mercator", VisualThemeMode.Dark)]
    [InlineData("mercator", VisualThemeMode.Light)]
    public async Task RawSeamTargetsRetainDistinctIdsThroughSelectionNativeUpdateFitAndDownloads(string variant, VisualThemeMode mode) {
        if (!Enabled) return;
        var chart = DatelineMapRecipe.Create(variant, mode); var width = variant.StartsWith("compact", StringComparison.Ordinal) ? 390 : 800;
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(Options), width + 24, 850);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        await page.EvaluateAsync("() => { window.datelineChart = ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart')); }");
        var east = page.Locator(Point("seam-east")); var west = page.Locator(Point("seam-west"));
        Assert.Equal("180", await east.GetAttributeAsync("data-cfx-longitude")); Assert.Equal("-180", await west.GetAttributeAsync("data-cfx-longitude"));
        Assert.NotEqual(await east.GetAttributeAsync("data-cfx-target-id"), await west.GetAttributeAsync("data-cfx-target-id"));
        await FocusWithKeyboardAsync(page, east); await page.Keyboard.PressAsync("Space");
        Assert.Contains((await Selection(page)).EnumerateArray(), item => item.TryGetProperty("observationId", out var id) && id.GetString() == "seam-east");
        await FocusWithKeyboardAsync(page, west); await page.Keyboard.PressAsync("Space");
        Assert.Contains((await Selection(page)).EnumerateArray(), item => item.TryGetProperty("observationId", out var id) && id.GetString() == "seam-west");
        var hidden = page.Locator(Point("outside")); Assert.Equal("false", await hidden.GetAttributeAsync("data-cfx-visible"));
        Assert.Null(await hidden.GetAttributeAsync("tabindex")); Assert.NotEqual("button", await hidden.GetAttributeAsync("role"));
        var fiji = page.Locator(Point("fiji")); await FocusWithKeyboardAsync(page, fiji); await page.Keyboard.PressAsync("Space");
        chart.Series[0].UpdateMapPoint(0, new ChartMapPoint("Fiji moved", -179, -18, 70));
        await page.EvaluateAsync("html => datelineChart.update(html)", chart.ToInteractiveHtmlFragmentWithoutAssets(Options));
        Assert.Equal("-179", await page.Locator(Point("fiji")).GetAttributeAsync("data-cfx-longitude"));
        Assert.Contains((await Selection(page)).EnumerateArray(), item => item.TryGetProperty("observationId", out var id) && id.GetString() == "fiji");
        await page.SetViewportSizeAsync(width == 390 ? 824 : 414, 850);
        Assert.Equal("-170", await page.Locator("[data-cfx-role='geographic-map']").GetAttributeAsync("data-cfx-viewport-max-longitude"));
        Assert.True(await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth + 1"));
        await page.SetViewportSizeAsync(width + 24, 850);
        var svgDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export='svg']").ClickAsync());
        var svg = XDocument.Parse(await File.ReadAllTextAsync(await svgDownload.PathAsync() ?? throw new InvalidOperationException("No dateline SVG download.")));
        Assert.DoesNotContain(svg.Descendants(), node => node.Name.LocalName == "script");
        var points = svg.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "dotted-map-point-source").ToArray(); Assert.Equal(5, points.Length);
        Assert.Equal("180", (string?)Assert.Single(points, node => (string?)node.Attribute("data-cfx-observation-id") == "seam-east").Attribute("data-cfx-longitude"));
        Assert.Equal("-180", (string?)Assert.Single(points, node => (string?)node.Attribute("data-cfx-observation-id") == "seam-west").Attribute("data-cfx-longitude"));
        var partial = Assert.Single(svg.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "dotted-map-connector-source" && (string?)node.Attribute("data-cfx-connector") == "1");
        Assert.Equal("3", (string?)partial.Attribute("data-cfx-waypoint-count")); Assert.Equal("true", (string?)partial.Attribute("data-cfx-route-view-clipped"));
        var pngDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export='png']").ClickAsync());
        var image = RasterImageDecoder.Decode(await File.ReadAllBytesAsync(await pngDownload.PathAsync() ?? throw new InvalidOperationException("No dateline PNG download.")));
        Assert.Equal(width, image.Width); Assert.Equal(520, image.Height);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            if (!Path.IsPathRooted(directory)) throw new InvalidOperationException("Captures require an absolute task path.");
            Directory.CreateDirectory(directory); var stem = Path.Combine(directory, "dateline-map-" + variant + "-" + mode);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
            await svgDownload.SaveAsAsync(stem + ".export.svg"); await pngDownload.SaveAsAsync(stem + ".export.png");
            await File.WriteAllTextAsync(stem + ".selection.json", (await Selection(page)).GetRawText());
        }
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }
    private static Task<JsonElement> Selection(IPage page) => page.EvaluateAsync<JsonElement>("() => datelineChart.getSelection()");
    private static string Point(string id) => "[data-cfx-role='dotted-map-point-source'][data-cfx-observation-id='" + id + "']";
    private static void Options(HtmlChartInteractionOptions options) {
        options.Interaction.Enable(ChartInteractionFeatures.Export); options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit; options.Tooltip.PinOnSelect = false;
    }
}

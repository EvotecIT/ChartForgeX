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
public sealed class MapCompositionDepthBrowserTests {
    [BrowserTheory]
    [InlineData(390, VisualThemeMode.Light)]
    [InlineData(390, VisualThemeMode.Dark)]
    [InlineData(800, VisualThemeMode.Light)]
    [InlineData(800, VisualThemeMode.Dark)]
    public async Task IndependentTileSelectionAndFeatureMetadataSurviveNativeUpdateReorderAndExports(int width, VisualThemeMode mode) {
        if (!Enabled) return;
        var initial = Fixture(width, mode); var updated = Fixture(width, mode);
        var summary = updated.Series[2]; summary.UpdateRegionValue(2, new ChartRegionMapItem("W", 35)); updated.MoveSeries(summary, 0);
        await using var session = await OpenAsync(initial.ToInteractiveHtmlPage(Options), width + 24, 900);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        await page.EvaluateAsync("() => { window.depthMap = ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart')); }");
        Assert.Equal(3, await page.Locator("[data-cfx-role='map-color-scale-source']").CountAsync());
        var tile = page.Locator(Tile("west"));
        await FocusWithKeyboardAsync(page, tile); await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await tile.GetAttributeAsync("aria-selected"));
        Assert.Equal("92 %", await tile.GetAttributeAsync("data-cfx-formatted-value"));
        var point = page.Locator(Depot);
        Assert.Equal("depot-madrid", await point.GetAttributeAsync("data-cfx-feature-id"));
        var properties = JsonDocument.Parse(await point.GetAttributeAsync("data-cfx-feature-properties") ?? throw new InvalidOperationException("Missing native feature properties."));
        Assert.Equal("Example Logistics", properties.RootElement.GetProperty("operator").GetString());
        Assert.Equal(2, properties.RootElement.GetProperty("priority").GetInt32());
        await FocusWithKeyboardAsync(page, point); await page.Keyboard.PressAsync("Space");
        var selection = await page.EvaluateAsync<JsonElement>("() => depthMap.getSelection()");
        Assert.Contains(selection.EnumerateArray(), item => item.GetProperty("observationId").GetString() == "madrid");
        Assert.DoesNotContain(selection.EnumerateArray(), item => item.TryGetProperty("observationId", out var id) && id.GetString() == "depot-madrid");
        await page.EvaluateAsync("html => depthMap.update(html)", updated.ToInteractiveHtmlFragmentWithoutAssets(Options));
        Assert.Equal("0", await page.Locator(Tile("west")).GetAttributeAsync("data-cfx-series"));
        Assert.Equal("35 %", await page.Locator(Tile("west")).GetAttributeAsync("data-cfx-formatted-value"));
        Assert.Equal("depot-madrid", await page.Locator(Depot).GetAttributeAsync("data-cfx-feature-id"));
        Assert.Contains((await page.EvaluateAsync<JsonElement>("() => depthMap.getSelection()")).EnumerateArray(), item => item.GetProperty("observationId").GetString() == "madrid");
        await page.SetViewportSizeAsync(width == 390 ? 824 : 414, 900);
        Assert.Equal("35 %", await page.Locator(Tile("west")).GetAttributeAsync("data-cfx-formatted-value"));
        Assert.Equal("depot-madrid", await page.Locator(Depot).GetAttributeAsync("data-cfx-feature-id"));
        Assert.True(await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth + 1"));
        await page.SetViewportSizeAsync(width + 24, 900);
        var svgDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var svgText = await File.ReadAllTextAsync(await svgDownload.PathAsync() ?? throw new InvalidOperationException("No map SVG download."));
        var svg = XDocument.Parse(svgText);
        Assert.DoesNotContain(svg.Descendants(), node => node.Name.LocalName == "script");
        Assert.Equal(3, svg.Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "tile-map-region-source"));
        var source = Assert.Single(svg.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "dotted-map-point-source" && (string?)node.Attribute("data-cfx-observation-id") == "madrid");
        Assert.Equal("depot-madrid", (string?)source.Attribute("data-cfx-feature-id"));
        Assert.Equal("independent-tiles", (string?)Assert.Single(svg.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "tile-map").Attribute("data-cfx-coordinate-system"));
        var pngDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=png]").ClickAsync());
        var png = RasterImageDecoder.Decode(await File.ReadAllBytesAsync(await pngDownload.PathAsync() ?? throw new InvalidOperationException("No map PNG download.")));
        Assert.Equal(width, png.Width); Assert.Equal(600, png.Height);
        Assert.True(await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth + 1"));
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            if (!Path.IsPathRooted(directory)) throw new InvalidOperationException("Captures require an absolute task-owned path.");
            Directory.CreateDirectory(directory); var stem = Path.Combine(directory, "map-depth-" + width + "-" + mode);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
            await svgDownload.SaveAsAsync(stem + ".export.svg"); await pngDownload.SaveAsAsync(stem + ".export.png");
            await File.WriteAllTextAsync(stem + ".runtime.json", await page.EvaluateAsync<string>("""
                () => JSON.stringify({selection:depthMap.getSelection(),features:[...document.querySelectorAll('[data-cfx-feature-id]')].map(n=>({...n.dataset})),
                    tiles:[...document.querySelectorAll('[data-cfx-role=tile-map-region-source]')].map(n=>({...n.dataset}))},null,2)
                """));
        }
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }

    private static Chart Fixture(int width, VisualThemeMode mode) {
        var chart = MapCompositionDepthRecipe.Create(width == 390 ? "compact" : "wide", mode);
        chart.Series[2].WithObservationIds(new[] { new ChartObservationId("central"), new ChartObservationId("north"), new ChartObservationId("west") });
        return chart;
    }
    private static void Options(HtmlChartInteractionOptions options) {
        options.Interaction.Enable(ChartInteractionFeatures.Export);
        options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit; options.Tooltip.PinOnSelect = false;
    }
    private static string Tile(string id) => "[data-cfx-role='tile-map-region-source'][data-cfx-series-key='summary'][data-cfx-observation-id='" + id + "']";
    private const string Depot = "[data-cfx-role='dotted-map-point-source'][data-cfx-series-key='depots'][data-cfx-observation-id='madrid']";
}

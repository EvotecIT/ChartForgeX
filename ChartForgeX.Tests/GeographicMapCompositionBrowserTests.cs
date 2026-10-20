using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Geographic datasets retain independent measurements and authored identity through actual native HTML interaction.</summary>
public sealed class GeographicMapCompositionBrowserTests {
    [BrowserTheory]
    [InlineData(390, VisualThemeMode.Light)]
    [InlineData(800, VisualThemeMode.Dark)]
    public async Task IndependentMapKeysReadoutsAndSelectionSurviveTypedReplacementAndDatasetOrder(int width, VisualThemeMode mode) {
        if (!Enabled) return;
        var initial = Fixture(width, mode);
        var updated = Fixture(width, mode);
        var sites = updated.Series[1];
        sites.ReplaceMapPoints(new[] {
            new ChartMapPoint("North gateway", 10, 60, 100).WithColorValue(30),
            new ChartMapPoint("West gateway", 0, 50, 25).WithColorValue(-5)
        }).WithObservationIds(new[] { new ChartObservationId("north"), new ChartObservationId("west") });
        updated.MoveSeries(sites, 0);
        await using var session = await OpenAsync(initial.ToInteractiveHtmlPage(Options), width + 24, 850);
        var page = session.Page;
        var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        await page.EvaluateAsync("() => { window.layerMap = ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart')); }");
        Assert.Equal(2, await page.Locator("[data-cfx-role='map-color-scale-source']").CountAsync());
        Assert.Equal(1, await page.Locator("[data-cfx-role='map-size-key-1']").CountAsync());
        Assert.Equal(0, await page.Locator("[data-cfx-role='map-color-scale-source'] [data-cfx-target-kind], [data-cfx-role='map-size-key-1'] [data-cfx-target-kind]").CountAsync());
        Assert.Equal(new[] { "0 trips", "100 trips" }, await page.Locator("[data-cfx-role='map-size-key-1-label']").AllTextContentsAsync());
        Assert.Equal(new[] { "West gateway 0 trips", "North gateway 100 trips" },
            await page.Locator("[data-cfx-role='dotted-map-data-label']").AllTextContentsAsync());
        await MoveToAsync(page, Mark("west") + " [data-cfx-role='dotted-map-point']");
        Assert.Equal("0 trips", await Row(page, "Traffic")); Assert.Equal("-10 min", await Row(page, "Delay"));
        Assert.Equal("0", await page.Locator(Mark("west")).GetAttributeAsync("data-cfx-value"));
        Assert.Equal("-10", await page.Locator(Mark("west")).GetAttributeAsync("data-cfx-color-value"));
        await Capture(page, width, mode, "pointer");
        await MoveAwayAsync(page); await FocusWithKeyboardAsync(page, page.Locator(Mark("west")));
        Assert.Equal("0 trips", await Row(page, "Traffic")); Assert.Equal("-10 min", await Row(page, "Delay"));
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await page.Locator(Mark("west")).GetAttributeAsync("aria-selected"));
        await page.EvaluateAsync("html => layerMap.update(html)", updated.ToInteractiveHtmlFragmentWithoutAssets(Options));
        Assert.Equal("0", await page.Locator(Mark("west")).GetAttributeAsync("data-cfx-series"));
        Assert.Equal("1", await page.Locator(Mark("west")).GetAttributeAsync("data-cfx-source-point"));
        Assert.Equal("true", await page.Locator(Mark("west")).GetAttributeAsync("aria-selected"));
        var selection = Assert.Single((await page.EvaluateAsync<JsonElement>("() => layerMap.getSelection()")).EnumerateArray());
        Assert.Equal("gateways", selection.GetProperty("seriesKey").GetString()); Assert.Equal("west", selection.GetProperty("observationId").GetString());
        await FocusWithKeyboardAsync(page, page.Locator(Mark("west")));
        Assert.Equal("25 trips", await Row(page, "Traffic")); Assert.Equal("-5 min", await Row(page, "Delay"));
        Assert.Equal(1, await page.Locator("[data-cfx-role='map-size-key-0']").CountAsync());
        Assert.Equal(2, await page.Locator("[data-cfx-role='map-color-scale-source']").CountAsync());
        Assert.True(await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth + 1"));
        await Capture(page, width, mode, "replaced-keyboard");
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }

    private static Chart Fixture(int width, VisualThemeMode mode) {
        var chart = GeographicMapCompositionTests.Fixture(ChartMapGeoJsonProjection.Equirectangular, width, mode).WithSize(width, 600);
        chart.Series[1].ConfigureColorDimension(options => options.ValueFormat = ChartValueFormat.Number("0 'min'"));
        chart.Series[1].ConfigureMapPointSize(options => {
            options.WithSizeDomain(0, 100); options.MinimumRadius = 3; options.MaximumRadius = 11;
            options.SizeDimension.Name = "Traffic"; options.SizeDimension.ShowLegend = true;
            options.SizeDimension.ValueFormat = ChartValueFormat.Number("0 'trips'");
        });
        return chart;
    }
    private static void Options(HtmlChartInteractionOptions options) {
        options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
        options.Tooltip.Mode = HtmlChartTooltipMode.Single; options.Tooltip.PinOnSelect = false;
    }
    private static string Mark(string id) => "[data-cfx-role='dotted-map-point-source'][data-cfx-series-key='gateways'][data-cfx-observation-id='" + id + "']";
    private static Task<string> Row(IPage page, string name) => page.Locator(".cfx-tooltip dt").Filter(new LocatorFilterOptions { HasText = name }).Locator("+ dd").InnerTextAsync();
    private static async Task Capture(IPage page, int width, VisualThemeMode mode, string state) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        if (!Path.IsPathRooted(directory)) throw new InvalidOperationException("Browser evidence requires an absolute task-owned path.");
        Directory.CreateDirectory(directory);
        var stem = Path.Combine(directory, "geographic-map-" + width + "-" + mode + "-" + state);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
        await File.WriteAllTextAsync(stem + ".runtime.json", await page.EvaluateAsync<string>("""
            () => JSON.stringify({ tooltip: document.querySelector('.cfx-tooltip')?.innerText, selection: layerMap.getSelection(),
                keys: [...document.querySelectorAll('[data-cfx-role=map-color-scale-source]')].map(node=>({...node.dataset})),
                points: [...document.querySelectorAll('[data-cfx-role=dotted-map-point-source]')].map(node=>({...node.dataset})) }, null, 2)
            """));
    }
}

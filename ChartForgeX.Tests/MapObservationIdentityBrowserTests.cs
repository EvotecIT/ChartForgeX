using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class MapObservationIdentityBrowserTests {
    [BrowserTheory]
    [InlineData(ChartSeriesKind.CalendarHeatmap)]
    [InlineData(ChartSeriesKind.DottedMap)]
    [InlineData(ChartSeriesKind.RegionMap)]
    [InlineData(ChartSeriesKind.TileMap)]
    public async Task SelectionAndReadoutsRetainObservationIdsAfterTypedUpdates(ChartSeriesKind kind) {
        if (!Enabled) return;
        var signedMap = kind is ChartSeriesKind.RegionMap or ChartSeriesKind.TileMap;
        var expectedRaw = signedMap ? "-7" : "7";
        var expectedDisplay = signedMap ? "-7.0 °C" : "7";
        var chart = Chart.Create().WithSize(640, 360).WithTitle("Observed activity");
        switch (kind) {
            case ChartSeriesKind.CalendarHeatmap:
                chart.AddCalendarHeatmap("Days", new[] {
                    new ChartCalendarHeatmapItem(new DateTime(2026, 1, 5), 1),
                    new ChartCalendarHeatmapItem(new DateTime(2026, 1, 5), 2)
                }, id: new ChartSeriesId("activity"));
                break;
            case ChartSeriesKind.DottedMap:
                chart.AddDottedMap("Locations", new[] { new ChartMapPoint("Warsaw", 21, 52, 3) });
                break;
            case ChartSeriesKind.RegionMap:
                chart.AddRegionMap("Regions", new ChartMapDefinition("regions", "Regions", 100, 50, new[] {
                    new ChartMapRegion("A", "Alpha", "M0 0H50V50H0Z"), new ChartMapRegion("B", "Beta", "M50 0H100V50H50Z")
                }), new[] { new ChartRegionMapItem("A", 3) }, id: new ChartSeriesId("activity"));
                break;
            default:
                chart.AddTileMap("Tiles", new ChartTileMapDefinition("tiles", "Tiles", new[] {
                    new ChartTileMapRegion("A", "Alpha", 0, 0), new ChartTileMapRegion("B", "Beta", 1, 0)
                }), new[] { new ChartRegionMapItem("A", 3) }, id: new ChartSeriesId("activity"));
                break;
        }
        var series = chart.Series[0].WithObservationId(0, new ChartObservationId("observed-a"));
        switch (kind) {
            case ChartSeriesKind.CalendarHeatmap:
                series.UpdateCalendarItem(0, new ChartCalendarHeatmapItem(new DateTime(2026, 1, 5), 7));
                break;
            case ChartSeriesKind.DottedMap:
                series.UpdateMapPoint(0, new ChartMapPoint("Warsaw", 21, 52, 7));
                break;
            default:
                series.UpdateRegionValue(0, new ChartRegionMapItem("Alpha", -7));
                break;
        }
        if (signedMap) chart.WithMapColorScale(ChartColorScale.Diverging(ChartColor.Black, ChartColor.FromHex("#808080"), ChartColor.White, 0)
                .WithValueRange(-10, 10))
            .ConfigureMapColorDimension(options => {
                options.Name = "Temperature";
                options.ValueFormat = ChartValueFormat.Number("0.0 '°C'", System.Globalization.CultureInfo.InvariantCulture);
            });
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), 700, 540);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        var target = page.Locator("[data-cfx-observation-id='observed-a']");
        Assert.Equal(1, await target.CountAsync());
        Assert.Equal(chart.Series[0].Id.Value, await target.GetAttributeAsync("data-cfx-series-key"));
        Assert.Equal(expectedRaw, await target.GetAttributeAsync("data-cfx-value"));
        await page.EvaluateAsync("() => document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.mapIdentitySelection = event.detail.target)");
        await MoveToAsync(page, "[data-cfx-observation-id='observed-a']");
        Assert.Contains(expectedDisplay, await TooltipTextAsync(page));
        await target.FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await target.GetAttributeAsync("aria-selected"));
        Assert.Equal("observed-a", await page.EvaluateAsync<string>("() => window.mapIdentitySelection.observationId"));
        Assert.Equal(chart.Series[0].Id.Value, await page.EvaluateAsync<string>("() => window.mapIdentitySelection.seriesKey"));
        Assert.Equal(expectedDisplay, await page.EvaluateAsync<string>("() => window.mapIdentitySelection.value"));
        Assert.Equal(signedMap ? -7 : 7, await page.EvaluateAsync<double>("() => window.mapIdentitySelection.numericValue"));
        if (signedMap) Assert.Equal("A", await page.EvaluateAsync<string>("() => window.mapIdentitySelection.targetId"));
        if (kind == ChartSeriesKind.CalendarHeatmap) {
            Assert.Equal("2026-01-05", await page.EvaluateAsync<string>("() => window.mapIdentitySelection.targetId"));
            Assert.Equal(new[] { 0 }, await page.EvaluateAsync<int[]>("() => window.mapIdentitySelection.sourcePoints"));
        } else Assert.Equal("0", await page.EvaluateAsync<string>("() => window.mapIdentitySelection.sourcePoint"));
        var missing = page.Locator("[data-cfx-empty='true']");
        if (await missing.CountAsync() > 0)
            Assert.Equal(0, await page.Locator("[data-cfx-empty='true'][data-cfx-observation-id]").CountAsync());
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(capture, "map-authored-identity-" + kind + ".png"), FullPage = false });
            await File.WriteAllTextAsync(Path.Combine(capture, "map-authored-identity-" + kind + ".json"),
                await page.EvaluateAsync<string>("() => JSON.stringify({ selection: window.mapIdentitySelection, tooltip: document.querySelector('.cfx-tooltip').innerText }, null, 2)"));
        }
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }
}

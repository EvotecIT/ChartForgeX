using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class WaterfallObservationIdentityBrowserTests {
    [BrowserTheory]
    [InlineData(400, false)]
    [InlineData(900, true)]
    public async Task DeltaAndCheckpointSelectionsUseTheirAuthoredIdsAndRetainNumericReadouts(int width, bool dark) {
        if (!Enabled) return;
        var series = ChartSeries.CreateWaterfall(new ChartSeriesId("movement"), "Movement", new[] {
            ChartWaterfallItem.Delta(1, 10), ChartWaterfallItem.Subtotal(2), ChartWaterfallItem.Delta(3, 20)
        }).WithObservationId(0, new ChartObservationId("first-change"))
            .WithObservationId(1, new ChartObservationId("checkpoint"))
            .WithObservationId(2, new ChartObservationId("second-change"));
        series.UpdateWaterfallItem(2, ChartWaterfallItem.Delta(3, 25));
        var chart = Chart.Create().WithSize(width, 360).WithTitle("Movement by stage").WithPointLegend()
            .WithXLabels("Opening change", "Stage subtotal", "Later change")
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light).AddSeries(series);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), width + 24, 520);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        await page.EvaluateAsync("() => { window.waterfallIdentitySelections=[]; document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.waterfallIdentitySelections.push(event.detail.target)); }");
        var delta = page.Locator(Mark(2));
        Assert.Equal("second-change", await delta.GetAttributeAsync("data-cfx-observation-id"));
        Assert.Equal("1", await delta.GetAttributeAsync("data-cfx-source-point"));
        Assert.Equal("25", await delta.GetAttributeAsync("data-cfx-value"));
        await MoveToAsync(page, Mark(2) + " [data-cfx-role='waterfall-bar']");
        Assert.Contains("Later change", await TooltipTextAsync(page));
        Assert.Equal(new[] { "Change", "25", "Start", "10", "End", "35" },
            (await page.Locator(".cfx-tooltip dt,.cfx-tooltip dd").AllTextContentsAsync()).TakeLast(6));
        await delta.FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await delta.GetAttributeAsync("aria-selected"));
        Assert.Equal("second-change", await page.EvaluateAsync<string>("() => window.waterfallIdentitySelections.at(-1).observationId"));
        Assert.Equal("2", await page.EvaluateAsync<string>("() => window.waterfallIdentitySelections.at(-1).point"));
        Assert.Equal("1", await page.EvaluateAsync<string>("() => window.waterfallIdentitySelections.at(-1).sourcePoint"));
        var deltaLegend = page.Locator("[data-cfx-role='legend-item'][data-cfx-point='2']");
        Assert.Equal(await delta.GetAttributeAsync("data-cfx-target-id"), await deltaLegend.GetAttributeAsync("data-cfx-target-id"));
        await delta.FocusAsync(); await page.Keyboard.PressAsync("Space");
        var checkpoint = page.Locator(Mark(1));
        Assert.Equal("checkpoint", await checkpoint.GetAttributeAsync("data-cfx-observation-id"));
        Assert.Null(await checkpoint.GetAttributeAsync("data-cfx-source-point"));
        Assert.Equal("0", await checkpoint.GetAttributeAsync("data-cfx-source-points"));
        await checkpoint.FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("checkpoint", await page.EvaluateAsync<string>("() => window.waterfallIdentitySelections.at(-1).observationId"));
        Assert.True(await page.EvaluateAsync<bool>("() => window.waterfallIdentitySelections.at(-1).sourcePoint === undefined"));
        Assert.Equal(new[] { 0 }, await page.EvaluateAsync<int[]>("() => window.waterfallIdentitySelections.at(-1).sourcePoints"));
        Assert.Equal(new[] { "Subtotal", "10", "Start", "0", "End", "10" },
            (await page.Locator(".cfx-tooltip dt,.cfx-tooltip dd").AllTextContentsAsync()).TakeLast(6));
        var checkpointLegend = page.Locator("[data-cfx-role='legend-item'][data-cfx-point='1']");
        Assert.Equal(await checkpoint.GetAttributeAsync("data-cfx-target-id"), await checkpointLegend.GetAttributeAsync("data-cfx-target-id"));
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(capture, "waterfall-authored-identity-" + width + ".png"), FullPage = false });
            await File.WriteAllTextAsync(Path.Combine(capture, "waterfall-authored-identity-" + width + ".json"),
                await page.EvaluateAsync<string>("() => JSON.stringify({ selections: window.waterfallIdentitySelections, tooltip: document.querySelector('.cfx-tooltip').innerText }, null, 2)"));
        }
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }

    private static string Mark(int item) => "[data-cfx-waterfall-kind][data-cfx-point='" + item + "']:not([data-cfx-role='legend-item'])";
}

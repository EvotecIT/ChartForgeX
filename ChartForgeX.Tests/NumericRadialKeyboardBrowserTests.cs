using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class NumericRadialKeyboardBrowserTests {
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public async Task SeriesLegendSummarizesRawSignedSourceValuesOnceIncludingZeros(bool bars, bool zeroOnly) {
        if (!Enabled) return;
        var chart = NumericRadialSeriesTests.Add(Chart.Create().WithSize(600, 440).WithLegend().WithDataLabels()
            .WithDataLabelPlacement(ChartDataLabelPlacement.Outside).WithYAxisBounds(-100, 100), bars, "Requests",
            new(1, zeroOnly ? 0 : 20), new(2, zeroOnly ? 0 : -5), new(3, 0));
        NumericRadialSeriesTests.Add(chart, bars, "Peers", new(1, 80), new(2, -15), new(3, 0));
        foreach (var series in chart.Series) series.WithStackGroup("work").WithNormalization(100);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), 700, 520);
        var page = session.Page;
        var legend = page.Locator(Legend(0));
        await MoveToAsync(page, Legend(0));
        Assert.Equal(new[] { "Total" }, await page.Locator(".cfx-tooltip dt").AllTextContentsAsync());
        Assert.Equal(zeroOnly ? "0" : "15", await page.Locator(".cfx-tooltip dd").TextContentAsync());
        Assert.Contains("Total", await TooltipTextAsync(page));
        await MoveAwayAsync(page); await legend.FocusAsync();
        Assert.Equal(zeroOnly ? "0" : "15", await page.Locator(".cfx-tooltip dd").TextContentAsync());
        if (!zeroOnly) Assert.True(await page.Locator("[data-cfx-label-for]").CountAsync() > 0);
        await legend.Locator("text").ClickAsync();
        await legend.BlurAsync(); await MoveAwayAsync(page); await legend.FocusAsync();
        Assert.Equal(zeroOnly ? "0" : "15", await page.Locator(".cfx-tooltip dd").TextContentAsync());
        Assert.Equal("Requests\nTotal\n" + (zeroOnly ? "0" : "15"), await TooltipTextAsync(page));
        await CaptureAsync(page, $"legend-{(bars ? "bar" : "column")}-{(zeroOnly ? "zero" : "signed")}");
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(true, "mute")]
    [InlineData(false, "mute")]
    [InlineData(true, "unpainted")]
    [InlineData(false, "hidden")]
    public async Task KeyboardTabAndArrowSkipUnavailableObservationsAndRestoreTheirReadout(bool bars, string state) {
        if (!Enabled) return;
        var chart = NumericRadialSeriesTests.Add(Chart.Create().WithSize(600, 440).WithLegend().WithDataLabels(false), bars, "First", new ChartPoint(1, 100));
        NumericRadialSeriesTests.Add(chart, bars, "Second", new ChartPoint(1, 200));
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), 700, 520);
        var page = session.Page;
        var unavailable = page.Locator(Point(0, 0));
        if (state == "mute") {
            await page.Locator(Legend(0)).Locator("text").ClickAsync();
        } else {
            await unavailable.EvaluateAsync("(node,state)=>{if(state==='hidden')node.style.visibility='hidden';else node.querySelectorAll('path').forEach(path=>{path.style.fill='none';path.style.stroke='none';});}", state);
        }
        Assert.Equal("-1", await unavailable.GetAttributeAsync("tabindex"));
        var available = page.Locator(Point(1, 0));
        await available.FocusAsync();
        var id = await unavailable.GetAttributeAsync("data-cfx-target-id");
        foreach (var key in new[] { "ArrowUp", "ArrowDown", "ArrowLeft", "ArrowRight", "Home", "End" }) {
            await page.Keyboard.PressAsync(key);
            Assert.NotEqual(id, await page.EvaluateAsync<string>("()=>document.activeElement.dataset.cfxTargetId||''"));
        }
        await page.Locator(Legend(1)).FocusAsync();
        for (var step = 0; step < 8; step++) {
            await page.Keyboard.PressAsync("Tab");
            Assert.NotEqual(id, await page.EvaluateAsync<string>("()=>document.activeElement.dataset.cfxTargetId||''"));
        }
        if (state == "mute") await page.Locator(Legend(0)).Locator("text").ClickAsync();
        else await unavailable.EvaluateAsync("node=>{node.style.visibility='';node.querySelectorAll('path').forEach(path=>{path.style.fill='';path.style.stroke='';});}");
        await available.FocusAsync(); await page.Keyboard.PressAsync("ArrowUp");
        Assert.Equal(id, await page.EvaluateAsync<string>("()=>document.activeElement.dataset.cfxTargetId||''"));
        Assert.Equal("0", await unavailable.GetAttributeAsync("tabindex"));
        Assert.Contains("100", await TooltipTextAsync(page));
        await page.Keyboard.PressAsync("Space"); Assert.Equal("true", await unavailable.GetAttributeAsync("aria-selected"));
        await CaptureAsync(page, $"navigation-{(bars ? "bar" : "column")}-{state}");
        AssertNoConsoleErrors(session);
    }

    private static async Task CaptureAsync(IPage page, string stem) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, stem + "-browser.png") });
        await File.WriteAllTextAsync(Path.Combine(directory, stem + "-state.json"), JsonSerializer.Serialize(
            await page.EvaluateAsync<JsonElement>("()=>Array.from(document.querySelectorAll('[data-cfx-target-id]')).map(node=>({id:node.dataset.cfxTargetId,kind:node.dataset.cfxTargetKind,geometry:node.dataset.cfxGeometryStatus,tabindex:node.getAttribute('tabindex'),muted:!!node.closest('.cfx-series-muted'),value:node.dataset.cfxY||node.dataset.cfxValue}))"), new JsonSerializerOptions { WriteIndented = true }));
    }
}

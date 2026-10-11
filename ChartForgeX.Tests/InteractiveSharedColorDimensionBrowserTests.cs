using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;
using static ChartForgeX.Tests.ScatterBubbleColorRenderingTests;

namespace ChartForgeX.Tests;

/// <summary>Default shared and point-legend readouts preserve each observation's independent measurements.</summary>
public sealed class InteractiveSharedColorDimensionBrowserTests {
    [Theory]
    [InlineData(false, 360)]
    [InlineData(true, 800)]
    public async Task DefaultSharedReadoutRetainsDimensionsZeroMissingSourceSortingAndPinning(bool bubble, int width) {
        if (!Enabled) return;
        var chart = Model(width);
        Add(chart, bubble, "Samples", new[] { 1d, 1, 2 }, new[] { 18d, 0, 24 }, new[] { 9d, 36, 81 }, new double?[] { -12, 0, null });
        Add(chart, bubble, "Alerts", new[] { 1d, 2 }, new[] { 2d, 30 }, new[] { 4d, 16 }, new double?[] { null, -8 });
        chart.Series[0].WithInteractionKey("samples-source"); chart.Series[1].WithInteractionKey("alerts-source");
        chart.Series[1].StateRole = ChartSeriesState.Danger;
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), width + 24, 600);
        var page = session.Page; var root = page.Locator(".cfx-interactive-chart");
        Assert.Equal("shared-x", await root.GetAttributeAsync("data-cfx-tooltip-mode"));
        await page.EvaluateAsync("() => document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.dimensionSelection = event.detail.target)");
        await page.Locator(Point(0, 1)).FocusAsync();
        Assert.Equal(new[] { "Alerts", "Samples" }, await page.Locator("dt[data-cfx-tooltip-series]").AllTextContentsAsync());
        Assert.Equal("0", await page.Locator("dt[data-cfx-tooltip-series='0'] + dd").InnerTextAsync());
        Assert.Equal("1", await page.Locator("dt[data-cfx-tooltip-series='0']").GetAttributeAsync("data-cfx-tooltip-source-point"));
        await AssertDimensionAsync(page, "samples-source", "color", "Samples · Temperature (°C)", "0", "1");
        await AssertDimensionAsync(page, "alerts-source", "color", "Alerts · Temperature (°C)", "Not sampled", "0");
        if (bubble) await AssertDimensionAsync(page, "samples-source", "size", "Samples · Volume (m³)", "36", "1");
        else Assert.Equal(0, await page.Locator("[data-cfx-tooltip-dimension='size']").CountAsync());
        await CaptureAsync(page, "shared-dimensions-zero-" + (bubble ? "bubble" : "scatter"));

        await page.Locator(Point(0, 1)).BlurAsync(); await MoveToAsync(page, Point(0, 0));
        await AssertDimensionAsync(page, "samples-source", "color", "Samples · Temperature (°C)", "-12", "0");
        Assert.Equal("18", await page.Locator("dt[data-cfx-tooltip-series='0'] + dd").InnerTextAsync());
        await page.Locator(Point(0, 0)).FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await root.GetAttributeAsync("data-cfx-tooltip-pinned"));
        var selected = await page.EvaluateAsync<JsonElement>("() => window.dimensionSelection");
        Assert.Equal("samples-source:0", selected.GetProperty("targetId").GetString());
        Assert.Equal("0", selected.GetProperty("sourcePoint").GetString());
        Assert.Equal("18", selected.GetProperty("value").GetString());
        await MoveToAsync(page, Point(0, 2));
        await AssertDimensionAsync(page, "samples-source", "color", "Samples · Temperature (°C)", "-12", "0");
        await page.Keyboard.PressAsync("Space"); await MoveAwayAsync(page);
        await page.Locator(Point(0, 2)).FocusAsync();
        await AssertDimensionAsync(page, "samples-source", "color", "Samples · Temperature (°C)", "Not sampled", "2");
        Assert.Equal("24", await page.Locator("dt[data-cfx-tooltip-series='0'] + dd").InnerTextAsync());
        if (bubble) await AssertDimensionAsync(page, "samples-source", "size", "Samples · Volume (m³)", "81", "2");
        await page.Locator(Legend(1)).FocusAsync(); await page.Keyboard.PressAsync("Space");
        await page.Locator(Point(0, 2)).FocusAsync();
        Assert.Equal(new[] { "Samples" }, await page.Locator("dt[data-cfx-tooltip-series]").AllTextContentsAsync());
        Assert.Equal(0, await page.Locator("[data-cfx-tooltip-observation='alerts-source']").CountAsync());
        await CaptureAsync(page, "shared-dimensions-missing-" + (bubble ? "bubble" : "scatter"));
        await page.Locator(Legend(0)).FocusAsync();
        Assert.Equal(0, await page.Locator(".cfx-tooltip dt").CountAsync()); // A whole-series legend has no single size or color fact.
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PointLegendUsesReferencedColorAndLocalizedSizeWithoutInventingSeriesAggregates(bool bubble) {
        if (!Enabled) return;
        var chart = Model(800).WithPointLegend();
        Add(chart, bubble, "Samples", new[] { 1d, 2 }, new[] { 0d, 24 }, new[] { 9d, 81 }, new double?[] { 0, null });
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), 824, 600);
        var page = session.Page;
        foreach (var point in new[] { 0, 1 }) {
            await page.Locator("[data-cfx-role='legend-item'][data-cfx-point='" + point + "']").FocusAsync();
            Assert.Equal(point == 0 ? "0" : "24", await page.Locator(".cfx-tooltip dt").Filter(new LocatorFilterOptions { HasText = "Value" }).Locator("+ dd").InnerTextAsync());
            Assert.Equal(point == 0 ? "0" : "Not sampled", await page.Locator(".cfx-tooltip dt").Filter(new LocatorFilterOptions { HasText = "Temperature (°C)" }).Locator("+ dd").InnerTextAsync());
            if (bubble) Assert.Equal(point == 0 ? "9" : "81", await page.Locator(".cfx-tooltip dt").Filter(new LocatorFilterOptions { HasText = "Volume (m³)" }).Locator("+ dd").InnerTextAsync());
        }
        await CaptureAsync(page, "legend-dimensions-" + (bubble ? "bubble" : "scatter"));
        AssertNoConsoleErrors(session);
    }

    private static Chart Model(int width) => Chart.Create().WithSize(width, 500).WithDataLabels(false)
        .WithTheme(ChartTheme.GraphiteLight()).ConfigureLabels(labels => { labels.Color = "Temperature (°C)"; labels.Size = "Volume (m³)"; labels.NoData = "Not sampled"; });

    private static void Add(Chart chart, bool bubble, string name, double[] x, double[] y, double[] size, double?[] color) {
        if (bubble) chart.AddBubble(name, Enumerable.Range(0, x.Length).Select(index => new ChartBubble(x[index], y[index], size[index]).WithColorValue(color[index])));
        else chart.AddScatter(name, Enumerable.Range(0, x.Length).Select(index => new ChartPoint(x[index], y[index]).WithColorValue(color[index])));
        chart.Series[chart.Series.Count - 1].WithColorScale(ChartColorScale.Sequential(Low, High));
    }

    private static async Task AssertDimensionAsync(IPage page, string series, string dimension, string label, string value, string source) {
        var selector = "dt[data-cfx-tooltip-observation='" + series + "'][data-cfx-tooltip-dimension='" + dimension + "']";
        await page.WaitForFunctionAsync("expected => { const row = document.querySelector(expected.selector); return row && row.nextElementSibling.textContent === expected.value; }", new { selector, value });
        var term = page.Locator(selector);
        Assert.Equal(label, await term.InnerTextAsync()); Assert.Equal(source, await term.GetAttributeAsync("data-cfx-tooltip-source-point"));
    }

    private static async Task CaptureAsync(IPage page, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".png") });
        await File.WriteAllTextAsync(Path.Combine(directory, name + ".json"), await page.EvaluateAsync<string>("() => JSON.stringify({tooltip:document.querySelector('.cfx-tooltip').innerText,selection:window.dimensionSelection,rows:Array.from(document.querySelectorAll('.cfx-tooltip dt')).map(term=>({name:term.textContent,value:term.nextElementSibling.textContent,...term.dataset}))},null,2)"));
    }
}

using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class InteractiveTooltipRegressionBrowserTests {
    [Fact]
    public async Task SharedDuplicateXUsesTheInteractedObservationAndDeterministicCounterparts() {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(596, 338).WithTheme(ChartTheme.GraphiteLight()).WithTitle("Observations sharing X")
            .AddScatter("Observed", new[] { new ChartPoint(1, 10), new ChartPoint(1, 20), new ChartPoint(2, 15) })
            .AddScatter("Comparison", new[] { new ChartPoint(1, 30), new ChartPoint(1, 40), new ChartPoint(2, 25) });
        chart.Series[0].WithInteractionKey("observations").WithPointColor(1, "#7b61e8");
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => options.Interaction.Disable(ChartInteractionFeatures.Crosshair)));
        var page = session.Page;
        await page.Locator(Point(0, 1)).FocusAsync();
        var focused = await ObservationAsync(page, "observations");
        await page.Locator(Point(0, 1)).BlurAsync();
        await MoveToAsync(page, Point(0, 1));
        var hovered = await ObservationAsync(page, "observations");
        await RecordAsync(page, "tooltip-duplicate-x", new { focused, hovered });
        Assert.Equal(new[] { "20", "1", "1", "rgb(123, 97, 232)" }, focused);
        Assert.Equal(focused, hovered);
        Assert.Equal("30", await page.Locator("dt[data-cfx-tooltip-series='1'] + dd").InnerTextAsync());
        await page.Locator(Point(0, 1)).FocusAsync();
        await page.Keyboard.PressAsync("ArrowUp");
        Assert.Equal(6, await page.Locator("[data-cfx-keyboard-component='data']").CountAsync());
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false, HtmlChartTooltipMode.Single, 700)]
    [InlineData(false, HtmlChartTooltipMode.SharedX, 340)]
    [InlineData(true, HtmlChartTooltipMode.Single, 340)]
    [InlineData(true, HtmlChartTooltipMode.SharedX, 700)]
    public async Task SolidBarSwatchesUseThePaintedGradientInsteadOfWhiteDecoration(bool dark, HtmlChartTooltipMode mode, int width) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(596, 338).WithBarStyle(ChartBarStyle.Solid).WithTitle("Decorated bar observations")
            .WithTheme((dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight()).WithPalette("#176eaf", "#df4263"))
            .AddBar("Current", ChartPoints.FromValues(3, 7, 5)).AddBar("Previous", ChartPoints.FromValues(1, 2, 1));
        chart.Series[0].WithPointColor(1, "#7b61e8").WithPointFillPattern(1, ChartFillPattern.Crosshatch);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => options.Tooltip.Mode = mode), width, 560);
        var page = session.Page;
        await page.Locator(Point(0, 1)).FocusAsync();
        var swatch = mode == HtmlChartTooltipMode.SharedX ? "dt[data-cfx-tooltip-series='0'] .cfx-tooltip__swatch" : ".cfx-tooltip__title .cfx-tooltip__swatch";
        var gradient = await GradientPaintAsync(page, Point(0, 1) + " [data-cfx-role='bar']");
        var colour = await page.Locator(swatch).EvaluateAsync<string>("node => getComputedStyle(node).backgroundColor");
        await RecordAsync(page, "tooltip-solid-bar-" + (dark ? "dark" : "light") + "-" + mode + "-" + width, new { gradient, colour });
        Assert.StartsWith("url(", gradient[0], StringComparison.Ordinal);
        Assert.Equal(gradient[1], colour);
        Assert.Equal(1, await page.Locator(Point(0, 1) + " [data-cfx-role='bar-highlight']").CountAsync());
        Assert.True(await page.Locator(Point(0, 1) + " [data-cfx-role='bar-pattern']").CountAsync() > 0);
        Assert.True(await page.EvaluateAsync<bool>("() => { const box = document.querySelector('.cfx-tooltip').getBoundingClientRect(); return box.left >= 0 && box.right <= innerWidth && box.top >= 0 && box.bottom <= innerHeight; }"));
        await page.Keyboard.PressAsync("ArrowRight");
        Assert.Equal(6, await page.Locator("[data-cfx-keyboard-component='data']").CountAsync());
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HorizontalAndRangeBarGradientsRemainAuthoritativeWithoutALegend(bool range) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(596, 338).WithLegend(false).WithTheme(ChartTheme.GraphiteLight())
            .WithBarStyle(ChartBarStyle.Solid);
        if (range) chart.AddRangeBar("Expected", new[] { new ChartInterval(1, 2, 8), new ChartInterval(2, 3, 9) });
        else chart.AddHorizontalBar("Expected", ChartPoints.FromValues(3, 7, 5));
        chart.Series[0].WithPointColor(0, "#7b61e8").WithPointFillPattern(0, ChartFillPattern.DiagonalForward);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => options.Tooltip.Mode = HtmlChartTooltipMode.Single));
        var page = session.Page;
        var mark = Point(0, 0) + " [data-cfx-role='" + (range ? "range-bar" : "horizontal-bar") + "']";
        await page.Locator(mark).EvaluateAsync("node => document.getElementById(node.getAttribute('fill').slice(5, -1)).querySelector('stop').setAttribute('stop-opacity', '0')");
        await page.Locator(Point(0, 0)).FocusAsync();
        var gradient = await GradientPaintAsync(page, mark, 1);
        Assert.Equal(gradient[1], await page.Locator(".cfx-tooltip__swatch").EvaluateAsync<string>("node => getComputedStyle(node).backgroundColor"));
        Assert.Equal(0, await page.Locator("[data-cfx-role='legend-item']").CountAsync());
        AssertNoConsoleErrors(session);
    }

    private static Task<string[]> GradientPaintAsync(IPage page, string selector, int stop = 0) => page.Locator(selector)
        .EvaluateAsync<string[]>("(node, index) => { const id = node.getAttribute('fill').slice(5, -1); const stop = document.getElementById(id).querySelectorAll('stop')[index]; return [getComputedStyle(node).fill, getComputedStyle(stop).stopColor]; }", stop);

    private static Task<string[]> ObservationAsync(IPage page, string key) => page.Locator("dt[data-cfx-tooltip-series-key='" + key + "']")
        .EvaluateAsync<string[]>("node => [node.nextElementSibling.textContent, node.dataset.cfxTooltipPoint, node.dataset.cfxTooltipSourcePoint, getComputedStyle(node.querySelector('.cfx-tooltip__swatch')).backgroundColor]");

    private static async Task RecordAsync(IPage page, string name, object result) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, name + ".json"), JsonSerializer.Serialize(result));
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".png") });
    }
}

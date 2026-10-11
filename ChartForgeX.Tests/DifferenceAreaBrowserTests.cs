using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Checks paired native readouts, logical source selection and shared-X rows through the HTML adapter.</summary>
public sealed class DifferenceAreaBrowserTests {
    [Theory]
    [InlineData(false, false, HtmlChartTooltipMode.Single)]
    [InlineData(true, false, HtmlChartTooltipMode.Single)]
    [InlineData(false, true, HtmlChartTooltipMode.Single)]
    [InlineData(true, true, HtmlChartTooltipMode.Single)]
    [InlineData(false, false, HtmlChartTooltipMode.SharedX)]
    [InlineData(true, false, HtmlChartTooltipMode.SharedX)]
    [InlineData(false, true, HtmlChartTooltipMode.SharedX)]
    [InlineData(true, true, HtmlChartTooltipMode.SharedX)]
    public async Task NamedCrossingPairsKeepBothReadoutsAndOneSelectionIdentity(bool compact, bool dark, HtmlChartTooltipMode mode) {
        if (!Enabled) return;
        var chart = V2GalleryModels.Create(ChartSeriesKind.DifferenceArea)
            .WithSize(compact ? 360 : 800, compact ? 360 : 440).WithLegend().WithTitle("Actual and forecast")
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight());
        chart.Series[0].WithInteractionKey("comparison");
        chart.AddLine("Reference", new[] { new ChartPoint(3, 55) });
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => options.Tooltip.Mode = mode),
            compact ? 390 : 880, compact ? 560 : 600);
        var page = session.Page; var point = page.Locator(Point(0, 2));
        await point.Locator("[data-cfx-role='difference-marker']").First.HoverAsync();
        var rows = await page.Locator(".cfx-tooltip dt").AllTextContentsAsync();
        Assert.Contains(rows, row => row.Contains("Actual", StringComparison.Ordinal));
        Assert.Contains(rows, row => row.Contains("Forecast", StringComparison.Ordinal));
        Assert.Contains("48", await TooltipTextAsync(page)); Assert.Contains("60", await TooltipTextAsync(page));
        if (mode == HtmlChartTooltipMode.SharedX) Assert.Contains("Reference", await TooltipTextAsync(page));
        Assert.Equal("48", await point.GetAttributeAsync("data-cfx-first"));
        Assert.Equal("60", await point.GetAttributeAsync("data-cfx-second"));
        await point.FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await point.GetAttributeAsync("aria-selected"));
        Assert.Equal("comparison:2", await point.GetAttributeAsync("data-cfx-target-id"));
        Assert.Equal("2", await point.GetAttributeAsync("data-cfx-source-point"));
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            await page.ScreenshotAsync(new PageScreenshotOptions {
                Path = Path.Combine(capture, $"difference-area-{(compact ? "compact" : "wide")}-{(dark ? "dark" : "light")}-{mode}.png")
            });
        }
        await page.Keyboard.PressAsync("ArrowRight");
        Assert.NotEqual("comparison:2", await page.EvaluateAsync<string>("() => document.activeElement.dataset.cfxTargetId"));
        await page.Locator(Legend(0)).ClickAsync(); await MoveAwayAsync(page);
        Assert.Equal(string.Empty, await TooltipTextAsync(page));
        await page.Locator("[data-cfx-reset]").ClickAsync();
        Assert.Equal(0, await page.Locator("[data-cfx-point][aria-selected='true']").CountAsync());
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task TransparentDirectionDoesNotBorrowTheOtherDirectionsPaint() {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(600, 380).AddDifferenceArea("Pair", new[] {
            new ChartDifferencePoint(0, 8, 2), new ChartDifferencePoint(1, 2, 8)
        }).WithLineVisualStyle(ChartLineVisualStyle.Plain());
        chart.Series[0].WithStrokeWidth(0).ConfigureMarkers(markers => markers.Enabled = false)
            .ConfigureDifferenceArea(options => { options.PositiveFillOpacity = 0; options.NegativeFillOpacity = .8; });
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage());
        var page = session.Page;
        await page.Locator(Point(0, 0)).FocusAsync();
        Assert.Equal(string.Empty, await TooltipTextAsync(page));
        await page.Locator(Point(0, 1)).FocusAsync();
        Assert.Contains("Pair", await TooltipTextAsync(page));
        AssertNoConsoleErrors(session);
    }
}

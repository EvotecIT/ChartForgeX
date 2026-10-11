using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class CartesianPlotRegionBrowserTests {
    [Theory]
    [InlineData(false, 360)]
    [InlineData(true, 360)]
    [InlineData(false, 800)]
    [InlineData(true, 800)]
    public async Task BackgroundAndCaptionRemainPassiveWhilePointsKeepPointerAndKeyboardActions(bool dark, int width) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(width, 320).WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .WithTitle("Planning zone").WithLegend(false).WithXAxisBounds(0, 10).WithYAxisBounds(0, 10)
            .AddScatter("Observed", new[] { new ChartPoint(2, 3), new ChartPoint(8, 7) })
            .AddPlotRegion(new(ChartColor.FromHex("#5c9e75"), label: "Background guidance", opacity: .2));
        var html = chart.ToInteractiveHtmlPage(options => {
            options.Tooltip.Mode = HtmlChartTooltipMode.Single;
            options.Tooltip.Range = HtmlChartTooltipRange.Exact;
            options.Interaction.Disable(ChartInteractionFeatures.Crosshair);
        });
        await using var session = await OpenAsync(html, width + 24, 410);
        var page = session.Page;
        await page.EvaluateAsync("() => { window.selections = []; document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.selections.push(event.detail.target)); }");
        var passive = page.Locator("[data-cfx-passive='true']");
        Assert.Equal(2, await passive.CountAsync());
        Assert.Equal(0, await passive.Locator("[tabindex], [data-cfx-target-kind], .cfx-interactive-region").CountAsync());
        Assert.Equal(0, await page.Locator("[data-cfx-passive='true'][tabindex], [data-cfx-passive='true'][data-cfx-target-kind]").CountAsync());
        await MoveToAsync(page, "[data-cfx-role='plot-region-label']");
        Assert.Equal(string.Empty, await TooltipTextAsync(page));
        await page.Locator("[data-cfx-role='plot-region-label']").ClickAsync();
        Assert.Equal(0, await page.EvaluateAsync<int>("() => window.selections.filter(Boolean).length"));
        await MoveToAsync(page, Point(0, 0));
        Assert.Contains("Observed", await TooltipTextAsync(page));
        await page.Locator(Point(0, 0)).ClickAsync();
        Assert.Equal("true", await page.Locator(Point(0, 0)).GetAttributeAsync("aria-selected"));
        Assert.Equal("point", await page.EvaluateAsync<string>("() => window.selections.filter(Boolean).at(-1).targetKind"));
        await page.Locator(Point(0, 0)).FocusAsync();
        await page.Keyboard.PressAsync("End");
        Assert.True(await page.Locator(Point(0, 1)).EvaluateAsync<bool>("node => node === document.activeElement"));
        Assert.Equal(1, await page.Locator("[data-cfx-keyboard-component='data'][tabindex='0']").CountAsync());
        Assert.DoesNotContain("Background guidance", await TooltipTextAsync(page));
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            var name = "plot-regions-" + width + "-" + (dark ? "dark" : "light");
            await File.WriteAllTextAsync(Path.Combine(capture, name + ".html"), html);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(capture, name + ".png") });
        }
        AssertNoConsoleErrors(session);
    }
}

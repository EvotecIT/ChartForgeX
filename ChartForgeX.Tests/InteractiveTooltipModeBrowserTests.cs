using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class InteractiveTooltipModeBrowserTests {
    [Theory]
    [InlineData(false, HtmlChartTooltipMode.Single)]
    [InlineData(false, HtmlChartTooltipMode.SharedX)]
    [InlineData(true, HtmlChartTooltipMode.Single)]
    [InlineData(true, HtmlChartTooltipMode.SharedX)]
    public async Task ExplicitModeIsIndependentOfPaletteAndGraphiteLayout(bool graphite, HtmlChartTooltipMode mode) {
        if (!Enabled) return;
        var chart = Lines(graphite ? ChartTheme.GraphiteLight() : ChartTheme.Dark());
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            options.TooltipMode = mode;
            options.Interaction.Disable(ChartInteractionFeatures.Crosshair);
        }));
        var page = session.Page;
        await page.Locator(Point(0, 1)).FocusAsync();
        var shared = mode == HtmlChartTooltipMode.SharedX;
        Assert.Equal(shared ? new[] { "Current", "Baseline" } : new[] { "Role", "Series", "Point", "X", "Y", "Kind" },
            await page.Locator(".cfx-tooltip dt").AllTextContentsAsync());
        Assert.Equal(shared ? "Tue" : await page.Locator(Point(0, 1)).GetAttributeAsync("aria-label"),
            await page.Locator(".cfx-tooltip__title").InnerTextAsync());
        await page.Locator(Point(0, 1)).BlurAsync();
        await MoveToAsync(page, Point(0, 1));
        Assert.Equal(shared, (await TooltipTextAsync(page)).Contains("Baseline", StringComparison.Ordinal));
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false, 700)]
    [InlineData(true, 700)]
    [InlineData(false, 340)]
    [InlineData(true, 340)]
    public async Task MarkerFreeSharedTooltipUsesActualLinePaintInLightDarkAndCompactViews(bool dark, int width) {
        if (!Enabled) return;
        var chart = Lines(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight()).WithLineMarkers(ChartLineMarkerMode.None);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), width, 560);
        var page = session.Page;
        await page.AddStyleTagAsync(new PageAddStyleTagOptions { Content = "[data-cfx-role='series'][data-cfx-series='1'] [data-cfx-role='line'] { stroke:#7b61e8; }" });
        await page.Locator(Point(0, 1)).FocusAsync();
        var colours = await page.EvaluateAsync<string[][]>("() => Array.from(document.querySelectorAll('.cfx-tooltip dt')).map(row => [getComputedStyle(row.querySelector('.cfx-tooltip__swatch')).backgroundColor, getComputedStyle(document.querySelector('[data-cfx-role=\"series\"][data-cfx-series=\"' + row.dataset.cfxTooltipSeries + '\"] [data-cfx-role=\"line\"]')).stroke])");
        Assert.Equal(2, colours.Length);
        Assert.All(colours, colour => Assert.Equal(colour[1], colour[0]));
        Assert.Equal(new[] { "1", "1" }, await OpacitiesAsync(page, ".cfx-tooltip dt"));
        await page.Keyboard.PressAsync("ArrowRight");
        Assert.Equal(6, await page.Locator("[data-cfx-keyboard-component='data']").CountAsync());
        await page.Locator(Point(0, 1)).FocusAsync();
        Assert.True(await page.EvaluateAsync<bool>("() => { const box = document.querySelector('.cfx-tooltip').getBoundingClientRect(); return box.left >= 0 && box.right <= innerWidth && box.top >= 0 && box.bottom <= innerHeight; }"));
        await CaptureAsync(page, "tooltip-shared-line-" + (dark ? "dark" : "light") + "-" + width + ".png");
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(HtmlChartTooltipMode.Single)]
    [InlineData(HtmlChartTooltipMode.SharedX)]
    public async Task TooltipSwatchRetainsPointOverrideInsteadOfGroupOrSeriesFill(HtmlChartTooltipMode mode) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(596, 338).WithTheme(ChartTheme.GraphiteLight().WithPalette("#176eaf", "#df4263"))
            .AddBar("Requests", ChartPoints.FromValues(3, 7, 5)).AddBar("Previous", ChartPoints.FromValues(1, 2, 1));
        chart.Series[0].WithPointColor(1, "#7b61e8");
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => options.TooltipMode = mode));
        var page = session.Page;
        await page.Locator(Point(0, 1)).FocusAsync();
        var swatch = mode == HtmlChartTooltipMode.SharedX ? ".cfx-tooltip dt[data-cfx-tooltip-series='0'] .cfx-tooltip__swatch" : ".cfx-tooltip__title .cfx-tooltip__swatch";
        var colour = await page.Locator(swatch).EvaluateAsync<string>("node => getComputedStyle(node).backgroundColor");
        Assert.Equal("rgb(123, 97, 232)", colour);
        Assert.Equal(colour, await page.Locator(Point(0, 1) + " [data-cfx-role='bar']").EvaluateAsync<string>("node => getComputedStyle(node).fill"));
        await page.Locator(Legend(0)).FocusAsync();
        Assert.NotEqual(colour, await page.Locator(".cfx-tooltip__swatch").EvaluateAsync<string>("node => getComputedStyle(node).backgroundColor"));
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SingleRangeTooltipUsesPaintedEnvelopeWithoutRequiringALegend(bool area) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(596, 338).WithLegend(false).WithTheme(ChartTheme.GraphiteLight());
        var ranges = new[] { new ChartRangeBand(1, 2, 8), new ChartRangeBand(2, 3, 9) };
        if (area) chart.AddRangeArea("Expected", ranges, ChartColor.FromHex("#176eaf"));
        else chart.AddRangeBand("Expected", ranges, ChartColor.FromHex("#176eaf"));
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => options.TooltipMode = HtmlChartTooltipMode.Single));
        var page = session.Page;
        await page.Locator(Point(0, 0)).FocusAsync();
        Assert.Contains("Range", await page.Locator(".cfx-tooltip dt").AllTextContentsAsync());
        var envelope = page.Locator(area ? "[data-cfx-role='range-area']" : "[data-cfx-role='range-band']");
        Assert.Equal(await envelope.EvaluateAsync<string>("node => getComputedStyle(node).fill"),
            await page.Locator(".cfx-tooltip__swatch").EvaluateAsync<string>("node => getComputedStyle(node).backgroundColor"));
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task SharedRowsRetainRawValuesSourceIdentityAndStatePriorityWithSafeText() {
        if (!Enabled) return;
        const string dangerName = "<img src=x onerror=alert(1)>";
        var chart = Chart.Create().WithSize(596, 338).WithLineMarkers(ChartLineMarkerMode.None)
            .AddDecimatedLine("Latency", Enumerable.Range(0, 100).Select(index => new ChartPoint(index, 100 + Math.Sin(index / 4d))), 12);
        chart.Series[0].WithInteractionKey("latency-source");
        var observed = chart.Series[0].Points[3];
        const double precise = 0.12345678901234566;
        chart.AddLine(dangerName, new[] { new ChartPoint(observed.X, precise) }).WithSeriesState(dangerName, ChartSeriesState.Danger);
        chart.Series[1].WithInteractionKey("alerts");
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage());
        var page = session.Page;
        await page.Locator(Point(0, 3)).FocusAsync();
        Assert.Equal(new[] { dangerName, "Latency" }, await page.Locator(".cfx-tooltip dt").AllTextContentsAsync());
        Assert.Equal(precise.ToString("R", CultureInfo.InvariantCulture), await page.Locator(".cfx-tooltip dd").First.InnerTextAsync());
        var latency = page.Locator(".cfx-tooltip dt[data-cfx-tooltip-series-key='latency-source']");
        Assert.Equal("0", await latency.GetAttributeAsync("data-cfx-tooltip-series"));
        Assert.Equal("3", await latency.GetAttributeAsync("data-cfx-tooltip-point"));
        Assert.Equal(chart.Series[0].SourcePointIndices[3].ToString(CultureInfo.InvariantCulture), await latency.GetAttributeAsync("data-cfx-tooltip-source-point"));
        Assert.Equal(await page.Locator(Point(0, 3)).GetAttributeAsync("data-cfx-y"), await latency.Locator("+ dd").InnerTextAsync());
        Assert.Equal(0, await page.Locator(".cfx-tooltip img").CountAsync());
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData("display:none")]
    [InlineData("visibility:hidden")]
    [InlineData("opacity:0")]
    public async Task SharedRowsOmitMutedAndHostHiddenSeriesWhileMutedLegendRemainsUsable(string style) {
        if (!Enabled) return;
        await using var session = await OpenAsync(StatusLines(false).ToInteractiveHtmlPage());
        var page = session.Page;
        await page.Locator(Legend(1)).FocusAsync();
        await page.Keyboard.PressAsync("Space");
        await page.AddStyleTagAsync(new PageAddStyleTagOptions { Content = "[data-cfx-role='series'][data-cfx-series='2'] { " + style + " !important; }" });
        await page.Locator(Point(0, 1)).FocusAsync();
        var hiddenStyle = style.Split(':');
        Assert.Equal(hiddenStyle[1], await page.Locator("[data-cfx-role='series'][data-cfx-series='2']").EvaluateAsync<string>(
            "(node, property) => getComputedStyle(node).getPropertyValue(property)", hiddenStyle[0]));
        Assert.Equal(new[] { "Passed" }, await page.Locator(".cfx-tooltip dt").AllTextContentsAsync());
        await page.Locator(Legend(1)).FocusAsync();
        Assert.Contains("Warnings", await TooltipTextAsync(page), StringComparison.Ordinal);
        Assert.Contains("Latest (Sun)", await TooltipTextAsync(page), StringComparison.Ordinal);
        Assert.Equal("true", await page.Locator(Legend(1)).GetAttributeAsync("data-cfx-muted"));
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("")]
    [InlineData(null)]
    public async Task SharedRowsIgnoreMissingOrNonFiniteHostValues(string? value) {
        if (!Enabled) return;
        await using var session = await OpenAsync(Lines(ChartTheme.Light()).ToInteractiveHtmlPage());
        var page = session.Page;
        await page.Locator(Point(1, 1)).EvaluateAsync("(node, value) => { if (value === null) node.removeAttribute('data-cfx-y'); else node.dataset.cfxY = value; }", value);
        await page.Locator(Point(0, 1)).FocusAsync();
        Assert.Equal(new[] { "Current" }, await page.Locator(".cfx-tooltip dt").AllTextContentsAsync());
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task SharedModeFallsBackForPieTargetsAndBothModesRespectFeatureGate() {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(596, 338).WithXLabels("North", "South").AddPie("Requests", ChartPoints.FromValues(7, 3));
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage());
        var page = session.Page;
        var target = page.Locator("[data-cfx-keyboard-component='data']").First;
        await target.FocusAsync();
        Assert.Contains("North", await page.Locator(".cfx-tooltip__title").InnerTextAsync(), StringComparison.Ordinal);
        Assert.Contains("Role", await page.Locator(".cfx-tooltip dt").AllTextContentsAsync());
        Assert.Equal("7", await page.Locator(".cfx-tooltip dt").Filter(new LocatorFilterOptions { HasText = "Value" }).Locator("+ dd").InnerTextAsync());
        foreach (var mode in new[] { HtmlChartTooltipMode.Single, HtmlChartTooltipMode.SharedX }) {
            await page.SetContentAsync(Lines(ChartTheme.Light()).ToInteractiveHtmlPage(options => {
                options.TooltipMode = mode;
                options.Interaction.Disable(ChartInteractionFeatures.Tooltips);
            }));
            await page.Locator(Point(0, 1)).FocusAsync();
            Assert.True(await page.Locator(".cfx-tooltip").IsHiddenAsync());
            await page.Keyboard.PressAsync("t");
            Assert.True(await page.Locator(".cfx-tooltip").IsHiddenAsync());
        }
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task SharedModeKeepsHeatmapCellQuantityAndMetadataInASingleReadout() {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(596, 338).WithXLabels("North", "South")
            .AddHeatmapRow("Requests", new[] { 37d, 48d }).AddHeatmapRow("Previous", new[] { 61d, 54d });
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage());
        var page = session.Page;
        var target = page.Locator("[data-cfx-role='heatmap-cell'][data-cfx-series='0'][data-cfx-point='0']");
        await target.FocusAsync();
        Assert.Equal("37", await target.GetAttributeAsync("data-cfx-value"));
        Assert.Equal("37", await target.GetAttributeAsync("data-cfx-y"));
        Assert.Equal("37", await page.Locator(".cfx-tooltip dt").Filter(new LocatorFilterOptions { HasText = "Value" }).Locator("+ dd").InnerTextAsync());
        Assert.Contains("Role", await page.Locator(".cfx-tooltip dt").AllTextContentsAsync());
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(HtmlChartTooltipMode.Single)]
    [InlineData(HtmlChartTooltipMode.SharedX)]
    public async Task DashboardChildTooltipsUseTheConfiguredMode(HtmlChartTooltipMode mode) {
        if (!Enabled) return;
        var chart = Lines(ChartTheme.Light());
        await using var session = await OpenAsync(new[] { chart, chart }.ToInteractiveHtmlDashboardPage(options => {
            options.Columns = 1; options.TooltipMode = mode;
        }), 700, 1000);
        var page = session.Page;
        var children = page.Locator(".cfx-interactive-chart");
        for (var index = 0; index < 2; index++) {
            var child = children.Nth(index);
            await child.Locator(Point(0, 1)).FocusAsync();
            Assert.Equal(mode == HtmlChartTooltipMode.SharedX, (await child.Locator(".cfx-tooltip").InnerTextAsync()).Contains("Baseline", StringComparison.Ordinal));
        }
        AssertNoConsoleErrors(session);
    }

    private static Chart Lines(ChartTheme theme) => Chart.Create().WithSize(596, 338).WithTitle("Observations over time")
        .WithTheme(theme.WithPalette("#176eaf", "#df4263")).WithXLabels("Mon", "Tue", "Wed")
        .AddLine("Current", ChartPoints.FromValues(12, 19, 16)).AddLine("Baseline", ChartPoints.FromValues(7, 11, 8));

    private static async Task CaptureAsync(IPage page, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name) });
    }
}

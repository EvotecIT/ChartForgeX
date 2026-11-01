using System.Globalization;
using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Exercises prepared display captions through real tooltip and compare surfaces.</summary>
public sealed class InteractiveLinkedReadableValueBrowserTests {
    [BrowserTheory]
    [InlineData("UTC", "2026-03-02", false, 1100)]
    [InlineData("Europe/Warsaw", "2026-03-02 01:00", false, 1100)]
    [InlineData("America/New_York", "2026-03-01 19:00", false, 1100)]
    [InlineData("Europe/Warsaw", "2026-03-02 01:00", true, 390)]
    public async Task ZonedPointTooltipsRetainCalendarDatesAndRawInstants(string zone, string expected, bool dark, int width) {
        if (!Enabled) return;
        var chart = ChartLinkedReadableValueContractsTests.ZonedTimeChart(zone)
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
            options.Tooltip.Mode = HtmlChartTooltipMode.Single;
            options.Tooltip.PinOnSelect = false;
        }), width, 650);
        var page = session.Page;
        await page.EvaluateAsync("() => document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.readableSelection = event.detail.target)");
        await page.Locator(Point(0, 0)).FocusAsync();
        Assert.Equal(expected, await page.Locator(".cfx-tooltip__title").InnerTextAsync());
        Assert.Equal(expected, await page.Locator(".cfx-tooltip dt").Filter(new LocatorFilterOptions { HasText = "X" }).Locator("+ dd").InnerTextAsync());
        await page.Keyboard.PressAsync("Space");
        var selected = await page.EvaluateAsync<JsonElement>("() => window.readableSelection");
        Assert.Equal(expected, selected.GetProperty("label").GetString());
        Assert.Equal(new DateTime(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc), DateTime.Parse(selected.GetProperty("x").GetString()!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
        Assert.Equal(7, selected.GetProperty("numericValue").GetDouble());
        await CaptureAsync(page, "zoned-point-" + zone.Replace('/', '-') + "-" + width, selected);
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AuthoredZonedPointCaptionsIncludingEmptyTextOwnTooltips(bool empty) {
        if (!Enabled) return;
        var chart = ChartLinkedReadableValueContractsTests.ZonedTimeChart("Europe/Warsaw");
        var expected = empty ? string.Empty : "Authored clock";
        chart.Options.XAxis.LabelFormatter = _ => expected;
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => options.Tooltip.Mode = HtmlChartTooltipMode.Single), 1100, 650);
        await session.Page.Locator(Point(0, 0)).FocusAsync();
        Assert.Equal(expected, await session.Page.Locator(".cfx-tooltip__title").InnerTextAsync());
        Assert.Equal(expected, await session.Page.Locator(".cfx-tooltip dt").Filter(new LocatorFilterOptions { HasText = "X" }).Locator("+ dd").InnerTextAsync());
        Assert.False(await session.Page.Locator(".cfx-tooltip").IsHiddenAsync());
        await CaptureAsync(session.Page, "zoned-point-authored-" + empty);
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false, 1440)]
    [InlineData(true, 390)]
    public async Task LinkedExampleCompareChipsNameEachSeriesForRepeatedDates(bool dark, int width) {
        if (!Enabled) return;
        await using var session = await OpenAsync(LinkedChartViewsExamples.CreatePage(dark), width, 1000);
        var page = session.Page;
        Assert.True(await page.EvaluateAsync<bool>("""
            async () => {
                await document.fonts.ready;
                const faces = [...document.fonts].filter(face => face.status === 'loaded');
                const root=document.querySelector('[data-cfx-chart-id=linked-time]'),svg=root.querySelector('.cfx-stage svg');
                const text=[...svg.querySelectorAll('text')].find(node=>node.textContent==='2026-03-05');
                if(!faces.length||!text)return false;
                const clip=text.closest('[clip-path]'),id=clip.getAttribute('clip-path').slice(5,-1);
                const rect=svg.querySelector('#'+CSS.escape(id)+' rect'),box=text.getBBox();
                // Browser font advances and serialized native positions can differ by a fraction of a CSS pixel.
                return box.x>=rect.x.baseVal.value-.05&&box.x+box.width<=rect.x.baseVal.value+rect.width.baseVal.value+.05;
            }
            """), "The self-contained linked example must load its native font and keep the complete final date inside its text clip.");
        await page.EvaluateAsync("() => ChartForgeXCharts.get(document.querySelector('[data-cfx-chart-id=linked-time]')).select({observationIds:['api-2','worker-2']})");
        Assert.Equal(new[] { "API · 2026-03-04", "Worker · 2026-03-04" }, await page.Locator("[data-cfx-chart-id=linked-time] .cfx-compare-chip").AllTextContentsAsync());
        await CaptureAsync(page, "linked-compare-series-" + width);
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(HtmlChartTooltipMode.Single)]
    [InlineData(HtmlChartTooltipMode.SharedX)]
    public async Task ScatterValueReadoutsAreFormattedWhileSelectionNumbersStayExact(HtmlChartTooltipMode mode) {
        if (!Enabled) return;
        const double x = 66.09458950219, y = 2.6130852767327268;
        var chart = Chart.Create().WithSize(640, 360).WithXAxisBounds(0, 100).WithYAxisBounds(0, 50)
            .AddSeries(ChartSeries.CreateScatter(new ChartSeriesId("samples"), "Samples", new[] { new ChartPoint(x, y) }));
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            options.Tooltip.Mode = mode;
            options.Tooltip.PinOnSelect = false;
        }), 820, 650);
        var page = session.Page;
        await page.EvaluateAsync("() => document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.readableSelection = event.detail.target)");
        var point = page.Locator(Point(0, 0));
        await point.FocusAsync();
        Assert.Equal("66.09", await page.Locator(".cfx-tooltip__title").InnerTextAsync());
        var value = mode == HtmlChartTooltipMode.SharedX ? page.Locator(".cfx-tooltip dd").First
            : page.Locator(".cfx-tooltip dt").Filter(new LocatorFilterOptions { HasText = "Y" }).Locator("+ dd");
        Assert.Equal("2.61", await value.InnerTextAsync());
        await page.Keyboard.PressAsync("Space");
        var selected = await page.EvaluateAsync<JsonElement>("() => window.readableSelection");
        Assert.Equal("2.61", selected.GetProperty("value").GetString());
        Assert.Equal(y, selected.GetProperty("numericValue").GetDouble());
        Assert.Equal(x, selected.GetProperty("x").GetDouble());
        Assert.Equal(y, double.Parse((await point.GetAttributeAsync("data-cfx-y"))!, CultureInfo.InvariantCulture));
        await CaptureAsync(page, "scatter-formatted-value-" + mode, selected);
        AssertNoConsoleErrors(session);
    }

    private static async Task CaptureAsync(IPage page, string name, object? facts = null) {
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".png"), FullPage = true });
        if (facts != null) await File.WriteAllTextAsync(Path.Combine(output, name + ".json"), JsonSerializer.Serialize(facts, new JsonSerializerOptions { WriteIndented = true }));
    }
}

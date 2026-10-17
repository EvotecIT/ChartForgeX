using System.Globalization;
using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using HtmlTinkerX;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Checks display captions independently of the source numbers used by interaction.</summary>
public sealed class InteractiveNumericReadoutBrowserTests {
    [BrowserTheory]
    [InlineData(HtmlChartTooltipMode.Single)]
    [InlineData(HtmlChartTooltipMode.SharedX)]
    public async Task NumericReadoutsUsePreparedAxisCaptionsAndKeepRawSelectionFacts(HtmlChartTooltipMode mode) {
        if (!Enabled) return;
        var values = new[] {
            new ChartPoint(.025, 5.15), new ChartPoint(1e-10, -1e-12),
            new ChartPoint(.10000000000000002, .10000000000000003)
        };
        var expectedX = new[] { "0.03", "1E-10", "0.1" };
        var expectedY = new[] { "5.15", "-1E-12", "0.1" };
        var expectedTitles = new[] { "0.03", "1E-10", "0.1" };
        var compact = mode == HtmlChartTooltipMode.SharedX;
        var chart = Chart.Create().WithSize(compact ? 316 : 596, 338).WithLegend(false)
            .WithTheme(VisualTheme.Graphite(), compact ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithTitle("Observed numeric values").AddSeries(ChartSeries.CreateScatter(new ChartSeriesId("observed"), "Observed", values));
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => options.Tooltip.Mode = mode), compact ? 340 : 700, 500);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        await page.EvaluateAsync("() => document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.numericSelection = event.detail.target)");
        for (var index = 0; index < values.Length; index++) {
            var selector = Point(0, index); var point = page.Locator(selector);
            var rawX = await point.GetAttributeAsync("data-cfx-x");
            var rawY = await point.GetAttributeAsync("data-cfx-y");
            Assert.Equal(values[index].X, double.Parse(rawX!, CultureInfo.InvariantCulture));
            Assert.Equal(values[index].Y, double.Parse(rawY!, CultureInfo.InvariantCulture));
            await point.FocusAsync(); await page.Keyboard.PressAsync("Space");
            var keyboard = await ReadoutAsync(page, mode);
            var selected = await page.EvaluateAsync<JsonElement>("() => window.numericSelection");
            Assert.Equal("observed:" + index, selected.GetProperty("targetId").GetString());
            Assert.Equal(index.ToString(CultureInfo.InvariantCulture), selected.GetProperty("sourcePoint").GetString());
            Assert.Equal(expectedY[index], selected.GetProperty("value").GetString());
            Assert.Equal(values[index].Y, selected.GetProperty("numericValue").GetDouble());
            Assert.Equal(values[index].X, selected.GetProperty("x").GetDouble());
            if (index == 0) await CaptureAsync(page, "numeric-" + mode + "-keyboard", new { rawX, rawY, keyboard, selected, Errors = errors, ConsoleErrors = ErrorCount(session) });
            await page.Keyboard.PressAsync("Space"); await point.BlurAsync();
            await MoveAwayAsync(page); await MoveToAsync(page, selector);
            var pointer = await ReadoutAsync(page, mode);
            if (index == 0) await CaptureAsync(page, "numeric-" + mode + "-pointer", new { rawX, rawY, pointer, Errors = errors, ConsoleErrors = ErrorCount(session) });
            Assert.Equal(keyboard, pointer);
            Assert.Equal(new[] { mode == HtmlChartTooltipMode.SharedX ? expectedTitles[index] : expectedX[index], expectedY[index] }, keyboard);
            Assert.Equal(rawX, await point.GetAttributeAsync("data-cfx-x"));
            Assert.Equal(rawY, await point.GetAttributeAsync("data-cfx-y"));
            Assert.Equal("observed:" + index, await point.GetAttributeAsync("data-cfx-target-id"));
        }
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(HtmlChartTooltipMode.Single, false)]
    [InlineData(HtmlChartTooltipMode.SharedX, false)]
    [InlineData(HtmlChartTooltipMode.Single, true)]
    [InlineData(HtmlChartTooltipMode.SharedX, true)]
    public async Task CapturedCaptionsIncludingEmptyStringsOwnTheReadout(HtmlChartTooltipMode mode, bool empty) {
        if (!Enabled) return;
        var xCaption = empty ? "" : "Sample <A>"; var valueCaption = empty ? "" : "5.15 ms";
        var chart = Chart.Create().WithSize(596, 338).WithLegend(false)
            .WithXLabels(new[] { new ChartAxisLabel(.025, "Mapped coordinate") })
            .AddSeries(ChartSeries.CreateScatter(new ChartSeriesId("observed"), "Observed", new[] { new ChartPoint(.025, 5.15) }));
        var html = chart.ToInteractiveHtmlPage(options => options.Tooltip.Mode = mode);
        const string source = "data-cfx-source-id=\"series-0-point-0\"";
        Assert.Equal(2, html.Split(source, StringSplitOptions.None).Length);
        // An embedding host may author captured mark attributes before the adapter binds its chart.
        html = html.Replace(source, source + " data-cfx-x-label=\"" + System.Net.WebUtility.HtmlEncode(xCaption)
            + "\" data-cfx-formatted-value=\"" + System.Net.WebUtility.HtmlEncode(valueCaption) + "\"", StringComparison.Ordinal);
        await using var session = await OpenAsync(html);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        var point = page.Locator(Point(0, 0));
        await point.FocusAsync(); var keyboard = await ReadoutAsync(page, mode);
        await point.BlurAsync(); await MoveAwayAsync(page); await MoveToAsync(page, Point(0, 0));
        var pointer = await ReadoutAsync(page, mode);
        await CaptureAsync(page, "captured-" + mode + "-" + (empty ? "blank" : "text"), new { keyboard, pointer, Errors = errors, ConsoleErrors = ErrorCount(session) });
        Assert.Equal(new[] { xCaption, valueCaption }, keyboard);
        Assert.Equal(keyboard, pointer);
        Assert.Equal(xCaption, await point.GetAttributeAsync("data-cfx-x-label"));
        Assert.Equal(valueCaption, await point.GetAttributeAsync("data-cfx-formatted-value"));
        Assert.Equal(5.15, double.Parse((await point.GetAttributeAsync("data-cfx-y"))!, CultureInfo.InvariantCulture));
        Assert.Equal("observed:0", await point.GetAttributeAsync("data-cfx-target-id"));
        Assert.Equal(0, await page.Locator(".cfx-tooltip a").CountAsync());
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task SharedRowsKeepEqualXGroupingAndSeparateAdjacentUnequalNumbers() {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(596, 338).WithLegend(false)
            .AddSeries(ChartSeries.CreateScatter(new ChartSeriesId("observed"), "Observed", new[] { new ChartPoint(.025, 5.15), new ChartPoint(.10000000000000002, 8) }))
            .AddSeries(ChartSeries.CreateScatter(new ChartSeriesId("peer"), "Peer", new[] { new ChartPoint(.025, 2), new ChartPoint(.10000000000000003, 3) }));
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage());
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        var equal = page.Locator(Point(0, 0)); await equal.FocusAsync();
        Assert.Equal(new[] { "Observed", "Peer" }, await page.Locator(".cfx-tooltip dt").AllTextContentsAsync());
        Assert.Equal("0.03", await page.Locator(".cfx-tooltip__title").InnerTextAsync());
        await page.Keyboard.PressAsync("ArrowDown");
        Assert.Equal("peer:0", await page.EvaluateAsync<string>("() => document.activeElement.dataset.cfxTargetId"));
        var distinct = page.Locator(Point(0, 1)); var rawX = await distinct.GetAttributeAsync("data-cfx-x");
        var peerRawX = await page.Locator(Point(1, 1)).GetAttributeAsync("data-cfx-x");
        Assert.NotEqual(double.Parse(rawX!, CultureInfo.InvariantCulture), double.Parse(peerRawX!, CultureInfo.InvariantCulture));
        await distinct.FocusAsync();
        Assert.Equal(new[] { "Observed" }, await page.Locator(".cfx-tooltip dt").AllTextContentsAsync());
        Assert.Equal("0.1", await page.Locator(".cfx-tooltip__title").InnerTextAsync());
        await CaptureAsync(page, "numeric-distinct-x", new { rawX, peerRawX, Errors = errors, ConsoleErrors = ErrorCount(session) });
        await page.Keyboard.PressAsync("ArrowDown");
        Assert.Equal("peer:1", await page.EvaluateAsync<string>("() => document.activeElement.dataset.cfxTargetId"));
        Assert.Equal(rawX, await distinct.GetAttributeAsync("data-cfx-x"));
        Assert.Equal(peerRawX, await page.Locator(Point(1, 1)).GetAttributeAsync("data-cfx-x"));
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }

    private static Task<string[]> ReadoutAsync(IPage page, HtmlChartTooltipMode mode) => page.EvaluateAsync<string[]>("""
        shared => {
          const tip = document.querySelector('.cfx-tooltip');
          const field = name => Array.from(tip.querySelectorAll('dt')).find(term => term.textContent === name)?.nextElementSibling.textContent;
          return shared ? [tip.querySelector('.cfx-tooltip__title').textContent, tip.querySelector('dd').textContent] : [field('X'), field('Y')];
        }
        """, mode == HtmlChartTooltipMode.SharedX);

    private static int ErrorCount(HtmlBrowserSession session) => session.ConsoleLog.Count(entry => entry.Type == HtmlConsoleMessageType.Error);

    private static async Task CaptureAsync(IPage page, string name, object facts) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory); var path = Path.Combine(directory, name);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = path + ".png" });
        await File.WriteAllTextAsync(path + ".html", await page.ContentAsync());
        await File.WriteAllTextAsync(path + ".json", JsonSerializer.Serialize(facts, new JsonSerializerOptions { WriteIndented = true }));
    }
}

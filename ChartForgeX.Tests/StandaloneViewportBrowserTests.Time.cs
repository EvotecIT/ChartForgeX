using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class StandaloneViewportBrowserTests {
    private static Chart TimeChart(ChartSeriesKind kind, double? minimum = null, double? maximum = null) {
        var first = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var points = Enumerable.Range(0, 90).Select(day => new ChartPoint(first.AddDays(day), 40 + 30 * Math.Sin(day / 9.0))).ToArray();
        var series = new ChartSeries(new ChartSeriesId("samples"), "Samples", kind, points);
        if (kind == ChartSeriesKind.Line) series.ConfigureMarkers(markers => { markers.Enabled = true; markers.Radius = 4; });
        var chart = Chart.Create().WithSize(800, 440).WithTitle("Saved daily readings").WithLegend().WithDataLabels(false)
            .WithXAxisTimeScale().WithYAxisBounds(0, 100).AddSeries(series);
        if (minimum.HasValue && maximum.HasValue) chart.WithXAxisBounds(minimum.Value, maximum.Value);
        return chart;
    }

    // What a native preparation of the same X bounds paints: its fitted tick values and axis captions.
    private static (double Value, string Text)[] NativeTimeTicks(ChartSeriesKind kind, JsonElement window) {
        var html = TimeChart(kind, window.GetProperty("minimum").GetDouble(), window.GetProperty("maximum").GetDouble()).ToInteractiveHtmlFragmentWithoutAssets();
        var match = Regex.Match(html, "data-cfx-prepared-chart=\"([^\"]+)\"");
        Assert.True(match.Success);
        using var metadata = JsonDocument.Parse(WebUtility.HtmlDecode(match.Groups[1].Value));
        var values = metadata.RootElement.GetProperty("dataProjection").GetProperty("x").GetProperty("displayTicks").EnumerateArray()
            .Select(tick => tick.GetProperty("value").GetDouble()).ToArray();
        var svg = html.Substring(html.IndexOf("<svg", StringComparison.Ordinal), html.LastIndexOf("</svg>", StringComparison.Ordinal) + 6 - html.IndexOf("<svg", StringComparison.Ordinal));
        var captions = XDocument.Parse(svg).Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "axis-x-label")
            .Select(element => element.Value).ToArray();
        Assert.Equal(values.Length, captions.Length);
        return values.Zip(captions).ToArray();
    }

    private static async Task AssertNativeTimeTicksAsync(IPage page, ChartSeriesKind kind, JsonElement window) {
        await SettleMotionAsync(page);
        var painted = (await page.Locator("[data-cfx-browser-axis=x] text[data-cfx-axis-value]").EvaluateAllAsync<JsonElement>(
            "nodes=>nodes.map(node=>({value:Number(node.dataset.cfxAxisValue),text:node.textContent}))")).EnumerateArray()
            .Select(tick => (tick.GetProperty("value").GetDouble(), tick.GetProperty("text").GetString()!)).ToArray();
        var native = NativeTimeTicks(kind, window);
        Assert.True(native.Length >= 2, "The native window must caption at least two dates.");
        Assert.Equal(native.Select(tick => tick.Text), painted.Select(tick => tick.Item2));
        Assert.All(native.Zip(painted), pair => Assert.Equal(pair.First.Value, pair.Second.Item1, 9));
    }

    [BrowserTheory]
    [InlineData(ChartSeriesKind.Line)]
    [InlineData(ChartSeriesKind.Bar)]
    public async Task SavedTimeSeriesZoomsWithNativeCalendarTicksKeepsInkSharpAndResets(ChartSeriesKind kind) {
        if (!Enabled) return;
        await using var session = await OpenAsync(FamilyPage(TimeChart(kind)), 980, 800);
        var page = session.Page;
        await MountAsync(page);
        Assert.Equal("true", await page.Locator(Root).GetAttributeAsync("data-cfx-standalone-projection"));
        Assert.Null(await page.Locator(Root).GetAttributeAsync("data-cfx-zoom-declined"));
        var full = (await ViewportAsync(page)).GetProperty("x");
        const string Ink = "[data-cfx-role=series][data-cfx-series-key=samples] :is(path,rect)";
        var stroke = await page.Locator(Ink).First.EvaluateAsync<double>("node=>parseFloat(getComputedStyle(node).strokeWidth)||0");

        await MoveToAsync(page, ".cfx-stage");
        await page.Locator("[data-cfx-zoom=in]").ClickAsync();
        var toolbar = (await ViewportAsync(page)).GetProperty("x");
        Assert.True(toolbar.GetProperty("maximum").GetDouble() - toolbar.GetProperty("minimum").GetDouble()
            < full.GetProperty("maximum").GetDouble() - full.GetProperty("minimum").GetDouble() - 1);
        await AssertNativeTimeTicksAsync(page, kind, toolbar);

        var pointer = await PlotPointerAsync(page, .5, .5);
        await page.Mouse.MoveAsync(pointer.X, pointer.Y);
        await page.Mouse.WheelAsync(0, -300);
        await page.WaitForFunctionAsync("span=>controller.getXWindow().maximum-controller.getXWindow().minimum<span-1",
            toolbar.GetProperty("maximum").GetDouble() - toolbar.GetProperty("minimum").GetDouble());
        var wheeled = (await ViewportAsync(page)).GetProperty("x");
        await AssertNativeTimeTicksAsync(page, kind, wheeled);
        // Only data positions move: series strokes keep their drawn width.
        Assert.Equal(stroke, await page.Locator(Ink).First.EvaluateAsync<double>("node=>parseFloat(getComputedStyle(node).strokeWidth)||0"), 5);
        await CaptureAsync(page, "time-" + kind.ToString().ToLowerInvariant());
        // A window of a few days moves from date captions to the native clock captions; daily bars keep their cadence.
        var day = ChartDateTime.ToOADate(new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc));
        await page.EvaluateAsync("([day,span])=>controller.setXWindow(day+.3,day+span)", new[] { day, kind == ChartSeriesKind.Bar ? 6.9 : 1.9 });
        await AssertNativeTimeTicksAsync(page, kind, (await ViewportAsync(page)).GetProperty("x"));

        await page.Locator("[data-cfx-reset]").ClickAsync();
        await SettleMotionAsync(page);
        AssertWindow((await ViewportAsync(page)).GetProperty("x"), full.GetProperty("minimum").GetDouble(), full.GetProperty("maximum").GetDouble());
        Assert.Equal(0, await page.Locator("[data-cfx-browser-axis]").CountAsync());
        AssertNoConsoleErrors(session);
    }
}

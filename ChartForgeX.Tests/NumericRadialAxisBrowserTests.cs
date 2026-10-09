using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class NumericRadialAxisBrowserTests {
    [Theory]
    [InlineData("axes-wide-light", -75, 800, false, false, true)]
    [InlineData("axes-compact-dark-reversed", -75, 360, true, true, true)]
    [InlineData("gallery-axes-wide-light", 0, 800, false, false, true)]
    [InlineData("gallery-axes-compact-dark", 0, 360, true, false, true)]
    [InlineData("categories-right-compact-light", 0, 360, false, false, false)]
    [InlineData("categories-bottom-wide-dark-reversed", 90, 800, true, true, false)]
    [InlineData("categories-left-compact-dark-reversed", 180, 360, true, true, false)]
    [InlineData("categories-top-wide-light", 270, 800, false, false, false)]
    [InlineData("categories-diagonal-wide-dark", 45, 800, true, false, false)]
    public async Task RadialAxisCaptionsRemainReadableInActualHosts(string name, int start, int width, bool dark, bool reversed, bool dual) {
        if (!Enabled) return;
        var chart = dual ? NumericRadialAxisLabelTests.DualAxes(width, reversed) : NumericRadialAxisLabelTests.Categories(start, width, reversed);
        if (name.StartsWith("gallery-", StringComparison.Ordinal)) chart = V2GalleryModels.CreateRadialBarScales().WithSize(width, width == 360 ? 360 : 440)
            .WithTitle("Requests and resolution rate").WithSubtitle("Request counts and percentages use independent scales");
        chart.WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight());
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), width + 40, width == 360 ? 420 : 510);
        await CaptureAsync(session, chart, name);
        Assert.Equal(dual ? 10 : 3, await session.Page.Locator(dual ? "[data-cfx-role='radial-value-label'] text" : "[data-cfx-role='radial-category-label'] text").CountAsync());
        if (dual) Assert.True(await session.Page.EvaluateAsync<bool>("() => {const boxes=Array.from(document.querySelectorAll('[data-cfx-role=\"radial-value-label\"] text')).map(text=>text.getBoundingClientRect());return boxes.every((a,i)=>boxes.slice(i+1).every(b=>a.right<=b.left||b.right<=a.left||a.bottom<=b.top||b.bottom<=a.top));}"));
        if (!dual && width == 800) Assert.Equal(new[] { "North", "South", "East" }, await session.Page.Locator("[data-cfx-role='radial-category-label'] text").AllTextContentsAsync());
        Assert.True(await session.Page.EvaluateAsync<bool>("() => Array.from(document.querySelectorAll('[data-cfx-role=\"radial-value-label\"] text,[data-cfx-role=\"radial-category-label\"] text')).every(text => {const b=text.getBoundingClientRect();return b.width>0&&b.height>0&&b.left>=0&&b.right<=innerWidth;})"));
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false, true, 1)]
    [InlineData(true, true, 1)]
    [InlineData(false, true, -1)]
    [InlineData(true, true, -1)]
    public async Task ReversedCartesianValueEndCaptionsRenderWithTheirSourceFacts(bool horizontal, bool reversed, int sign) {
        if (!Enabled) return;
        var chart = CartesianReversalLabelTests.Grouped(horizontal, reversed, sign);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), 680, 420);
        await CaptureAsync(session, chart, $"reversed-{(horizontal ? "horizontal" : "vertical")}-{(sign > 0 ? "positive" : "negative")}");
        Assert.Equal(2, await session.Page.Locator("[data-cfx-role='data-label'] text").CountAsync());
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-90)]
    public async Task FullTurnCategoryOverflowRetainsItsDescriptionsInActualHosts(int start) {
        if (!Enabled) return;
        var chart = NumericRadialAxisLabelTests.Categories(start, 800, false).WithRadialGeometry(new(start, start + 360, .1, .25, .1));
        for (var point = 0; point < chart.Series[0].Points.Count; point++) chart.Series[0].Points[point] = new ChartForgeX.Primitives.ChartPoint(point + 1, 100);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), 840, 510);
        await CaptureAsync(session, chart, start == 0 ? "full-turn-right" : "full-turn-top");
        Assert.Equal(new[] { "North", "South", "East" }, await session.Page.Locator("[data-cfx-role='radial-category-label-source']")
            .EvaluateAllAsync<string[]>("nodes => nodes.map(node => node.getAttribute('data-cfx-full-label'))"));
        Assert.True(await session.Page.Locator("[data-cfx-role='radial-category-label'] text").CountAsync() < 3);
        AssertNoConsoleErrors(session);
    }

    private static async Task CaptureAsync(HtmlTinkerX.HtmlBrowserSession session, Chart chart, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        File.WriteAllText(Path.Combine(directory, name + ".svg"), prepared.ToSvg());
        File.WriteAllBytes(Path.Combine(directory, name + "-native.png"), prepared.ToPng());
        File.WriteAllText(Path.Combine(directory, name + ".html"), await session.Page.ContentAsync());
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + "-browser.png"), FullPage = true });
        File.WriteAllText(Path.Combine(directory, name + "-console.json"), System.Text.Json.JsonSerializer.Serialize(session.ConsoleLog.Select(entry => new { type = entry.Type.ToString(), entry.Text })));
    }
}

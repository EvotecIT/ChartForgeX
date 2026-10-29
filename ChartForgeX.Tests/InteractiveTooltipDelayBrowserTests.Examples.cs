using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveTooltipDelayBrowserTests {
    [BrowserTheory]
    [InlineData("light", 340)]
    [InlineData("light", 950)]
    [InlineData("dark", 340)]
    [InlineData("dark", 950)]
    public async Task PromotedGraphiteExamplesShowDelayedPointerAndImmediateKeyboardReadouts(string theme, int width) {
        if (!Enabled) return;
        var file = "graphite-" + theme + "-line-interactive.html";
        var html = await File.ReadAllTextAsync(Path.Combine(TestRepository.Root, "Website", "static", "examples", "generated", file));
        await using var session = await OpenAsync(html, width, 760);
        var page = session.Page;
        var pageErrors = new List<string>();
        page.PageError += (_, error) => pageErrors.Add(error);
        var served = Environment.GetEnvironmentVariable("CFX_TOOLTIP_DELAY_EXAMPLE_URL");
        if (!string.IsNullOrWhiteSpace(served)) {
            Assert.True(Uri.TryCreate(served, UriKind.Absolute, out var uri) && uri.IsLoopback && uri.Scheme == "http");
            await page.GotoAsync(new Uri(new Uri(served), file).AbsoluteUri);
        }
        Assert.Equal("360", await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-tooltip-delay"));
        await CentreReadableTargetAsync(page, Point(0, 3));
        await TraceAsync(page);
        await PointerAsync(page, Point(0, 3));
        await CaptureDelayAsync(page, "delay-example-" + theme + "-" + width + "-entry", await TraceStateAsync(page), session);
        await WaitForTipAsync(page);
        var trace = await TraceStateAsync(page);
        await CaptureDelayAsync(page, "delay-example-" + theme + "-" + width + "-shown", trace, session);
        Assert.Equal(new[] { "Failed", "Passed", "Warnings" }, (await page.Locator(".cfx-tooltip [data-cfx-tooltip-series-key]").AllTextContentsAsync()).OrderBy(name => name, StringComparer.Ordinal));
        Assert.All(trace.GetProperty("entry").EnumerateArray(), entry => Assert.True(entry.GetProperty("hidden").GetBoolean()));
        AssertDelay(trace);
        await MoveAwayAsync(page);
        var target = page.Locator(Point(0, 4));
        await target.FocusAsync();
        Assert.False(await TipHiddenAsync(page));
        Assert.Equal("passed", await target.GetAttributeAsync("data-cfx-series-key"));
        Assert.Equal("4", await target.GetAttributeAsync("data-cfx-source-point"));
        Assert.Equal("1120", await target.GetAttributeAsync("data-cfx-y"));
        var caption = await target.GetAttributeAsync("data-cfx-formatted-value");
        Assert.Equal("1.1k", caption);
        Assert.Equal(caption, await page.Locator(".cfx-tooltip [data-cfx-tooltip-series-key='passed'] + dd").InnerTextAsync());
        await CaptureDelayAsync(page, "delay-example-" + theme + "-" + width + "-keyboard", new { trace = await TraceStateAsync(page), caption, pageErrors }, session);
        Assert.Empty(pageErrors);
        AssertNoConsoleErrors(session);
    }
}

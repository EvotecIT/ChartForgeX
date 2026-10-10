using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveTooltipDelayBrowserTests {
    [Theory]
    [InlineData("light", 340)]
    [InlineData("light", 950)]
    [InlineData("dark", 340)]
    [InlineData("dark", 950)]
    public async Task PromotedGraphiteExamplesShowDelayedPointerAndImmediateKeyboardReadouts(string theme, int width) {
        if (!Enabled) return;
        var repository = new DirectoryInfo(AppContext.BaseDirectory);
        while (repository != null && !File.Exists(Path.Combine(repository.FullName, "ChartForgeX.sln"))) repository = repository.Parent;
        Assert.NotNull(repository);
        var file = "graphite-" + theme + "-line-interactive.html";
        var html = await File.ReadAllTextAsync(Path.Combine(repository.FullName, "Website", "static", "examples", "generated", file));
        await using var session = await OpenAsync(html, width, 760);
        var page = session.Page;
        var served = Environment.GetEnvironmentVariable("CFX_TOOLTIP_DELAY_EXAMPLE_URL");
        if (!string.IsNullOrWhiteSpace(served)) {
            Assert.True(Uri.TryCreate(served, UriKind.Absolute, out var uri) && uri.IsLoopback && uri.Scheme == "http");
            await page.GotoAsync(new Uri(new Uri(served), file).AbsoluteUri);
        }
        Assert.Equal("360", await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-tooltip-delay"));
        await CentreReadableTargetAsync(page, Point(0, 3));
        await TraceAsync(page);
        await PointerAsync(page, Point(0, 3));
        Assert.True(await TipHiddenAsync(page));
        await CaptureDelayAsync(page, "delay-example-" + theme + "-" + width + "-pending", await TraceStateAsync(page), session);
        await WaitForTipAsync(page);
        var trace = await TraceStateAsync(page);
        await CaptureDelayAsync(page, "delay-example-" + theme + "-" + width + "-shown", trace, session);
        Assert.Equal(new[] { "Failed", "Passed", "Warnings" }, (await page.Locator(".cfx-tooltip [data-cfx-tooltip-series-key]").AllTextContentsAsync()).OrderBy(name => name, StringComparer.Ordinal));
        AssertDelay(trace);
        await MoveAwayAsync(page);
        await page.Locator(Point(0, 4)).FocusAsync();
        Assert.False(await TipHiddenAsync(page));
        Assert.Contains("1120", (await TooltipTextAsync(page)).Replace(",", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
        AssertNoConsoleErrors(session);
    }
}

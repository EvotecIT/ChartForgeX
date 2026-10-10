using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class InteractiveKeyboardFocusRecoveryBrowserTests {
    private const string DataStop = "[data-cfx-keyboard-component='data'][tabindex='0']";

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task MutingTheFocusedSeriesRecoversNavigationWithinTheLocalOrPeerChart(bool synchronized, bool dark) {
        if (!Enabled) return;
        var chart = StatusLines(dark);
        if (!synchronized) chart.WithSize(900, 420);
        var html = synchronized ? new[] { chart, chart }.ToInteractiveHtmlDashboardPage(options => {
            options.Columns = 1;
            options.Interaction.Enable(ChartInteractionFeatures.SynchronizedCharts);
        }) : chart.ToInteractiveHtmlPage();
        await using var session = await OpenAsync(html, synchronized ? 700 : 340, synchronized ? 1000 : 560);
        var page = session.Page;
        var roots = page.Locator(".cfx-interactive-chart");
        var active = roots.Nth(synchronized ? 1 : 0);
        await active.Locator(Point(1, 3)).FocusAsync();

        // A host can activate the existing legend binding without transferring focus from the datum.
        await ActivateLegendAsync(roots.Nth(0), 1);
        Assert.Equal("true", await active.Locator(Legend(1)).GetAttributeAsync("data-cfx-muted"));
        await AssertFocusedAsync(active.Locator(Point(0, 0)));
        Assert.True(await active.EvaluateAsync<bool>("root => { const stage = root.querySelector('.cfx-stage'); const box = stage.getBoundingClientRect(); const target = document.activeElement.getBoundingClientRect(); const x = target.left + target.width / 2; return x >= box.left + stage.clientLeft && x <= box.left + stage.clientLeft + stage.clientWidth; }"));
        await page.Keyboard.PressAsync("End");
        await AssertFocusedAsync(active.Locator(Point(0, 6)));
        await page.Keyboard.PressAsync("ArrowDown");
        await AssertFocusedAsync(active.Locator(Point(2, 6)));
        await page.Keyboard.PressAsync("Home");
        await AssertFocusedAsync(active.Locator(Point(2, 0)));
        await page.Keyboard.PressAsync("ArrowRight");
        await AssertFocusedAsync(active.Locator(Point(2, 1)));
        Assert.Equal(1, await active.Locator(DataStop).CountAsync());
        await CaptureAsync(page, "keyboard-focus-recovered-" + (synchronized ? "sync-dark" : "local-compact-light") + ".png");
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task MutingAllFocusedDataRecoversToTheLegendForKeyboardUnmute() {
        if (!Enabled) return;
        await using var session = await OpenAsync(StatusLines(true).ToInteractiveHtmlPage());
        var page = session.Page;
        var root = page.Locator(".cfx-interactive-chart");
        await root.Locator(Point(1, 3)).FocusAsync();
        await ActivateLegendAsync(root, 0);
        await AssertFocusedAsync(root.Locator(Point(1, 3)));
        await ActivateLegendAsync(root, 1);
        await AssertFocusedAsync(root.Locator(Point(2, 0)));
        await ActivateLegendAsync(root, 2);
        Assert.Equal(0, await root.Locator(DataStop).CountAsync());
        await AssertFocusedAsync(root.Locator(Legend(0)));
        await CaptureAsync(page, "keyboard-focus-recovered-all-muted-legend.png");

        await page.Keyboard.PressAsync("Space");
        Assert.Equal("false", await root.Locator(Legend(0)).GetAttributeAsync("data-cfx-muted"));
        await AssertFocusedAsync(root.Locator(Legend(0)));
        await page.Keyboard.PressAsync("Shift+Tab");
        await AssertFocusedAsync(root.Locator(Point(0, 0)));
        await page.Keyboard.PressAsync("ArrowRight");
        await AssertFocusedAsync(root.Locator(Point(0, 1)));
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task SynchronizedMutePreservesEligibleDataControlAndExternalHostFocus() {
        if (!Enabled) return;
        var html = new[] { StatusLines(false), StatusLines(false) }.ToInteractiveHtmlDashboardPage(options => {
            options.Columns = 1;
            options.Interaction.Enable(ChartInteractionFeatures.SynchronizedCharts);
        }).Replace("<main ", "<button id=\"host-control\">Host control</button><main ", StringComparison.Ordinal);
        await using var session = await OpenAsync(html, 700, 1000);
        var page = session.Page;
        var roots = page.Locator(".cfx-interactive-chart");
        await roots.Nth(1).Locator(Point(0, 3)).FocusAsync();
        await ActivateLegendAsync(roots.Nth(0), 1);
        await AssertFocusedAsync(roots.Nth(1).Locator(Point(0, 3)));
        await page.Keyboard.PressAsync("End");
        await AssertFocusedAsync(roots.Nth(1).Locator(Point(0, 6)));

        var reset = roots.Nth(1).Locator("[data-cfx-reset]");
        await reset.FocusAsync();
        await ActivateLegendAsync(roots.Nth(0), 2);
        await AssertFocusedAsync(reset);
        var host = page.Locator("#host-control");
        await host.FocusAsync();
        await ActivateLegendAsync(roots.Nth(0), 1);
        await AssertFocusedAsync(host);
        await page.Keyboard.PressAsync("ArrowRight");
        await AssertFocusedAsync(host);
        AssertNoConsoleErrors(session);
    }

    private static Task ActivateLegendAsync(ILocator root, int series) => root.Locator(Legend(series))
        .EvaluateAsync("node => node.dispatchEvent(new MouseEvent('click', { bubbles: true }))");

    private static async Task AssertFocusedAsync(ILocator target) =>
        Assert.True(await target.EvaluateAsync<bool>("node => node === document.activeElement"));

    private static async Task CaptureAsync(IPage page, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name) });
    }
}

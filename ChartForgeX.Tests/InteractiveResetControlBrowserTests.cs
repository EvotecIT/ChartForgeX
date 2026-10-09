using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class InteractiveResetControlBrowserTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResetControl_SelectionOnly_ShowsUntilDeselectedClearedOrReset(bool dark) {
        if (!Enabled) return;
        await using var session = await OpenAsync(StatusLines(dark).ToInteractiveHtmlPage());
        var page = session.Page;
        var target = page.Locator(Point(1, 2));
        var reset = page.Locator("[data-cfx-reset]");
        Assert.False(await reset.IsVisibleAsync());

        await target.ClickAsync();
        Assert.Equal("true", await target.GetAttributeAsync("aria-selected"));
        await AssertResetInCardCornerAsync(page);
        await CaptureAsync(page, dark ? "selection-reset-dark.png" : "selection-reset-light.png");
        await target.ClickAsync();
        Assert.False(await reset.IsVisibleAsync());

        await target.FocusAsync();
        await page.Keyboard.PressAsync("Space");
        Assert.True(await reset.IsVisibleAsync());
        await page.Locator("[data-cfx-compare-clear]").ClickAsync();
        Assert.False(await reset.IsVisibleAsync());
        Assert.Equal(0, await page.Locator(".cfx-selected").CountAsync());
        Assert.Equal("cfx-interactive-chart", await page.EvaluateAsync<string>("() => document.activeElement.className"));

        await target.ClickAsync();
        await ResetAsync(page);
        Assert.Equal(0, await page.Locator(".cfx-selected").CountAsync());
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task ResetControl_SynchronizedSelectionOnly_TracksBothChartsWithoutCompareMarkers() {
        if (!Enabled) return;
        var html = new[] { StatusLines(false), StatusLines(false) }.ToInteractiveHtmlDashboardPage(options => {
            options.Columns = 1;
            options.Interaction.Features = ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.Selection | ChartInteractionFeatures.SynchronizedCharts;
        });
        await using var session = await OpenAsync(html, 700, 1000);
        var page = session.Page;
        var roots = page.Locator(".cfx-interactive-chart");
        var target = roots.Nth(0).Locator(Point(1, 2));
        foreach (var index in new[] { 0, 1 }) Assert.False(await roots.Nth(index).Locator("[data-cfx-reset]").IsVisibleAsync());

        await target.ClickAsync();
        foreach (var index in new[] { 0, 1 }) {
            Assert.Equal("true", await roots.Nth(index).Locator(Point(1, 2)).GetAttributeAsync("aria-selected"));
            Assert.True(await roots.Nth(index).Locator("[data-cfx-reset]").IsVisibleAsync());
        }
        await CaptureAsync(page, "selection-reset-synchronized.png");
        await target.ClickAsync();
        foreach (var index in new[] { 0, 1 }) Assert.False(await roots.Nth(index).Locator("[data-cfx-reset]").IsVisibleAsync());
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResetControl_ViewChanges_ShowsCornerButtonOnlyUntilReset(bool dark) {
        if (!Enabled) return;
        var html = StatusLines(dark).ToInteractiveHtmlPage(options => options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan | ChartInteractionFeatures.Brush));
        await using var session = await OpenAsync(html);
        var page = session.Page;
        var reset = page.Locator("[data-cfx-reset]");
        Assert.False(await reset.IsVisibleAsync());

        await page.Locator(Legend(1)).ClickAsync();
        await AssertResetInCardCornerAsync(page);
        await ResetAsync(page);
        Assert.Equal(0, await page.Locator(".cfx-series-muted").CountAsync());
        Assert.Equal("cfx-interactive-chart", await page.EvaluateAsync<string>("() => document.activeElement.className"));

        await page.Locator(Legend(2)).ClickAsync(new LocatorClickOptions { Modifiers = new[] { KeyboardModifier.Shift } });
        Assert.True(await reset.IsVisibleAsync());
        await ResetAsync(page);
        Assert.Equal(0, await page.Locator(".cfx-series-isolated-out").CountAsync());

        await page.Locator("[data-cfx-zoom=\"in\"]").ClickAsync();
        Assert.True(await reset.IsVisibleAsync());
        await ResetAsync(page);
        Assert.Equal("1.000", await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-zoom"));

        foreach (var mode in new[] { "pan", "brush" }) {
            await page.Locator("[data-cfx-mode-button=\"" + mode + "\"]").ClickAsync();
            await DragAcrossStageAsync(page);
            Assert.True(await reset.IsVisibleAsync(), mode + " should reveal the reset control.");
            await ResetAsync(page);
        }

        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResetControl_CompactReportReview_HasNoToolbarAndStaysInView(bool dark) {
        if (!Enabled) return;
        await using var session = await OpenAsync(StatusLines(dark).ToInteractiveHtmlPage(), 380, 560);
        var page = session.Page;
        Assert.Equal(0, await page.Locator(".cfx-toolbar").CountAsync());
        Assert.False(await page.Locator("[data-cfx-reset]").IsVisibleAsync());

        await page.Locator(Legend(1)).ClickAsync();
        var button = await BoxAsync(page, "[data-cfx-reset]");
        var frame = await BoxAsync(page, ".cfx-frame");
        // The readable layout scrolls the chart horizontally; the control pins to the visible stage edge.
        var visibleRight = await page.EvaluateAsync<double>("() => { const stage = document.querySelector('.cfx-stage'); return stage.getBoundingClientRect().left + stage.clientLeft + stage.clientWidth; }");
        Assert.Equal(28, button.Height, 1);
        Assert.InRange(visibleRight - (button.X + button.Width), 6, 10);
        Assert.InRange(button.Y - frame.Y, 6, 10);
        Assert.True(button.X + button.Width <= 380, "The reset control should stay inside a 380 px viewport.");
        await ResetAsync(page);
    }

    private static async Task CaptureAsync(IPage page, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name), FullPage = true });
    }

    private static async Task AssertResetInCardCornerAsync(IPage page) {
        var named = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Reset view", Exact = true });
        Assert.Equal(1, await named.CountAsync());
        Assert.True(await named.IsVisibleAsync());
        var button = await BoxAsync(page, "[data-cfx-reset]");
        var card = await BoxAsync(page, ".cfx-stage svg");
        Assert.Equal(28, button.Height, 1);
        Assert.InRange(card.X + card.Width - (button.X + button.Width), 6, 10);
        Assert.InRange(button.Y - card.Y, 6, 10);
        var style = await page.Locator("[data-cfx-reset]").EvaluateAsync<string[]>("node => { const s = getComputedStyle(node); return [s.backgroundColor, s.borderTopWidth, s.borderRadius, s.boxShadow]; }");
        var surface = await page.EvaluateAsync<string>("() => getComputedStyle(document.querySelector('[data-cfx-role=\"frame-card\"]')).fill");
        Assert.Equal(surface, style[0]);
        Assert.Equal(new[] { "1px", "6px", "none" }, style.Skip(1).ToArray());
    }

    private static async Task ResetAsync(IPage page) {
        await page.Locator("[data-cfx-reset]").ClickAsync();
        Assert.False(await page.Locator("[data-cfx-reset]").IsVisibleAsync());
    }

    private static async Task DragAcrossStageAsync(IPage page) {
        var stage = await BoxAsync(page, ".cfx-stage");
        await page.Mouse.MoveAsync((float)(stage.X + stage.Width * 0.35), (float)(stage.Y + stage.Height * 0.45));
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync((float)(stage.X + stage.Width * 0.55), (float)(stage.Y + stage.Height * 0.65), new MouseMoveOptions { Steps = 6 });
        await page.Mouse.UpAsync();
    }
}

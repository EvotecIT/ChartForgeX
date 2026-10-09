using ChartForgeX.Core;
using ChartForgeX.Themes;
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
        await AssertResetBelowCardAsync(page);
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
    public async Task ResetControl_ViewChanges_ShowsContextualButtonOnlyUntilReset(bool dark) {
        if (!Enabled) return;
        var html = StatusLines(dark).ToInteractiveHtmlPage(options => options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan | ChartInteractionFeatures.Brush));
        await using var session = await OpenAsync(html);
        var page = session.Page;
        var reset = page.Locator("[data-cfx-reset]");
        Assert.False(await reset.IsVisibleAsync());

        await page.Locator(Legend(1)).ClickAsync();
        await AssertResetBelowCardAsync(page);
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
        var stage = await BoxAsync(page, ".cfx-stage");
        // The readable layout scrolls the chart horizontally; the control follows below the visible frame.
        Assert.Equal(28, button.Height, 1);
        Assert.InRange(frame.X + frame.Width - (button.X + button.Width), 6, 10);
        Assert.InRange(button.Y - stage.Y - stage.Height, 6, 10);
        Assert.True(button.X + button.Width <= 380, "The reset control should stay inside a 380 px viewport.");
        await ResetAsync(page);
    }

    [Theory]
    [InlineData(false, 380, HtmlChartResponsiveLayout.Readable)]
    [InlineData(false, 380, HtmlChartResponsiveLayout.Fit)]
    [InlineData(false, 900, HtmlChartResponsiveLayout.Readable)]
    [InlineData(false, 900, HtmlChartResponsiveLayout.Fit)]
    [InlineData(true, 380, HtmlChartResponsiveLayout.Readable)]
    [InlineData(true, 380, HtmlChartResponsiveLayout.Fit)]
    [InlineData(true, 900, HtmlChartResponsiveLayout.Readable)]
    [InlineData(true, 900, HtmlChartResponsiveLayout.Fit)]
    public async Task ResetControlDoesNotCoverTheTitleOrChartAfterNativeNodeSelection(bool dark, int width, HtmlChartResponsiveLayout layout) {
        if (!Enabled) return;
        var chart = V2GalleryModels.Create(ChartSeriesKind.Sankey, "compact-options", dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithSize(360, 360).WithTitle("Requests across processing stages").WithSubtitle("Aligned and ordered weighted flows")
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight());
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => options.ResponsiveLayout = layout), width, 650);
        var page = session.Page;
        var target = page.Locator("[data-cfx-target-kind='node'][data-cfx-target-id='south-support']");
        var reset = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Reset view", Exact = true });
        Assert.False(await reset.IsVisibleAsync());
        await target.FocusAsync(); await page.Keyboard.PressAsync("Enter");
        Assert.Equal("true", await target.GetAttributeAsync("aria-selected"));
        var button = await BoxAsync(page, "[data-cfx-reset]");
        var card = await BoxAsync(page, ".cfx-stage");
        Assert.True(button.Y >= card.Y + card.Height + 6, "The contextual Reset view control must clear every title and mark in the chart.");
        Assert.True(button.X >= 0 && button.X + button.Width <= width, "The reset control must stay in the visible host viewport.");
        await CaptureAsync(page, "reset-native-node-" + width + "-" + layout.ToString().ToLowerInvariant() + "-" + (dark ? "dark" : "light") + ".png");
        await reset.ClickAsync();
        Assert.False(await reset.IsVisibleAsync());
        Assert.Equal(0, await page.Locator(".cfx-selected").CountAsync());
        Assert.Equal("cfx-interactive-chart", await page.EvaluateAsync<string>("() => document.activeElement.className"));
        AssertNoConsoleErrors(session);
    }

    private static async Task CaptureAsync(IPage page, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name), FullPage = true });
    }

    private static async Task AssertResetBelowCardAsync(IPage page) {
        var named = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Reset view", Exact = true });
        Assert.Equal(1, await named.CountAsync());
        Assert.True(await named.IsVisibleAsync());
        var button = await BoxAsync(page, "[data-cfx-reset]");
        var card = await BoxAsync(page, ".cfx-stage");
        var frame = await BoxAsync(page, ".cfx-frame");
        Assert.Equal(28, button.Height, 1);
        Assert.InRange(frame.X + frame.Width - (button.X + button.Width), 6, 10);
        Assert.InRange(button.Y - card.Y - card.Height, 6, 10);
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

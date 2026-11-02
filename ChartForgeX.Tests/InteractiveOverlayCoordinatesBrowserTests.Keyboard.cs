using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveOverlayCoordinatesBrowserTests {
    [BrowserTheory]
    [InlineData(false, HtmlChartResponsiveLayout.Fit, 950, "Enter")]
    [InlineData(true, HtmlChartResponsiveLayout.Fit, 950, "Space")]
    [InlineData(false, HtmlChartResponsiveLayout.Readable, 340, "Space")]
    [InlineData(true, HtmlChartResponsiveLayout.Readable, 340, "Enter")]
    public async Task KeyboardActivationPinsTheFocusedReadoutAtItsNativeMark(bool graphite, HtmlChartResponsiveLayout layout, int width, string key) {
        if (!Enabled) return;
        var html = CoordinateChart(graphite).ToInteractiveHtmlPage(options => {
            options.ResponsiveLayout = layout;
            options.Interaction.Features = ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.Selection | ChartInteractionFeatures.KeyboardNavigation;
            options.IncludeResetButton = false;
        });
        await using var session = await OpenAsync(html, width, 760);
        var page = session.Page;
        await RecordEventsAsync(page);
        await page.Locator(Point(0, 1)).FocusAsync();
        Assert.True(await page.Locator(Point(0, 1)).EvaluateAsync<bool>("n => n === document.activeElement"));
        Assert.False(await page.Locator(".cfx-tooltip").IsHiddenAsync());
        // The readout eases in and is re-placed once its content is measured; compare settled positions.
        await SettleTooltipAsync(page);
        var focused = await BoxAsync(page, ".cfx-tooltip");
        var native = await BoxAsync(page, Point(0, 1));
        var beforeText = await ReadoutTextAsync(page);
        await page.Keyboard.PressAsync(key);
        await SettleTooltipAsync(page);
        var pinned = await BoxAsync(page, ".cfx-tooltip");
        var events = await EventsAsync(page);
        await CaptureAsync(session, "overlay-keyboard-" + Variant(graphite, layout, width, 1, false) + "-" + key, new { focused, native, pinned, beforeText, events });
        Assert.InRange(pinned.X - focused.X, -1, 1);
        Assert.InRange(pinned.Y - focused.Y, -1, 1);
        Assert.Equal(beforeText, await ReadoutTextAsync(page));
        Assert.Equal("true", await page.Locator(Root).GetAttributeAsync("data-cfx-tooltip-pinned"));
        Assert.Equal("true", await page.Locator(Point(0, 1)).GetAttributeAsync("aria-selected"));
        var selection = Assert.Single(events.EnumerateArray(), e => e.GetProperty("name").GetString() == "cfxselect");
        Assert.Equal("reading-source:1", selection.GetProperty("detail").GetProperty("target").GetProperty("targetId").GetString());
        AssertNoConsoleErrors(session);
    }

    private static Task SettleTooltipAsync(Microsoft.Playwright.IPage page) => page.Locator(".cfx-tooltip").EvaluateAsync("async tip => { let last = \"\"; for (let stable = 0; stable < 3;) { await new Promise(requestAnimationFrame); await Promise.all(tip.getAnimations().map(animation => animation.finished)); const box = tip.getBoundingClientRect(), key = box.x + \",\" + box.y; stable = key === last ? stable + 1 : 0; last = key; } }");

    private static Task<string> ReadoutTextAsync(Microsoft.Playwright.IPage page) => page.Locator(".cfx-tooltip").EvaluateAsync<string>(
        "tip => [...tip.querySelectorAll('.cfx-tooltip__title,.cfx-tooltip__secondary,dt,dd')].map(node=>node.textContent).join('\\n')");
}

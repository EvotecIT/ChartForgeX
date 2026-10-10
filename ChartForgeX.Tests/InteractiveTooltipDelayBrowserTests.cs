using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Protects pointer pacing through actual acquisition, focus, pin and host lifecycle paths.</summary>
public sealed partial class InteractiveTooltipDelayBrowserTests {
    private const int Delay = 360;

    [Fact]
    public async Task ZeroDefaultShowsDuringNativeEntryWithoutQueuing() {
        if (!Enabled) return;
        await using var session = await OpenAsync(Observations(false).ToInteractiveHtmlPage(options => options.Tooltip.Mode = HtmlChartTooltipMode.Single), 950, 760);
        var page = session.Page;
        await TraceAsync(page);
        await PointerAsync(page, Point(0, 0));
        Assert.False(await TipHiddenAsync(page));
        var trace = await TraceStateAsync(page);
        Assert.False(trace.GetProperty("entry")[0].GetProperty("hidden").GetBoolean());
        Assert.Equal("0", await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-tooltip-delay"));
        await CaptureDelayAsync(page, "delay-default-zero", trace);
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(HtmlChartResponsiveLayout.Fit, 340, true, "exact", false)]
    [InlineData(HtmlChartResponsiveLayout.Fit, 950, false, "nearest", false)]
    [InlineData(HtmlChartResponsiveLayout.Readable, 340, false, "distance", true)]
    [InlineData(HtmlChartResponsiveLayout.Readable, 950, true, "distance", true)]
    public async Task StableAcquisitionShowsOnceAtLatestPointerWithoutPostponing(HtmlChartResponsiveLayout layout, int width, bool dark, string range, bool guide) {
        if (!Enabled) return;
        await using var session = await OpenAsync(Observations(dark).ToInteractiveHtmlPage(options => {
            ConfigureDelay(options, range);
            options.ResponsiveLayout = layout;
            if (!guide) options.Interaction.Disable(ChartInteractionFeatures.Crosshair);
        }), width, 760);
        var page = session.Page;
        await CentreReadableTargetAsync(page, Point(0, 0));
        await TraceAsync(page);
        var position = await PointerAsync(page, Point(0, 0));
        Assert.True(await TipHiddenAsync(page));
        Assert.NotNull(await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-hover-key"));
        Assert.Equal(!guide, await page.Locator(".cfx-crosshair").IsHiddenAsync());
        // Crossing from native paint into nearby stage space keeps the same acquired observation.
        var y = position[1] + (range == "exact" ? 0 : 36);
        for (var step = 0; step < 30 && await TipHiddenAsync(page); step++) {
            await Task.Delay(40);
            await page.Mouse.MoveAsync((float)(position[0] + (step % 2 == 0 ? .15 : -.15)), (float)y);
        }
        Assert.False(await TipHiddenAsync(page));
        var trace = await TraceStateAsync(page);
        Assert.True(trace.GetProperty("entry")[0].GetProperty("hidden").GetBoolean());
        AssertDelay(trace);
        Assert.Equal(1, trace.GetProperty("shown").GetArrayLength());
        var rows = await page.Locator(".cfx-tooltip [data-cfx-tooltip-series-key]").AllTextContentsAsync();
        Assert.Equal(new[] { "Reading", "Companion" }, rows);
        Assert.True(await page.EvaluateAsync<bool>("() => { const tip=document.querySelector('.cfx-tooltip'), p=window.delayTrace.pointer; return Math.abs(parseFloat(tip.style.left)-Math.max(8,Math.min(innerWidth-tip.offsetWidth-8,p[0]+14)))<.01 && document.documentElement.scrollWidth<=innerWidth; }"));
        await CaptureDelayAsync(page, "delay-stable-" + layout + "-" + width + "-" + (dark ? "dark" : "light"), trace);
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task SwitchingTargetsCancelsOldDeadlineAndLeavingCancelsTheNewReadout() {
        if (!Enabled) return;
        await using var session = await OpenAsync(Observations(false).ToInteractiveHtmlPage(options => { ConfigureDelay(options, "exact"); options.Tooltip.Mode = HtmlChartTooltipMode.Single; }), 950, 760);
        var page = session.Page;
        await TraceAsync(page);
        await PointerAsync(page, Point(0, 0));
        await Task.Delay(200);
        await PointerAsync(page, Point(0, 1));
        await Task.Delay(200);
        Assert.True(await TipHiddenAsync(page));
        await WaitForTipAsync(page);
        Assert.Equal("6", await page.Locator(".cfx-tooltip dt").Filter(new LocatorFilterOptions { HasText = "Y" }).Locator("+ dd").InnerTextAsync());
        await MoveAwayAsync(page);
        await PointerAsync(page, Point(0, 0));
        Assert.True(await TipHiddenAsync(page));
        await MoveAwayAsync(page);
        await Task.Delay(Delay + 60);
        Assert.True(await TipHiddenAsync(page));
        await CaptureDelayAsync(page, "delay-switch-leave", await TraceStateAsync(page));
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task KeyboardFocusAndExplicitPinAreImmediateAndCannotBeOverwritten(bool keyboard) {
        if (!Enabled) return;
        await using var session = await OpenAsync(Observations(true).ToInteractiveHtmlPage(options => {
            ConfigureDelay(options, "exact"); options.Tooltip.Mode = HtmlChartTooltipMode.Single;
            if (!keyboard) options.Interaction.Disable(ChartInteractionFeatures.KeyboardNavigation);
        }), 950, 760);
        var page = session.Page;
        await TraceAsync(page);
        await PointerAsync(page, Point(0, 0));
        Assert.True(await TipHiddenAsync(page));
        var chosen = page.Locator(Point(0, 1));
        if (keyboard) {
            await chosen.FocusAsync();
            Assert.False(await TipHiddenAsync(page));
            await PointerAsync(page, Point(0, 1));
            Assert.False(await TipHiddenAsync(page));
            await page.Keyboard.PressAsync("Space");
        } else {
            var position = await PointerAsync(page, Point(0, 1));
            await page.Mouse.ClickAsync((float)position[0], (float)position[1]);
        }
        var id = await chosen.GetAttributeAsync("data-cfx-target-id");
        var pinned = await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-pinned-target");
        Assert.Contains(id!, pinned!, StringComparison.Ordinal);
        Assert.False(await TipHiddenAsync(page));
        await Task.Delay(Delay + 60);
        Assert.Equal(pinned, await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-pinned-target"));
        Assert.Equal("6", await page.Locator(".cfx-tooltip dt").Filter(new LocatorFilterOptions { HasText = "Y" }).Locator("+ dd").InnerTextAsync());
        await CaptureDelayAsync(page, "delay-immediate-" + (keyboard ? "keyboard" : "pointer-pin"), await TraceStateAsync(page));
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task KeyboardOnlyZeroFactBypassesPointerDelay() {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(596, 338).WithTheme(ChartTheme.GraphiteDark()).AddGauge("Capacity", 0);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => ConfigureDelay(options, "exact")), 340, 620);
        await session.Page.Locator("[data-cfx-role=gauge]").FocusAsync();
        Assert.Contains("Capacity", await TooltipTextAsync(session.Page), StringComparison.Ordinal);
        Assert.Contains("0%", await TooltipTextAsync(session.Page), StringComparison.Ordinal);
        AssertNoConsoleErrors(session);
    }
}

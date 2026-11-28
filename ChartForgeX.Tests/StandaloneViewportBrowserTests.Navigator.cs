using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class StandaloneViewportBrowserTests {
    // A saved reader with no zoom toolbar and no controller: only the navigator windows the drawing.
    private static string NavigatorPage(Chart chart, bool dark = false) {
        chart.ThemeMode(dark ? ThemeMode.Dark : ThemeMode.Light);
        return chart.ToInteractiveHtmlPage(options => {
            options.Runtime = HtmlChartRuntime.Standalone;
            options.Interaction.ChartId = "saved-navigator";
            options.Interaction.Features = ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.KeyboardNavigation | ChartInteractionFeatures.Export;
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
            options.XNavigation.Enabled = true;
            options.XNavigation.RangePresets.Add(HtmlChartXRangePreset.FromDuration("1W", TimeSpan.FromDays(7)));
            options.XNavigation.RangePresets.Add(HtmlChartXRangePreset.FromDuration("1M", TimeSpan.FromDays(30)));
        });
    }

    private static async Task<JsonElement> NextWindowAsync(IPage page, Func<Task> action) {
        await page.EvaluateAsync("()=>{window.savedWindow=null;document.querySelector('.cfx-interactive-chart').addEventListener('cfxviewport',event=>{window.savedWindow=event.detail.viewport.x;},{once:true});}");
        await action();
        await page.WaitForFunctionAsync("()=>window.savedWindow!==null");
        return await page.EvaluateAsync<JsonElement>("()=>window.savedWindow");
    }

    private static Task<string?> ValueTextAsync(IPage page, string range) => page.Locator("[data-cfx-x-range=" + range + "]").GetAttributeAsync("aria-valuetext");

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SavedNavigatorPresetsHandlesScrollbarAndResetRepaintNativeTicksWithoutAHost(bool dark) {
        if (!Enabled) return;
        await using var session = await OpenAsync(NavigatorPage(TimeChart(ChartSeriesKind.Line, null, null), dark), 980, 900);
        var page = session.Page;
        var panel = page.Locator("[data-cfx-x-navigation]");
        Assert.True(await panel.IsVisibleAsync());
        Assert.Equal("true", await panel.GetAttributeAsync("data-cfx-x-standalone"));
        Assert.Equal(0, await page.Locator("[data-cfx-zoom]").CountAsync());
        Assert.False(await page.EvaluateAsync<bool>("()=>typeof ChartForgeXCharts!=='undefined'"));
        var first = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        double Day(int day) => ChartDateTime.ToOADate(first.AddDays(day));
        Assert.Equal("true", await page.Locator("[data-cfx-x-full]").GetAttributeAsync("aria-pressed"));

        // A one-week preset ends at the full-domain maximum and ticks as a native preparation of that week.
        var week = await NextWindowAsync(page, () => page.GetByRole(AriaRole.Button, new() { Name = "1W" }).ClickAsync());
        var maximum = week.GetProperty("maximum").GetDouble();
        Assert.Equal(maximum - 7, week.GetProperty("minimum").GetDouble(), 7);
        Assert.InRange(maximum, Day(89), Day(90));
        await AssertNativeTimeTicksAsync(page, ChartSeriesKind.Line, null, null, week);
        Assert.Equal("true", await page.GetByRole(AriaRole.Button, new() { Name = "1W" }).GetAttributeAsync("aria-pressed"));
        Assert.Equal("false", await page.Locator("[data-cfx-x-full]").GetAttributeAsync("aria-pressed"));
        Assert.Equal(await page.Locator("[data-cfx-x-value=start]").TextContentAsync(), await ValueTextAsync(page, "start"));
        // Bounds between native ticks read in the axis calendar, like the native bound captions.
        Assert.Matches("^\\d{4}-\\d{2}-\\d{2}( \\d{2}:\\d{2}(:\\d{2})?)?$", await ValueTextAsync(page, "start"));
        Assert.Matches("^\\d{4}-\\d{2}-\\d{2}( \\d{2}:\\d{2}(:\\d{2})?)?$", await ValueTextAsync(page, "end"));
        Assert.False(await page.Locator("[data-cfx-x-range=position]").IsDisabledAsync());
        await CaptureAsync(page, "navigator-week" + (dark ? "-dark" : ""));

        // The start handle is a slider: arrows move it by 1% of the full domain, keep focus and announce the new bound.
        var before = await ValueTextAsync(page, "start");
        await page.Locator("[data-cfx-x-range=start]").FocusAsync();
        var shorter = await NextWindowAsync(page, () => page.Keyboard.PressAsync("ArrowRight"));
        Assert.True(shorter.GetProperty("minimum").GetDouble() > week.GetProperty("minimum").GetDouble());
        Assert.Equal(maximum, shorter.GetProperty("maximum").GetDouble(), 7);
        Assert.NotEqual(before, await ValueTextAsync(page, "start"));
        Assert.Equal("start", await page.EvaluateAsync<string>("()=>document.activeElement.dataset.cfxXRange"));
        await AssertNativeTimeTicksAsync(page, ChartSeriesKind.Line, null, null, shorter);

        // The scrollbar keeps the span and Home moves the window to the start of the data.
        var span = shorter.GetProperty("maximum").GetDouble() - shorter.GetProperty("minimum").GetDouble();
        await page.Locator("[data-cfx-x-range=position]").FocusAsync();
        var start = await NextWindowAsync(page, () => page.Keyboard.PressAsync("Home"));
        Assert.Equal(span, start.GetProperty("maximum").GetDouble() - start.GetProperty("minimum").GetDouble(), 6);
        Assert.InRange(start.GetProperty("minimum").GetDouble(), Day(-1), Day(0) + 1e-9);
        await AssertNativeTimeTicksAsync(page, ChartSeriesKind.Line, null, null, start);

        // Overview grips: dragging the window's interior moves it; dragging its right edge resizes it.
        var overview = await page.Locator(".cfx-x-navigation__overview").BoundingBoxAsync() ?? throw new InvalidOperationException("No overview.");
        var window = await page.Locator("[data-cfx-x-window]").BoundingBoxAsync() ?? throw new InvalidOperationException("No overview window.");
        var middle = window.Y + window.Height / 2;
        var moved = await NextWindowAsync(page, async () => {
            await page.Mouse.MoveAsync(window.X + window.Width / 2, middle);
            Assert.Equal("move", await page.Locator(".cfx-x-navigation__overview").GetAttributeAsync("data-cfx-x-grip"));
            await page.Mouse.DownAsync(); await page.Mouse.MoveAsync((float)(window.X + window.Width / 2 + overview.Width * .25), middle, new() { Steps = 4 }); await page.Mouse.UpAsync();
        });
        Assert.Equal(span, moved.GetProperty("maximum").GetDouble() - moved.GetProperty("minimum").GetDouble(), 6);
        Assert.True(moved.GetProperty("minimum").GetDouble() > Day(15));
        window = await page.Locator("[data-cfx-x-window]").BoundingBoxAsync() ?? throw new InvalidOperationException("No overview window.");
        var wider = await NextWindowAsync(page, async () => {
            await page.Mouse.MoveAsync(window.X + window.Width - 1, middle);
            Assert.Equal("end", await page.Locator(".cfx-x-navigation__overview").GetAttributeAsync("data-cfx-x-grip"));
            await page.Mouse.DownAsync(); await page.Mouse.MoveAsync((float)(window.X + window.Width + overview.Width * .2), middle, new() { Steps = 4 }); await page.Mouse.UpAsync();
        });
        Assert.Equal(moved.GetProperty("minimum").GetDouble(), wider.GetProperty("minimum").GetDouble(), 7);
        Assert.True(wider.GetProperty("maximum").GetDouble() > moved.GetProperty("maximum").GetDouble() + 10);
        await AssertNativeTimeTicksAsync(page, ChartSeriesKind.Line, null, null, wider);

        // Tooltips and keyboard targets follow the window: the first reachable observation lies inside it.
        const string FocusedX = "()=>Number(document.activeElement.closest('[data-cfx-x]')?.dataset.cfxX)";
        await page.Locator(".cfx-stage svg [tabindex='0'][data-cfx-keyboard-component='data']").FocusAsync();
        Assert.InRange(await page.EvaluateAsync<double>(FocusedX), wider.GetProperty("minimum").GetDouble(), wider.GetProperty("maximum").GetDouble());
        await page.Keyboard.PressAsync("ArrowRight");
        Assert.InRange(await page.EvaluateAsync<double>(FocusedX), wider.GetProperty("minimum").GetDouble(), wider.GetProperty("maximum").GetDouble());
        var readout = page.Locator(".cfx-tooltip[role='status']");
        Assert.True(await readout.IsVisibleAsync());
        Assert.Contains("Samples", await readout.InnerTextAsync());

        // SVG export carries the current window's ticks.
        var painted = await page.Locator("[data-cfx-browser-axis=x] text[data-cfx-axis-value]").AllTextContentsAsync();
        await page.Locator(Root).HoverAsync();
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var exported = XDocument.Load(await download.PathAsync() ?? throw new InvalidOperationException("No SVG export."));
        var exportedTicks = exported.Descendants().Where(node => (string?)node.Attribute("data-cfx-browser-axis") == "x")
            .SelectMany(axis => axis.Descendants().Where(node => node.Name.LocalName == "text" && node.Attribute("data-cfx-axis-value") != null))
            .Select(node => node.Value).ToArray();
        Assert.Equal(painted, exportedTicks);

        // The full-range button resets to the saved drawing and disables the scrollbar.
        var full = await NextWindowAsync(page, () => page.Locator("[data-cfx-x-full]").ClickAsync());
        Assert.InRange(full.GetProperty("minimum").GetDouble(), Day(-1), Day(0) + 1e-9);
        Assert.Equal(maximum, full.GetProperty("maximum").GetDouble(), 7);
        Assert.Equal(0, await page.Locator("[data-cfx-browser-axis]").CountAsync());
        Assert.True(await page.Locator("[data-cfx-x-range=position]").IsDisabledAsync());
        Assert.Equal("true", await page.Locator("[data-cfx-x-full]").GetAttributeAsync("aria-pressed"));
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task SavedNavigatorStaysHiddenForAChartItCannotWindowAndNamesTheReason() {
        if (!Enabled) return;
        var chart = TimeChart(ChartSeriesKind.Line, null, null);
        chart.Options.XAxis.LabelFormatter = value => "Day " + Math.Round(value);
        Assert.Contains(HtmlChartXNavigationSupport.DiagnoseStandalone(chart), issue => issue.Issue == HtmlChartXNavigationIssue.UnsupportedTimeAxis);
        await using var session = await OpenAsync(NavigatorPage(chart), 980, 800);
        var panel = session.Page.Locator("[data-cfx-x-navigation]");
        Assert.Equal("UnsupportedTimeAxis", await panel.GetAttributeAsync("data-cfx-x-standalone-issues"));
        Assert.False(await panel.IsVisibleAsync());
        Assert.Null(await session.Page.Locator(Root).GetAttributeAsync("data-cfx-standalone-projection"));
        AssertNoConsoleErrors(session);
    }
}

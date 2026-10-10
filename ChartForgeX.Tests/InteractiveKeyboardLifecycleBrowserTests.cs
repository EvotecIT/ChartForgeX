using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class InteractiveKeyboardLifecycleBrowserTests {
    private const string DataStop = "[data-cfx-keyboard-component='data'][tabindex='0']";
    private const string LegendStop = "[data-cfx-keyboard-component='legend'][tabindex='0']";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MutedDataLeavesTheComponentWhileItsLegendRemainsReachableForUnmute(bool dark) {
        if (!Enabled) return;
        await using var session = await OpenAsync(StatusLines(dark).ToInteractiveHtmlPage());
        var page = session.Page;
        await page.Locator(Point(1, 2)).FocusAsync();
        await page.Locator(Legend(1)).FocusAsync();
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await page.Locator(Legend(1)).GetAttributeAsync("data-cfx-muted"));
        Assert.Equal(1, await page.Locator(DataStop).CountAsync());
        Assert.Equal(0, await page.Locator(DataStop + "[data-cfx-series='1']").CountAsync());

        // Tab entry must update immediately when the active series is muted, before an arrow refreshes the component.
        await page.Keyboard.PressAsync("Shift+Tab");
        await AssertActivePointAsync(page, 0, 0);
        await page.Keyboard.PressAsync("ArrowDown");
        await AssertActivePointAsync(page, 2, 0);
        await page.Keyboard.PressAsync("Tab");
        Assert.True(await page.Locator(Legend(1)).EvaluateAsync<bool>("node => node === document.activeElement"));
        await CaptureAsync(page, "keyboard-muted-legend-" + (dark ? "dark" : "light") + ".png");
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("false", await page.Locator(Legend(1)).GetAttributeAsync("data-cfx-muted"));
        await page.Keyboard.PressAsync("Shift+Tab");
        await page.Keyboard.PressAsync("ArrowUp");
        await AssertActivePointAsync(page, 1, 0);

        foreach (var series in new[] { 0, 1, 2 }) {
            await page.Locator(Legend(series)).FocusAsync();
            await page.Keyboard.PressAsync("Space");
        }
        Assert.Equal(0, await page.Locator(DataStop).CountAsync());
        Assert.Equal(1, await page.Locator(LegendStop).CountAsync());
        await page.Locator("[data-cfx-reset]").ClickAsync();
        Assert.Equal(0, await page.Locator(".cfx-series-muted").CountAsync());
        Assert.Equal(1, await page.Locator(DataStop).CountAsync());
        Assert.Equal(1, await page.Locator(LegendStop).CountAsync());
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false, 700, 460)]
    [InlineData(true, 340, 560)]
    public async Task RevealedHostEntersDataThenLegendAndRestoresEachComponentAfterAnotherReveal(bool dark, int width, int height) {
        if (!Enabled) return;
        var chart = StatusLines(dark);
        if (width < 400) chart.WithSize(900, 420);
        await using var session = await OpenAsync(HiddenHost(chart.ToInteractiveHtmlFragment(), dark, width < 400), width, height);
        var page = session.Page;
        Assert.Equal(0, await page.Locator(DataStop).CountAsync());
        await page.Locator("#show-chart").ClickAsync();
        await WaitForComponentStopsAsync(page, 2);

        await page.Keyboard.PressAsync("Tab");
        await AssertActivePointAsync(page, 0, 0);
        await page.Keyboard.PressAsync("End");
        await AssertActivePointAsync(page, 0, 6);
        await CaptureAsync(page, "keyboard-reveal-data-" + (dark ? "compact-dark" : "wide-light") + ".png");
        await page.Keyboard.PressAsync("Tab");
        Assert.True(await page.Locator(Legend(0)).EvaluateAsync<bool>("node => node === document.activeElement"));
        await page.Keyboard.PressAsync("ArrowRight");
        Assert.True(await page.Locator(Legend(1)).EvaluateAsync<bool>("node => node === document.activeElement"));
        await CaptureAsync(page, "keyboard-reveal-legend-" + (dark ? "compact-dark" : "wide-light") + ".png");
        await page.Keyboard.PressAsync("Tab");
        Assert.True(await page.Locator("#after-chart").EvaluateAsync<bool>("node => node === document.activeElement"));

        await page.Locator("#chart-host").EvaluateAsync("node => node.hidden = true");
        await WaitForComponentStopsAsync(page, 0);
        await page.Locator("#show-chart").ClickAsync();
        await WaitForComponentStopsAsync(page, 2);
        await page.Keyboard.PressAsync("Tab");
        await AssertActivePointAsync(page, 0, 6);
        await page.Keyboard.PressAsync("Tab");
        Assert.True(await page.Locator(Legend(1)).EvaluateAsync<bool>("node => node === document.activeElement"));
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task SynchronizedMuteRefreshesThePeerTabEntryWithoutMovingItsFocus() {
        if (!Enabled) return;
        var html = new[] { StatusLines(false), StatusLines(false) }.ToInteractiveHtmlDashboardPage(options => {
            options.Columns = 1;
            options.Interaction.Enable(ChartInteractionFeatures.SynchronizedCharts);
        });
        await using var session = await OpenAsync(html, 700, 1000);
        var page = session.Page;
        var roots = page.Locator(".cfx-interactive-chart");
        await roots.Nth(1).Locator(Point(1, 3)).FocusAsync();
        await roots.Nth(0).Locator(Legend(1)).FocusAsync();
        await page.Keyboard.PressAsync("Space");
        foreach (var index in new[] { 0, 1 }) {
            Assert.Equal("true", await roots.Nth(index).Locator(Legend(1)).GetAttributeAsync("data-cfx-muted"));
            Assert.Equal(1, await roots.Nth(index).Locator(DataStop).CountAsync());
            Assert.Equal(0, await roots.Nth(index).Locator(DataStop + "[data-cfx-series='1']").CountAsync());
        }
        Assert.True(await roots.Nth(0).Locator(Legend(1)).EvaluateAsync<bool>("node => node === document.activeElement"));
        await page.Keyboard.PressAsync("Space");
        await roots.Nth(1).Locator(Point(0, 3)).FocusAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        Assert.True(await roots.Nth(1).Locator(Point(1, 3)).EvaluateAsync<bool>("node => node === document.activeElement"));
        AssertNoConsoleErrors(session);
    }

    private static async Task AssertActivePointAsync(IPage page, int series, int point) {
        var actual = await page.EvaluateAsync<string[]>("() => { const node = document.activeElement.closest('[data-cfx-point]'); return node ? [node.dataset.cfxSeries, node.dataset.cfxPoint] : []; }");
        Assert.Equal(new[] { series.ToString(), point.ToString() }, actual);
    }

    private static Task WaitForComponentStopsAsync(IPage page, int count) => page.WaitForFunctionAsync(
        "count => document.querySelectorAll('[data-cfx-keyboard-component][tabindex=\"0\"]').length === count", count,
        new PageWaitForFunctionOptions { Timeout = 5000 });

    private static string HiddenHost(string fragment, bool dark, bool cssHidden) => "<!doctype html><html><body style=\"margin:12px;font-family:system-ui;background:" +
        (dark ? "#151b27;color:#dce4f0" : "#f5f7fa;color:#17263a") + "\">" +
        "<button id=\"show-chart\" onclick=\"const host=document.getElementById('chart-host');host.hidden=false;host.style.display=''\">Show chart</button>" +
        "<div id=\"chart-host\" " + (cssHidden ? "style=\"display:none\"" : "hidden") + ">" + fragment + "</div><button id=\"after-chart\">After chart</button></body></html>";

    private static async Task CaptureAsync(IPage page, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name) });
    }
}

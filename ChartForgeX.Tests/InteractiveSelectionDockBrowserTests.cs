using System.Text.Json;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class InteractiveSelectionDockBrowserTests {
    [Theory]
    [InlineData(false, HtmlChartResponsiveLayout.Readable, 700, 460)]
    [InlineData(true, HtmlChartResponsiveLayout.Readable, 700, 360)]
    [InlineData(false, HtmlChartResponsiveLayout.Readable, 340, 560)]
    [InlineData(true, HtmlChartResponsiveLayout.Readable, 340, 360)]
    [InlineData(false, HtmlChartResponsiveLayout.Fit, 340, 360)]
    [InlineData(true, HtmlChartResponsiveLayout.Fit, 700, 460)]
    public async Task SelectedTargetsRemainBelowTheNativeViewportWithoutResizingIt(bool dark, HtmlChartResponsiveLayout layout, int width, int height) {
        if (!Enabled) return;
        await using var session = await OpenAsync(StatusLines(dark).ToInteractiveHtmlPage(options => options.ResponsiveLayout = layout), width, height);
        var page = session.Page;
        var before = await BoxAsync(page, ".cfx-stage svg");
        Assert.False(await page.Locator("[data-cfx-compare-tray]").IsVisibleAsync());

        for (var point = 0; point < 7; point++) {
            await page.Locator(Point(1, point)).FocusAsync();
            await page.Keyboard.PressAsync("Space");
        }
        Assert.Equal("7", await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-compare-count"));
        Assert.Equal(6, await page.Locator(".cfx-compare-chip").CountAsync());
        Assert.Equal("+1", await page.Locator(".cfx-compare-tray__more").InnerTextAsync());
        var after = await BoxAsync(page, ".cfx-stage svg");
        Assert.Equal(before.Width, after.Width, 1);
        Assert.Equal(before.Height, after.Height, 1);
        await CaptureAsync(page, "selection-dock-" + (dark ? "dark" : "light") + "-" + layout + "-" + width + "x" + height);
        await AssertDockBoundsAsync(page, ".cfx-interactive-chart");

        await page.Locator("[data-cfx-compare-clear]").FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        Assert.False(await page.Locator("[data-cfx-compare-tray]").IsVisibleAsync());
        Assert.Equal(0, await page.Locator(".cfx-selected").CountAsync());
        Assert.Equal("cfx-interactive-chart", await page.EvaluateAsync<string>("() => document.activeElement.className"));
        await page.Keyboard.PressAsync("Tab");
        Assert.Equal("data", await page.EvaluateAsync<string>("() => document.activeElement.dataset.cfxKeyboardComponent"));
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompareControlsStayInsideAnEmbeddedHostAndPreserveTheFollowingContent(bool dark) {
        if (!Enabled) return;
        var fragment = StatusLines(dark).ToInteractiveHtmlFragment(options => options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit);
        var html = "<!doctype html><html><head><style>body{margin:0;background:#ececec}.report{width:280px;padding:8px}button{font:inherit}</style></head><body>"
            + "<main class='report'>" + fragment + "<button id='following-content'>Continue reading</button></main></body></html>";
        await using var session = await OpenAsync(html, 320, 560);
        var page = session.Page;
        for (var point = 0; point < 6; point++) {
            await page.Locator(Point(1, point)).FocusAsync();
            await page.Keyboard.PressAsync("Space");
        }
        await CaptureAsync(page, "selection-dock-fragment-" + (dark ? "dark" : "light"));
        await AssertDockBoundsAsync(page, ".cfx-interactive-chart");
        var dock = await BoxAsync(page, "[data-cfx-compare-tray]");
        var next = await BoxAsync(page, "#following-content");
        Assert.True(next.Y >= dock.Y + dock.Height, "The host's following content must move below the selection controls.");
        var style = await page.Locator("[data-cfx-compare-tray]").EvaluateAsync<string[]>("node => { const s = getComputedStyle(node); return [s.backgroundColor, s.color]; }");
        var expected = await page.Locator(".cfx-tooltip").EvaluateAsync<string[]>("node => { const s = getComputedStyle(node); return [s.backgroundColor, s.color]; }");
        Assert.Equal(expected, style);
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task SynchronizedDashboardDocksPreserveBothChartViewports() {
        if (!Enabled) return;
        var html = new[] { StatusLines(false), StatusLines(true) }.ToInteractiveHtmlDashboardPage(options => {
            options.Columns = 2;
            options.Interaction.Enable(ChartInteractionFeatures.SynchronizedCharts);
        });
        await using var session = await OpenAsync(html, 1100, 560);
        var page = session.Page;
        await page.Locator(".cfx-interactive-chart").Nth(0).Locator(Point(1, 2)).FocusAsync();
        await page.Keyboard.PressAsync("Space");
        for (var i = 0; i < 2; i++) {
            var root = ".cfx-interactive-chart:nth-child(" + (i + 1) + ")";
            Assert.Equal("1", await page.Locator(root).GetAttributeAsync("data-cfx-compare-count"));
            await AssertDockBoundsAsync(page, root);
            var last = page.Locator(root).Locator(Point(1, 6));
            await last.FocusAsync();
            Assert.True(await last.EvaluateAsync<bool>("node => { const stage = node.closest('.cfx-stage'); const b = stage.getBoundingClientRect(); const target = node.getBoundingClientRect(); const center = (target.left + target.right) / 2; return stage.scrollLeft > 0 && center >= b.left + stage.clientLeft && center <= b.left + stage.clientLeft + stage.clientWidth; }"), "Readable charts must scroll inside narrow dashboard columns even when the window is wide.");
        }
        await CaptureAsync(page, "selection-dock-dashboard");
        AssertNoConsoleErrors(session);
    }

    private static async Task AssertDockBoundsAsync(IPage page, string selector) {
        var root = page.Locator(selector);
        var dock = await root.Locator("[data-cfx-compare-tray]").BoundingBoxAsync() ?? throw new InvalidOperationException("No selection dock.");
        var stage = await root.Locator(".cfx-stage").BoundingBoxAsync() ?? throw new InvalidOperationException("No chart viewport.");
        Assert.True(dock.Y >= stage.Y + stage.Height, "Selection controls must not cover chart labels, marks or viewport scrollbars.");
        var box = await root.BoundingBoxAsync() ?? throw new InvalidOperationException("No chart host.");
        Assert.True(dock.X >= box.X - 1 && dock.X + dock.Width <= box.X + box.Width + 1, "The dock must remain inside its host width.");
        var controls = await root.Locator(".cfx-compare-chip,[data-cfx-compare-clear]").AllAsync();
        foreach (var control in controls) {
            var bounds = await control.BoundingBoxAsync() ?? throw new InvalidOperationException("No compare control.");
            Assert.True(bounds.X >= dock.X && bounds.X + bounds.Width <= dock.X + dock.Width + 1);
            Assert.True(bounds.Y >= dock.Y && bounds.Y + bounds.Height <= dock.Y + dock.Height + 1);
        }
        Assert.True(await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"), "Chart selection must not introduce horizontal page overflow.");
    }

    private static async Task CaptureAsync(IPage page, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".png"), FullPage = true });
        var facts = await page.EvaluateAsync<object>("() => Array.from(document.querySelectorAll('.cfx-interactive-chart')).map(root => { const box = selector => { const b = root.querySelector(selector).getBoundingClientRect(); return {x:b.x,y:b.y,width:b.width,height:b.height}; }; return {chart:box('.cfx-stage'),dock:box('[data-cfx-compare-tray]'),count:root.dataset.cfxCompareCount}; })");
        File.WriteAllText(Path.Combine(directory, name + ".json"), JsonSerializer.Serialize(facts));
    }
}

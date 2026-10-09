using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class InteractiveKeyboardMixedTargetsBrowserTests {
    private const string DataStop = "[data-cfx-keyboard-component='data'][tabindex='0']";
    private const string LegendStop = "[data-cfx-keyboard-component='legend'][tabindex='0']";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MixedCartesianAnnotationsAreReachableWithinOneDataTabStop(bool compactDark) {
        if (!Enabled) return;
        var html = MixedChart(compactDark).ToInteractiveHtmlPage()
            .Replace("<main ", "<button id='before-chart'>Before chart</button><main ", StringComparison.Ordinal)
            .Replace("</main>", "</main><button id='after-chart'>After chart</button>", StringComparison.Ordinal);
        await using var session = await OpenAsync(html, compactDark ? 390 : 700, compactDark ? 620 : 550);
        var page = session.Page;
        await page.EvaluateAsync("() => { window.mixedNavigation=[]; const root=document.querySelector('.cfx-interactive-chart'); root.addEventListener('cfxnavigate', event=>window.mixedNavigation.push(event.detail)); }");
        await page.Locator("#before-chart").FocusAsync();
        await page.Keyboard.PressAsync("Tab");
        await AssertFocusedAsync(page.Locator(Point(0, 0)));
        await page.Keyboard.PressAsync("ArrowRight");
        await page.Keyboard.PressAsync("ArrowDown");
        await AssertFocusedAsync(page.Locator(Point(1, 1)));
        await page.Keyboard.PressAsync("ArrowDown");
        await AssertFocusedAsync(page.Locator(Annotation(1)));
        Assert.Equal("annotation", await page.EvaluateAsync<string>("() => window.mixedNavigation.at(-1).target.targetKind"));
        Assert.Equal(await page.Locator(Annotation(1)).GetAttributeAsync("data-cfx-target-id"), await page.EvaluateAsync<string>("() => window.mixedNavigation.at(-1).target.targetId"));
        await page.Keyboard.PressAsync("Home");
        await page.Keyboard.PressAsync("ArrowLeft");
        await AssertFocusedAsync(page.Locator(Annotation(0)));
        await page.Keyboard.PressAsync("End");
        await page.Keyboard.PressAsync("ArrowDown");
        await AssertFocusedAsync(page.Locator(Annotation(1)));
        await page.Keyboard.PressAsync("ArrowUp");
        await AssertFocusedAsync(page.Locator(Point(1, 1)));
        await page.Keyboard.PressAsync("Home");
        await AssertFocusedAsync(page.Locator(Point(1, 0)));
        await page.Keyboard.PressAsync("ArrowDown");
        await AssertFocusedAsync(page.Locator(Annotation(0)));
        await page.Keyboard.PressAsync("ArrowRight");
        Assert.Equal(1, await page.Locator(DataStop).CountAsync());
        Assert.Equal(1, await page.Locator(LegendStop).CountAsync());

        await page.Keyboard.PressAsync("Tab");
        await AssertFocusedAsync(page.Locator(Legend(0)));
        await page.Keyboard.PressAsync("End");
        await AssertFocusedAsync(page.Locator(Legend(1)));
        await page.Keyboard.PressAsync("Tab");
        await AssertFocusedAsync(page.Locator("#after-chart"));
        await page.Keyboard.PressAsync("Shift+Tab");
        await page.Keyboard.PressAsync("Shift+Tab");
        await AssertFocusedAsync(page.Locator(Annotation(1)));
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await page.Locator(Annotation(1)).GetAttributeAsync("aria-selected"));
        Assert.Contains("Review window", await TooltipTextAsync(page));
        await CaptureAsync(page, "mixed-annotation-" + (compactDark ? "compact-dark" : "wide-light") + ".png");
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task StandaloneAnnotationsUseSequentialArrowsAndHomeEndWithinOneComponent() {
        if (!Enabled) return;
        var chart = Frame(false).AddHorizontalLine(6, "Warning threshold").AddVerticalLine(2, "Review window");
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage());
        var page = session.Page;
        Assert.Equal(1, await page.Locator(DataStop).CountAsync());
        Assert.Equal(0, await page.Locator(LegendStop).CountAsync());
        await page.Locator(DataStop).FocusAsync();
        await AssertFocusedAsync(page.Locator(Annotation(0)));
        await page.Keyboard.PressAsync("ArrowDown");
        await AssertFocusedAsync(page.Locator(Annotation(1)));
        await page.Keyboard.PressAsync("ArrowUp");
        await AssertFocusedAsync(page.Locator(Annotation(0)));
        await page.Keyboard.PressAsync("End");
        await page.Keyboard.PressAsync("ArrowRight");
        await AssertFocusedAsync(page.Locator(Annotation(1)));
        await page.Keyboard.PressAsync("Home");
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await page.Locator(Annotation(0)).GetAttributeAsync("aria-selected"));
        await CaptureAsync(page, "standalone-annotations.png");
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task MutingCartesianDataRecoversToAnnotationsAndUnmuteRestoresCrossGroupTraversal() {
        if (!Enabled) return;
        await using var session = await OpenAsync(MixedChart(true).ToInteractiveHtmlPage());
        var page = session.Page;
        await page.Locator(Point(1, 1)).FocusAsync();
        await ActivateLegendAsync(page, 0);
        await AssertFocusedAsync(page.Locator(Point(1, 1)));
        await ActivateLegendAsync(page, 1);
        await AssertFocusedAsync(page.Locator(Annotation(0)));
        Assert.Equal(1, await page.Locator(DataStop).CountAsync());
        await page.Keyboard.PressAsync("ArrowDown");
        await AssertFocusedAsync(page.Locator(Annotation(1)));
        await page.Keyboard.PressAsync("Home");
        await AssertFocusedAsync(page.Locator(Annotation(0)));
        await page.Keyboard.PressAsync("End");
        await CaptureAsync(page, "muted-data-annotations.png");
        await page.Keyboard.PressAsync("Tab");
        await AssertFocusedAsync(page.Locator(Legend(0)));
        await page.Keyboard.PressAsync("Space");
        await page.Keyboard.PressAsync("Shift+Tab");
        await AssertFocusedAsync(page.Locator(Annotation(1)));
        await page.Keyboard.PressAsync("ArrowUp");
        await AssertFocusedAsync(page.Locator(Point(0, 1)));
        Assert.Equal(1, await page.Locator(DataStop).CountAsync());
        Assert.Equal(1, await page.Locator(LegendStop).CountAsync());
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task SingleCartesianSeriesKeepsVerticalArrowsWithinItsCurrentObservation() {
        if (!Enabled) return;
        await using var session = await OpenAsync(Frame(false).AddLine("Actual", ChartPoints.FromValues(3, 5, 7)).ToInteractiveHtmlPage());
        var page = session.Page;
        await page.Locator(Point(0, 1)).FocusAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("ArrowUp");
        await AssertFocusedAsync(page.Locator(Point(0, 1)));
        await page.Keyboard.PressAsync("ArrowRight");
        await AssertFocusedAsync(page.Locator(Point(0, 2)));
        AssertNoConsoleErrors(session);
    }

    private static Chart MixedChart(bool dark) => Frame(dark)
        .AddLine("Actual", ChartPoints.FromValues(3, 5, 7))
        .AddLine("Target", ChartPoints.FromValues(4, 6, 8))
        .AddHorizontalLine(6, "Warning threshold").AddVerticalLine(2, "Review window");

    private static Chart Frame(bool dark) => Chart.Create().WithSize(596, 420).WithTitle("Observed values with reference guides")
        .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
        .WithXLabels("A", "B", "C").WithXAxisBounds(1, 3).WithYAxisBounds(0, 10);

    private static string Annotation(int index) => "[data-cfx-role='annotation'][data-cfx-source-id='annotation-" + index + "']";
    private static async Task AssertFocusedAsync(ILocator target) => Assert.True(await target.EvaluateAsync<bool>("node => node === document.activeElement"));
    private static Task ActivateLegendAsync(IPage page, int series) => page.Locator(Legend(series)).EvaluateAsync("node => node.dispatchEvent(new MouseEvent('click', { bubbles:true }))");

    private static async Task CaptureAsync(IPage page, string name) {
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(capture)) return;
        Directory.CreateDirectory(capture);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(capture, "keyboard-" + name) });
    }
}

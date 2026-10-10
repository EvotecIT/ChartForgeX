using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class InteractiveKeyboardNavigationBrowserTests {
    private const string DataStop = "[data-cfx-keyboard-component='data'][tabindex='0']";
    private const string LegendStop = "[data-cfx-keyboard-component='legend'][tabindex='0']";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RovingComponentsKeepDataSeriesAndLegendNavigationSeparate(bool dark) {
        if (!Enabled) return;
        await using var session = await OpenAsync(WithHostControls(StatusLines(dark).ToInteractiveHtmlPage()));
        var page = session.Page;
        Assert.Equal(1, await page.Locator(DataStop).CountAsync());
        Assert.Equal(1, await page.Locator(LegendStop).CountAsync());
        Assert.Equal(0, await page.Locator("[data-cfx-role='series'][tabindex='0']").CountAsync());

        await page.Locator("#before-chart").FocusAsync();
        await page.Keyboard.PressAsync("Tab");
        await AssertActivePointAsync(page, 0, 0);
        await page.Keyboard.PressAsync("ArrowRight");
        await AssertActivePointAsync(page, 0, 1);
        await page.Keyboard.PressAsync("ArrowDown");
        await AssertActivePointAsync(page, 1, 1);
        await page.Keyboard.PressAsync("ArrowUp");
        await AssertActivePointAsync(page, 0, 1);
        await page.Keyboard.PressAsync("Home");
        await page.Keyboard.PressAsync("ArrowLeft");
        await AssertActivePointAsync(page, 0, 0);
        await page.Keyboard.PressAsync("End");
        await page.Keyboard.PressAsync("ArrowRight");
        await AssertActivePointAsync(page, 0, 6);
        await page.Keyboard.PressAsync("Tab");
        Assert.True(await page.Locator(Legend(0)).EvaluateAsync<bool>("node => node === document.activeElement"));
        await page.Keyboard.PressAsync("ArrowRight");
        Assert.True(await page.Locator(Legend(1)).EvaluateAsync<bool>("node => node === document.activeElement"));
        await page.Keyboard.PressAsync("End");
        Assert.True(await page.Locator(Legend(2)).EvaluateAsync<bool>("node => node === document.activeElement"));
        await CaptureAsync(page, "keyboard-components-" + (dark ? "dark" : "light") + ".png");

        await page.Keyboard.PressAsync("Tab");
        Assert.True(await page.Locator("#after-chart").EvaluateAsync<bool>("node => node === document.activeElement"));
        await page.Keyboard.PressAsync("Shift+Tab");
        Assert.True(await page.Locator(Legend(2)).EvaluateAsync<bool>("node => node === document.activeElement"));
        await page.Keyboard.PressAsync("Shift+Tab");
        await AssertActivePointAsync(page, 0, 6);
        await page.Keyboard.PressAsync("Shift+Tab");
        Assert.True(await page.Locator("#before-chart").EvaluateAsync<bool>("node => node === document.activeElement"));

        await page.Locator(Legend(1)).FocusAsync();
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await page.Locator(Legend(1)).GetAttributeAsync("data-cfx-muted"));
        await page.Keyboard.PressAsync("Shift+Space");
        Assert.Equal("true", await page.Locator(Legend(1)).GetAttributeAsync("data-cfx-isolated"));
        var chosenSeries = await page.Locator(Legend(1)).GetAttributeAsync("data-cfx-series");
        var chosenKey = await page.Locator(Legend(1)).GetAttributeAsync("data-cfx-series-key");
        Assert.Equal(chosenSeries + ":series:" + chosenKey, await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-isolated-series"));
        await page.Keyboard.PressAsync("i");
        Assert.Null(await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-isolated-series"));
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(ChartInteractionFeatures.None)]
    [InlineData(ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.Selection)]
    public async Task DisabledKeyboardNavigationRetainsNativeLinkAndHostKeyBehavior(ChartInteractionFeatures features) {
        if (!Enabled) return;
        var html = LinkedHeatmap().WithLegend(false).ToInteractiveHtmlPage(options => options.Interaction.Features = features);
        await using var session = await OpenAsync(WithHostControls(html));
        var page = session.Page;
        Assert.Equal(0, await page.Locator("[data-cfx-keyboard-component]").CountAsync());
        Assert.Equal(0, await page.Locator(".cfx-stage [tabindex]:not(a[href])").CountAsync());
        Assert.Equal("0", await page.Locator("a[href='#evidence']").GetAttributeAsync("tabindex"));
        await page.EvaluateAsync("() => { window.hostKeys = []; document.addEventListener('keydown', event => window.hostKeys.push({ key: event.key, prevented: event.defaultPrevented })); }");
        await page.Locator("#before-chart").FocusAsync();
        await page.Keyboard.PressAsync("Tab");
        Assert.True(await page.Locator("a[href='#evidence']").EvaluateAsync<bool>("node => node === document.activeElement"));
        await page.Keyboard.PressAsync("ArrowRight");
        Assert.True(await page.EvaluateAsync<bool>("() => window.hostKeys.some(item => item.key === 'ArrowRight' && !item.prevented)"));
        await page.Keyboard.PressAsync("Space");
        Assert.Equal(0, await page.Locator(".cfx-selected").CountAsync());
        await page.Keyboard.PressAsync("Enter");
        await page.WaitForFunctionAsync("() => location.hash === '#evidence'");
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task LinkedMatrixCellsRoveAcrossRowsAndPreserveNativeEnterNavigation() {
        if (!Enabled) return;
        await using var session = await OpenAsync(WithHostControls(LinkedHeatmap().ToInteractiveHtmlPage()));
        var page = session.Page;
        await page.Locator("#before-chart").FocusAsync();
        await page.Keyboard.PressAsync("Tab");
        await AssertActivePointAsync(page, 0, 0);
        await page.Keyboard.PressAsync("ArrowRight");
        var cell = page.Locator("[data-cfx-role='heatmap-cell'][data-cfx-series='0'][data-cfx-point='1']");
        Assert.True(await cell.Locator("a[href]").EvaluateAsync<bool>("node => node === document.activeElement"));
        Assert.Equal(1, await cell.Locator("a[href], [tabindex='0']").CountAsync());
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await cell.GetAttributeAsync("aria-selected"));
        Assert.Equal("", await page.EvaluateAsync<string>("() => location.hash"));
        await page.Keyboard.PressAsync("ArrowDown");
        await AssertActivePointAsync(page, 1, 1);
        await page.Keyboard.PressAsync("ArrowUp");
        await page.Keyboard.PressAsync("Enter");
        await page.WaitForFunctionAsync("() => location.hash === '#evidence'");
        await CaptureAsync(page, "keyboard-linked-matrix.png");
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompactNavigationRevealsTheFocusedPointInsideTheLocalStage(bool dark) {
        if (!Enabled) return;
        await using var session = await OpenAsync(StatusLines(dark).WithSize(900, 420).ToInteractiveHtmlPage(), 340, 560);
        var page = session.Page;
        await page.Locator(DataStop).FocusAsync();
        var pageScroll = await page.EvaluateAsync<double>("() => window.scrollY");
        await page.Keyboard.PressAsync("End");
        await AssertActivePointAsync(page, 0, 6);
        var visible = await page.EvaluateAsync<bool>("() => { const stage = document.querySelector('.cfx-stage'); const box = stage.getBoundingClientRect(); const target = document.activeElement.getBoundingClientRect(); const x = target.left + target.width / 2; return stage.scrollLeft > 0 && x >= box.left + stage.clientLeft && x <= box.left + stage.clientLeft + stage.clientWidth; }");
        Assert.True(visible);
        Assert.Equal(pageScroll, await page.EvaluateAsync<double>("() => window.scrollY"));
        Assert.NotEqual("", await TooltipTextAsync(page));
        Assert.True(await page.EvaluateAsync<bool>("() => { const box = document.querySelector('.cfx-tooltip').getBoundingClientRect(); return box.left >= 0 && box.right <= innerWidth && box.top >= 0 && box.bottom <= innerHeight; }"));
        await CaptureAsync(page, "keyboard-compact-" + (dark ? "dark" : "light") + ".png");
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task HiddenSeriesAreSkippedWithoutMakingAggregateGroupsIntoDataItems() {
        if (!Enabled) return;
        await using var session = await OpenAsync(StatusLines(false).ToInteractiveHtmlPage());
        var page = session.Page;
        await page.Locator(Point(0, 2)).FocusAsync();
        await page.AddStyleTagAsync(new PageAddStyleTagOptions { Content = "[data-cfx-role='series'][data-cfx-series='1'] { display:none; }" });
        await page.Keyboard.PressAsync("ArrowDown");
        await AssertActivePointAsync(page, 2, 2);
        Assert.Equal(0, await page.Locator("[data-cfx-role='series'][tabindex='0']").CountAsync());
        Assert.Equal(0, await page.Locator("[data-cfx-series='1'][tabindex='0']").CountAsync());
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task SeriesNavigationMatchesSharedXAndClampsShorterSeries() {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(596, 338).WithTheme(ChartTheme.GraphiteLight())
            .AddLine("Sparse", new[] { new ChartPoint(1, 10), new ChartPoint(3, 30), new ChartPoint(7, 40) })
            .AddLine("Dense", new[] { new ChartPoint(1, 20), new ChartPoint(2, 22), new ChartPoint(3, 25), new ChartPoint(7, 50) })
            .AddLine("Short", new[] { new ChartPoint(9, 40), new ChartPoint(10, 50) });
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage());
        var page = session.Page;
        await page.Locator(Point(0, 1)).FocusAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await AssertActivePointAsync(page, 1, 2);
        await page.Keyboard.PressAsync("ArrowDown");
        await AssertActivePointAsync(page, 2, 1);
        await page.Keyboard.PressAsync("ArrowUp");
        await AssertActivePointAsync(page, 1, 1);
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData("pie")]
    [InlineData("map")]
    [InlineData("sankey")]
    public async Task NonCartesianTargetsHaveOneDataComponentAndDeterministicTraversal(string family) {
        if (!Enabled) return;
        var chart = NonCartesian(family).WithSize(596, 338).WithTheme(ChartTheme.GraphiteLight());
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage());
        var page = session.Page;
        var ids = await page.EvaluateAsync<string[]>("() => Array.from(document.querySelectorAll('[data-cfx-keyboard-component=\"data\"]')).map(node => node.dataset.cfxTargetId)");
        Assert.True(ids.Length > 1);
        Assert.Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(1, await page.Locator(DataStop).CountAsync());
        await page.Locator(DataStop).FocusAsync();
        Assert.Equal(ids[0], await ActiveTargetIdAsync(page));
        await page.Keyboard.PressAsync("ArrowDown");
        Assert.Equal(ids[1], await ActiveTargetIdAsync(page));
        await page.Keyboard.PressAsync("End");
        Assert.Equal(ids[^1], await ActiveTargetIdAsync(page));
        await page.Keyboard.PressAsync("ArrowRight");
        Assert.Equal(ids[^1], await ActiveTargetIdAsync(page));
        await CaptureAsync(page, "keyboard-" + family + ".png");
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task DecimatedNavigationKeepsSourceIdentityAndSynchronizesSelectionWithoutMovingPeerFocus() {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(596, 338).WithLineMarkers(ChartLineMarkerMode.None)
            .AddDecimatedLine("Latency", Enumerable.Range(0, 100).Select(index => new ChartPoint(index, Math.Sin(index / 4d))), 12);
        chart.Series[0].WithInteractionKey("latency-source");
        var html = new[] { chart, chart }.ToInteractiveHtmlDashboardPage(options => {
            options.Columns = 1;
            options.Interaction.Enable(ChartInteractionFeatures.SynchronizedCharts);
        });
        await using var session = await OpenAsync(html, 700, 1000);
        var page = session.Page;
        var roots = page.Locator(".cfx-interactive-chart");
        await roots.Nth(0).EvaluateAsync("root => { window.navigation = []; root.addEventListener('cfxnavigate', event => window.navigation.push(event.detail)); }");
        await roots.Nth(0).Locator(Point(0, 2)).FocusAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        Assert.Equal(1, await page.EvaluateAsync<int>("() => window.navigation.length"));
        Assert.Equal(chart.Series[0].SourcePointIndices[3].ToString(), await page.EvaluateAsync<string>("() => window.navigation[0].target.sourcePoint"));
        Assert.Equal("latency-source:" + chart.Series[0].SourcePointIndices[3], await page.EvaluateAsync<string>("() => window.navigation[0].target.targetId"));
        Assert.True(await roots.Nth(0).EvaluateAsync<bool>("root => root.contains(document.activeElement)"));
        Assert.Contains("cfx-hovered", await roots.Nth(1).Locator(Point(0, 3)).GetAttributeAsync("class") ?? "", StringComparison.Ordinal);
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await roots.Nth(0).Locator(Point(0, 3)).GetAttributeAsync("aria-selected"));
        Assert.Equal("true", await roots.Nth(1).Locator(Point(0, 3)).GetAttributeAsync("aria-selected"));
        AssertNoConsoleErrors(session);
    }

    private static Task AssertActivePointAsync(IPage page, int series, int point) => AssertActiveAsync(page, new[] { series.ToString(), point.ToString() });

    private static async Task AssertActiveAsync(IPage page, string[] expected) {
        var actual = await page.EvaluateAsync<string[]>("() => { const node = document.activeElement.closest('[data-cfx-point]'); return node ? [node.dataset.cfxSeries, node.dataset.cfxPoint] : []; }");
        Assert.Equal(expected, actual);
    }

    private static Task<string> ActiveTargetIdAsync(IPage page) => page.EvaluateAsync<string>("() => document.activeElement.dataset.cfxTargetId || ''");

    private static string WithHostControls(string html) => html.Replace("<main ", "<button id=\"before-chart\">Before chart</button><main ", StringComparison.Ordinal)
        .Replace("</main>", "</main><button id=\"after-chart\">After chart</button>", StringComparison.Ordinal);

    private static Chart LinkedHeatmap() => Chart.Create().WithSize(596, 338).WithTheme(ChartTheme.GraphiteLight())
        .WithXLabels("Directory", "API", "Mail")
        .WithStateCategories(new ChartStateCategory("pass", "Passed", ChartColor.FromHex("#1d8a52")))
        .AddHeatmapCategoryRow("First", new[] { new ChartHeatmapCell("pass", "1"), new ChartHeatmapCell("pass", "2", href: "#evidence"), new ChartHeatmapCell("pass", "3") })
        .AddHeatmapCategoryRow("Second", new[] { new ChartHeatmapCell("pass", "4"), new ChartHeatmapCell("pass", "5"), new ChartHeatmapCell("pass", "6") });

    private static Chart NonCartesian(string family) => family switch {
        "pie" => Chart.Create().WithXLabels("First", "Second", "Third").AddPie("Share", ChartPoints.FromValues(5, 3, 2)),
        "sankey" => Chart.Create().AddSankey("Flow", new[] { new ChartNode("Input", "Input"), new ChartNode("API", "API"), new ChartNode("Mail", "Mail") }, new[] { new ChartFlowLink("input-api", "Input", "API", 4), new ChartFlowLink("input-mail", "Input", "Mail", 2) }),
        "map" => Chart.Create().WithMapLabels(false).WithMapScaleLegend(false).AddRegionMap("Coverage", new ChartMapDefinition("two", "Two regions", 100, 100, new[] {
            new ChartMapRegion("A", "First", "M0 0H50V100H0Z"), new ChartMapRegion("B", "Second", "M50 0H100V100H50Z")
        }), new[] { new ChartRegionMapItem("A", 10) }),
        _ => throw new ArgumentOutOfRangeException(nameof(family))
    };

    private static async Task CaptureAsync(IPage page, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name) });
    }
}

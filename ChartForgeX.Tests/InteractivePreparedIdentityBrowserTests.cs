using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class InteractivePreparedIdentityBrowserTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LinkedHeatmapCellHasOneKeyboardTargetAndRetainsNativeEnterNavigation(bool dark) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(596, 338).WithTitle("Service evidence")
            .WithTheme(dark ? ChartForgeX.Themes.ChartTheme.GraphiteDark() : ChartForgeX.Themes.ChartTheme.GraphiteLight())
            .WithXLabels("Directory")
            .WithStateCategories(new ChartStateCategory("pass", "Passed", ChartColor.FromHex("#1d8a52")))
            .AddHeatmapCategoryRow("DC01", new ChartHeatmapCell("pass", "3", href: "#evidence", tooltip: "Open evidence"));
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage());
        var page = session.Page;
        var cell = page.Locator("[data-cfx-role=\"heatmap-cell\"]");
        var link = cell.Locator("a[data-cfx-role=\"heatmap-cell-link\"]");
        Assert.Null(await cell.GetAttributeAsync("tabindex"));
        Assert.Equal(1, await cell.Locator("a[href], [tabindex='0']").CountAsync());
        await link.FocusAsync();
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await cell.GetAttributeAsync("aria-selected"));
        Assert.Equal("", await page.EvaluateAsync<string>("() => location.hash"));
        await page.Keyboard.PressAsync("Enter");
        await page.WaitForFunctionAsync("() => location.hash === '#evidence'");
        Assert.Equal("#evidence", await page.EvaluateAsync<string>("() => location.hash"));
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            await page.ScreenshotAsync(new Microsoft.Playwright.PageScreenshotOptions {
                Path = Path.Combine(capture, "heatmap-link-keyboard-" + (dark ? "dark" : "light") + ".png")
            });
        }
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task MarkerFreeDecimatedObservationEmitsOneOriginalSourceSelectionAndSupportsKeyboard() {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(596, 338).WithLineMarkers(ChartLineMarkerMode.None)
            .AddDecimatedLine("Latency", Enumerable.Range(0, 100).Select(index => new ChartPoint(index, Math.Sin(index / 4d))), 12);
        chart.Series[0].WithInteractionKey("latency-source");
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage());
        var page = session.Page;
        await page.EvaluateAsync("() => { window.cfxSelections = []; document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.cfxSelections.push(event.detail)); }");
        var point = page.Locator(Point(0, 3));
        Assert.Equal("latency-source:" + chart.Series[0].SourcePointIndices[3], await point.GetAttributeAsync("data-cfx-target-id"));
        Assert.Equal(1, await point.Locator("[data-cfx-browser-hit-area]").CountAsync());
        await point.ClickAsync();
        Assert.Equal("true", await point.GetAttributeAsync("aria-selected"));
        Assert.Equal(1, await page.EvaluateAsync<int>("() => window.cfxSelections.length"));
        Assert.Equal("point", await page.EvaluateAsync<string>("() => window.cfxSelections[0].target.targetKind"));
        Assert.Equal(chart.Series[0].SourcePointIndices[3].ToString(), await page.EvaluateAsync<string>("() => window.cfxSelections[0].target.sourcePoint"));
        await point.FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("false", await point.GetAttributeAsync("aria-selected"));
        Assert.Equal(2, await page.EvaluateAsync<int>("() => window.cfxSelections.length"));
        AssertNoConsoleErrors(session);
    }
}

using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>The browser consumes the native curve/ribbon and retains the two derived interaction handles.</summary>
public sealed class InteractiveTrendLineModelsBrowserTests {
    [BrowserTheory]
    [Trait("BrowserSuite", "Compatibility")]
    [InlineData(360, false)]
    [InlineData(1000, true)]
    public async System.Threading.Tasks.Task NativePolynomialIntervalRetainsItsPathAndDerivedTargetsInTheHtmlAdapter(int width, bool dark) {
        if (!Enabled) return;
        var series = ChartSeries.CreateTrendLine(new ChartSeriesId("fitted-source"), "Native polynomial fit",
            new[] { new ChartPoint(-2, 10), new ChartPoint(-1, -2), new ChartPoint(0, 7), new ChartPoint(1, 2), new ChartPoint(2, 18) })
            .ConfigureTrendLine(options => { options.Model = ChartTrendLineModel.Polynomial; options.Interval = ChartTrendLineIntervalKind.Prediction; });
        var chart = Chart.Create().WithSize(900, 500).WithLegend(false).WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light).AddSeries(series);
        var svg = System.Xml.Linq.XDocument.Parse(chart.ToSvg());
        var curve = svg.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "trend-line");
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), width, 650);
        var page = session.Page;
        Assert.Equal((string?)curve.Attribute("d"), await page.Locator("[data-cfx-role='trend-line']").First.GetAttributeAsync("d"));
        Assert.Equal(1, await page.Locator("[data-cfx-role='trend-line-interval']").CountAsync());
        Assert.Equal(2, await page.Locator(PointTarget).CountAsync());
        Assert.Equal("5", await page.Locator("[data-cfx-role='regression']").GetAttributeAsync("data-cfx-source-count"));
        Assert.Equal("polynomial", await page.Locator("[data-cfx-role='regression']").GetAttributeAsync("data-cfx-regression-model"));
        var first = page.Locator(Point(0, 0));
        await FocusWithKeyboardAsync(page, first); await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await first.GetAttributeAsync("aria-selected"));
        Assert.Equal("fitted-source:derived:regression-endpoint:0", await first.GetAttributeAsync("data-cfx-target-id"));
        Assert.Null(await first.GetAttributeAsync("data-cfx-source-point"));
        await CaptureAsync(page, "native-trend-interval-" + width + "-" + (dark ? "dark" : "light"));
        AssertNoConsoleErrors(session);
    }

    private static async System.Threading.Tasks.Task CaptureAsync(Microsoft.Playwright.IPage page, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await page.ScreenshotAsync(new Microsoft.Playwright.PageScreenshotOptions { Path = Path.Combine(directory, name + ".png"), FullPage = true });
        await File.WriteAllTextAsync(Path.Combine(directory, name + ".svg"), await page.Locator(".cfx-stage svg").First.EvaluateAsync<string>("node => node.outerHTML"));
    }
}

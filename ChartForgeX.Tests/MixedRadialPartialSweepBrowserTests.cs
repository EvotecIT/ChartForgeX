using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class MixedRadialPartialSweepBrowserTests {
    [BrowserTheory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public async Task PartialSweepNativeDrawingsKeepReferencePointSelectionAcrossResponsiveReplacements(bool dark, bool narrow) {
        if (!Enabled) return;
        var chart = MixedRadialPartialSweepTests.Create(270, 800, dark).WithTitle("Regional counts and references").WithHeader(true).WithLegend().WithAxes().WithGrid();
        chart.Options.XAxis.LabelOrientation = ChartAxisLabelOrientation.Parallel;
        var html = chart.ToInteractiveHtmlPage(options => { options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive; options.Adaptive.NarrowWidth = 360; });
        await using var session = await OpenAsync(html, narrow ? 390 : 1100, 760); var page = session.Page;
        await page.WaitForFunctionAsync("drawing=>document.querySelector('.cfx-interactive-chart').dataset.cfxAdaptiveDrawing===drawing", narrow ? "narrow" : "wide");
        const string selector = "[data-cfx-role=radar-point-source][data-cfx-source-id='series-1-point-2']";
        await page.Locator(selector).FocusAsync(); await page.Keyboard.PressAsync("Enter");
        Assert.Equal("true", await page.Locator(selector).GetAttributeAsync("aria-selected"));
        Assert.Equal("reference", await page.Locator(selector).GetAttributeAsync("data-cfx-series-key"));
        Assert.Equal(6, await page.Locator("[data-cfx-role=radar-point-source]").CountAsync());
        await page.SetViewportSizeAsync(narrow ? 1100 : 390, 760);
        await page.WaitForFunctionAsync("drawing=>document.querySelector('.cfx-interactive-chart').dataset.cfxAdaptiveDrawing===drawing", narrow ? "wide" : "narrow");
        Assert.Equal("true", await page.Locator(selector).GetAttributeAsync("aria-selected"));
        Assert.Equal(6, await page.Locator("[data-cfx-role=radial-column]").CountAsync());
        Assert.Equal(6, await page.Locator("[data-cfx-role=radar-point-source]").CountAsync());
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory); await page.ScreenshotAsync(new PageScreenshotOptions {
                Path = Path.Combine(directory, "mixed-partial-responsive-" + dark + "-" + narrow + ".png"), FullPage = true });
        }
        AssertNoConsoleErrors(session);
    }
}

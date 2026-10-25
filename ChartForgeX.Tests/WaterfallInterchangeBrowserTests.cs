using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class WaterfallInterchangeBrowserTests {
    [BrowserTheory]
    [InlineData(false, 390, false)] [InlineData(false, 900, true)]
    [InlineData(true, 390, true)] [InlineData(true, 900, false)]
    public async Task ImportedEditedCheckpointKeepsAuthoredIdentityReadoutAndSelectionAfterResize(bool horizontal, int width, bool dark) {
        if (!Enabled) return;
        var source = WaterfallInterchangeTests.Model(true, horizontal, dark, 800);
        source.Series[0].WithObservationId(2, new ChartObservationId("stage-checkpoint"));
        var chart = VisualArtifactInterchangeEnvelope.FromUtf8Json(source.ToVisualArtifact().ToInterchangeUtf8Json()).ToWaterfallChart();
        chart.Series[0].UpdateWaterfallItem(1, ChartWaterfallItem.Delta(2, -40)); chart.WithLegend(false).WithDataLabels(false);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit), width, 620);
        var page = session.Page;
        const string selector = "[data-cfx-waterfall-kind='subtotal'][data-cfx-point='2']:not([data-cfx-role='legend-item'])";
        var checkpoint = page.Locator(selector);
        Assert.Equal("stage-checkpoint", await checkpoint.GetAttributeAsync("data-cfx-observation-id"));
        Assert.Equal("80", await checkpoint.GetAttributeAsync("data-cfx-end"));
        Assert.Equal("-40", await checkpoint.GetAttributeAsync("data-cfx-value"));
        Assert.Equal("1", await checkpoint.GetAttributeAsync("data-cfx-source-points"));
        Assert.Null(await checkpoint.GetAttributeAsync("data-cfx-source-point"));
        await MoveToAsync(page, selector + " [data-cfx-role='waterfall-bar']");
        Assert.Contains("Stage sum", await TooltipTextAsync(page));
        await checkpoint.FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await checkpoint.GetAttributeAsync("aria-selected"));
        await page.SetViewportSizeAsync(width < 500 ? 900 : 390, 620);
        Assert.Equal("stage-checkpoint", await checkpoint.GetAttributeAsync("data-cfx-observation-id"));
        Assert.Equal("true", await checkpoint.GetAttributeAsync("aria-selected"));
        Assert.Equal("80", await checkpoint.GetAttributeAsync("data-cfx-end"));
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(capture, "waterfall-portable-" + horizontal + "-" + width + "-" + dark + ".png") });
        }
        AssertNoConsoleErrors(session);
    }
}

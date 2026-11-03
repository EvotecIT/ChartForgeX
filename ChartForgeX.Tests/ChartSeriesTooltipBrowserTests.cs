using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Exercises captured core tooltip text through both maintained browser adapters.</summary>
public sealed class ChartSeriesTooltipBrowserTests {
    private const string Caption = "<img src=x onerror='window.__tooltipInjected=1'> & formatted";

    [BrowserFact]
    public async Task SavedAndEnhancedTooltipsUseCapturedPlainTextSuppressEmptyAndAllowHostOverrides() {
        if (!Enabled) return;
        foreach (var mode in new[] { HtmlChartTooltipMode.Single, HtmlChartTooltipMode.SharedX }) {
            var file = Path.Combine(Path.GetTempPath(), "cfx-series-tooltip-" + Guid.NewGuid().ToString("N") + ".html");
            string html;
            try {
                Model().SaveInteractiveHtml(file, options => {
                    options.Tooltip.Mode = mode; options.Tooltip.PinOnSelect = false;
                });
                html = await File.ReadAllTextAsync(file);
            } finally {
                if (File.Exists(file)) File.Delete(file);
            }
            await using var session = await OpenAsync(html, 1000, 650);
            var page = session.Page; var tip = page.Locator(".cfx-tooltip");
            await MoveToAsync(page, Point(0, 0));
            Assert.True(await tip.IsVisibleAsync());
            Assert.Contains(Caption, await tip.InnerTextAsync());
            Assert.Equal(0, await tip.Locator("img").CountAsync());
            Assert.False(await page.EvaluateAsync<bool>("() => !!window.__tooltipInjected"));
            Assert.Equal("11", await page.Locator(Point(0, 0)).GetAttributeAsync("data-cfx-y"));
            if (mode == HtmlChartTooltipMode.SharedX) {
                Assert.Equal(2, await tip.Locator("dt[data-cfx-tooltip-series]").CountAsync());
                Assert.Equal("22", await tip.Locator("dt[data-cfx-tooltip-series='1'] + dd").InnerTextAsync());
                Assert.Equal(0, await tip.Locator("dt[data-cfx-tooltip-series='2']").CountAsync());
            }
            await MoveToAsync(page, Point(0, 1));
            // An empty authored tooltip hides the readout; the shared chrome fades it out before display ends.
            await tip.WaitForAsync(new Microsoft.Playwright.LocatorWaitForOptions { State = Microsoft.Playwright.WaitForSelectorState.Hidden, Timeout = 2000 });
            Assert.Equal("", await tip.GetAttributeAsync("hidden"));
            await MoveAwayAsync(page);
            await FocusWithKeyboardAsync(page, page.Locator(Point(0, 0)));
            Assert.Contains(Caption, await tip.InnerTextAsync());
            AssertNoConsoleErrors(session);
        }

        var chart = Model();
        var svg = chart.Prepare(VisualExportRequest.ForChart(chart).Context)
            .ToSvg(new VisualSvgOptions("series-formatting").WithEnhanceHooks());
        var enhancedHtml = "<!doctype html><html><head><meta charset='utf-8'></head><body>" + svg
            + HtmlEnhanceAssets.InlineMarkup() + "</body></html>";
        await using var enhanced = await OpenAsync(enhancedHtml, 1000, 650);
        var enhancedPage = enhanced.Page; var enhancedTip = enhancedPage.Locator("#cfx-e-tip");
        await MoveToAsync(enhancedPage, EnhancedPoint(0, 0));
        Assert.Contains(Caption, await enhancedTip.InnerTextAsync());
        Assert.Equal(0, await enhancedTip.Locator("img").CountAsync());
        Assert.False(await enhancedPage.EvaluateAsync<bool>("() => !!window.__tooltipInjected"));
        await MoveToAsync(enhancedPage, EnhancedPoint(0, 1));
        await enhancedTip.WaitForAsync(new Microsoft.Playwright.LocatorWaitForOptions { State = Microsoft.Playwright.WaitForSelectorState.Hidden, Timeout = 2000 });

        await enhancedPage.EvaluateAsync("""
            () => CfxEnhance.tooltip(() => true, (marks, content) => ({
              head: '', hint: '', rows: [{ value: 'Host <override>', name: '' }]
            }))
            """);
        await MoveAwayAsync(enhancedPage);
        await MoveToAsync(enhancedPage, EnhancedPoint(0, 0));
        Assert.Contains("Host <override>", await enhancedTip.InnerTextAsync());
        Assert.DoesNotContain(Caption, await enhancedTip.InnerTextAsync());
        await MoveToAsync(enhancedPage, EnhancedPoint(0, 1));
        Assert.Contains("Host <override>", await enhancedTip.InnerTextAsync());
        Assert.True(await enhancedTip.IsVisibleAsync());
        Assert.Equal("12", await enhancedPage.Locator(EnhancedPoint(0, 1)).GetAttributeAsync("data-cfx-y"));
        AssertNoConsoleErrors(enhanced);
    }

    private static string EnhancedPoint(int series, int point) =>
        "[data-cfx-mark][data-cfx-series='" + series + "'][data-cfx-point='" + point + "']";

    private static Chart Model() => Chart.Create().WithSize(900, 500).WithLegend(false)
        .AddSeries(new ChartSeries(new ChartSeriesId("custom"), "Custom", ChartSeriesKind.Bar,
            new[] { new ChartPoint(1, 11), new ChartPoint(2, 12) })
            .WithTooltipFormatter(context => context.Index == 0 ? Caption : ""))
        .AddBar("Default", new[] { new ChartPoint(1, 22) })
        .AddSeries(new ChartSeries(new ChartSeriesId("suppressed"), "Suppressed", ChartSeriesKind.Bar,
            new[] { new ChartPoint(1, 33) }).WithTooltipFormatter(_ => ""));
}

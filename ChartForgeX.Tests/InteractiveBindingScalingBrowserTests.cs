using System.Diagnostics;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using HtmlTinkerX;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>
/// Catches superlinear runtime binding of a saved interactive line chart. Four times the points may take at most
/// about twice the linear expectation; per-point work that scans the whole series (about 16x) fails.
/// </summary>
[Collection(nameof(RenderingBudgetCollection))]
public sealed class InteractiveBindingScalingBrowserTests {
    private const int SmallPoints = 600;
    private const int Scale = 4;
    private const double TimeRatioLimit = 8;
    private readonly ITestOutputHelper _output;

    public InteractiveBindingScalingBrowserTests(ITestOutputHelper output) => _output = output;

    [BrowserFact]
    public async Task LineRuntimeBindingTimeGrowsLinearlyWithPointCount() {
        if (!Enabled) return;
        await using var session = await HtmlBrowser.OpenSessionForNavigationAsync(new HtmlBrowserLaunchOptions {
            Browser = Engine, Headless = true, Timeout = 300_000, ViewportWidth = 1400, ViewportHeight = 900
        });
        var small = await BindAsync(session.Page, SmallPoints);
        var large = await BindAsync(session.Page, SmallPoints * Scale);
        var ratio = large / Math.Max(small, 1);
        _output.WriteLine($"{SmallPoints} points bound in {small:F0} ms; {SmallPoints * Scale} in {large:F0} ms; ratio {ratio:F2}x.");
        Assert.True(ratio <= TimeRatioLimit, $"{Scale}x points took {ratio:F2}x as long to bind (limit {TimeRatioLimit}x): {small:F0} ms vs {large:F0} ms.");
        AssertNoConsoleErrors(session);
    }

    // Load to the runtime's bound marker, best of three loads in one warm browser.
    private static async Task<double> BindAsync(IPage page, int points) {
        var html = Chart.Create().WithSize(1200, 600).WithLegend(false)
            .AddLine("Signal", Enumerable.Range(0, points).Select(index => new ChartPoint(index, Math.Sin(index * .013) * 40 + index % 17)))
            .ToInteractiveHtmlPage(options => options.Interaction.Features =
                ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan);
        var best = double.MaxValue;
        for (var load = 0; load < 3; load++) {
            await page.SetContentAsync("<!doctype html><title>reset</title>");
            var watch = Stopwatch.StartNew();
            await page.SetContentAsync(html);
            await page.WaitForFunctionAsync("() => document.querySelector('.cfx-interactive-chart')?.dataset.cfxRuntimeBound === 'true'");
            watch.Stop();
            best = Math.Min(best, watch.Elapsed.TotalMilliseconds);
        }
        Assert.Equal(points, await page.Locator(PointTarget).CountAsync());
        return best;
    }
}

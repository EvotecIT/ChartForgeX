using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveTooltipPaintBrowserTests {
    [Fact]
    public async Task FullyOffscreenOversizedScheduleTargetScrollsIntoViewAndOrdinaryNavigationStillReturns() {
        if (!Enabled) return;
        var chart = Frame(false).WithTitle("Offscreen intervals").WithXAxisBounds(0, 10)
            .AddTimelineRange("Beginning", 0, 1).AddTimelineRange("Outside", 5, 10);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(ExactFeatures), 300, 720);
        var page = session.Page;
        var targets = page.Locator("[data-cfx-keyboard-component=data]");
        Assert.Equal(2, await targets.CountAsync());
        var first = await targets.First.GetAttributeAsync("data-cfx-target-id");
        var outside = await targets.Last.GetAttributeAsync("data-cfx-target-id");
        var before = await FocusGeometryAsync(targets.Last);
        Assert.True(before[2] > before[1] - before[0]);
        Assert.True(before[3] >= before[1]);
        await targets.First.FocusAsync();
        await page.Keyboard.PressAsync("End");
        Assert.Equal(outside, await page.EvaluateAsync<string>("() => document.activeElement.dataset.cfxTargetId"));
        var after = await FocusGeometryAsync(targets.Last);
        Assert.True(after[4] > before[4]);
        Assert.True(after[3] < after[1] && after[3] + after[2] > after[0]);
        Assert.Contains("Outside", await TooltipTextAsync(page), StringComparison.Ordinal);
        await CaptureContractAsync(page, "paint-focus-oversized-offscreen-readable", new { Before = before, After = after, TargetId = outside });
        await page.Keyboard.PressAsync("Home");
        Assert.Equal(first, await page.EvaluateAsync<string>("() => document.activeElement.dataset.cfxTargetId"));
        var ordinary = await FocusGeometryAsync(targets.First);
        Assert.True(ordinary[2] < ordinary[1] - ordinary[0]);
        Assert.True(ordinary[3] >= ordinary[0] - 1 && ordinary[3] + ordinary[2] <= ordinary[1] + 1);
        Assert.True(ordinary[4] < after[4]);
        Assert.Contains("Beginning", await TooltipTextAsync(page), StringComparison.Ordinal);
        AssertNoConsoleErrors(session);
    }

    private static async Task<double[]> FocusGeometryAsync(ILocator target) => await target.EvaluateAsync<double[]>("n => { const s=n.closest('.cfx-stage'), b=s.getBoundingClientRect(), t=n.getBoundingClientRect(), left=b.left+s.clientLeft+8; return [left,left+s.clientWidth-16,t.width,t.left,s.scrollLeft]; }");
}

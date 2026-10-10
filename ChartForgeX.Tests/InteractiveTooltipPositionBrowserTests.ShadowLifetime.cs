using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveTooltipPositionBrowserTests {
    [Theory]
    [InlineData(HtmlChartTooltipAnchor.Node, "target", false)]
    [InlineData(HtmlChartTooltipAnchor.Chart, "target", false)]
    [InlineData(HtmlChartTooltipAnchor.Node, "root", false)]
    [InlineData(HtmlChartTooltipAnchor.Chart, "root", false)]
    [InlineData(HtmlChartTooltipAnchor.Node, "host", false)]
    [InlineData(HtmlChartTooltipAnchor.Chart, "host", false)]
    [InlineData(HtmlChartTooltipAnchor.Node, "target", true)]
    [InlineData(HtmlChartTooltipAnchor.Chart, "target", true)]
    [InlineData(HtmlChartTooltipAnchor.Node, "root", true)]
    [InlineData(HtmlChartTooltipAnchor.Chart, "root", true)]
    [InlineData(HtmlChartTooltipAnchor.Node, "host", true)]
    [InlineData(HtmlChartTooltipAnchor.Chart, "host", true)]
    public async Task BriefClosedShadowRemovalHidesTheShownReadoutAndReleasesPositionWatchers(HtmlChartTooltipAnchor anchor, string subject, bool pinned) {
        if (!Enabled) return;
        await using var session = await OpenAsync("<html><body></body></html>", 950, 760);
        var page = session.Page;
        await InstallListenerProbeAsync(page);
        await page.SetContentAsync("<html><body style='margin:20px'>" + Observations(true).WithLegend().ToInteractiveHtmlFragment(options => {
            options.Tooltip.Position.Anchor = anchor;
        }) + "</body></html>");
        await MountPositionFocusChartAsync(page, shadow: true);
        var baseline = await ListenerStateAsync(page);
        await page.EvaluateAsync("selector=>window.positionFocusChart.querySelector(selector).focus()", Reading);
        if (pinned) await page.Keyboard.PressAsync("Space");
        await page.WaitForFunctionAsync("expected=>window.positionListeners.resize.size===expected", baseline.GetProperty("resize").GetInt32() + 1);
        await page.EvaluateAsync("()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)))");
        var shown = await GeometryAsync(page, hosted: true);
        Assert.Equal(pinned, shown.GetProperty("pinned").GetBoolean());
        var active = await ListenerStateAsync(page);
        await page.EvaluateAsync("""
            ({subject,selector})=>{
                const root=window.positionFocusChart, node=subject==='target'?root.querySelector(selector):subject==='root'?root:root.getRootNode().host;
                const parent=node.parentNode,next=node.nextSibling;node.remove();parent.insertBefore(node,next);
            }
            """, new { subject, selector = Reading });
        await page.EvaluateAsync("()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)))");
        var detached = await GeometryAsync(page, hosted: true);
        var remaining = await ListenerStateAsync(page);
        var lifetime = await PositionFocusLifetimeAsync(page);
        await CaptureAsync(page, $"position-shadow-lifetime-{anchor}-{subject}-{(pinned ? "pinned" : "focused")}", new { baseline, shown, active, detached, remaining, lifetime }, session);
        Assert.True(detached.GetProperty("hidden").GetBoolean());
        foreach (var name in new[] { "resize", "scroll", "transitionend", "observed" })
            Assert.Equal(baseline.GetProperty(name).GetInt32(), remaining.GetProperty(name).GetInt32());
        await page.EvaluateAsync("selector=>window.positionFocusChart.querySelector(selector).focus()", Reading);
        var recovered = await AssertPositionAsync(page, anchor, new[] { HtmlChartTooltipPlacement.BottomRight }, hosted: true);
        await CaptureAsync(page, $"position-shadow-lifetime-recovery-{anchor}-{subject}-{(pinned ? "pinned" : "focused")}", recovered, session);
        AssertNoConsoleErrors(session);
    }
}

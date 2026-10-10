using System.Text.Json;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveTooltipPositionBrowserTests {
    [Theory]
    [InlineData(HtmlChartTooltipAnchor.Pointer, false, 950, false)]
    [InlineData(HtmlChartTooltipAnchor.Pointer, true, 340, true)]
    [InlineData(HtmlChartTooltipAnchor.Node, false, 340, true)]
    [InlineData(HtmlChartTooltipAnchor.Node, true, 950, false)]
    [InlineData(HtmlChartTooltipAnchor.Chart, false, 950, true)]
    [InlineData(HtmlChartTooltipAnchor.Chart, true, 340, false)]
    public async Task PointerExitRepositionsTheFocusedLegendUsingItsCurrentNativeAnchor(HtmlChartTooltipAnchor anchor, bool shadow, int width, bool dark) {
        if (!Enabled) return;
        var placements = FocusRecoveryPlacements(anchor);
        var fragment = Observations(dark).WithLegend().ToInteractiveHtmlFragment(options => {
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
            options.Tooltip.Mode = HtmlChartTooltipMode.Single;
            options.Tooltip.DelayMilliseconds = shadow ? 360 : 0;
            options.Tooltip.Range = HtmlChartTooltipRange.Exact;
            options.Tooltip.Position.Anchor = anchor;
            options.Tooltip.Position.Placements = placements;
            options.Tooltip.Position.Gap = 11.5;
            options.Tooltip.Position.OffsetX = -3.25;
            options.Tooltip.Position.OffsetY = 5.75;
        });
        await using var session = await OpenAsync("<html><body style='margin:20px'>" + fragment + "</body></html>", width, 760);
        var page = session.Page;
        await MountPositionFocusChartAsync(page, shadow);
        await page.EvaluateAsync("selector=>window.positionFocusChart.querySelector(selector).focus()", Legend(0));
        var initial = await AssertPositionAsync(page, anchor, placements, 11.5, -3.25, 5.75, selector: Legend(0), hosted: true);
        var point = await page.EvaluateAsync<double[]>("selector=>{const b=window.positionFocusChart.querySelector(selector).getBoundingClientRect();return[b.x+b.width/2,b.y+b.height/2];}", Point(0, 1));
        await page.Mouse.MoveAsync((float)point[0], (float)point[1]);
        await page.WaitForFunctionAsync("()=>!window.positionFocusChart.querySelector('.cfx-tooltip').hidden && window.positionFocusChart.dataset.cfxHoverKey?.startsWith('point|')");
        var pointer = await GeometryAsync(page, Point(0, 1), hosted: true);
        Assert.NotEqual(initial.GetProperty("text").GetString(), pointer.GetProperty("text").GetString());
        await MoveAwayAsync(page);
        var recovered = await GeometryAsync(page, Legend(0), hosted: true);
        var lifetime = await PositionFocusLifetimeAsync(page);
        await CaptureAsync(page, $"position-focus-legend-{anchor}-{(shadow ? "shadow" : "light")}-{width}-{(dark ? "dark" : "light")}", new { initial, point, pointer, recovered, lifetime }, session);
        Assert.Equal(initial.GetProperty("text").GetString(), recovered.GetProperty("text").GetString());
        Assert.StartsWith("legend|", lifetime.GetProperty("hover").GetString()!, StringComparison.Ordinal);
        Assert.Equal("legend", lifetime.GetProperty("focusedKind").GetString());
        await AssertPositionAsync(page, anchor, placements, 11.5, -3.25, 5.75, selector: Legend(0), hosted: true);
        await page.EvaluateAsync("selector=>window.positionFocusChart.querySelector(selector).blur()", Legend(0));
        Assert.True(await page.EvaluateAsync<bool>("()=>window.positionFocusChart.querySelector('.cfx-tooltip').hidden"));
        AssertNoConsoleErrors(session);
    }

    private static HtmlChartTooltipPlacement[] FocusRecoveryPlacements(HtmlChartTooltipAnchor anchor) => anchor == HtmlChartTooltipAnchor.Chart
        ? new[] { HtmlChartTooltipPlacement.Right, HtmlChartTooltipPlacement.Left }
        : new[] { HtmlChartTooltipPlacement.TopRight, HtmlChartTooltipPlacement.BottomLeft, HtmlChartTooltipPlacement.BottomRight };

    private static Task MountPositionFocusChartAsync(IPage page, bool shadow, string name = "position-focus") => page.EvaluateAsync("""
        ({shadow,name})=>{
            const root=window.positionFocusChart=document.querySelector('.cfx-interactive-chart');
            window.positionFocusEvents=[];
            for(const type of ['cfxhover','cfxselect','cfxtooltip'])root.addEventListener(type,event=>window.positionFocusEvents.push({type,detail:event.detail}));
            for(const type of ['focus','blur'])root.addEventListener(type,event=>window.positionFocusEvents.push({type,kind:event.target.dataset.cfxTargetKind,id:event.target.dataset.cfxTargetId}),true);
            if(!shadow)return;
            const style=document.querySelector('style[data-cfx-interactive-assets]')||document.querySelector('style');
            const outer=document.createElement('div');outer.id=name+'-outer';root.before(outer);
            const inner=document.createElement('div');inner.id=name+'-inner';outer.attachShadow({mode:'closed'}).append(inner);
            inner.attachShadow({mode:'closed'}).append(style.cloneNode(true),root);
        }
        """, new { shadow, name });

    private static Task<JsonElement> PositionFocusLifetimeAsync(IPage page) => page.EvaluateAsync<JsonElement>("""
        ()=>{const root=window.positionFocusChart,active=root.getRootNode().activeElement;return{
            hover:root.dataset.cfxHoverKey,focusedKind:active?.dataset.cfxTargetKind,focusedId:active?.dataset.cfxTargetId,
            guideHidden:root.querySelector('.cfx-crosshair')?.hidden,pinned:root.dataset.cfxTooltipPinned==='true',events:window.positionFocusEvents};}
        """);
}

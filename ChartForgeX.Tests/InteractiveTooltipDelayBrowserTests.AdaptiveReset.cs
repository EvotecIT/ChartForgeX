using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveTooltipDelayBrowserTests {
    [BrowserFact]
    public async Task AdaptiveRedrawPreservesResetDismissalUntilFreshPointerOrKeyboardInput() {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(800, 440).WithHeader(false).WithAxes(false).WithGrid(false)
            .WithLegend(false).WithDataLabels(false).WithXAxisBounds(0, 10)
            .AddHorizontalBar("Reading", new[] { new ChartPoint(1, 5) });
        chart.Series[0].WithObservationId(0, new ChartObservationId("reading"));
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            ConfigureDelay(options, "exact");
            options.Tooltip.Mode = HtmlChartTooltipMode.Single;
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
            options.Adaptive.NarrowWidth = 360;
            options.Adaptive.NarrowHeight = 440;
        }), 950, 760);
        var page = session.Page;
        await page.Locator(".cfx-interactive-chart").EvaluateAsync("root=>root.style.width='360px'");
        await page.WaitForFunctionAsync("()=>document.querySelector('.cfx-interactive-chart').dataset.cfxAdaptiveDrawing==='narrow'", null,
            new PageWaitForFunctionOptions { Timeout = 10000 });
        await page.EvaluateAsync("""
            () => {
                const root=window.adaptiveResetRoot=document.querySelector('.cfx-interactive-chart');
                window.adaptiveResetController=ChartForgeXCharts.get(root);
                window.adaptiveResetEvents={entries:[],moves:[],shown:[],layouts:[]};
                root.addEventListener('pointerenter',event=>{
                    if(window.adaptiveResetPhase!=='redraw')return;
                    const target=event.target instanceof Element&&event.target.closest('[data-cfx-target-id]');
                    if(target)adaptiveResetEvents.entries.push({t:performance.now(),id:target.dataset.cfxTargetId,trusted:event.isTrusted});
                },true);
                root.addEventListener('pointermove',event=>{
                    if(window.adaptiveResetPhase==='redraw')adaptiveResetEvents.moves.push({x:event.clientX,y:event.clientY});
                },true);
                root.addEventListener('cfxlayoutchange',event=>adaptiveResetEvents.layouts.push(event.detail));
                new MutationObserver(()=>{
                    const tip=root.querySelector('.cfx-tooltip');
                    if(window.adaptiveResetPhase==='redraw'&&tip&&!tip.hidden)
                        adaptiveResetEvents.shown.push({t:performance.now(),text:tip.innerText});
                }).observe(root,{childList:true,subtree:true,attributes:true,attributeFilter:['hidden']});
                adaptiveResetController.select({observationIds:['reading']});
            }
            """);
        var pointer = await PointerAsync(page, Point(0, 0));
        await WaitForTipAsync(page);
        await page.Locator("[data-cfx-reset]").FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        Assert.True(await TipHiddenAsync(page));
        Assert.True(await page.Locator("[data-cfx-reset]").IsHiddenAsync());
        await page.EvaluateAsync("""
            pointer => {
                window.adaptiveResetPointer=pointer;
                window.adaptiveResetTarget=adaptiveResetRoot.querySelector('[data-cfx-role=point][data-cfx-point="0"]').dataset.cfxTargetId;
                window.adaptiveResetPreviousSvg=adaptiveResetRoot.querySelector('.cfx-stage svg');
                window.adaptiveResetPhase='redraw';
                // Extending a horizontal bar keeps the stationary pointer inside its actual native paint.
                adaptiveResetRoot.style.width='800px';
            }
            """, pointer);
        await page.WaitForFunctionAsync("()=>adaptiveResetRoot.dataset.cfxAdaptiveDrawing==='wide'&&adaptiveResetEvents.layouts.some(event=>event.drawing==='wide')", null,
            new PageWaitForFunctionOptions { Timeout = 10000 });
        await page.WaitForFunctionAsync("()=>adaptiveResetEvents.entries.some(event=>event.id===adaptiveResetTarget&&event.trusted)", null,
            new PageWaitForFunctionOptions { Timeout = 5000 });
        // Observe the whole delayed-readout window, including a tooltip that appears only transiently.
        await Task.Delay(Delay + 80);
        var dismissed = await AdaptiveResetStateAsync(page);
        await CaptureDelayAsync(page, "delay-adaptive-reset-dismissed", dismissed, session);
        Assert.True(dismissed.GetProperty("oldSvgDetached").GetBoolean());
        Assert.True(dismissed.GetProperty("sameController").GetBoolean());
        Assert.True(dismissed.GetProperty("pointerInsideMark").GetBoolean(), "The stationary pointer must still overlap the newly prepared mark.");
        Assert.Equal(dismissed.GetProperty("target").GetString(), dismissed.GetProperty("hit").GetString());
        Assert.Empty(dismissed.GetProperty("events").GetProperty("moves").EnumerateArray());
        Assert.True(dismissed.GetProperty("hidden").GetBoolean(), "An adaptive redraw must retain the reader's reset dismissal under a stationary pointer.");
        Assert.Empty(dismissed.GetProperty("events").GetProperty("shown").EnumerateArray());

        await page.EvaluateAsync("()=>window.adaptiveResetPhase='recovery'");
        await PointerAsync(page, Point(0, 0));
        await WaitForTipAsync(page);
        Assert.Contains("Reading", await TooltipTextAsync(page));
        await CaptureDelayAsync(page, "delay-adaptive-reset-pointer-recovery", await AdaptiveResetStateAsync(page), session);
        await page.EvaluateAsync("()=>adaptiveResetController.select({observationIds:['reading']})");
        await page.Locator("[data-cfx-reset]").FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        Assert.True(await TipHiddenAsync(page));
        await page.Locator(Point(0, 0)).FocusAsync();
        Assert.False(await TipHiddenAsync(page));
        Assert.Contains("Reading", await TooltipTextAsync(page));
        await CaptureDelayAsync(page, "delay-adaptive-reset-keyboard-recovery", await AdaptiveResetStateAsync(page), session);
        AssertNoConsoleErrors(session);
    }

    private static Task<JsonElement> AdaptiveResetStateAsync(IPage page) => page.EvaluateAsync<JsonElement>("""
        () => {
            const root=adaptiveResetRoot, point=root.querySelector('[data-cfx-role=point][data-cfx-point="0"]');
            const box=point.getBoundingClientRect(), [x,y]=adaptiveResetPointer;
            const hit=document.elementFromPoint(x,y)?.closest('[data-cfx-target-id]');
            return {hidden:root.querySelector('.cfx-tooltip').hidden,target:point.dataset.cfxTargetId,hit:hit?.dataset.cfxTargetId,
                pointerInsideMark:x>box.left&&x<box.right&&y>box.top&&y<box.bottom,
                box:{left:box.left,right:box.right,top:box.top,bottom:box.bottom},pointer:adaptiveResetPointer,
                oldSvgDetached:!adaptiveResetPreviousSvg.isConnected,sameController:ChartForgeXCharts.get(root)===adaptiveResetController,
                drawing:root.dataset.cfxAdaptiveDrawing,events:adaptiveResetEvents};
        }
        """);
}

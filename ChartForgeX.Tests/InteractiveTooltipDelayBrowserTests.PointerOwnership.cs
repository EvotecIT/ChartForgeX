using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveTooltipDelayBrowserTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EndingAnOlderNativeTouchContactKeepsTheNewContactsDeadline(bool stageAcquisition) {
        if (!Enabled) return;
        const int contactDelay = 1000;
        await using var session = await OpenAsync(SeverityBars(false).ToInteractiveHtmlPage(options => {
            options.Tooltip.DelayMilliseconds = contactDelay;
            options.Tooltip.Range = stageAcquisition ? HtmlChartTooltipRange.Nearest : HtmlChartTooltipRange.Exact;
            options.Tooltip.Mode = HtmlChartTooltipMode.Single;
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
            options.Interaction.Disable(ChartInteractionFeatures.KeyboardNavigation);
        }), 950, 760);
        var page = session.Page;
        await TraceAsync(page);
        await page.EvaluateAsync("""
            () => {
                window.contactTrace=[];
                ['pointerenter','pointermove','pointerleave','pointercancel','click'].forEach(type => document.addEventListener(type,event => {
                    const point=event.target.closest?.('[data-cfx-role=point]');
                    window.contactTrace.push({type,id:event.pointerId,pointerType:event.pointerType,primary:event.isPrimary,x:event.clientX,y:event.clientY,point:point?.dataset.cfxPoint,series:point?.dataset.cfxSeries});
                },true));
            }
            """);
        var first = await BoxAsync(page, Point(0, 3));
        var second = await BoxAsync(page, Point(0, 4));
        object Contact(int id, double x, double y) => new { id, x, y };
        var a = Contact(1, first.X + first.Width / 2, first.Y + first.Height / 2);
        var movedA = Contact(1, first.X + first.Width / 2, first.Y + first.Height / 2 + 24);
        var secondX = stageAcquisition ? second.X - 8 : second.X + second.Width / 2;
        var b = Contact(2, secondX, second.Y + second.Height / 2);
        var cdp = await page.Context.NewCDPSessionAsync(page);
        Task TouchAsync(string type, params object[] contacts) => cdp.SendAsync("Input.dispatchTouchEvent",
            new Dictionary<string, object> { ["type"] = type, ["touchPoints"] = contacts });
        try {
            await cdp.SendAsync("Emulation.setTouchEmulationEnabled", new Dictionary<string, object> { ["enabled"] = true, ["maxTouchPoints"] = 2 });
            await TouchAsync("touchStart", a);
            // Moving within the tall bar suppresses a compatibility click from the first contact.
            await TouchAsync("touchMove", movedA);
            await TouchAsync("touchStart", movedA, b);
            if (stageAcquisition) await TouchAsync("touchMove", movedA, Contact(2, secondX, second.Y + second.Height / 2 + 16));
            await TouchAsync("touchEnd", movedA);
            await Task.Delay(contactDelay + 120);
            var receipt = await page.EvaluateAsync<object>("() => { const root=document.querySelector('.cfx-interactive-chart'), guide=root.querySelector('.cfx-crosshair'); return {hidden:root.querySelector('.cfx-tooltip').hidden,text:root.querySelector('.cfx-tooltip').innerText,guideHidden:guide.hidden,guide:root.dataset.cfxCrosshair,guideLabel:guide.innerText,trace:window.delayTrace,contacts:window.contactTrace}; }");
            await CaptureDelayAsync(page, "delay-native-touch-" + (stageAcquisition ? "stage-" : "") + "ownership", receipt, session);
            Assert.False(await TipHiddenAsync(page));
            Assert.Contains("208", await TooltipTextAsync(page), StringComparison.Ordinal);
            Assert.True(await page.EvaluateAsync<bool>("() => { const root=document.querySelector('.cfx-interactive-chart'), guide=root.querySelector('.cfx-crosshair'); return guide.hidden || root.dataset.cfxCrosshair===root.dataset.cfxHoverKey; }"));
            Assert.True(await page.EvaluateAsync<bool>("stage => window.contactTrace.some(e=>e.type==='pointerenter'&&!e.primary&&(stage ? !e.point : e.point==='4')) && window.contactTrace.some(e=>e.type==='pointerleave'&&e.primary&&e.point==='3')", stageAcquisition));
            if (stageAcquisition) Assert.True(await page.EvaluateAsync<bool>("() => window.contactTrace.some(e=>e.type==='pointermove'&&!e.primary&&!e.point)"));
            AssertNoConsoleErrors(session);
        } finally {
            await TouchAsync("touchEnd");
            await cdp.DetachAsync();
        }
    }

    [Theory]
    [InlineData("target-move", false)]
    [InlineData("target-leave", false)]
    [InlineData("stage-leave", false)]
    [InlineData("stage-cancel", false)]
    [InlineData("target-move", true)]
    [InlineData("target-leave", true)]
    [InlineData("stage-leave", true)]
    [InlineData("stage-cancel", true)]
    public async Task OlderPenEventsCannotMoveOrDismissANewerContactsReadout(string action, bool shown) {
        if (!Enabled) return;
        await using var session = await OpenAsync(HostFragment(), 950, 760);
        var page = session.Page;
        await MountFragmentAsync(page, "light");
        await page.EvaluateAsync("""
            () => {
                const root=window.hostedChart, points=root.querySelectorAll('[data-cfx-role=point][data-cfx-series="0"]');
                const eventFor=(type,node,id,pointerType) => {
                    const b=node.getBoundingClientRect();
                    return new PointerEvent(type,{bubbles:type==='pointermove'||type==='pointercancel',pointerId:id,pointerType,clientX:b.x+b.width/2,clientY:b.y+b.height/2});
                };
                window.oldContactEvent=type=>eventFor(type,points[0],17,'pen');
                points[0].dispatchEvent(eventFor('pointerenter',points[0],17,'pen'));
                points[1].dispatchEvent(eventFor('pointerenter',points[1],22,'touch'));
            }
            """);
        if (shown) await WaitForTipAsync(page);
        await page.EvaluateAsync("""
            action => {
                const root=window.hostedChart, old=root.querySelector('[data-cfx-role=point][data-cfx-series="0"][data-cfx-point="0"]');
                const surface=action.startsWith('stage-')?root.querySelector('.cfx-stage'):old;
                const type=action.endsWith('move')?'pointermove':action.endsWith('cancel')?'pointercancel':'pointerleave';
                surface.dispatchEvent(window.oldContactEvent(type));
            }
            """, action);
        if (!shown) await Task.Delay(Delay + 80);
        var receipt = await HostReceiptAsync(page);
        await CaptureDelayAsync(page, "delay-contact-owner-" + action + "-" + shown, receipt, session);
        Assert.False(await TipHiddenAsync(page));
        Assert.Equal("6", await page.Locator(".cfx-tooltip [data-cfx-tooltip-series-key=\"Reading\"] + dd").InnerTextAsync());
        Assert.True(await page.EvaluateAsync<bool>("""
            () => {
                const root=window.hostedChart, node=root.querySelector('[data-cfx-role=point][data-cfx-series="0"][data-cfx-point="1"]'), b=node.getBoundingClientRect(), tip=root.querySelector('.cfx-tooltip');
                return root.dataset.cfxHoverKey.includes('|1|') && Math.abs(parseFloat(tip.style.left)-Math.min(innerWidth-tip.offsetWidth-8,b.x+b.width/2+14))<.01;
            }
            """));
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task ANewContactOnTheSameTargetStartsItsOwnFullDelay() {
        if (!Enabled) return;
        const int contactDelay = 1000;
        await using var session = await OpenAsync(HostFragment(delay: contactDelay), 950, 760);
        var page = session.Page;
        await MountFragmentAsync(page, "light");
        await page.EvaluateAsync("""
            async () => {
                const node=window.hostedChart.querySelector('[data-cfx-role=point][data-cfx-series="0"][data-cfx-point="0"]'), b=node.getBoundingClientRect();
                const enter=id=>node.dispatchEvent(new PointerEvent('pointerenter',{pointerId:id,pointerType:'touch',clientX:b.x+b.width/2,clientY:b.y+b.height/2}));
                enter(17);
                await new Promise(resolve=>setTimeout(resolve,200));
                window.newContactAt=performance.now();
                enter(22);
            }
            """);
        await WaitForTipAsync(page);
        var elapsed = await page.EvaluateAsync<double>("() => window.delayTrace.shown[0].t-window.newContactAt");
        await CaptureDelayAsync(page, "delay-new-contact-same-target", new { Elapsed = elapsed, Trace = await TraceStateAsync(page) }, session);
        Assert.InRange(elapsed, contactDelay - 4, 5000);
        AssertNoConsoleErrors(session);
    }
}

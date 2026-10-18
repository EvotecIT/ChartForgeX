using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveTooltipDelayBrowserTests {
    [BrowserTheory]
    [InlineData("nearest")]
    [InlineData("distance")]
    [InlineData("exact")]
    public async Task ExternalOverlayCannotPublishAReadoutAtExpiry(string range) {
        if (!Enabled) return;
        await using var session = await OpenAsync(Observations(false).ToInteractiveHtmlPage(options => ConfigureDelay(options, range)), 950, 760);
        var page = session.Page;
        await TraceAsync(page);
        await page.EvaluateAsync("""
            () => {
                const root=document.querySelector('.cfx-interactive-chart'), tip=root.querySelector('.cfx-tooltip');
                const nativeTimer=window.setTimeout.bind(window), delay=Number(root.dataset.cfxTooltipDelay);
                window.overlayExpiry=[];
                // Place the real host overlay at the timer boundary and record the callback's transient state.
                // A final hidden-state assertion alone would miss a readout shown before pointerleave arrives.
                window.setTimeout=(callback,ms,...args)=>nativeTimer((...values)=>{
                    if(ms!==delay) { callback(...values); return; }
                    const overlay=document.createElement('div');
                    overlay.id='delay-host-overlay'; overlay.textContent='Host dialog';
                    overlay.style.cssText='position:fixed;inset:0;background:#eee;z-index:5;padding:24px;';
                    document.body.append(overlay);
                    const hit=document.elementFromPoint(...window.delayTrace.pointer);
                    callback(...values);
                    window.overlayExpiry.push({hit:hit?.id,owned:!!hit&&root.contains(hit),hidden:tip.hidden,readout:tip.innerText});
                },ms,...args);
            }
            """);
        await PointerAsync(page, Point(0, 0));
        Assert.True(await TipHiddenAsync(page));
        await page.WaitForFunctionAsync("() => window.overlayExpiry.length>0", null, new PageWaitForFunctionOptions { Timeout = 5000 });
        var expiry = await page.EvaluateAsync<JsonElement>("() => window.overlayExpiry");
        var trace = await TraceStateAsync(page);
        await CaptureDelayAsync(page, "delay-overlay-expiry-" + range, new { expiry, trace });
        var observed = Assert.Single(expiry.EnumerateArray());
        Assert.Equal("delay-host-overlay", observed.GetProperty("hit").GetString());
        Assert.False(observed.GetProperty("owned").GetBoolean());
        Assert.True(observed.GetProperty("hidden").GetBoolean());
        Assert.Empty(trace.GetProperty("shown").EnumerateArray());
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData("target-paint")]
    [InlineData("root-hidden")]
    [InlineData("root-removed")]
    [InlineData("target-removed")]
    [InlineData("features")]
    public async Task HostInvalidationCannotReviveAQueuedReadout(string change) {
        if (!Enabled) return;
        await using var session = await OpenAsync(Observations(false).ToInteractiveHtmlPage(options => ConfigureDelay(options, "exact")), 950, 760);
        var page = session.Page;
        await MountFragmentAsync(page, "light");
        await RecordHostExpiryAsync(page);
        await ArmQueuedHostMutationAsync(page, change);
        await PointerAsync(page, Point(0, 0));
        var mutation = await WaitForHostMutationAsync(page);
        if (change == "target-paint") Assert.Equal("0", await page.Locator(Point(0, 0)).EvaluateAsync<string>("node=>getComputedStyle(node).opacity"));
        await Task.Delay(Delay + 80);
        var receipt = await HostReceiptAsync(page);
        AssertQueuedHostCancellation(mutation, receipt);
        Assert.True(receipt.GetProperty("hidden").GetBoolean());
        if (change == "root-hidden") {
            // Restoring a host beneath the pointer can legitimately start a fresh native hover.
            // Keep the pointer away so this assertion checks cancellation of the old queued readout.
            await MoveAwayAsync(page);
            await page.EvaluateAsync("() => window.hostedChart.hidden=false");
            await Task.Delay(60);
            Assert.True(await TipHiddenAsync(page));
        }
        Assert.Empty((await TraceStateAsync(page)).GetProperty("shown").EnumerateArray());
        await CaptureDelayAsync(page, "delay-host-invalidation-" + change, await HostReceiptAsync(page), session);
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task BriefHostDetachCancelsBeforeReattachmentAndFreshAcquisitionCanRecover() {
        if (!Enabled) return;
        await using var session = await OpenAsync(Observations(false).ToInteractiveHtmlPage(options => ConfigureDelay(options, "exact")), 950, 760);
        var page = session.Page;
        await MountFragmentAsync(page, "light");
        await RecordHostExpiryAsync(page);
        await ArmQueuedHostMutationAsync(page, "detach-root");
        await PointerAsync(page, Point(0, 0));
        var mutation = await WaitForHostMutationAsync(page);
        // Reinsertion may legitimately acquire a new hover; cancellation belongs to the original timer.
        await MoveAwayAsync(page);
        await Task.Delay(Delay + 80);
        var canceled = await HostReceiptAsync(page);
        await CaptureDelayAsync(page, "delay-light-detach-reattach", canceled, session);
        AssertQueuedHostCancellation(mutation, canceled);
        // A real new pointerenter after reinsertion owns a fresh request, independently of the canceled timer.
        Assert.True(await TipHiddenAsync(page));
        await PointerAsync(page, Point(0, 0));
        await AssertDelayedNativeEntryAsync(page);
        await WaitForTipAsync(page);
        Assert.Contains("Reading", await TooltipTextAsync(page), StringComparison.Ordinal);
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task ResetFromKeyboardCancelsPointerWorkWithoutPointerLeave() {
        if (!Enabled) return;
        await using var session = await OpenAsync(Observations(false).ToInteractiveHtmlPage(options => { ConfigureDelay(options, "exact"); options.Interaction.Enable(ChartInteractionFeatures.Zoom); }), 950, 760);
        var page = session.Page;
        var pageErrors = new List<string>();
        page.PageError += (_, error) => pageErrors.Add(error);
        await page.Locator("[data-cfx-zoom=in]").ClickAsync();
        await SettleCameraAsync(page);
        var clockStart = DateTime.UtcNow;
        await page.Clock.InstallAsync(new ClockInstallOptions { TimeDate = clockStart });
        await page.Clock.PauseAtAsync(clockStart.AddHours(1));
        await TraceAsync(page);
        await page.EvaluateAsync("""
            delay => {
                const root=document.querySelector('.cfx-interactive-chart');
                window.resetKeyboardEvents=[];
                const timers=window.resetKeyboardTimers={queued:[],canceled:[],fired:[]};
                const schedule=window.setTimeout.bind(window),cancel=window.clearTimeout.bind(window);
                window.setTimeout=(callback,milliseconds,...args)=>{
                    if(milliseconds!==delay)return schedule(callback,milliseconds,...args);
                    const id=schedule(()=>{timers.fired.push({id,t:performance.now()});callback(...args);},milliseconds);
                    timers.queued.push({id,t:performance.now(),delay:milliseconds});return id;
                };
                window.clearTimeout=id=>{timers.canceled.push({id,t:performance.now()});cancel(id);};
                for(const type of ['cfxreset','cfxviewport','focus','blur','pointerenter','pointermove','keydown','keyup','click'])
                    root.addEventListener(type,event=>window.resetKeyboardEvents.push({type,t:performance.now(),trusted:event.isTrusted,
                        role:event.target?.dataset?.cfxRole,target:event.target?.dataset?.cfxTargetId,
                        reset:event.target?.matches?.('[data-cfx-reset]'),key:event.key,x:event.clientX,y:event.clientY}),true);
            }
            """, Delay);
        const string resetState = """
            () => {
                const root=document.querySelector('.cfx-interactive-chart'),active=root.getRootNode().activeElement;
                return {t:performance.now(),hidden:root.querySelector('.cfx-tooltip').hidden,zoom:root.dataset.cfxZoom,
                    resetHidden:root.querySelector('[data-cfx-reset]').hidden,focusRole:active?.dataset?.cfxRole,
                    focusTarget:active?.dataset?.cfxTargetId,focusReset:active===root.querySelector('[data-cfx-reset]'),
                    focusRoot:active===root,hover:root.dataset.cfxHoverKey,trace:window.delayTrace,
                    timers:window.resetKeyboardTimers,events:window.resetKeyboardEvents};
            }
            """;
        await PointerAsync(page, Point(0, 0));
        Assert.True(await TipHiddenAsync(page));
        var queued = await page.EvaluateAsync<JsonElement>(resetState);
        var timers = queued.GetProperty("timers");
        var canceledTimers = timers.GetProperty("canceled").EnumerateArray().Select(timer => timer.GetProperty("id").GetInt64()).ToArray();
        var queuedTimer = Assert.Single(timers.GetProperty("queued").EnumerateArray(),
            timer => !canceledTimers.Contains(timer.GetProperty("id").GetInt64())).GetProperty("id").GetInt64();
        Assert.Empty(queued.GetProperty("timers").GetProperty("fired").EnumerateArray());
        Assert.True(await page.Locator(Point(0, 0)).EvaluateAsync<bool>("node=>{const b=node.getBoundingClientRect();return node.contains(document.elementFromPoint(b.x+b.width/2,b.y+b.height/2));}"));
        await page.Locator("[data-cfx-reset]").FocusAsync();
        var before = await page.EvaluateAsync<JsonElement>(resetState);
        await page.Keyboard.PressAsync("Enter");
        var reset = await page.EvaluateAsync<JsonElement>(resetState);
        await page.Clock.RunForAsync(Delay + 80);
        var deadline = await page.EvaluateAsync<JsonElement>(resetState);
        await CaptureDelayAsync(page, "delay-reset-keyboard", new { queued, before, reset, deadline, pageErrors }, session);
        Assert.True(await TipHiddenAsync(page));
        Assert.True(await page.Locator("[data-cfx-reset]").IsHiddenAsync());
        Assert.Empty((await TraceStateAsync(page)).GetProperty("shown").EnumerateArray());
        Assert.Contains(deadline.GetProperty("timers").GetProperty("canceled").EnumerateArray(), timer => timer.GetProperty("id").GetInt64() == queuedTimer);
        Assert.DoesNotContain(deadline.GetProperty("timers").GetProperty("fired").EnumerateArray(), timer => timer.GetProperty("id").GetInt64() == queuedTimer);
        await page.Clock.ResumeAsync();
        await page.Locator(Point(0, 0)).FocusAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        Assert.True(await page.Locator(Point(0, 1)).EvaluateAsync<bool>("node=>node===document.activeElement"));
        Assert.False(await TipHiddenAsync(page));
        Assert.Contains("Reading", await TooltipTextAsync(page), StringComparison.Ordinal);
        var keyboard = await page.EvaluateAsync<JsonElement>(resetState);
        // A second ordinary reset lets fresh pointer movement independently resume the delayed readout.
        await page.Locator("[data-cfx-zoom=in]").ClickAsync();
        await page.Locator("[data-cfx-reset]").FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        Assert.True(await TipHiddenAsync(page));
        // Acquire the final native paint, rather than a camera position sampled mid-transition.
        await SettleCameraAsync(page);
        await PointerAsync(page, Point(0, 0));
        await AssertDelayedNativeEntryAsync(page);
        try {
            await WaitForTipAsync(page);
        } finally {
            await CaptureDelayAsync(page, "delay-reset-keyboard-recovery", new { keyboard, pointer = await page.EvaluateAsync<JsonElement>(resetState), pageErrors }, session);
        }
        Assert.Contains("Reading", await TooltipTextAsync(page), StringComparison.Ordinal);
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task SynchronizedMuteInvalidatesPendingDataWhileLegendAccessStaysImmediate() {
        if (!Enabled) return;
        var source = Observations(false);
        var peer = Chart.Create().WithSize(800, 540).WithLegend(true).AddSeries(ChartSeries.CreateScatter(new ChartSeriesId("readings"), "Different label", new[] { new ChartPoint(3, 5), new ChartPoint(7, 6) }));
        var html = new[] { source, peer }.ToInteractiveHtmlDashboardPage(options => {
            options.Columns = 1; options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
            options.Tooltip.DelayMilliseconds = Delay; options.Tooltip.Range = HtmlChartTooltipRange.Exact;
            options.Interaction.Enable(ChartInteractionFeatures.SynchronizedCharts);
        });
        await using var session = await OpenAsync(html, 950, 1300);
        var page = session.Page;
        var clockStart = DateTime.UtcNow;
        await page.Clock.InstallAsync(new ClockInstallOptions { TimeDate = clockStart });
        await page.Clock.PauseAtAsync(clockStart.AddHours(1));
        await TraceAsync(page);
        await PointerAsync(page, ".cfx-interactive-chart:nth-child(1) " + Point(0, 0));
        var roots = page.Locator(".cfx-interactive-chart");
        await roots.Nth(1).Locator(Legend(0)).FocusAsync();
        Assert.False(await roots.Nth(1).Locator(".cfx-tooltip").EvaluateAsync<bool>("tip=>tip.hidden"));
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await roots.Nth(0).Locator(Legend(0)).GetAttributeAsync("data-cfx-muted"));
        await page.Clock.RunForAsync(Delay + 80);
        Assert.True(await TipHiddenAsync(page));
        Assert.Empty((await TraceStateAsync(page)).GetProperty("shown").EnumerateArray());
        await CaptureDelayAsync(page, "delay-semantic-mute", await TraceStateAsync(page));
        AssertNoConsoleErrors(session);
    }
}

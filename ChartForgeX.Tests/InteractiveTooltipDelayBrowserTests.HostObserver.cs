using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveTooltipDelayBrowserTests {
    [BrowserFact]
    public async Task SharedPendingObserverReleasesACanceledShadowTreeWhileAnotherChartIsQueued() {
        if (!Enabled) return;
        const int queuedDelay = 1000;
        var html = new[] { Observations(false), Observations(false) }.ToInteractiveHtmlDashboardPage(options => {
            options.Interaction.Features = ChartInteractionFeatures.Tooltips;
            options.Tooltip.Range = HtmlChartTooltipRange.Exact; options.Tooltip.DelayMilliseconds = queuedDelay;
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
        });
        await using var session = await OpenAsync(html, 950, 1400);
        var page = session.Page;
        var clockStart = DateTime.UtcNow;
        await page.Clock.InstallAsync(new ClockInstallOptions { TimeDate = clockStart });
        await page.Clock.PauseAtAsync(clockStart.AddHours(1));
        await MountFragmentAsync(page, "closed");
        await page.EvaluateAsync("() => { window.firstHostedChart=window.hostedChart; window.firstHostedChart.getRootNode().host.id='first-shadow-host'; }");
        await MountFragmentAsync(page, "closed");
        await page.EvaluateAsync("""
            () => {
                window.firstChartHost=window.firstHostedChart.getRootNode().host;
                window.secondChartHost=window.hostedChart.getRootNode().host;
                window.secondChartHost.id='second-shadow-host';
                document.querySelector('.cfx-shell').remove();
                [window.firstChartHost,window.secondChartHost].forEach(host => host.style.cssText='display:inline-block;width:46%;vertical-align:top;');
                const NativeObserver=window.MutationObserver;
                window.tooltipObserverTargets=new Map();
                window.MutationObserver=class extends NativeObserver {
                    constructor(callback) { super(callback); window.tooltipObserverTargets.set(this,new Set()); }
                    observe(target,options) { super.observe(target,options); window.tooltipObserverTargets.get(this).add(target); }
                    disconnect() { super.disconnect(); window.tooltipObserverTargets.get(this).clear(); }
                };
                // Pending readouts observe their enclosing elements as well as trees; chart lifetime observes only trees.
                window.pendingTooltipObserverTrees=()=>new Set(Array.from(window.tooltipObserverTargets.values())
                    .filter(targets=>Array.from(targets).some(target=>target instanceof Element))
                    .flatMap(targets=>Array.from(targets).filter(target=>target instanceof Document || target instanceof ShadowRoot)));
                // Two touch contacts can queue separate charts. Native MutationObserver and timers remain in use.
                window.queueBothCharts=() => [window.firstHostedChart,window.hostedChart].forEach((root,index) => {
                    const node=root.querySelector('[data-cfx-role=point][data-cfx-series="0"][data-cfx-point="0"]'), b=node.getBoundingClientRect();
                    window.delayTrace.pointer=[b.x+b.width/2,b.y+b.height/2];
                    node.dispatchEvent(new PointerEvent('pointerenter',{pointerId:index+1,pointerType:'touch',clientX:b.x+b.width/2,clientY:b.y+b.height/2}));
                });
            }
            """);
        await RecordHostExpiryAsync(page);
        await page.EvaluateAsync("() => window.queueBothCharts()");
        Assert.Equal(3, await page.EvaluateAsync<int>("() => window.pendingTooltipObserverTrees().size"));
        await page.EvaluateAsync("() => window.firstHostedChart.getRootNode().host.remove()");
        var retained = await page.EvaluateAsync<string[]>("() => Array.from(window.pendingTooltipObserverTrees(),tree => tree.host?.id || 'document').sort()");
        await page.EvaluateAsync("() => window.hostedChart.getRootNode().host.remove()");
        Assert.Empty(await page.EvaluateAsync<string[]>("() => Array.from(window.pendingTooltipObserverTrees(),tree => tree.host?.id || 'document')"));
        await page.EvaluateAsync("""
            () => {
                document.body.append(window.firstChartHost,window.secondChartHost);
                ChartForgeXCharts.mount(window.firstHostedChart);
                ChartForgeXCharts.mount(window.hostedChart);
                window.queueBothCharts();
                window.interleavedTimer=window.hostTimerTrace.queued.at(-1);
                // A host update before another contact's exit must keep the already queued detach record.
                const host=window.secondChartHost, parent=host.parentNode;
                host.remove(); parent.append(host);
                window.firstHostedChart.querySelector('[data-cfx-role=point][data-cfx-series="0"][data-cfx-point="0"]')
                    .dispatchEvent(new PointerEvent('pointerleave',{pointerId:1,pointerType:'touch',clientX:-1,clientY:-1}));
            }
            """);
        var afterInterleavedDetach = await page.EvaluateAsync<string[]>("() => Array.from(window.pendingTooltipObserverTrees(),tree => tree.host?.id || 'document').sort()");
        var timer = await page.EvaluateAsync<long>("() => window.interleavedTimer");
        await page.Clock.RunForAsync(queuedDelay + 80);
        var expiry = await HostReceiptAsync(page);
        await CaptureDelayAsync(page, "delay-shadow-observer-release", new { RetainedWhileQueued = retained, RetainedAfterLastCancellation = 0,
            AfterInterleavedDetach = afterInterleavedDetach, InterleavedTimer = timer, Expiry = expiry }, session);
        Assert.Equal(new[] { "document", "second-shadow-host" }, retained);
        Assert.Contains(timer, expiry.GetProperty("timers").GetProperty("canceled").EnumerateArray().Select(item => item.GetInt64()));
        Assert.DoesNotContain(timer, expiry.GetProperty("timers").GetProperty("fired").EnumerateArray().Select(item => item.GetInt64()));
        Assert.Empty(afterInterleavedDetach);
        Assert.True(expiry.GetProperty("hidden").GetBoolean());
        Assert.Empty(expiry.GetProperty("expiry").EnumerateArray());
        await page.Clock.ResumeAsync();
        await MoveAwayAsync(page);
        await MoveToHostTargetAsync(page, Point(0, 0));
        await AssertDelayedNativeEntryAsync(page);
        await WaitForHostExpiryAsync(page);
        Assert.False(await HostedTipHiddenAsync(page));
        await CaptureDelayAsync(page, "delay-shadow-observer-recovery", await HostReceiptAsync(page), session);
        AssertNoConsoleErrors(session);
    }
}

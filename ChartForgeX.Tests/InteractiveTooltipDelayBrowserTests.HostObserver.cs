using ChartForgeX.Interactivity.Html;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveTooltipDelayBrowserTests {
    [Fact]
    public async Task SharedPendingObserverReleasesACanceledShadowTreeWhileAnotherChartIsQueued() {
        if (!Enabled) return;
        var html = new[] { Observations(false), Observations(false) }.ToInteractiveHtmlDashboardPage(options => {
            options.Tooltip.Range = HtmlChartTooltipRange.Exact; options.Tooltip.DelayMilliseconds = 10000;
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
        });
        await using var session = await OpenAsync(html, 950, 1400);
        var page = session.Page;
        await MountFragmentAsync(page, "closed");
        await page.EvaluateAsync("() => { window.firstHostedChart=window.hostedChart; window.firstHostedChart.getRootNode().host.id='first-shadow-host'; }");
        await MountFragmentAsync(page, "closed");
        await page.EvaluateAsync("""
            () => {
                window.hostedChart.getRootNode().host.id='second-shadow-host';
                const NativeObserver=window.MutationObserver;
                window.tooltipObserverTargets=new Set();
                window.MutationObserver=class extends NativeObserver {
                    observe(tree,options) { window.tooltipObserverTargets.add(tree); super.observe(tree,options); }
                    disconnect() { window.tooltipObserverTargets.clear(); super.disconnect(); }
                };
                // Two touch contacts can queue separate charts. Native MutationObserver and timers remain in use.
                [window.firstHostedChart,window.hostedChart].forEach((root,index) => {
                    const node=root.querySelector('[data-cfx-role=point][data-cfx-series="0"][data-cfx-point="0"]'), b=node.getBoundingClientRect();
                    node.dispatchEvent(new PointerEvent('pointerenter',{pointerId:index+1,pointerType:'touch',clientX:b.x+b.width/2,clientY:b.y+b.height/2}));
                });
            }
            """);
        Assert.Equal(3, await page.EvaluateAsync<int>("() => window.tooltipObserverTargets.size"));
        await page.EvaluateAsync("() => window.firstHostedChart.getRootNode().host.remove()");
        var retained = await page.EvaluateAsync<string[]>("() => Array.from(window.tooltipObserverTargets,tree => tree.host?.id || 'document').sort()");
        await page.EvaluateAsync("() => window.hostedChart.getRootNode().host.remove()");
        Assert.Empty(await page.EvaluateAsync<string[]>("() => Array.from(window.tooltipObserverTargets,tree => tree.host?.id || 'document')"));
        await CaptureDelayAsync(page, "delay-shadow-observer-release", new { RetainedWhileQueued = retained, RetainedAfterLastCancellation = 0 }, session);
        Assert.Equal(new[] { "document", "second-shadow-host" }, retained);
        AssertNoConsoleErrors(session);
    }
}

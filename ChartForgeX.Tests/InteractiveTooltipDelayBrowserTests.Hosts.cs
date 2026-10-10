using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveTooltipDelayBrowserTests {
    [Theory]
    [InlineData("light")]
    [InlineData("open")]
    [InlineData("nested-open")]
    [InlineData("closed")]
    [InlineData("nested-closed")]
    public async Task MountedFragmentAcquiresDelayedNativePaintThroughItsHostRoots(string host) {
        if (!Enabled) return;
        await using var session = await OpenAsync(HostFragment(), 950, 760);
        var page = session.Page;
        await MountFragmentAsync(page, host);
        await RecordHostExpiryAsync(page);
        await MoveToHostTargetAsync(page, Point(0, 0));
        Assert.True(await HostedTipHiddenAsync(page));
        await WaitForHostExpiryAsync(page);
        var receipt = await HostReceiptAsync(page);
        await CaptureDelayAsync(page, "delay-host-native-" + host, receipt, session);
        Assert.False(receipt.GetProperty("hidden").GetBoolean());
        Assert.Contains("Reading", receipt.GetProperty("text").GetString()!, StringComparison.Ordinal);
        Assert.Single(receipt.GetProperty("trace").GetProperty("shown").EnumerateArray());
        AssertDelay(receipt.GetProperty("trace"));
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData("light", "root", "distance")]
    [InlineData("light", "root", "nearest")]
    [InlineData("light", "stage", "nearest")]
    [InlineData("nested-closed", "root", "nearest")]
    [InlineData("nested-closed", "document", "nearest")]
    [InlineData("nested-closed", "outer-shadow", "nearest")]
    public async Task HostVeilCannotPublishAnObscuredReadoutAtTheDeadline(string host, string location, string range) {
        if (!Enabled) return;
        await using var session = await OpenAsync(HostFragment(range), 950, 760);
        var page = session.Page;
        await MountFragmentAsync(page, host);
        await RecordHostExpiryAsync(page, location);
        await MoveToHostTargetAsync(page, Point(0, 0));
        Assert.True(await HostedTipHiddenAsync(page));
        await WaitForHostExpiryAsync(page);
        var receipt = await HostReceiptAsync(page);
        await CaptureDelayAsync(page, "delay-host-veil-" + host + "-" + location + "-" + range, receipt, session);
        var expiry = Assert.Single(receipt.GetProperty("expiry").EnumerateArray());
        // Inspect the callback itself: pointerleave can hide a transiently published tooltip afterward.
        Assert.True(expiry.GetProperty("hidden").GetBoolean());
        Assert.Empty(receipt.GetProperty("trace").GetProperty("shown").EnumerateArray());
        AssertNoConsoleErrors(session);
    }

    private static string HostFragment(string range = "exact", Chart? chart = null, int delay = Delay) => "<!doctype html><html><body style='margin:20px'>"
        + (chart ?? Observations(false)).ToInteractiveHtmlFragment(options => {
            ConfigureDelay(options, range);
            options.Tooltip.DelayMilliseconds = delay;
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
        }) + "</body></html>";

    private static async Task MountFragmentAsync(IPage page, string host) {
        await TraceAsync(page);
        await page.EvaluateAsync("""
            host => {
                const chart=window.hostedChart=document.querySelector('.cfx-interactive-chart');
                if(host==='light') return;
                const style=document.querySelector('style[data-cfx-interactive-assets]') || document.querySelector('style');
                const outer=document.createElement('div'); outer.id='outer-chart-host';
                document.body.append(outer);
                const mode=host.includes('closed')?'closed':'open';
                let tree=outer.attachShadow({mode});
                window.outerChartTree=tree;
                if(host.startsWith('nested')) {
                    const inner=document.createElement('div'); inner.id='inner-chart-host';
                    tree.append(inner); tree=inner.attachShadow({mode});
                }
                tree.append(style.cloneNode(true),chart);
            }
            """, host);
    }

    private static Task RecordHostExpiryAsync(IPage page, string? location = null) => page.EvaluateAsync("""
        location => {
            const root=window.hostedChart, tip=root.querySelector('.cfx-tooltip');
            const nativeTimer=window.setTimeout.bind(window), nativeClear=window.clearTimeout.bind(window), delay=Number(root.dataset.cfxTooltipDelay);
            window.hostExpiry=[];
            const timers=window.hostTimerTrace={queued:[],canceled:[],fired:[]};
            window.clearTimeout=id=>{ if(timers.queued.includes(id)) timers.canceled.push(id); nativeClear(id); };
            window.setTimeout=(callback,ms,...args)=>{
                const id=nativeTimer((...values)=>{
                    if(ms!==delay) { callback(...values); return; }
                    timers.fired.push(id);
                    if(location) {
                        const veil=document.createElement('div'); veil.id='delay-owned-veil'; veil.textContent='Host loading veil';
                        veil.style.cssText='position:fixed;inset:0;background:#eee;z-index:20;padding:24px;';
                        const parent=location==='document'?document.body:location==='outer-shadow'?window.outerChartTree:
                            location==='stage'?root.querySelector('.cfx-stage'):root;
                        parent.append(veil);
                    }
                    const p=window.delayTrace.pointer, documentHit=document.elementFromPoint(...p), localHit=root.getRootNode().elementFromPoint(...p);
                    callback(...values);
                    window.hostExpiry.push({hidden:tip.hidden,text:tip.innerText,documentHit:documentHit?.id||documentHit?.tagName,
                        localHit:localHit?.id||localHit?.tagName,owned:!!localHit&&root.contains(localHit)});
                },ms,...args);
                if(ms===delay) timers.queued.push(id);
                return id;
            };
        }
        """, location);

    private static async Task<double[]> MoveToHostTargetAsync(IPage page, string selector, double offsetY = 0) {
        var position = await page.EvaluateAsync<double[]>("""
            selector => {
                const node=window.hostedChart.querySelector(selector), b=node.getBoundingClientRect();
                return [b.x+b.width/2,b.y+b.height/2];
            }
            """, selector);
        await page.Mouse.MoveAsync((float)position[0], (float)(position[1] + offsetY));
        return position;
    }

    private static Task<bool> HostedTipHiddenAsync(IPage page) => page.EvaluateAsync<bool>("() => window.hostedChart.querySelector('.cfx-tooltip').hidden");
    private static Task WaitForHostExpiryAsync(IPage page) => page.WaitForFunctionAsync("() => window.hostExpiry.length>0", null, new PageWaitForFunctionOptions { Timeout = 5000 });
    private static Task<JsonElement> HostReceiptAsync(IPage page) => page.EvaluateAsync<JsonElement>("""
        () => {
            const root=window.hostedChart, tip=root.querySelector('.cfx-tooltip');
            return {hidden:tip.hidden,text:tip.innerText,expiry:window.hostExpiry,trace:window.delayTrace,timers:window.hostTimerTrace,
                detachedTimer:window.detachedTimer,mutation:window.hostMutation,
                hover:root.dataset.cfxHoverKey,guide:root.dataset.cfxCrosshair};
        }
        """);
}

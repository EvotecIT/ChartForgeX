using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using HtmlTinkerX;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveTooltipDelayBrowserTests {
    private static Chart Observations(bool dark) => Chart.Create().WithSize(800, 540).WithLegend(true).WithTitle("Pointer readout pacing")
        .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight()).WithXAxisBounds(0, 10).WithYAxisBounds(0, 10)
        .AddScatter("Reading", new[] { new ChartPoint(3, 5), new ChartPoint(7, 6) })
        .AddScatter("Companion", new[] { new ChartPoint(3, 2), new ChartPoint(7, 4) });

    private static void ConfigureDelay(HtmlChartInteractionOptions options, string range) {
        options.Tooltip.DelayMilliseconds = Delay;
        options.Tooltip.Range = range == "exact" ? HtmlChartTooltipRange.Exact : range == "nearest" ? HtmlChartTooltipRange.Nearest : HtmlChartTooltipRange.WithinDistance(60);
    }

    private static Task<bool> TipHiddenAsync(IPage page) => page.Locator(".cfx-tooltip").First.EvaluateAsync<bool>("tip => tip.hidden");
    private static Task WaitForTipAsync(IPage page) => page.WaitForFunctionAsync("() => !document.querySelector('.cfx-tooltip').hidden", null, new PageWaitForFunctionOptions { Timeout = 5000 });
    private static Task<JsonElement> TraceStateAsync(IPage page) => page.EvaluateAsync<JsonElement>("() => window.delayTrace");

    private static async Task<double[]> PointerAsync(IPage page, string selector) {
        var box = await BoxAsync(page, selector);
        var position = new double[] { box.X + box.Width / 2, box.Y + box.Height / 2 };
        await page.Mouse.MoveAsync((float)position[0], (float)position[1]);
        return position;
    }

    private static Task CentreReadableTargetAsync(IPage page, string selector) => page.Locator(selector).EvaluateAsync("node => { const stage=node.closest('.cfx-stage'), b=node.getBoundingClientRect(), s=stage.getBoundingClientRect(); stage.scrollLeft += b.x+b.width/2-s.x-s.width/2; }");

    private static Task TraceAsync(IPage page) => page.EvaluateAsync("""
        () => {
            const root=document.querySelector('.cfx-interactive-chart'), tip=root.querySelector('.cfx-tooltip');
            const trace=window.delayTrace={ entry:[], hover:[], shown:[], guide:[], sync:[], pointer:[] };
            root.querySelectorAll('[data-cfx-target-kind][data-cfx-target-id]').forEach(node => node.addEventListener('pointerenter',e => {
                trace.entry.push({t:performance.now(),id:node.dataset.cfxTargetId,hidden:tip.hidden});
            }));
            document.addEventListener('pointermove',e => trace.pointer=[e.clientX,e.clientY]);
            root.addEventListener('cfxhover',e => trace.hover.push({t:performance.now(),target:e.detail.target}));
            root.addEventListener('cfxcrosshair',e => trace.guide.push({t:performance.now(),target:e.detail.target}));
            root.addEventListener('cfxsync',e => trace.sync.push({t:performance.now(),action:e.detail.action,target:e.detail.target}));
            new MutationObserver(() => { if(!tip.hidden) trace.shown.push({t:performance.now(),text:tip.innerText}); })
                .observe(tip,{attributes:true,attributeFilter:['hidden']});
        }
        """);

    // Run host invalidation after the engine's native pointer handlers, before any timer task can expire.
    private static Task ArmQueuedHostMutationAsync(IPage page, string change, bool restore = false) => page.EvaluateAsync("""
        ({change,restore}) => {
            const root=window.hostedChart, tip=root.querySelector('.cfx-tooltip');
            window.hostMutation=null;
            root.addEventListener('pointermove',event => {
                const pointerAt=performance.now();
                queueMicrotask(async () => {
                    const target=root.querySelector('[data-cfx-role=point][data-cfx-series="0"][data-cfx-point="0"]');
                    const timers=window.hostTimerTrace, timer=timers.queued.at(-1);
                    const before={timer:timer??null,hidden:tip.hidden,rootConnected:root.isConnected,targetConnected:target.isConnected,
                        shown:window.delayTrace.shown.length,queued:timer!==undefined&&!timers.canceled.includes(timer)&&!timers.fired.includes(timer)};
                    const mutation=window.hostMutation={change,restore,pointerAt,mutationAt:performance.now(),before,complete:false};
                    try {
                        if(!before.hidden||!before.rootConnected||!before.targetConnected||!before.queued||before.shown)
                            throw new Error('Native pointer did not leave one hidden queued readout');
                        if(change.startsWith('detach-')) {
                            const subject=change.slice(7), node=subject==='root'?root:subject==='target'?target:
                                subject==='inner-host'?root.getRootNode().host:document.getElementById('outer-chart-host');
                            const parent=node.parentNode, next=node.nextSibling;
                            window.detachedTimer=timer;
                            node.remove(); mutation.unavailable=!node.isConnected; mutation.unavailableAt=performance.now();
                            parent.insertBefore(node,next); mutation.restoredAt=performance.now();
                        } else {
                            const host=root.getRootNode().host, features=root.dataset.cfxInteractionFeatures;
                            if(change==='target-paint') target.style.setProperty('opacity','0','important');
                            else if(change==='root-hidden') root.hidden=true;
                            else if(change==='root-removed') root.remove();
                            else if(change==='target-removed') target.remove();
                            else if(change==='shadow-host-opacity') host.style.opacity='0';
                            else if(change==='features') root.dataset.cfxInteractionFeatures='None';
                            else throw new Error('Unknown host invalidation');
                            mutation.unavailable=change==='root-hidden'?root.hidden:change==='root-removed'?!root.isConnected:
                                change==='target-removed'?!target.isConnected:change==='features'?root.dataset.cfxInteractionFeatures==='None':
                                getComputedStyle(change==='target-paint'?target:host).opacity==='0';
                            mutation.unavailableAt=performance.now();
                            if(restore) {
                                // Mutation delivery must see the unavailable state before this host restores it.
                                await Promise.resolve();
                                if(change==='root-hidden') root.hidden=false;
                                else if(change==='target-paint') target.style.removeProperty('opacity');
                                else if(change==='shadow-host-opacity') host.style.removeProperty('opacity');
                                else root.dataset.cfxInteractionFeatures=features;
                                mutation.restoredAt=performance.now();
                            }
                        }
                        if(!mutation.unavailable) throw new Error('Host fixture remained available');
                    } catch(error) { mutation.error=String(error); }
                    mutation.completedAt=performance.now(); mutation.complete=true;
                });
            },{once:true});
        }
        """, new { change, restore });

    private static async Task<JsonElement> WaitForHostMutationAsync(IPage page) {
        await page.WaitForFunctionAsync("() => window.hostMutation?.complete", null, new PageWaitForFunctionOptions { Timeout = 5000 });
        return await page.EvaluateAsync<JsonElement>("() => window.hostMutation");
    }

    private static void AssertQueuedHostCancellation(JsonElement mutation, JsonElement receipt) {
        Assert.False(mutation.TryGetProperty("error", out var error), error.ValueKind == JsonValueKind.String ? error.GetString() : null);
        var before = mutation.GetProperty("before");
        Assert.True(before.GetProperty("hidden").GetBoolean());
        Assert.True(before.GetProperty("rootConnected").GetBoolean());
        Assert.True(before.GetProperty("targetConnected").GetBoolean());
        Assert.True(before.GetProperty("queued").GetBoolean());
        Assert.Equal(0, before.GetProperty("shown").GetInt32());
        Assert.True(mutation.GetProperty("unavailable").GetBoolean());
        Assert.True(mutation.GetProperty("pointerAt").GetDouble() <= mutation.GetProperty("mutationAt").GetDouble());
        Assert.True(mutation.GetProperty("mutationAt").GetDouble() <= mutation.GetProperty("unavailableAt").GetDouble());
        var timer = before.GetProperty("timer").GetInt32();
        var timers = receipt.GetProperty("timers");
        Assert.Contains(timer, timers.GetProperty("queued").EnumerateArray().Select(item => item.GetInt32()));
        Assert.Contains(timer, timers.GetProperty("canceled").EnumerateArray().Select(item => item.GetInt32()));
        Assert.DoesNotContain(timer, timers.GetProperty("fired").EnumerateArray().Select(item => item.GetInt32()));
    }

    private static void AssertDelay(JsonElement trace) {
        var elapsed = trace.GetProperty("shown")[0].GetProperty("t").GetDouble() - trace.GetProperty("hover")[0].GetProperty("t").GetDouble();
        Assert.InRange(elapsed, Delay - 4, 5000);
    }

    private static async Task CaptureDelayAsync(IPage page, string name, object observations, HtmlBrowserSession? session = null) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, name + ".json"), JsonSerializer.Serialize(observations));
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".png") });
        if (session != null) await File.WriteAllTextAsync(Path.Combine(directory, name + ".console.json"), JsonSerializer.Serialize(session.ConsoleLog));
    }
}

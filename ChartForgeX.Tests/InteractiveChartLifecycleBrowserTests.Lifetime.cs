using System.Text.Json;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveChartLifecycleBrowserTests {
    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DefaultResetAndLoopingPlaybackReleaseResourcesAfterUpdateAndDisposalOrRemoval(bool detach) {
        if (!Enabled) return;
        await using var session = await OpenAsync("<!doctype html><html><body></body></html>", 820, 650);
        var page = session.Page;
        await InstallResourceProbeAsync(page);
        await page.SetContentAsync(RuntimePage());
        // Playwright installs its own interception listeners and observer on the first locator action.
        // Include that real harness footprint in the baseline rather than excluding event types.
        await page.EvaluateAsync("()=>document.body.insertAdjacentHTML('beforeend','<button id=prime-lifecycle>Ready</button>')");
        await page.Locator("#prime-lifecycle").ClickAsync();
        await page.Locator("#prime-lifecycle").EvaluateAsync("node=>node.remove()");
        var baseline = await ResourcesAsync(page);
        var fragment = Readings().ToInteractiveHtmlFragmentWithoutAssets(options => {
            Fit(options);
            options.Tooltip.DelayMilliseconds = 350;
            options.Interaction.Enable(ChartInteractionFeatures.Scenarios | ChartInteractionFeatures.StepPlayback);
            options.Interaction.AddScenario("loop", "Review observations", scenario => scenario
                .WithPlayback(250, loop: true)
                .AddSeriesStep("samples", "Samples").AddSeriesStep("reference", "Reference"));
        });
        var samples = new List<JsonElement>();
        for (var cycle = 0; cycle < 3; cycle++) {
            await InsertAsync(page, fragment);
            await BindControllerAsync(page);
            for (var update = 0; update < 2; update++) {
                await page.Locator("[data-cfx-scenario=loop]").ClickAsync();
                await page.Locator("[data-cfx-scenario-step-control=play]").ClickAsync();
                await page.WaitForFunctionAsync("()=>document.querySelector('.cfx-interactive-chart').dataset.cfxScenarioPlayback==='playing'");
                await MoveToAsync(page, North);
                if (update == 0) await UpdateAsync(page, fragment);
            }
            var active = await ResourcesAsync(page);
            Assert.True(active.GetProperty("listeners").GetInt32() > baseline.GetProperty("listeners").GetInt32());
            Assert.True(active.GetProperty("timeouts").GetInt32() > baseline.GetProperty("timeouts").GetInt32());
            if (detach) await page.EvaluateAsync("()=>chartController.root.remove()");
            else await page.EvaluateAsync("()=>{chartController.dispose();chartController.dispose();}");
            try { await WaitForResourcesAsync(page, baseline); }
            catch (TimeoutException) {
                var remaining = await ResourcesAsync(page);
                var details = await page.EvaluateAsync<JsonElement>("""
                    ()=>({listeners:lifecycleResources.listeners.map(item=>({type:item.type,target:item.target===window?'window':'document'})),
                        observers:[...lifecycleResources.observers.values()].flatMap(nodes=>[...nodes].map(node=>({name:node.nodeName,connected:node.isConnected}))),
                        timeouts:[...lifecycleResources.timeouts]})
                    """);
                await CaptureAsync(page, "resources-failed-" + detach + "-" + cycle, new { baseline, active, remaining, details });
                throw;
            }
            samples.Add(await ResourcesAsync(page));
            if (!detach) {
                Assert.False(await page.EvaluateAsync<bool>("""
                    () => {
                        const node=chartController.root.querySelector('g[data-cfx-role=point]');
                        const count=lifecycleSelections.length; node.dispatchEvent(new MouseEvent('click',{bubbles:true}));
                        return lifecycleSelections.length!==count;
                    }
                    """));
                await page.EvaluateAsync("()=>chartController.root.remove()");
            }
        }
        await CaptureAsync(page, "resources-" + (detach ? "detached" : "disposed"), new { baseline, samples });
        AssertNoConsoleErrors(session);
    }

    private static Task InstallResourceProbeAsync(IPage page) => page.EvaluateAsync("""
        () => {
            const probe=window.lifecycleResources={listeners:[],observers:new Map(),timeouts:new Set()};
            for(const target of [window,document]) {
                const add=target.addEventListener.bind(target),remove=target.removeEventListener.bind(target);
                const capture=options=>typeof options==='boolean'?options:!!options?.capture;
                target.addEventListener=(type,listener,options)=>{
                    if(!probe.listeners.some(item=>item.target===target&&item.type===type&&item.listener===listener&&item.capture===capture(options))) {
                        const item={target,type,listener,capture:capture(options)}; probe.listeners.push(item);
                        options?.signal?.addEventListener('abort',()=>{probe.listeners=probe.listeners.filter(entry=>entry!==item);},{once:true});
                    }
                    add(type,listener,options);
                };
                target.removeEventListener=(type,listener,options)=>{
                    probe.listeners=probe.listeners.filter(item=>item.target!==target||item.type!==type||item.listener!==listener||item.capture!==capture(options));
                    remove(type,listener,options);
                };
            }
            for(const name of ['ResizeObserver','MutationObserver']) {
                const Native=window[name];
                window[name]=class extends Native {
                    constructor(callback){super(callback);probe.observers.set(this,new Set());}
                    observe(node,...options){probe.observers.get(this).add(node);super.observe(node,...options);}
                    unobserve(node){probe.observers.get(this).delete(node);super.unobserve(node);}
                    disconnect(){probe.observers.get(this).clear();super.disconnect();}
                };
            }
            const set=window.setTimeout.bind(window),clear=window.clearTimeout.bind(window);
            window.setTimeout=(callback,delay,...args)=>{
                let id; id=set(()=>{probe.timeouts.delete(id);callback(...args);},delay);probe.timeouts.add(id);return id;
            };
            window.clearTimeout=id=>{probe.timeouts.delete(id);clear(id);};
            window.lifecycleResourceCounts=()=>({listeners:probe.listeners.length,
                observed:[...probe.observers.values()].reduce((sum,targets)=>sum+targets.size,0),timeouts:probe.timeouts.size});
        }
        """);

    private static Task<JsonElement> ResourcesAsync(IPage page) => page.EvaluateAsync<JsonElement>("()=>lifecycleResourceCounts()");

    private static Task WaitForResourcesAsync(IPage page, JsonElement baseline) => page.WaitForFunctionAsync("""
        expected => {
            const actual=lifecycleResourceCounts();
            return Object.keys(expected).every(key=>actual[key]===expected[key]);
        }
        """, new {
            listeners = baseline.GetProperty("listeners").GetInt32(),
            observed = baseline.GetProperty("observed").GetInt32(),
            timeouts = baseline.GetProperty("timeouts").GetInt32()
        }, new PageWaitForFunctionOptions { Timeout = 5000 });
}

using System.Text.Json;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveTooltipPositionBrowserTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SharedPositionWatcherReleasesUnusedClosedTreesWithoutLosingQueuedSiblingRemoval(bool interleavedRemoval) {
        if (!Enabled) return;
        await using var session = await OpenAsync("<html><body></body></html>", 950, 1300);
        var page = session.Page;
        await InstallListenerProbeAsync(page);
        await InstallPositionMutationProbeAsync(page);
        await page.SetContentAsync(new[] { Observations(false), Observations(true) }.ToInteractiveHtmlDashboardPage(options => {
            options.Columns = 1;
            options.Interaction.Features = ChartInteractionFeatures.Tooltips;
            options.Tooltip.Position.Anchor = HtmlChartTooltipAnchor.Node;
        }));
        await MountPositionFocusChartAsync(page, true, "position-a");
        await page.EvaluateAsync("()=>window.positionRootA=window.positionFocusChart");
        await MountPositionFocusChartAsync(page, true, "position-b");
        await page.EvaluateAsync("()=>window.positionRootB=window.positionFocusChart");
        var baseline = await ListenerStateAsync(page);
        foreach (var name in new[] { "positionRootA", "positionRootB" }) {
            var p = await page.EvaluateAsync<double[]>("({name,selector})=>{const b=window[name].querySelector(selector).getBoundingClientRect();return[b.x+b.width/2,b.y+b.height/2];}", new { name, selector = Reading });
            await page.Mouse.ClickAsync((float)p[0], (float)p[1]);
        }
        await MoveAwayAsync(page);
        var both = await PositionTreeReceiptAsync(page);
        var native = await page.EvaluateAsync<JsonElement>("""
            selector=>[window.positionRootA,window.positionRootB].map(root=>{const node=root.querySelector(selector),b=node.getBoundingClientRect(),hit=root.getRootNode().elementFromPoint(b.x+b.width/2,b.y+b.height/2);return{
                hidden:root.querySelector('.cfx-tooltip').hidden,pinned:root.dataset.cfxTooltipPinned,features:root.dataset.cfxInteractionFeatures,
                target:node.dataset.cfxTargetId,box:{x:b.x,y:b.y,width:b.width,height:b.height},hit:hit?.tagName,hitTarget:hit?.closest('[data-cfx-target-id]')?.dataset.cfxTargetId};})
            """, Reading);
        await CaptureAsync(page, "position-shared-shadow-acquisition-" + interleavedRemoval, new { baseline, both, native }, session);
        Assert.Equal(4, both.GetProperty("shadowTrees").GetInt32());
        Assert.Equal(2, both.GetProperty("shown").GetInt32());
        await page.EvaluateAsync("""
            interleaved=>{
                if(interleaved) {
                    const host=window.positionRootB.getRootNode().host,parent=host.parentNode,next=host.nextSibling;
                    host.remove();parent.insertBefore(host,next);
                }
                window.positionRootA.querySelector('[data-cfx-reset]').dispatchEvent(new MouseEvent('click',{bubbles:true}));
            }
            """, interleavedRemoval);
        await page.EvaluateAsync("()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)))");
        var released = await PositionTreeReceiptAsync(page);
        var listeners = await ListenerStateAsync(page);
        await CaptureAsync(page, "position-shared-shadow-watchers-" + interleavedRemoval, new { baseline, both, released, listeners }, session);
        Assert.Equal(interleavedRemoval ? 0 : 2, released.GetProperty("shadowTrees").GetInt32());
        Assert.Equal(interleavedRemoval ? 0 : 1, released.GetProperty("shown").GetInt32());
        Assert.Equal(baseline.GetProperty("observed").GetInt32() + (interleavedRemoval ? 0 : 1), listeners.GetProperty("observed").GetInt32());
        if (interleavedRemoval) {
            Assert.Equal(baseline.GetProperty("resize").GetInt32(), listeners.GetProperty("resize").GetInt32());
            Assert.Contains("position-b-inner", released.GetProperty("records").GetRawText(), StringComparison.Ordinal);
            var p = await page.EvaluateAsync<double[]>("selector=>{const b=window.positionRootB.querySelector(selector).getBoundingClientRect();return[b.x+b.width/2,b.y+b.height/2];}", Reading);
            await page.Mouse.MoveAsync(940, 1290);
            await page.Mouse.ClickAsync((float)p[0], (float)p[1]);
            Assert.False(await page.EvaluateAsync<bool>("()=>window.positionRootB.querySelector('.cfx-tooltip').hidden"));
            var recovery = await AssertPositionAsync(page, HtmlChartTooltipAnchor.Node, new[] { HtmlChartTooltipPlacement.BottomRight }, hosted: true);
            await CaptureAsync(page, "position-shared-shadow-recovery", new { recovery, trees = await PositionTreeReceiptAsync(page) }, session);
        }
        await page.EvaluateAsync("()=>window.positionRootB.querySelector('[data-cfx-reset]').dispatchEvent(new MouseEvent('click',{bubbles:true}))");
        var closed = await PositionTreeReceiptAsync(page);
        Assert.Equal(0, closed.GetProperty("shadowTrees").GetInt32());
        Assert.Equal(0, closed.GetProperty("shown").GetInt32());
        AssertNoConsoleErrors(session);
    }

    private static Task InstallPositionMutationProbeAsync(IPage page) => page.EvaluateAsync("""
        ()=>{
            window.positionMutationTargets=new Map();window.positionMutationRecords=[];
            const NativeMutation=window.MutationObserver;
            const record=(records,source)=>window.positionMutationRecords.push(...records.filter(r=>r.type==='childList'&&r.removedNodes.length).map(r=>({source,
                target:r.target.host?.id||r.target.nodeName,removed:Array.from(r.removedNodes).map(n=>n.id||n.dataset?.cfxTargetId||n.nodeName)})));
            window.MutationObserver=class extends NativeMutation {
                constructor(callback){super((records,observer)=>{record(records,'callback');callback(records,observer);});window.positionMutationTargets.set(this,new Set());}
                observe(target,options){super.observe(target,options);window.positionMutationTargets.get(this).add(target);}
                disconnect(){super.disconnect();window.positionMutationTargets.get(this).clear();}
                takeRecords(){const records=super.takeRecords();record(records,'takeRecords');return records;}
            };
        }
        """);

    private static Task<JsonElement> PositionTreeReceiptAsync(IPage page) => page.EvaluateAsync<JsonElement>("""
        ()=>({shadowTrees:Array.from(window.positionMutationTargets.values()).reduce((n,targets)=>n+Array.from(targets).filter(t=>t instanceof ShadowRoot).length,0),
            shown:[window.positionRootA,window.positionRootB].filter(root=>!root.querySelector('.cfx-tooltip').hidden).length,
            records:window.positionMutationRecords,events:window.positionFocusEvents,
            targets:Array.from(window.positionMutationTargets.values()).map(targets=>Array.from(targets).map(t=>t.host?.id||t.nodeName))})
        """);
}

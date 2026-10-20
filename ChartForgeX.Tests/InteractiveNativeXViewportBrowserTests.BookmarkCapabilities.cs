using System.Text.Json;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveNativeXViewportBrowserTests {
    [BrowserTheory]
    [InlineData("xy-missing-y")]
    [InlineData("legacy-unsupported")]
    [InlineData("legacy-empty")]
    [InlineData("disconnected")]
    [InlineData("xy-to-legacy")]
    public async Task NativeBookmarksCannotPartiallyApplyWhenTheirCompleteWindowCannotStart(string capability) {
        if (!Enabled) return;
        var xy = capability is "xy-missing-y" or "xy-to-legacy";
        var host = new NativeHost(scenarios: true, group: "bookmark-atomicity");
        await using var session = await OpenAsync(host.Page(), 980, 720);
        var page = session.Page; await ConnectAsync(page, host);
        if (xy) await ConnectCartesianAsync(page, "xy");
        await page.EvaluateAsync("xy=>xy?chartController.setViewport({x:{minimum:2,maximum:10},y:{minimum:20,maximum:80}}):chartController.setXWindow(2,10)", xy);
        await WaitForChangesAsync(page, 1);
        await page.Locator(Middle).FocusAsync(); await page.Keyboard.PressAsync("Space");
        var bookmark = await page.EvaluateAsync<JsonElement>("""
            ()=>{
                const root=chartController.root;
                root.dispatchEvent(new CustomEvent('cfx-apply-state',{detail:{emit:false,sync:false,snapshot:{
                    mode:'pan',scenario:{id:'review',stepIndex:'0',playback:'paused'},selectedTargets:chartController.getSelection()}}}));
                root.dispatchEvent(new CustomEvent('cfx-capture-state',{detail:{source:'captured-native',sync:false}}));
                window.capabilityBookmark=JSON.parse(root.dataset.cfxInteractionSnapshot);return capabilityBookmark;
            }
            """);
        AssertWindow(bookmark.GetProperty("xWindow"), 2, 10);
        Assert.Equal(xy, bookmark.GetProperty("yWindow").ValueKind == JsonValueKind.Object);
        Assert.Single(bookmark.GetProperty("selectedTargets").EnumerateArray());
        await page.EvaluateAsync("""
            ({capability,html})=>{
                window.controlled=[];
                const callback=(request,signal)=>new Promise((resolve,reject)=>controlled.push({request,signal,resolve,reject}));
                if(capability==='xy-missing-y')chartController.connectViewport(callback,{axes:'xy'});
                else chartController.connectXViewport(capability==='disconnected'?null:callback);
                chartController.update(html);
                const root=chartController.root;
                root.dispatchEvent(new CustomEvent('cfx-apply-state',{detail:{emit:false,sync:false,snapshot:{
                    viewport:{zoom:1.5,panX:12,panY:7},mode:'',scenario:{id:''},selectedTargets:[]}}}));
                root.dispatchEvent(new CustomEvent('cfx-capture-state',{detail:{source:'current-scene',sync:false}}));
                window.capabilityEvents={applied:0,sync:0,selection:0};
                root.addEventListener('cfxstateapplied',()=>capabilityEvents.applied++);
                root.addEventListener('cfxsync',()=>capabilityEvents.sync++);
                root.addEventListener('cfxselectionchange',()=>capabilityEvents.selection++);
                window.capabilityState=()=>JSON.stringify({revision:chartController.sceneRevision,viewport:chartController.getViewport(),
                    zoom:root.dataset.cfxZoom,panX:root.dataset.cfxPanX,panY:root.dataset.cfxPanY,mode:root.dataset.cfxMode,
                    brush:root.dataset.cfxBrush,scenario:root.dataset.cfxActiveScenario,step:root.dataset.cfxActiveScenarioStep,
                    playback:root.dataset.cfxScenarioPlayback,selection:chartController.getSelection(),snapshot:root.dataset.cfxInteractionSnapshot});
                window.capabilityBefore=capabilityState();window.capabilityRequestCount=nativeRequests.length;
                root.dispatchEvent(new CustomEvent('cfx-apply-state',{detail:{snapshot:capabilityBookmark}}));
            }
            """, new { capability, html = host.BookmarkReplacement(capability) });
        await page.EvaluateAsync("()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)))");
        var evidence = await page.EvaluateAsync<JsonElement>("""
            ()=>({before:JSON.parse(capabilityBefore),after:JSON.parse(capabilityState()),events:capabilityEvents,
                requestsBefore:capabilityRequestCount,requestsAfter:nativeRequests.length,
                started:controlled.map(item=>item.request),bookmark:capabilityBookmark})
            """);
        await CaptureAsync(page, "bookmark-capability-" + capability, evidence);
        Assert.Equal(evidence.GetProperty("requestsBefore").GetInt32(), evidence.GetProperty("requestsAfter").GetInt32());
        Assert.Empty(evidence.GetProperty("started").EnumerateArray());
        Assert.Equal(0, evidence.GetProperty("events").GetProperty("applied").GetInt32());
        Assert.Equal(0, evidence.GetProperty("events").GetProperty("sync").GetInt32());
        Assert.Equal(0, evidence.GetProperty("events").GetProperty("selection").GetInt32());
        Assert.True(await page.EvaluateAsync<bool>("()=>capabilityBefore===capabilityState()"), evidence.GetRawText());
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task GeometricOnlyBookmarksStillApplyWithoutANativeConnection() {
        if (!Enabled) return;
        var host = new NativeHost(scenarios: true);
        await using var session = await OpenAsync(host.Page(), 980, 720);
        var page = session.Page; await ControlledConnectionAsync(page);
        await page.EvaluateAsync("""
            ()=>{
                chartController.connectXViewport(null);window.geometricApplied=0;
                const root=chartController.root;root.addEventListener('cfxstateapplied',()=>geometricApplied++);
                root.dispatchEvent(new CustomEvent('cfx-apply-state',{detail:{sync:false,snapshot:{viewport:{zoom:2,panX:25,panY:15},
                    mode:'pan',scenario:{id:'review',stepIndex:'0',playback:'paused'},selectedTargets:[]}}}));
            }
            """);
        Assert.Equal(1, await page.EvaluateAsync<int>("()=>geometricApplied"));
        Assert.Equal("2.000", await page.Locator(Root).GetAttributeAsync("data-cfx-zoom"));
        Assert.Equal("25.0", await page.Locator(Root).GetAttributeAsync("data-cfx-pan-x"));
        Assert.Equal("15.0", await page.Locator(Root).GetAttributeAsync("data-cfx-pan-y"));
        Assert.Equal("pan", await page.Locator(Root).GetAttributeAsync("data-cfx-mode"));
        Assert.Equal("review", await page.Locator(Root).GetAttributeAsync("data-cfx-active-scenario"));
        Assert.Empty(await page.EvaluateAsync<JsonElement[]>("()=>controlled.map(item=>item.request)"));
        await CaptureAsync(page, "bookmark-geometric-only");
        AssertNoConsoleErrors(session);
    }
}
